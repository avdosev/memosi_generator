namespace MemesApi.Minio
{
	public interface IMinioService
	{
		Task InitializeAsync();

		Task UploadAsync(string path, string name);
		Task UploadAsync(Stream stream, string name);
	}
}