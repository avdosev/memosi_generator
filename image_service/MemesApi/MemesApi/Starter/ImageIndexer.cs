using MemesApi.Db;
using MemesApi.Db.Models;
using MemesApi.Minio;
using Microsoft.EntityFrameworkCore;

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
                ;

            var filesCount = await memeContext.Files.CountAsync();
            if (!memeContext.Files.Any())
            {
                var directoryPath = Path.Combine(Environment.CurrentDirectory, "static");

                var files = Directory.EnumerateFiles(directoryPath)
                    .Where(f => !f.Contains(GitKeepFile) && !f.Contains(".txt"))
                    .Select(f => new
                    {
                        FileName = f.Split(Path.DirectorySeparatorChar).Last(),
                        FilePath = f
                    }).ToList();

                foreach (var file in files)
                {
                    await minioService.UploadAsync(file.FilePath, file.FileName);
                }


                var memeFiles = files
                    .Select(f =>
                    {
                        FileSystemInfo info = new FileInfo(f.FilePath);
                        var meta = new FileMeta
                        {
                            Format = info.Extension.Remove(0), //  Extension возвращает расширение с точкой, нам оно не нужно
                            CreationDate = info.CreationTime,
                            UpdateDate = info.LastWriteTime
                        };
                        var file = new MemeFile
                        {
                            FileName = f.FilePath.Split(Path.DirectorySeparatorChar).Last(),
                            Meta = meta,
                        };

                        return file;
                    }).ToList();

                    

                await memeContext.Files.AddRangeAsync(memeFiles);
                await memeContext.SaveChangesAsync();
                await memeContext.SaveChangesAsync(cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

