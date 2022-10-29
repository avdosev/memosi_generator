using MemesApi.Db;
using MemesApi.Db.Models;
using MemesApi.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IO;

namespace MemesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly MemeContext _context;
        private readonly IOptions<AppSettings> _config;
        public ImagesController(MemeContext context, IOptions<AppSettings> config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("/estimate/{imageId:int}")]
        public async Task<ActionResult> Estimate(int imageId, EstimateRequest request)
        {

            var image = await _context.Files.FirstOrDefaultAsync(f => f.Id == imageId);
            if(image is null)
            {
                return NotFound();
            }

            var imageFile = image.FileName.Split('.', StringSplitOptions.RemoveEmptyEntries)[0];
            var scoreFileName = string.Join(".", imageFile, "txt");

            await System.IO.File.AppendAllTextAsync(
                Path.Combine(Environment.CurrentDirectory, "static", scoreFileName),
                $"{request.Estimage} ");

            await _context.Estimates.AddAsync(new Estimate
            {
                FileId = imageId,
                ClientId = request.ClientId,
                Score = request.Estimage
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("/next")]
        public async Task<ActionResult<ImageResponse>> GetNextImage(
            [FromQuery]string clientId, 
            [FromQuery]int? previousId)
        {
            if(clientId is null)
            {
                return BadRequest();
            }

            if (previousId != null)
            {
                var result = await _context.Files
                    .OrderBy(i => i.Id)
                    .FirstOrDefaultAsync(i => i.Id > previousId);

                return new ImageResponse(result?.Id, GetFullUrl(result?.FileName), result == null);
            }

            var lastEstimate = await _context.Estimates
                .OrderByDescending(e => e.FileId)
                .FirstOrDefaultAsync(e => e.ClientId == clientId);

            var nextFile = lastEstimate switch
            {
                null => await _context.Files.OrderBy(f => f.Id).FirstOrDefaultAsync(),
                _ => await _context.Files.OrderBy(f => f.Id).FirstOrDefaultAsync(f => f.Id > lastEstimate.FileId)
            };

            return new ImageResponse(nextFile?.Id, GetFullUrl(nextFile?.FileName), nextFile == null);

        }

        private string GetFullUrl(string? fileName)
        {
            return $"{_config.Value.UrlPrefix ?? ""}/{fileName}";
        }

    }
}
