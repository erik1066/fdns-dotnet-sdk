using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Foundation.Sdk.Services;
using Foundation.Sdk.Tests.Models;
using RichardSzalay.MockHttp;
using Newtonsoft.Json.Linq;

namespace Foundation.Sdk.Tests
{
    public class HttpObjectServiceTests : IClassFixture<ObjectFixture>
    {
        ObjectFixture _objectFixture;

        public HttpObjectServiceTests(ObjectFixture fixture)
        {
            this._objectFixture = fixture;
        }

        [Fact]
        public void Get()
        {
            var repo = new HttpObjectService("unittests", _objectFixture.ClientFactory, _objectFixture.Logger);

            ServiceResult<string> result = repo.GetAsync("bookstore", "customer", "1").Result;
            string customerResult = result.Value;
            Customer customer = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer>(customerResult);

            Assert.Equal((int)HttpStatusCode.OK, result.Status);
            Assert.Equal("John", customer.FirstName);
            Assert.Equal("Smith", customer.LastName);
            Assert.Equal(32, customer.Age);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Replace()
        {
            var customer = "{ 'name': 'Mary', 'lastName': 'Jane', 'age': 39 }";

            var repo = new HttpObjectService("unittests", _objectFixture.ClientFactory, _objectFixture.Logger);

            ServiceResult<string> result = repo.ReplaceAsync("bookstore", "customer", "2", customer).Result;
            
            string newCustomerResult = result.Value;
            Customer newCustomer = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer>(newCustomerResult);

            Assert.Equal((int)HttpStatusCode.OK, result.Status);
            Assert.Equal("Mary", newCustomer.FirstName);
            Assert.Equal("Jane", newCustomer.LastName);
            Assert.Equal(39, newCustomer.Age);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Count()
        {
            var repo = new HttpObjectService("unittests", _objectFixture.ClientFactory, _objectFixture.Logger);

            ServiceResult<long> result = repo.CountAsync("bookstore", "customer", string.Empty).Result;
            long count = result.Value;
            Assert.Equal((int)HttpStatusCode.OK, result.Status);
            Assert.Equal(2, count);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Find()
        {
            var repo = new HttpObjectService("unittests", _objectFixture.ClientFactory, _objectFixture.Logger);

            var findTask = repo.FindAsync("bookstore", "customer", findExpression: string.Empty, start: 0, limit: -1, sortFieldName: string.Empty, sortDirection: System.ComponentModel.ListSortDirection.Descending);
            ServiceResult<SearchResults> result = findTask.Result;
            SearchResults searchResults = result.Value;
            Assert.Equal((int)HttpStatusCode.OK, result.Status);
            Assert.Equal(2, searchResults.Items.Count);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Delete()
        {
            var repo = new HttpObjectService("unittests", _objectFixture.ClientFactory, _objectFixture.Logger);

            ServiceResult<int> result = repo.DeleteAsync("bookstore", "customer", "3").Result;
            int deleteResult = result.Value;
            Assert.Equal((int)HttpStatusCode.OK, result.Status);
            Assert.Equal(1, deleteResult);
            Assert.True(result.IsSuccess);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Insert_with_Id()
        {
            var customer = "{ 'name': 'Mary', 'lastName': 'Jane', 'age': 39 }";

            var repo = new HttpObjectService("unittests", _objectFixture.ClientFactory, _objectFixture.Logger);

            ServiceResult<string> result = repo.InsertAsync("bookstore", "customer", "4", customer).Result;
            
            string newCustomerResult = result.Value;
            Customer newCustomer = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer>(newCustomerResult);

            Assert.Equal((int)HttpStatusCode.Created, result.Status);
            Assert.Equal("Mary", newCustomer.FirstName);
            Assert.Equal("Jane", newCustomer.LastName);
            Assert.Equal(39, newCustomer.Age);
            Assert.Equal("Object", result.ServiceName);
        }

        [Fact]
        public void Multi_Insert()
        {
            var customers = new List<string>(2)
            {
                "{ 'name': 'Mary', 'lastName': 'Jane', 'age': 39 }",
                "{ 'name': 'John', 'lastName': 'Smith', 'age': 48 }"
            };

            var repo = new HttpObjectService("unittests", _objectFixture.ClientFactory, _objectFixture.Logger);

            ServiceResult<IEnumerable<string>> result = repo.InsertManyAsync("bookstore", "customer", customers).Result;
            List<string> ids = (List<string>)result.Value;
            Assert.Equal((int)HttpStatusCode.Created, result.Status);
            Assert.Equal("5c1fbe4dbe41760001060b25", ids[0]);
            Assert.Equal("5c1fbe4dbe41760001060b26", ids[1]);
        }

        [Fact]
        public void Aggregate()
        {
            var repo = new HttpObjectService("unittests", _objectFixture.ClientFactory, _objectFixture.Logger);

            ServiceResult<string> result = repo.AggregateAsync("bookstore", "customer", "[{ $match: { title: /^(the|a)/i } }]").Result;
            var array = JArray.Parse(result.Value);

            Assert.Equal((int)HttpStatusCode.OK, result.Status);
            Assert.Equal(3, array.Count);
        }
    }

    public class ObjectFixture : IDisposable
    {
        public ILogger<HttpObjectService> Logger { get; private set; }
        public HttpClient Client { get; private set; }
        public IHttpClientFactory ClientFactory { get; private set; }

        public ObjectFixture()
        {
            Logger = new Mock<ILogger<HttpObjectService>>().Object;

            var mockHttp = new MockHttpMessageHandler();

            // Setup a respond for the user api (including a wildcard in the URL)
            mockHttp.When("http://localhost/bookstore/customer/1")
                .Respond(HttpStatusCode.OK, "application/json", "{ \"firstName\" : \"John\", \"lastName\" : \"Smith\", \"age\" : 32 }");

            mockHttp.When("http://localhost/bookstore/customer/2")
                .Respond(HttpStatusCode.OK, "application/json", "{ \"firstName\" : \"Mary\", \"lastName\" : \"Jane\", \"age\" : 39 }");
            
            mockHttp.When("http://localhost/bookstore/customer/count")
                .Respond(HttpStatusCode.OK, "application/json", "{ \"count\": 2 }");

            mockHttp.When("http://localhost/bookstore/customer/find?from=0&order=1&size=-1")
                .Respond(HttpStatusCode.OK, "application/json", "[ { \"firstName\": \"John\", \"lastName\": \"Smith\", \"_id\": \"1\", \"age\": 32 }, { \"firstName\": \"Mary\", \"lastName\": \"Jane\", \"_id\": \"2\", \"age\": 39 } ]");

            mockHttp.When("http://localhost/bookstore/customer/3")
                .Respond(HttpStatusCode.OK, "application/json", "{ \"deleted\": 1, \"success\": true }");
            
            mockHttp.When("http://localhost/bookstore/customer/4")
                .Respond(HttpStatusCode.Created, "application/json", "{ \"firstName\" : \"Mary\", \"lastName\" : \"Jane\", \"age\" : 39 }");
            
            mockHttp.When("http://localhost/bookstore/customer/aggregate")
                .Respond(HttpStatusCode.OK, "application/json", "[ { \"_id\": 1, \"title\": \"The Red Badge of Courage\", \"author\": \"Stephen Crane\", \"pages\": 112, \"isbn\": { \"isbn-10\": \"0486264653\", \"isbn-13\": \"978-0486264653\" } }, { \"_id\": 3, \"title\": \"The Secret Garden\", \"author\": \"Frances Hodgson Burnett\", \"pages\": 126, \"isbn\": { \"isbn-10\": \"1514665956\", \"isbn-13\": \"978-1514665954\" } }, { \"_id\": 4, \"title\": \"A Connecticut Yankee in King Arthur's Court\", \"author\": \"Mark Twain\", \"pages\": 116, \"isbn\": { \"isbn-10\": \"1517061385\", \"isbn-13\": \"978-1517061388\" } } ]");
            
            mockHttp.When("http://localhost/multi/bookstore/customer")
                .Respond(HttpStatusCode.Created, "application/json", "{ \"inserted\": 2, \"ids\": [ \"5c1fbe4dbe41760001060b25\", \"5c1fbe4dbe41760001060b26\" ] }");

            // Inject the handler or client into your application code
            var client = mockHttp.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost/");
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
