using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MemesApi;
using MemesApi.Db;
using MemesApi.Dto;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace IntegrationTests;

public class ApiTests
{
	private const string ApiUrl = "http://127.0.0.1:9999/api/images/";
	
	private const string EstimateUrl = "estimate/{0}";
	private const string GetNextUrl = "next?clientId={0}&previousId={1}";

	private static readonly Dictionary<string, string> TestConfiguration = new()
	{
		{"API_URL", "http://127.0.0.1:9999"},
		{"CONNECTION_STRING", "Data Source=Tests.db;"}
	};
	
	private readonly Random _random = new();

	private WebApplicationFactory<Program> _application = null!;
	private MemeContext _db = null!;
	private HttpClient _client = null!;


	[SetUp]
	public void SetupApplication()
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
					configurationBuilder.AddInMemoryCollection(TestConfiguration);
				});
				
			});

		var scope = _application.Services.CreateScope();
		_db = scope.ServiceProvider.GetService<MemeContext>()!;
		_client = _application.CreateClient();
		_client.BaseAddress = new Uri(ApiUrl);
	}


	[Test]
	public async Task IndexingTest()
	{
		var path = Path.Combine(Environment.CurrentDirectory, "static");
		var files = Directory.EnumerateFiles(path)
			.Where(f => !f.Contains(".gitkeep"))
			.Select(path => path.Split(Path.DirectorySeparatorChar).Last())
			.ToList();
		
		var dbFiles = await _db.Files.ToListAsync();
		
		Assert.AreEqual(files.Count, dbFiles.Count);
		foreach (var file in files)
		{
			Assert.True(dbFiles.Any(f => f.FileName == file));
		}
	}

	[Test]
	public async Task GetNextImage_Flow_Test()
	{
		var userId = Guid.NewGuid().ToString();
		var previousId = 0;
		while (true)
		{
			var nextImageResponse = await _client.GetAsync(string.Format(GetNextUrl, userId, previousId));
			Assert.True(nextImageResponse.IsSuccessStatusCode);
			
			var nextImage = await nextImageResponse.Content.ReadFromJsonAsync<ImageResponse>();
		
			Assert.NotNull(nextImage);
			if (nextImage!.Finished)
			{
				Assert.Null(nextImage.ImageId);
				return;
			}
			
			Assert.AreNotEqual(previousId, nextImage.ImageId);

			var imageContent = await _client.GetAsync(nextImage.Url);
			Assert.True(imageContent.IsSuccessStatusCode);

			var dbImage = await _db.Files.FindAsync(nextImage.ImageId);
		
			Assert.NotNull(dbImage);
			Assert.True(nextImage.Url!.Contains(dbImage!.FileName));
			
			Assert.True(File.Exists(Path.Combine(Environment.CurrentDirectory, "static", dbImage.FileName)));
			
			previousId = nextImage.ImageId!.Value;
		}
	}


	[Test]
	public async Task EstimateBasic_Test()
	{
		var userId = Guid.NewGuid().ToString();
		var estimateValue = 2;
		
		var nextImageResponse = await _client.GetAsync(string.Format(GetNextUrl, userId, 0));
		Assert.True(nextImageResponse.IsSuccessStatusCode);
		
		var nextImage = await nextImageResponse.Content.ReadFromJsonAsync<ImageResponse>();
		Assert.NotNull(nextImage);
		Assert.False(nextImage!.Finished);

		var beforeCount = await _db.Estimates.CountAsync();

		var estimateRequest = await _client.PostAsJsonAsync(
			string.Format(EstimateUrl, nextImage.ImageId),
			new EstimateRequest(estimateValue, userId));
		
		Assert.True(estimateRequest.IsSuccessStatusCode);

		var afterCount = await _db.Estimates.CountAsync();
		
		Assert.AreEqual(beforeCount + 1, afterCount);

		var estimate = await _db.Estimates
			.Include(e => e.File)
			.FirstOrDefaultAsync(e => e.ClientId == userId);
		
		Assert.NotNull(estimate);
		Assert.AreEqual(estimateValue, estimate.Score);
		Assert.AreEqual(nextImage.ImageId, estimate.FileId);
		
		var imageFile = estimate.File.FileName.Split('.', StringSplitOptions.RemoveEmptyEntries)[0];
		var scoreFileName = string.Join(".", imageFile, "txt");
		
		Assert.True(File.Exists(Path.Combine(Environment.CurrentDirectory, "static", scoreFileName)));
	}
	
	[Test]
	public async Task Estimate_NotExistingImage_Negative_Test()
	{
		var request = new EstimateRequest(10, Guid.NewGuid().ToString());
		var randomImageId = _random.Next(1000000, 5000000);

		var response = await _client.PostAsJsonAsync(string.Format(EstimateUrl, randomImageId), request);

		Assert.False(response.IsSuccessStatusCode);
		Assert.AreEqual(response.StatusCode, HttpStatusCode.NotFound);
	}


	[Test]
	public async Task GetNextImage_EmptyClient_Negative_Test()
	{
		var response = await _client.GetAsync(GetNextUrl);
		Assert.False(response.IsSuccessStatusCode);
		Assert.AreEqual(response.StatusCode, HttpStatusCode.BadRequest);
	}

	[Test]
	public async Task Estimate_GetNextImage_FullFlow_Test()
	{
		var userId = Guid.NewGuid().ToString();
		var previousId = 0;
		var estimateValue = 2;
		while (true)
		{
			var nextImageResponse = await _client.GetAsync(string.Format(GetNextUrl, userId, ""));
			Assert.True(nextImageResponse.IsSuccessStatusCode);
			
			var nextImage = await nextImageResponse.Content.ReadFromJsonAsync<ImageResponse>();
			Assert.NotNull(nextImage);
			if (nextImage!.Finished)
			{
				Assert.Null(nextImage.ImageId);
				return;
			}
			
			Assert.AreNotEqual(previousId, nextImage.ImageId);

			var imageContent = await _client.GetAsync(nextImage.Url);
			Assert.True(imageContent.IsSuccessStatusCode);

			var dbImage = await _db.Files.FindAsync(nextImage.ImageId);
		
			Assert.NotNull(dbImage);
			Assert.True(nextImage.Url!.Contains(dbImage!.FileName));
			
			Assert.True(File.Exists(Path.Combine(Environment.CurrentDirectory, "static", dbImage.FileName)));

			
			var beforeCount = await _db.Estimates.CountAsync();

			var estimateResponse = await _client.PostAsJsonAsync(
				string.Format(EstimateUrl, nextImage.ImageId),
				new EstimateRequest(estimateValue, userId));
			
			Assert.True(estimateResponse.IsSuccessStatusCode);
			
			var afterCount = await _db.Estimates.CountAsync();
		
			Assert.AreEqual(beforeCount + 1, afterCount);

			var estimate = await _db.Estimates
				.Include(e => e.File)
				.FirstOrDefaultAsync(e => e.ClientId == userId && e.FileId == nextImage.ImageId);
		
			Assert.NotNull(estimate);
			Assert.AreEqual(estimateValue, estimate.Score);
			Assert.AreEqual(nextImage.ImageId, estimate.FileId);
		
			var imageFile = estimate.File.FileName.Split('.', StringSplitOptions.RemoveEmptyEntries)[0];
			var scoreFileName = string.Join(".", imageFile, "txt");
		
			Assert.True(File.Exists(Path.Combine(Environment.CurrentDirectory, "static", scoreFileName)));
			
			
			previousId = nextImage.ImageId!.Value;
		}
	}


}