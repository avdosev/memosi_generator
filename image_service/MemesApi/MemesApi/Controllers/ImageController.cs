using MemesApi.Db;
using MemesApi.Db.Models;
using MemesApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using MemesApi.Controllers.Attributes;
using MemesApi.Services;
using MemesApi.Minio;

namespace MemesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly MemeContext _context;
        private readonly IOptions<MinioConfiguration> _config;
        private readonly IModelService _modelService;
        private readonly IMinioService _minioService;
        public ImagesController(
            MemeContext context, 
            IOptions<MinioConfiguration> config, 
            IModelService modelService, 
            IMinioService minioService)
        {
            _context = context;
            _config = config;
            _modelService = modelService;
            _minioService = minioService;
        }

        [HttpPost("estimate/{imageId:int}")]
        public async Task<ActionResult> Estimate(int imageId, EstimateRequest request)
        {
            var image = await _context.Files.FirstOrDefaultAsync(f => f.Id == imageId);
            if(image is null) return NotFound();

            await _context.Estimates.AddAsync(new Estimate
            {
                FileId = imageId,
                ClientId = request.ClientId,
                Score = request.Estimate
            });

            await _context.SaveChangesAsync();
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
                    .FirstOrDefaultAsync(i => i.Id > previousId);

                return new ImageResponse(result?.Id, GetFullUrl(result?.FileName), result == null);
            }

            var lastEstimate = await _context.Estimates
                .OrderByDescending(e => e.FileId)
                .FirstOrDefaultAsync(e => e.ClientId == clientId);

            var nextFile = lastEstimate switch
            {
                null => await _context.Files.OrderBy(f => f.Id).FirstOrDefaultAsync(),
                _ => await _context.Files
                    .OrderBy(f => f.Id)
                    .FirstOrDefaultAsync(f => f.Id > lastEstimate.FileId)
            };

            return new ImageResponse(nextFile?.Id, GetFullUrl(nextFile?.FileName), nextFile == null);
        }
        
        [HttpPost("upload")]
        public async Task<ActionResult<ImageResponse>> UploadImage(
            [Required]
            [ImageValidation(MaxSize = 10 * 1024 * 1024, Extensions=".png,.jpg,.jpeg")] 
            IFormFile imageFile)
        {
            var modelStream = await _modelService.SendToModelAsync(imageFile.OpenReadStream(), imageFile.FileName);
            if(modelStream is null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Unable to generate meme. Please try again with different file");
            }

            var format = imageFile.ContentType.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
            var fileName = $"{Guid.NewGuid()}.{format}";
            
            await _minioService.UploadAsync(modelStream, fileName);

            var fileMeta = new FileMeta { Format = format, CreationDate = DateTime.Now };
            await _context.Metas.AddAsync(fileMeta);
            
            var memeFile = new MemeFile { FileName = fileName, Meta = fileMeta };
            var fileEntry = await _context.Files.AddAsync(memeFile);

            await _context.SaveChangesAsync();

            return new ImageResponse(fileEntry.Entity.Id, GetFullUrl(fileEntry.Entity.FileName), true);
        }

        private string GetFullUrl(string? fileName)
        {
            return $"http://{_config.Value.Endpoint}/{_config.Value.BucketName}/{fileName}";
        }
    }
}
