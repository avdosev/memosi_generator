using MemesApi.Controllers.Filters;
using MemesApi.Db;
using MemesApi.Minio;
using MemesApi.Starter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Prometheus;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace MemesApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            WebApplication.CreateBuilder(args);
            
            builder.WebHost.UseUrls("http://*:9999");
            
            ConfigureLogging(builder);

            ConfigureServices(builder);

            ConfigureApp(builder).Run();
        }


        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .WriteTo.Console(LogEventLevel.Information, outputTemplate: ConfigurationConsts.OutputTemplate)
                    .WriteTo.File("./logs/log.txt", LogEventLevel.Debug, rollingInterval: RollingInterval.Day, outputTemplate: ConfigurationConsts.OutputTemplate)
                    .Enrich.FromLogContext();

                configuration
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
            });
        }
        
        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<LoggingFilter>();
            });
            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.Configure<RouteOptions>(conf =>
            {
                conf.LowercaseUrls = true;
            });

            builder.Services.AddDbContext<MemeContext>(options =>
            {
                options.UseSqlite(builder.Configuration.GetValue<string>(ConfigurationConsts.ConnectionString));
            });

            builder.Services.AddHostedService<MigrationStarter>();
            builder.Services.AddHostedService<ImageIndexer>();

            builder.Services.AddMinioClient(conf =>
            {
                conf.Endpoint = builder.Configuration.GetValue<string>("MINIO_URL");
                conf.AccessKey = builder.Configuration.GetValue<string>("MINIO_ACCESS_KEY");
                conf.SecretKey = builder.Configuration.GetValue<string>("MINIO_SECRET_KEY");
                conf.BucketName = builder.Configuration.GetValue<string>("MINIO_BUCKET");
            });
        }

        private static WebApplication ConfigureApp(WebApplicationBuilder builder)
        {
            var app = builder.Build();
            
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseRouting();
            app.UseHttpMetrics();
            app.MapControllers();
            app.MapMetrics();
            return app;
        }
    }
}