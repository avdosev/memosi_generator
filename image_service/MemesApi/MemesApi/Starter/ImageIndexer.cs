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
                ;

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
                    await minioService.UploadAsync(file.FilePath, file.FileName);
                }

                var contextFiles = files.Select(f => new MemeFile()
                {
                    FileName = f.FileName
                }
                );

                var (memeFiles, metas) = files
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

                        return (file, meta);
                    })
                    .Aggregate((new List<MemeFile>(), new List<FileMeta>()), (unpacked, record) =>
                    {
                        unpacked.Item1.Add(record.file);
                        unpacked.Item2.Add(record.meta);
                        return unpacked;
                    });

                await memeContext.Metas.AddRangeAsync(metas);
                await memeContext.Files.AddRangeAsync(memeFiles);
                await memeContext.SaveChangesAsync();


                await memeContext.Files.AddRangeAsync(contextFiles, cancellationToken);
                await memeContext.SaveChangesAsync(cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

