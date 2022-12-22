namespace MemesApi.Services;

public class MockModelService : IModelService
{
    public MockModelService()
    {
        
    }
    
    public async Task<Stream?> SendToModelAsync(Stream stream, string fileName)
    {
        await Task.Delay(50); // Имитируем работу
        return stream;
    }
}