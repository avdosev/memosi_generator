using System.ComponentModel.DataAnnotations;

namespace MemesApi.Controllers.Attributes;

public class ImageValidationAttribute : ValidationAttribute
{
    public string? Extensions { get; set; }
    public int MaxSize { get; set; }

    public override bool IsValid(object? value)
    {
        if (value is not IFormFile imageFile) return false;
            
        if (imageFile.Length == 0 || imageFile.Length > MaxSize)
        {
            ErrorMessage =
                $"Invalid image size. Excepted 0 < size <= {MaxSize} bytes. Got {imageFile.Length} bytes";
            return false;
        }

        var extension = Path.GetExtension(imageFile.FileName);
        if (Extensions != null && !Extensions.Contains(extension))
        {
            ErrorMessage = $"Invalid image format. Excepted one of {Extensions}. Got {extension}";
            return false;
        }
            
        return true;
    }
}
