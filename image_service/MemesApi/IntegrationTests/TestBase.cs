﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using MemesApi;
using MemesApi.Db;
using MemesApi.Minio;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using NUnit.Framework;

namespace IntegrationTests
{
	public class TestBase
	{
		protected const string ApiUrl = "http://127.0.0.1:9999";

		private WebApplicationFactory<Program> _application = null!;
		private MemeContext _db = null!;
		private HttpClient _client = null!;
		private HttpClient _defaultClient = new HttpClient();

        private MinioClient _minioClient = null!;
		private MinioConfiguration _minioConfiguration = null!;


		protected MemeContext Db => _db;
		protected HttpClient Client => _client;
		protected MinioClient MinioClient => _minioClient;
		protected MinioConfiguration MinioConfiguration => _minioConfiguration;
		protected HttpClient DefaultClient => _defaultClient;
		
		protected void Setup(Dictionary<string, string> conf)
		{
			_application = new WebApplicationFactory<Program>()
			
				.WithWebHostBuilder(builder =>
				{
					builder.ConfigureServices(services =>
					{
						var sp = services.BuildServiceProvider();
						using var scope = sp.CreateScope();
						var db = scope.ServiceProvider.GetService<MemeContext>()!;
						db.Database.EnsureDeleted();
					
					});
				
					builder.ConfigureAppConfiguration((context, configurationBuilder) =>
					{
						configurationBuilder.AddInMemoryCollection(conf);
					});
				
				});

			var scope = _application.Services.CreateScope();
			_db = scope.ServiceProvider.GetService<MemeContext>()!;
		
			_client = _application.CreateClient();
			_client.BaseAddress = new Uri(ApiUrl);

			var minioConf = scope.ServiceProvider.GetService<IOptions<MinioConfiguration>>();
			_minioConfiguration = minioConf.Value;
            _minioClient = new MinioClient()
				.WithEndpoint(minioConf.Value.Endpoint)
				.WithCredentials(minioConf.Value.AccessKey, minioConf.Value.SecretKey)
				.WithSSL(false)
				.Build();
        }


		protected void TearDown()
		{
			_client.Dispose();
			_db.Dispose();
			_minioClient.Dispose();
			_application.Dispose();
			
		}
	}
}