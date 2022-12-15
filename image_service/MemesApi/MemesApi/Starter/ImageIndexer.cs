using MemesApi.Db;
using MemesApi.Db.Models;
using MemesApi.Minio;

namespace MemesApi.Starter
{
	public class ImageIndexer : IHostedService
	{
		private const string GitKeepFile = ".gitkeep";

		private readonly IServiceScopeFactory _serviceScopeFactory;
		public ImageIndexer(IServiceScopeFactory serviceScopeFactory)
		{
			_serviceScopeFactory = serviceScopeFactory;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var scope = _serviceScopeFactory.CreateScope();
			var memeContext = scope.ServiceProvider.GetService<MemeContext>();
			var minioService = scope.ServiceProvider.GetService<IMinioService>();

			if (memeContext is null) throw new ApplicationException("Can't get MemeContext service");
			if (minioService is null) throw new ApplicationException("Can't get MinioService service");

			await minioService.InitializeAsync()
				.ConfigureAwait(false);

			if (!memeContext.Files.Any())
			{
				var directoryPath = Path.Combine(Environment.CurrentDirectory, "static");

				var files = Directory.EnumerateFiles(directoryPath)
					.Where(f => !f.Contains(GitKeepFile))
					.Select(f => new
					{
						FileName = f.Split(Path.DirectorySeparatorChar).Last(),
						FilePath = f
					}).ToList();

				foreach (var file in files)
				{
					await minioService.UploadAsync(file.FilePath, file.FileName)
						.ConfigureAwait(false);
				}

				var contextFiles = files.Select(f => new MemeFile()
					{
						FileName = f.FileName
					}
				);

				await memeContext.Files.AddRangeAsync(contextFiles, cancellationToken).ConfigureAwait(false);
				await memeContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}