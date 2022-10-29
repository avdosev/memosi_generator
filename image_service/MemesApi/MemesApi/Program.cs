using MemesApi.Db;
using MemesApi.Starter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace MemesApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
            });
            builder.WebHost.UseUrls(builder.Configuration.GetValue<string>(ConfigurationConsts.ApiUrl));

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.Configure<AppSettings>(config =>
            {
                config.UrlPrefix = builder.Configuration.GetValue<string>(ConfigurationConsts.ApiUrl)
                    + "/static";
            });

            builder.Services.AddDbContext<MemeContext>(options =>
            {
                options.UseSqlite(builder.Configuration.GetValue<string>(ConfigurationConsts.ConnectionString));
                options.EnableSensitiveDataLogging();
            });

            builder.Services.AddHostedService<MigrationStarter>();
            builder.Services.AddHostedService<ImageIndexer>();
            
            var app = builder.Build();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                     Path.Combine(builder.Environment.ContentRootPath, "static")),
                RequestPath = "/static"
            });

            app.UseSwagger();
            app.UseSwaggerUI();
           
            app.MapControllers();

            app.Run();
        }
    }
}