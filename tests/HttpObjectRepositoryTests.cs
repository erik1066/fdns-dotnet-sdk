using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Foundation.Sdk.Data;
using Foundation.Sdk.Tests.Models;
using RichardSzalay.MockHttp;

namespace Foundation.Sdk.Tests
{
    public class HttpObjectRepositoryTests : IClassFixture<ObjectFixture>
    {
        ObjectFixture _objectFixture;

        public HttpObjectRepositoryTests(ObjectFixture fixture)
        {
            this._objectFixture = fixture;
        }

        [Fact]
        public void Get()
        {
            var repo = new HttpObjectRepository<Customer>(_objectFixture.ClientFactory, _objectFixture.Logger, "unittests");

            ServiceResult<Customer> result = repo.GetAsync("1").Result;
            Customer customerResult = result.Value;
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal("John", customerResult.FirstName);
            Assert.Equal("Smith", customerResult.LastName);
            Assert.Equal(32, customerResult.Age);
            Assert.True(result.Elapsed.TotalMilliseconds > 0);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Replace()
        {
            var customer = new Customer()
            {
                FirstName = "Mary",
                LastName = "Jane",
                Age = 39
            };

            var repo = new HttpObjectRepository<Customer>(_objectFixture.ClientFactory, _objectFixture.Logger, "unittests");

            ServiceResult<Customer> result = repo.ReplaceAsync("2", customer).Result;
            Customer customerResult = result.Value;
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal("Mary", customerResult.FirstName);
            Assert.Equal("Jane", customerResult.LastName);
            Assert.Equal(39, customerResult.Age);
            Assert.True(result.Elapsed.TotalMilliseconds > 0);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Count()
        {
            var repo = new HttpObjectRepository<Customer>(_objectFixture.ClientFactory, _objectFixture.Logger, "unittests");

            ServiceResult<int> result = repo.GetCountAsync(string.Empty).Result;
            int count = result.Value;
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal(2, count);
            Assert.True(result.Elapsed.TotalMilliseconds > 0);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Find()
        {
            var repo = new HttpObjectRepository<Customer>(_objectFixture.ClientFactory, _objectFixture.Logger, "unittests");

            ServiceResult<SearchResults<Customer>> result = repo.FindAsync(0, -1, string.Empty, string.Empty, true).Result;
            SearchResults<Customer> searchResults = result.Value;
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal(2, searchResults.Items.Count);
            Assert.True(result.Elapsed.TotalMilliseconds > 0);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Delete()
        {
            var repo = new HttpObjectRepository<Customer>(_objectFixture.ClientFactory, _objectFixture.Logger, "unittests");

            ServiceResult<DeleteResult> result = repo.DeleteAsync("3").Result;
            DeleteResult deleteResult = result.Value;
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal(1, deleteResult.Deleted);
            Assert.True(deleteResult.Success);
            Assert.True(result.Elapsed.TotalMilliseconds > 0);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Insert_with_Id()
        {
            var customer = new Customer()
            {
                FirstName = "Mary",
                LastName = "Jane",
                Age = 39
            };

            var repo = new HttpObjectRepository<Customer>(_objectFixture.ClientFactory, _objectFixture.Logger, "unittests");

            ServiceResult<Customer> result = repo.InsertAsync("4", customer).Result;
            Customer customerResult = result.Value;
            Assert.Equal(HttpStatusCode.Created, result.Code);
            Assert.Equal("Mary", customerResult.FirstName);
            Assert.Equal("Jane", customerResult.LastName);
            Assert.Equal(39, customerResult.Age);
            Assert.True(result.Elapsed.TotalMilliseconds > 0);
            Assert.Equal("Object", result.ServiceName);
        }
    }

    public class ObjectFixture : IDisposable
    {
        public ILogger<HttpObjectRepository<Customer>> Logger { get; private set; }
        public HttpClient Client { get; private set; }
        public IHttpClientFactory ClientFactory { get; private set; }

        public ObjectFixture()
        {
            Logger = new Mock<ILogger<HttpObjectRepository<Customer>>>().Object;

            var mockHttp = new MockHttpMessageHandler();

            // Setup a respond for the user api (including a wildcard in the URL)
            mockHttp.When("http://localhost/bookstore/customer/1")
                .Respond(HttpStatusCode.OK, "application/json", "{ \"firstName\" : \"John\", \"lastName\" : \"Smith\", \"age\" : 32 }");

            mockHttp.When("http://localhost/bookstore/customer/2")
                .Respond(HttpStatusCode.OK, "application/json", "{ \"firstName\" : \"Mary\", \"lastName\" : \"Jane\", \"age\" : 39 }");
            
            mockHttp.When("http://localhost/bookstore/customer/count")
                .Respond(HttpStatusCode.OK, "application/json", "{ \"count\": 2 }");

            mockHttp.When("http://localhost/bookstore/customer/find?from=0&order=1&size=-1")
                .Respond(HttpStatusCode.OK, "application/json", "{ \"total\": 2, \"items\": [ { \"firstName\": \"John\", \"lastName\": \"Smith\", \"_id\": \"1\", \"age\": 32 }, { \"firstName\": \"Mary\", \"lastName\": \"Jane\", \"_id\": \"2\", \"age\": 39 } ] }");

            mockHttp.When("http://localhost/bookstore/customer/3")
                .Respond(HttpStatusCode.OK, "application/json", "{ \"deleted\": 1, \"success\": true }");
            
            mockHttp.When("http://localhost/bookstore/customer/4")
                .Respond(HttpStatusCode.Created, "application/json", "{ \"firstName\" : \"Mary\", \"lastName\" : \"Jane\", \"age\" : 39 }");

            // Inject the handler or client into your application code
            var client = mockHttp.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost/bookstore/customer/");
            Client = client;

            var mock = new Mock<IHttpClientFactory>();
            mock.CallBase = true;
            mock.Setup(x => x.CreateClient($"unittests-{Common.OBJECT_SERVICE_NAME}")).Returns(Client);

            ClientFactory = mock.Object;
        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
