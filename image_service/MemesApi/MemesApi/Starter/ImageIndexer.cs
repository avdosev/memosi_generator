using MemesApi.Db;
using MemesApi.Db.Models;

namespace MemesApi.Starter
{
    public class ImageIndexer : IHostedService
    {

        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ImageIndexer(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var score = _serviceScopeFactory.CreateScope();
            var memeContext = score.ServiceProvider.GetService<MemeContext>();
            


            if(!memeContext.Files.Any())
            {
                var directoryPath = Path.Combine(Environment.CurrentDirectory, "static");

                var files = Directory.EnumerateFiles(directoryPath)
                    .Where(f => !f.Contains(".gitkeep"))
                    .Select(path => new MemeFile
                    {
                        FileName = path.Split(Path.DirectorySeparatorChar).Last()
                    }).ToList();

                await memeContext.Files.AddRangeAsync(files);
                await memeContext.SaveChangesAsync();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
