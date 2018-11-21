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
        public void Test1()
        {
            var repo = new HttpObjectRepository<Customer>(_objectFixture.ClientFactory, _objectFixture.Logger, "unittests");

            ServiceResult<Customer> result = repo.GetAsync("1").Result;
            Customer customerResult = result.Response;
            Assert.Equal(HttpStatusCode.OK, result.Code);
            Assert.Equal("John", customerResult.FirstName);
            Assert.Equal("Smith", customerResult.LastName);
            Assert.Equal(32, customerResult.Age);
            Assert.True(result.Elapsed.TotalMilliseconds > 0);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Test2()
        {
            var customer = new Customer()
            {
                FirstName = "Mary",
                LastName = "Jane",
                Age = 39
            };

            var repo = new HttpObjectRepository<Customer>(_objectFixture.ClientFactory, _objectFixture.Logger, "unittests");

            ServiceResult<Customer> result = repo.ReplaceAsync("2", customer).Result;
            Customer customerResult = result.Response;
            Assert.Equal(HttpStatusCode.OK, result.Code);
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
                    .Respond("application/json", "{ \"firstName\" : \"John\", \"lastName\" : \"Smith\", \"age\" : 32 }");

            mockHttp.When("http://localhost/bookstore/customer/2")
                    .Respond("application/json", "{ \"firstName\" : \"Mary\", \"lastName\" : \"Jane\", \"age\" : 39 }");

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
