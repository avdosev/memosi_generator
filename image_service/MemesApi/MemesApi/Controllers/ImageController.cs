using MemesApi.Db;
using MemesApi.Db.Models;
using MemesApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using MemesApi.Minio;

namespace MemesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly MemeContext _context;
        private readonly IOptions<MinioConfiguration> _config;
        public ImagesController(MemeContext context, IOptions<MinioConfiguration> config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("estimate/{imageId:int}")]
        public async Task<ActionResult> Estimate(int imageId, EstimateRequest request)
        {
            var image = await _context.Files.FirstOrDefaultAsync(f => f.Id == imageId)
                .ConfigureAwait(false);
            if(image is null) return NotFound();

            var imageFile = image.FileName.Split('.', StringSplitOptions.RemoveEmptyEntries)[0];
            var scoreFileName = string.Join(".", imageFile, "txt");

            await System.IO.File.AppendAllTextAsync(
                Path.Combine(Environment.CurrentDirectory, "static", scoreFileName),
                $"{request.Estimate} ").ConfigureAwait(false);

            await _context.Estimates.AddAsync(new Estimate
            {
                FileId = imageId,
                ClientId = request.ClientId,
                Score = request.Estimate
            }).ConfigureAwait(false);

            await _context.SaveChangesAsync().ConfigureAwait(false);
            return Ok();
        }

        [HttpGet("next")]
        public async Task<ActionResult<ImageResponse>> GetNextImage(
            [FromQuery][Required]string clientId, 
            [FromQuery]int? previousId)
        {
            if (previousId != null)
            {
                var result = await _context.Files
                    .OrderBy(i => i.Id)
                    .FirstOrDefaultAsync(i => i.Id > previousId).ConfigureAwait(false);

                return new ImageResponse(result?.Id, GetFullUrl(result?.FileName), result == null);
            }

            var lastEstimate = await _context.Estimates
                .OrderByDescending(e => e.FileId)
                .FirstOrDefaultAsync(e => e.ClientId == clientId).ConfigureAwait(false);

            var nextFile = lastEstimate switch
            {
                null => await _context.Files.OrderBy(f => f.Id).FirstOrDefaultAsync().ConfigureAwait(false),
                _ => await _context.Files
                    .OrderBy(f => f.Id)
                    .FirstOrDefaultAsync(f => f.Id > lastEstimate.FileId)
                    .ConfigureAwait(false)
            };

            return new ImageResponse(nextFile?.Id, GetFullUrl(nextFile?.FileName), nextFile == null);
        }

        private string GetFullUrl(string? fileName)
        {
            return $"http://{_config.Value.Endpoint}/{_config.Value.BucketName}/{fileName}";
        }
    }
}
