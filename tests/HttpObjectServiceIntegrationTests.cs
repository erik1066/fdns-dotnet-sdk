// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.Net;
// using System.Net.Http;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Logging;
// using Xunit;
// using Moq;
// using Foundation.Sdk.Data;
// using Foundation.Sdk.Tests.Models;
// using RichardSzalay.MockHttp;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
// using Newtonsoft.Json.Serialization;

// namespace Foundation.Sdk.Tests
// {
//     public class HttpObjectServiceIntegrationTests : IClassFixture<HttpObjectIntegrationFixture>
//     {
//         HttpObjectIntegrationFixture _fixture;

//         public HttpObjectServiceIntegrationTests(HttpObjectIntegrationFixture fixture)
//         {
//             this._fixture = fixture;
//         }

//         [Theory]
//         [InlineData("1", "{ \"title\": \"The Red Badge of Courage\" }", "The Red Badge of Courage")]
//         [InlineData("2", "{ \"title\": \"Don Quixote\" }", "Don Quixote")]
//         [InlineData("3", "{ \"title\": \"A Connecticut Yankee in King Arthur's Court\" }", "A Connecticut Yankee in King Arthur's Court")]
//         public async Task Get_Object_by_Primitive_Id(string id, string jsonToInsert, string expectedTitle)
//         {
//             var service = _fixture.BooksService;

//             // insert the book            
//             var insertResult = await service.InsertAsync(id, jsonToInsert);

//             JObject insertedObj = JObject.Parse(insertResult.Value);
//             var insertedTitle = insertedObj["title"].ToString();
//             var insertedId = insertedObj["_id"].ToString();

//             // now retrieve the book
//             var getResult = await service.GetAsync(id);            

//             JObject getObj = JObject.Parse(getResult.Value);
//             var retrievedTitle = getObj["title"].ToString();
//             var retrievedId = getObj["_id"].ToString();

//             // was it inserted correctly?
//             Assert.Equal(200, getResult.Status);

//             Assert.Equal(expectedTitle, retrievedTitle);
//             Assert.Equal(expectedTitle, insertedTitle);

//             Assert.Equal(insertedId, retrievedId);
//         }

//         [Theory]
//         [InlineData("{ 'amount': 32.54, 'customer': { 'firstName': 'John', 'lastName': 'Smith' } }", 32.54)]
//         public async Task Get_Object_by_ObjectId(string jsonToInsert, decimal expectedAmount)
//         {
//             var service = _fixture.OrdersService;

//             // insert the book
//             var insertResult = await service.InsertAsync(jsonToInsert);
//             JObject insertedObj = JObject.Parse(insertResult.Value);
//             var insertedId = insertedObj["_id"].ToString();
//             var insertedAmount = insertedObj["amount"].ToString();

//             // now retrieve the book
//             var getResult = await service.GetAsync(insertedId);
//             JObject retrievedObj = JObject.Parse(getResult.Value);
//             var retrievedId = retrievedObj["_id"].ToString();
//             var retrievedAmount = insertedObj["amount"].ToString();

//             // was it inserted correctly?
//             Assert.Equal(200, getResult.Status);
//             Assert.Equal(retrievedId, insertedId);
//             Assert.Equal(retrievedAmount, insertedAmount);
//         }
//     }

//     public class HttpObjectIntegrationFixture : IDisposable
//     {
//         public ILogger<HttpObjectService> BookLogger { get; private set; }
//         public ILogger<HttpObjectService> OrderLogger { get; private set; }
//         public HttpClient Client { get; private set; }
//         public IHttpClientFactory ClientFactory { get; private set; }
//         public IObjectService BooksService { get; private set; }
//         public IObjectService OrdersService { get; private set; }

//         public JsonSerializerSettings JsonSerializerSettings { get; set; } = new JsonSerializerSettings
//         {
//             ContractResolver = new CamelCasePropertyNamesContractResolver()
//         };

//         public HttpObjectIntegrationFixture()
//         {
//             var checkerClient = new HttpClient();

//             WaitUntilDockerIsDown(checkerClient);
//             WaitUntilDockerIsUp(checkerClient);

//             checkerClient.Dispose();            

//             Client = new HttpClient();
//             Client.BaseAddress = new Uri("http://localhost:9090/api/1.0/");

//             var mock = new Mock<IHttpClientFactory>();
//             mock.CallBase = true;
//             mock.Setup(x => x.CreateClient($"unittests-{Common.OBJECT_SERVICE_NAME}")).Returns(Client);

//             ClientFactory = mock.Object;

//             BookLogger = new Mock<ILogger<HttpObjectService>>().Object;
//             OrderLogger = new Mock<ILogger<HttpObjectService>>().Object;

//             BooksService = new HttpObjectService("unittests", "bookstore", "books", ClientFactory, BookLogger);
//             OrdersService = new HttpObjectService("unittests", "bookstore", "orders", ClientFactory, OrderLogger);
//         }

//         public void Dispose()
//         {
//             WaitUntilDockerIsDown(new HttpClient());

//             Client.Dispose();
//         }

//         private void WaitUntilDockerIsUp(HttpClient checkerClient)
//         {
//             var process = new Process {
//                 StartInfo = new ProcessStartInfo {
//                     FileName = "docker-compose",
//                     Arguments = "up -d",
//                     UseShellExecute = false,
//                     RedirectStandardOutput = true,
//                     CreateNoWindow = true
//                 }
//             };
//             process.Start();

//             bool started = false;

//             var startTime = DateTime.Now;

//             while (!started)
//             {
//                 try 
//                 {
//                     var result = checkerClient.GetAsync("http://localhost:9090/health/live");
//                     if (result.Result.IsSuccessStatusCode)
//                     {
//                         started = true;
//                     }
//                     else
//                     {
//                         started = false;
//                     }
//                     System.Threading.Thread.Sleep(1_000);
//                 }
//                 catch { }

//                 TimeSpan ts = DateTime.Now - startTime;
//                 if (ts.TotalSeconds > 30)
//                 {
//                     throw new InvalidOperationException("Service took too long to start");
//                 }
//             }
//         }

//         private void WaitUntilDockerIsDown(HttpClient checkerClient)
//         {
            
//             var process = new Process {
//                 StartInfo = new ProcessStartInfo {
//                     FileName = "docker-compose",
//                     Arguments = "down",
//                     UseShellExecute = false,
//                     RedirectStandardOutput = true,
//                     CreateNoWindow = true
//                 }
//             };
//             process.Start();
            
//             bool started = false;
//             var startTime = DateTime.Now;

//             while (started)
//             {
//                 try 
//                 {
//                     var result = checkerClient.GetAsync("http://localhost:9090/health/live");
//                     if (result.Result.IsSuccessStatusCode)
//                     {
//                         started = true;
//                     }
//                     else
//                     {
//                         started = false;
//                     }
//                     System.Threading.Thread.Sleep(500);
//                 }
//                 catch { }

//                 TimeSpan ts = DateTime.Now - startTime;
//                 if (ts.TotalSeconds > 20)
//                 {
//                     throw new InvalidOperationException("Service took too long to stop");
//                 }                
//             }
//         }
//     }
// }
