using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MemesApi;
using MemesApi.Db;
using MemesApi.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using NUnit.Framework;
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace IntegrationTests;

public class ApiTests: TestBase
{

	private const string MetricsUrl = "metrics";
	private const string EstimateUrl = "api/images/estimate/{0}";
	private const string GetNextUrl = "api/images/next?clientId={0}&previousId={1}";
	private const string UploadImage = "api/images/upload";

	private static readonly Dictionary<string, string> TestConfiguration = new()
	{
		{"API_URL", "http://127.0.0.1:9999"},
		{"CONNECTION_STRING", "Data Source=Tests.db;"},
		{"MINIO_URL", "127.0.0.1:9000" },
		{"MINIO_BUCKET", "memes" },
		{"MINIO_ROOT_USER", "TSf3cfIysZ2y2trh" },
		{"MINIO_ROOT_PASSWORD", "aU5hv99vEzqf20v2MlLZeLb6OiiwqAPc" }
	};

	private readonly Random _random = new();
	

	[OneTimeSetUp]
	public void SetupApplication()
	{
		base.Setup(TestConfiguration);
	}

	[OneTimeTearDown]
	public void TearDownApp()
	{
		base.TearDown();
	}


	[Test]
	public async Task IndexingTest()
	{
		var path = Path.Combine(Environment.CurrentDirectory, "static");
		var files = Directory.EnumerateFiles(path)
			.Where(f => !f.Contains(".gitkeep"))
			.Select(path => path.Split(Path.DirectorySeparatorChar).Last())
			.ToList();
		
		var dbFiles = await Db.Files.ToListAsync();
		
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
			var nextImageResponse = await Client.GetAsync(string.Format(GetNextUrl, userId, previousId));
			Assert.True(nextImageResponse.IsSuccessStatusCode);
			
			var nextImage = await nextImageResponse.Content.ReadFromJsonAsync<ImageResponse>();
		
			Assert.NotNull(nextImage);
			if (nextImage!.Finished)
			{
				Assert.Null(nextImage.ImageId);
				return;
			}
			
			Assert.AreNotEqual(previousId, nextImage.ImageId);

			var imageContent = await DefaultClient.GetAsync(nextImage.Url);
			Assert.True(imageContent.IsSuccessStatusCode);

			var dbImage = await Db.Files.FindAsync(nextImage.ImageId);
		
			Assert.NotNull(dbImage);
			Assert.True(nextImage.Url!.Contains(dbImage!.FileName));


			Assert.DoesNotThrowAsync(async () =>
			{
				var stat = await MinioClient.StatObjectAsync(new StatObjectArgs()
					.WithBucket(MinioConfiguration.BucketName)
					.WithObject(dbImage.FileName));

			});
			
			previousId = nextImage.ImageId!.Value;
		}
	}


	[Test]
	public async Task EstimateBasic_Test()
	{
		var userId = Guid.NewGuid().ToString();
		var estimateValue = 2;
		
		var nextImageResponse = await Client.GetAsync(string.Format(GetNextUrl, userId, 0));
		Assert.True(nextImageResponse.IsSuccessStatusCode);
		
		var nextImage = await nextImageResponse.Content.ReadFromJsonAsync<ImageResponse>();
		Assert.NotNull(nextImage);
		Assert.False(nextImage!.Finished);

		var beforeCount = await Db.Estimates.CountAsync();

		var estimateRequest = await Client.PostAsJsonAsync(
			string.Format(EstimateUrl, nextImage.ImageId),
			new EstimateRequest(estimateValue, userId));
		
		Assert.True(estimateRequest.IsSuccessStatusCode);

		var afterCount = await Db.Estimates.CountAsync();
		
		Assert.AreEqual(beforeCount + 1, afterCount);

		var estimate = await Db.Estimates
			.Include(e => e.File)
			.FirstOrDefaultAsync(e => e.ClientId == userId);
		
		Assert.NotNull(estimate);
		Assert.AreEqual(estimateValue, estimate!.Score);
		Assert.AreEqual(nextImage.ImageId, estimate.FileId);
		
	}
	
	[Test]
	public async Task Estimate_NotExistingImage_Negative_Test()
	{
		var request = new EstimateRequest(10, Guid.NewGuid().ToString());
		var randomImageId = _random.Next(1000000, 5000000);

		var response = await Client.PostAsJsonAsync(string.Format(EstimateUrl, randomImageId), request);

		Assert.False(response.IsSuccessStatusCode);
		Assert.AreEqual(response.StatusCode, HttpStatusCode.NotFound);
	}


	[Test]
	public async Task GetNextImage_EmptyClient_Negative_Test()
	{
		var response = await Client.GetAsync(GetNextUrl);
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
			var nextImageResponse = await Client.GetAsync(string.Format(GetNextUrl, userId, ""));
			Assert.True(nextImageResponse.IsSuccessStatusCode);
			
			var nextImage = await nextImageResponse.Content.ReadFromJsonAsync<ImageResponse>();
			Assert.NotNull(nextImage);
			if (nextImage!.Finished)
			{
				Assert.Null(nextImage.ImageId);
				return;
			}
			
			Assert.AreNotEqual(previousId, nextImage.ImageId);

			var imageContent = await DefaultClient.GetAsync(nextImage.Url);
			Assert.True(imageContent.IsSuccessStatusCode);

			var dbImage = await Db.Files.FindAsync(nextImage.ImageId);
		
			Assert.NotNull(dbImage);
			Assert.True(nextImage.Url!.Contains(dbImage!.FileName));

            Assert.DoesNotThrowAsync(async () =>
            {
                var stat = await MinioClient.StatObjectAsync(new StatObjectArgs()
                    .WithBucket(MinioConfiguration.BucketName)
                    .WithObject(dbImage.FileName));
            });

            var beforeCount = await Db.Estimates.CountAsync();

			var estimateResponse = await Client.PostAsJsonAsync(
				string.Format(EstimateUrl, nextImage.ImageId),
				new EstimateRequest(estimateValue, userId));
			
			Assert.True(estimateResponse.IsSuccessStatusCode);
			
			var afterCount = await Db.Estimates.CountAsync();
		
			Assert.AreEqual(beforeCount + 1, afterCount);

			var estimate = await Db.Estimates
				.Include(e => e.File)
				.FirstOrDefaultAsync(e => e.ClientId == userId && e.FileId == nextImage.ImageId);
		
			Assert.NotNull(estimate);
			Assert.AreEqual(estimateValue, estimate!.Score);
			Assert.AreEqual(nextImage.ImageId, estimate.FileId);
			
			
			previousId = nextImage.ImageId!.Value;
		}
	}


	private async Task<string> CreateTempFile(long length, string extension)
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, extension);
		await using(var stream = File.OpenWrite(tempFile))
		{
			var buffer = new byte[length];
			await stream.WriteAsync(buffer);
		}

		return tempFile;
	}

	[Test]
	public async Task InvalidImageFormat_Test()
	{
		var file = await CreateTempFile(50 * 1024, ".gif");
		HttpResponseMessage response;
		await using (var stream = File.OpenRead(file))
		{
			var streamContent = new StreamContent(stream);
			streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/gif");

			var multipart = new MultipartFormDataContent();
			multipart.Add(streamContent, "imageFile", Path.GetFileName(file));

			response = await Client.PostAsync(UploadImage, multipart);
		}
		
		var content = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.BadRequest, "Invalid response. " +
		                                                              $"Expected {HttpStatusCode.BadRequest}. " +
		                                                              $"Got {response.StatusCode}. " + 
		                                                              $"Content: {content}");
	}
	
	[Test]
	public async Task InvalidImageSize_Test()
	{
		var file = await CreateTempFile(20 * 1024 * 1024, ".png");
		HttpResponseMessage response;
		await using (var stream = File.OpenRead(file))
		{
			var streamContent = new StreamContent(stream);
			streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
			
			var multipart = new MultipartFormDataContent();
			multipart.Add(streamContent, "imageFile", Path.GetFileName(file));

			response = await Client.PostAsync(UploadImage, multipart);
		}
		
		var content = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.BadRequest, "Invalid response. " +
		                                                              $"Expected {HttpStatusCode.BadRequest}. " +
		                                                              $"Got {response.StatusCode}. " + 
		                                                              $"Content: {content}");
	}
	
	[Test]
	public async Task UploadImage_Test()
	{
		HttpResponseMessage response;
		await using(var stream = File.OpenRead("static/test_image.jpg"))
		{
			var streamContent = new StreamContent(stream);
			streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpg");

			var multipart = new MultipartFormDataContent();
			multipart.Add(streamContent, "imageFile", "test_image.jpg");

			response = await Client.PostAsync(UploadImage, multipart);
		}
		
		var content = await response.Content.ReadAsStringAsync();
		Assert.True(response.StatusCode == HttpStatusCode.OK, "Invalid response. " +
		                                                      $"Expected {HttpStatusCode.OK}. " +
		                                                      $"Got {response.StatusCode}. " + 
		                                                      $"Content: {content}");

		var imageResponse = await response.Content.ReadFromJsonAsync<ImageResponse>();
		response = await DefaultClient.GetAsync(imageResponse.Url);
		Assert.True(response.StatusCode == HttpStatusCode.OK, "Invalid response. " +
		                                                      $"Expected {HttpStatusCode.OK}. " +
		                                                      $"Got {response.StatusCode}. ");

		var memeFile = await Db.Files.FindAsync(imageResponse.ImageId);
		Assert.True(memeFile is not null, $"Can't find {imageResponse.ImageId} in db.");
	}

	[Test]
	public async Task MetricsAvailable_Test()
	{
		var metricsResponse = await Client.GetAsync(MetricsUrl);
		Assert.True(metricsResponse.IsSuccessStatusCode);

		var result = await metricsResponse.Content.ReadAsStringAsync();
		//some random metric
		Assert.True(result.Contains("dotnet_collection_count_total"));
	}


}