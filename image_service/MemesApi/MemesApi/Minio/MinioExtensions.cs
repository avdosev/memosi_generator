namespace MemesApi.Minio
{
	public static class MinioExtensions
	{
		public static IServiceCollection AddMinioClient(this IServiceCollection serviceCollection, Action<MinioConfiguration> conf)
		{
			serviceCollection.Configure(conf);
			serviceCollection.AddSingleton<IMinioService, MinioService>();
			return serviceCollection;
		}
	}
}