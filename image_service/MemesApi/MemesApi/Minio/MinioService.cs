using Microsoft.Extensions.Options;
using Minio;

namespace MemesApi.Minio
{
	public class MinioService: IMinioService
	{
		private readonly MinioClient _minioClient;
		private readonly MinioConfiguration _configuration;

		public MinioService(IOptions<MinioConfiguration> configuration)
		{
			_configuration = configuration.Value;
			_minioClient = new MinioClient()
				.WithEndpoint(_configuration.Endpoint)
				.WithCredentials(_configuration.AccessKey, _configuration.SecretKey)
				.WithSSL(false)
				.Build();
		}

		public async Task InitializeAsync()
		{
			var checkBucketArgs = new BucketExistsArgs()
				.WithBucket(_configuration.BucketName);

			var exists = await _minioClient.BucketExistsAsync(checkBucketArgs)
				.ConfigureAwait(false);
			if (!exists)
			{
				var createArgs = new MakeBucketArgs()
					.WithBucket(_configuration.BucketName);
				await _minioClient.MakeBucketAsync(createArgs).ConfigureAwait(false);
			}
		}
		
		public async Task UploadAsync(string filePath, string name)
		{
			var uploadArgs = new PutObjectArgs()
				.WithBucket(_configuration.BucketName)
				.WithObject(name)
				.WithFileName(filePath);

			await _minioClient.PutObjectAsync(uploadArgs).ConfigureAwait(false);
		}
		
		
		
		public async Task UploadAsync(Stream stream, string name)
		{
			var uploadArgs = new PutObjectArgs()
				.WithBucket(_configuration.BucketName)
				.WithObject(name)
				.WithStreamData(stream);

			await _minioClient.PutObjectAsync(uploadArgs).ConfigureAwait(false);
		}
	}
}