// using System;
// using System.Net;
// using System.Net.Http;
// using Microsoft.Extensions.Logging;
// using Xunit;
// using Moq;
// using Foundation.Sdk.Data;
// using Foundation.Sdk.Tests.Models;
// using RichardSzalay.MockHttp;

// namespace Foundation.Sdk.Tests
// {
//     public class HttpObjectServiceWithOneRoutePrefixTests : IClassFixture<ObjectServiceWithRoutePrefixFixture>
//     {
//         ObjectServiceWithRoutePrefixFixture _objectFixture;

//         public HttpObjectServiceWithOneRoutePrefixTests(ObjectServiceWithRoutePrefixFixture fixture)
//         {
//             this._objectFixture = fixture;
//         }

//         [Fact]
//         public void Get()
//         {
//             var repo = new HttpObjectService<Customer>("unittests", "bookstore", "customer", _objectFixture.ClientFactory, _objectFixture.Logger);

//             ServiceResult<Customer> result = repo.GetAsync("1").Result;
//             Customer customerResult = result.Value;
//             Assert.Equal((int)HttpStatusCode.OK, result.Status);
//             Assert.Equal("John", customerResult.FirstName);
//             Assert.Equal("Smith", customerResult.LastName);
//             Assert.Equal(32, customerResult.Age);
//             Assert.Equal("Object", result.ServiceName);
//         }

//         [Fact]
//         public void Replace()
//         {
//             var customer = new Customer()
//             {
//                 FirstName = "Mary",
//                 LastName = "Jane",
//                 Age = 39
//             };

//             var repo = new HttpObjectService<Customer>("unittests", "bookstore", "customer", _objectFixture.ClientFactory, _objectFixture.Logger);

//             ServiceResult<Customer> result = repo.ReplaceAsync("2", customer).Result;
//             Customer customerResult = result.Value;
//             Assert.Equal((int)HttpStatusCode.OK, result.Status);
//             Assert.Equal("Mary", customerResult.FirstName);
//             Assert.Equal("Jane", customerResult.LastName);
//             Assert.Equal(39, customerResult.Age);
//             Assert.Equal("Object", result.ServiceName);
//         }

//         [Fact]
//         public void Count()
//         {
//             var repo = new HttpObjectService<Customer>("unittests", "bookstore", "customer", _objectFixture.ClientFactory, _objectFixture.Logger);

//             ServiceResult<long> result = repo.CountAsync(string.Empty).Result;
//             long count = result.Value;
//             Assert.Equal((int)HttpStatusCode.OK, result.Status);
//             Assert.Equal(2, count);
//             Assert.Equal("Object", result.ServiceName);
//         }

//         [Fact]
//         public void Find()
//         {
//             var repo = new HttpObjectService<Customer>("unittests", "bookstore", "customer", _objectFixture.ClientFactory, _objectFixture.Logger);

//             ServiceResult<SearchResults<Customer>> result = repo.FindAsync(string.Empty, 0, -1, string.Empty, System.ComponentModel.ListSortDirection.Descending).Result;
//             SearchResults<Customer> searchResults = result.Value;
//             Assert.Equal((int)HttpStatusCode.OK, result.Status);
//             Assert.Equal(2, searchResults.Items.Count);
//             Assert.Equal("Object", result.ServiceName);
//         }

//         [Fact]
//         public void Delete()
//         {
//             var repo = new HttpObjectService<Customer>("unittests", "bookstore", "customer", _objectFixture.ClientFactory, _objectFixture.Logger);

//             ServiceResult<int> result = repo.DeleteAsync("3").Result;
//             int deleteResult = result.Value;
//             Assert.Equal((int)HttpStatusCode.OK, result.Status);
//             Assert.Equal(1, deleteResult);
//             Assert.True(result.IsSuccess);
//             Assert.Equal("Object", result.ServiceName);
//         }

//         [Fact]
//         public void Insert_with_Id()
//         {
//             var customer = new Customer()
//             {
//                 FirstName = "Mary",
//                 LastName = "Jane",
//                 Age = 39
//             };

//             var repo = new HttpObjectService<Customer>("unittests", "bookstore", "customer", _objectFixture.ClientFactory, _objectFixture.Logger);

//             ServiceResult<Customer> result = repo.InsertAsync("4", customer).Result;
//             Customer customerResult = result.Value;
//             Assert.Equal((int)HttpStatusCode.Created, result.Status);
//             Assert.Equal("Mary", customerResult.FirstName);
//             Assert.Equal("Jane", customerResult.LastName);
//             Assert.Equal(39, customerResult.Age);
//             Assert.Equal("Object", result.ServiceName);
//         }
//     }

//     public class ObjectServiceWithRoutePrefixFixture : IDisposable
//     {
//         public ILogger<HttpObjectService<Customer>> Logger { get; private set; }
//         public HttpClient Client { get; private set; }
//         public IHttpClientFactory ClientFactory { get; private set; }

//         public ObjectServiceWithRoutePrefixFixture()
//         {
//             Logger = new Mock<ILogger<HttpObjectService<Customer>>>().Object;

//             var mockHttp = new MockHttpMessageHandler();

//             // Setup a respond for the user api (including a wildcard in the URL)
//             mockHttp.When("http://localhost/bookstore/customer/1")
//                 .Respond("application/json", "{ \"firstName\" : \"John\", \"lastName\" : \"Smith\", \"age\" : 32 }");

//             mockHttp.When("http://localhost/bookstore/customer/2")
//                 .Respond("application/json", "{ \"firstName\" : \"Mary\", \"lastName\" : \"Jane\", \"age\" : 39 }");
            
//             mockHttp.When("http://localhost/bookstore/customer/count")
//                 .Respond("application/json", "{ \"count\": 2 }");

//             mockHttp.When("http://localhost/bookstore/customer/find?from=0&order=1&size=-1")
//                 .Respond("application/json", "{ \"total\": 2, \"items\": [ { \"firstName\": \"John\", \"lastName\": \"Smith\", \"_id\": \"1\", \"age\": 32 }, { \"firstName\": \"Mary\", \"lastName\": \"Jane\", \"_id\": \"2\", \"age\": 39 } ] }");

//             mockHttp.When("http://localhost/bookstore/customer/3")
//                 .Respond(HttpStatusCode.OK, "application/json", "{ \"deleted\": 1, \"success\": true }");
            
//             mockHttp.When("http://localhost/bookstore/customer/4")
//                 .Respond(HttpStatusCode.Created, "application/json", "{ \"firstName\" : \"Mary\", \"lastName\" : \"Jane\", \"age\" : 39 }");

//             // Inject the handler or client into your application code
//             var client = mockHttp.ToHttpClient();
//             client.BaseAddress = new Uri("http://localhost/");
//             Client = client;

//             var mock = new Mock<IHttpClientFactory>();
//             mock.CallBase = true;
//             mock.Setup(x => x.CreateClient($"unittests-{Common.OBJECT_SERVICE_NAME}")).Returns(Client);

//             ClientFactory = mock.Object;
//         }

//         public void Dispose()
//         {
//             Client.Dispose();
//         }
//     }
// }
