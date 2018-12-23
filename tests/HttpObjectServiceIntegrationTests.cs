using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Foundation.Sdk.Data;
using Foundation.Sdk.Tests.Models;
using RichardSzalay.MockHttp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Foundation.Sdk.Tests
{
    public class HttpObjectServiceIntegrationTests : IClassFixture<HttpObjectIntegrationFixture>
    {
        HttpObjectIntegrationFixture _fixture;

        public HttpObjectServiceIntegrationTests(HttpObjectIntegrationFixture fixture)
        {
            this._fixture = fixture;
        }

        [Theory]
        [InlineData("1", "{ \"title\": \"The Red Badge of Courage\" }", "The Red Badge of Courage")]
        // [InlineData("2", "{ \"title\": \"Don Quixote\" }", "{ \"_id\" : \"2\", \"title\" : \"Don Quixote\" }")]
        // [InlineData("3", "{ \"title\": \"A Connecticut Yankee in King Arthur's Court\" }", "{ \"_id\" : \"3\", \"title\" : \"A Connecticut Yankee in King Arthur's Court\" }")]
        public async Task Get_Object_by_Primitive_Id(string id, string insertedJson, string expectedTitle)
        {
            var service = _fixture.BooksService;

            // insert the book
            Book book = JsonConvert.DeserializeObject<Book>(insertedJson, _fixture.JsonSerializerSettings);
            var insertResult = await service.InsertAsync(id, book);

            // now get it...
            var getResult = await service.GetAsync(id);

            // was it inserted correctly?
            Assert.Equal(200, getResult.Status);
            Assert.Equal(expectedTitle, getResult.Value.Title);
            // Assert.Equal(id, getResult.Value.Id);
            Assert.Equal(insertResult.Value.Title, getResult.Value.Title);
            // Assert.Equal(insertResult.Value.Id, getResult.Value.Id);
        }
    }

    public class HttpObjectIntegrationFixture : IDisposable
    {
        public ILogger<HttpObjectService<Book>> Logger { get; private set; }
        public HttpClient Client { get; private set; }
        public IHttpClientFactory ClientFactory { get; private set; }
        public IObjectService<Book> BooksService { get; private set; }

        public JsonSerializerSettings JsonSerializerSettings { get; set; } = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public HttpObjectIntegrationFixture()
        {
            var killOldProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "docker-compose",
                    Arguments = "down",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            killOldProcess.Start();

            System.Threading.Thread.Sleep(5_000);

            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "docker-compose",
                    Arguments = "up -d",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            
            var checkerClient = new HttpClient();

            bool started = false;

            var startTime = DateTime.Now;

            while (!started)
            {
                try 
                {
                    var result = checkerClient.GetAsync("http://localhost:9090/health/live");
                    if (result.Result.IsSuccessStatusCode)
                    {
                        started = true;
                    }
                    System.Threading.Thread.Sleep(1_000);
                }
                catch { }

                TimeSpan ts = DateTime.Now - startTime;
                if (ts.TotalSeconds > 30)
                {
                    throw new InvalidOperationException("Service took too long to start");
                }
            }

            Client = new HttpClient();
            Client.BaseAddress = new Uri("http://localhost:9090/api/1.0/");

            var mock = new Mock<IHttpClientFactory>();
            mock.CallBase = true;
            mock.Setup(x => x.CreateClient($"unittests-{Common.OBJECT_SERVICE_NAME}")).Returns(Client);

            ClientFactory = mock.Object;

            Logger = new Mock<ILogger<HttpObjectService<Book>>>().Object;

            BooksService = new HttpObjectService<Book>("unittests", "bookstore", "books", ClientFactory, Logger);
        }

        public void Dispose()
        {
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "docker-compose",
                    Arguments = "down",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();

            System.Threading.Thread.Sleep(2_000);

            // Client.Dispose();
        }
    }
}
