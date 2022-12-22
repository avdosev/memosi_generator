namespace MemesApi.Services;

public interface IModelService
{
    public Task<Stream?> SendToModelAsync(Stream stream, string fileName);
}