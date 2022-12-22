using Microsoft.Extensions.Options;

namespace MemesApi.Services;

public class MlModelService : IModelService
{
    private const string GetMemeUrl = "get_meme";
    private const string ParamName = "img";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<IModelService> _logger;

    public MlModelService(IHttpClientFactory httpClientFactory, ILogger<IModelService> logger)
    {
        this.httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Stream?> SendToModelAsync(Stream stream, string fileName)
    {
        using var client = httpClientFactory.CreateClient(ModelServiceConsts.ClientName);

        var streamContent = new StreamContent(stream);
       

        var multipart = new MultipartFormDataContent();
        multipart.Add(streamContent, ParamName, fileName);

        _logger.LogDebug("Sending request to meme generator");
        var response = await client.PostAsync(GetMemeUrl, multipart);
        
        if(!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Status code indicates error: {statusCode} {error}", response.StatusCode, error);
            return null;

        }

        return await response.Content.ReadAsStreamAsync();
      
    }
}