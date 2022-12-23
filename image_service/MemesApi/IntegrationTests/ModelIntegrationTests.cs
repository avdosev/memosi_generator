using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MemesApi.Dto;
using NUnit.Framework;

namespace IntegrationTests
{
	//Requires meme generator service to be launched at http://127.0.0.1:8088
	public class ModelIntegrationTests: TestBase
	{
		private const string UploadImage = "api/images/upload";

		private static readonly Dictionary<string, string> TestConfiguration = new()
		{
			{"API_URL", "http://127.0.0.1:9999"},
			{"CONNECTION_STRING", "Data Source=Tests.db;"},
			
			{"MODEL_CONNECTION_STRING", "http://127.0.0.1:8088"}
		};
		
		
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
			response = await Client.GetAsync(imageResponse.Url);
			Assert.True(response.StatusCode == HttpStatusCode.OK, "Invalid response. " +
			                                                      $"Expected {HttpStatusCode.OK}. " +
			                                                      $"Got {response.StatusCode}. ");

			var memeImageContent = await response.Content.ReadAsByteArrayAsync();
			var originalImageContent = await File.ReadAllBytesAsync("static/test_image.jpg");
			
			//Assert that image changed
			Assert.AreNotEqual(memeImageContent, originalImageContent);

			var memeFile = await Db.Files.FindAsync(imageResponse.ImageId);
			Assert.True(memeFile is not null, $"Can't find {imageResponse.ImageId} in db.");
		}

	}
}