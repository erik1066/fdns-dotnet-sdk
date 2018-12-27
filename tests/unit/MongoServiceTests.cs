using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Core;
using Mongo2Go;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Foundation.Sdk.Data;

namespace Foundation.Sdk.Tests
{
    public partial class MongoServiceTests : IClassFixture<MongoServiceFixture>
    {
        MongoServiceFixture _fixture;

        public MongoServiceTests(MongoServiceFixture fixture)
        {
            this._fixture = fixture;
        }

        [Theory]
        [InlineData("1", "{ \"title\": \"The Red Badge of Courage\" }", "{ \"_id\" : \"1\", \"title\" : \"The Red Badge of Courage\" }")]
        [InlineData("2", "{ \"title\": \"Don Quixote\" }", "{ \"_id\" : \"2\", \"title\" : \"Don Quixote\" }")]
        [InlineData("3", "{ \"title\": \"A Connecticut Yankee in King Arthur's Court\" }", "{ \"_id\" : \"3\", \"title\" : \"A Connecticut Yankee in King Arthur's Court\" }")]
        public async Task Get_Object_by_Primitive_Id(string id, string insertedJson, string expectedJson)
        {
            // Arrange
            IObjectService service = _fixture.CustomersService;
            var collectionName = "customers";

            // Act
            var insertResult = await service.InsertAsync("bookstore", collectionName, id, insertedJson);
            var getResult = await service.GetAsync("bookstore", collectionName, id);
            
            // Assert
            Assert.Equal(200, getResult.Status);
            Assert.Equal(expectedJson, getResult.Value);
        }

        [Theory]
        [InlineData("X1")]
        [InlineData("X2")]
        [InlineData("XABCD")]
        public async Task Get_Object_fail_Not_Found(string id)
        {
            IObjectService service = _fixture.CustomersService;
            var collectionName = "customers";
            var getResult = await service.GetAsync("bookstore", collectionName, id);
            Assert.Equal(404, getResult.Status);
        }

        [Theory]
        [InlineData("21", "{ \"name\": \"A\" }", "A")]
        [InlineData("22", "{ \"name\": \"AB\", \"fullname\": { first: \"A\", last: \"B\" } }", "AB")]
        [InlineData("23", "{ \"name\": \"A\", status: 5643 }", "A")]
        [InlineData("24", "{ \"name\": \"A\", status: 5643, \"events\": [ 1, 2, 3, 4 ] }", "A")]
        [InlineData("25", "{ \"name\": \"A\", status: 0, \"events\": [ { \"id\": 5 }, { \"id\": 6 } ] }", "A")]
        [InlineData("26", "{ \"name\": 'C' }", "C")]
        [InlineData("27", "{ 'name': 'D' }", "D")]
        [InlineData("28", "{ name: 'E' }", "E")]
        public async Task Insert_with_Primitive_Id(string id, string json, string expectedName)
        {
            // Arrange
            IObjectService service = _fixture.CustomersService;
            var collectionName = "customers";

            // Act
            var result = await service.InsertAsync("bookstore", collectionName, id, json);
            
            JObject jsonObject = JObject.Parse(result.Value);
            
            var insertedId = jsonObject["_id"].ToString();
            var insertedName = jsonObject["name"].ToString();

            // Assert
            Assert.Equal(id, insertedId);
            Assert.Equal(expectedName, insertedName);
            Assert.Equal(201, result.Status);
        }

        [Theory]
        [InlineData("XFS1", "{ \"name\": \"A\" }")]
        [InlineData("XQP2", "{ \"name\": \"A\" }")]
        public async Task Insert_fails_Duplicate_Ids(string id, string json)
        {
            IObjectService service = _fixture.CustomersService;
            var collectionName = "customers";
            var result1 = await service.InsertAsync("bookstore", collectionName, id, json);

            Assert.Equal(201, result1.Status);

            var result2 = await service.InsertAsync("bookstore", collectionName, id, json);
            Assert.Equal(400, result2.Status);
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" ")]
        [InlineData("2", " \"name\": \"A\" }")]
        [InlineData("3", " \"name\": \"A\" ")]
        [InlineData("4", " ")]
        // Disallow inserting an object with malformed Json
        public async Task Insert_fails_Malformed_Json(string id, string json)
        {
            IObjectService service = _fixture.CustomersService;
            var collectionName = "customers";
            var result = await service.InsertAsync("bookstore", collectionName, id, json);
            Assert.Equal(400, result.Status);
        }

        [Theory]
        [InlineData("31", "{ \"name\": \"A\" }", "{ \"name\": \"B\" }", "B")]
        [InlineData("36", "{ \"name\": 'B' }", "{ \"name\": 'C' }", "C")]
        [InlineData("37", "{ 'name': 'C' }", "{ 'name': 'D' }", "D")]
        [InlineData("38", "{ name: 'D' }", "{ 'name': 'E' }", "E")]
        public async Task Replace_with_Primitive_Id(string id, string json1, string json2, string expectedName)
        {
            // Arrange
            IObjectService service = _fixture.CustomersService;
            var collectionName = "customers";

            // Act
            var insertResult = await service.InsertAsync("bookstore", collectionName, id, json1);

            var replaceResult = await service.ReplaceAsync("bookstore", collectionName, id, json2);

            JObject jsonObject = JObject.Parse(replaceResult.Value);
            
            var insertedId = jsonObject["_id"].ToString();
            var insertedName = jsonObject["name"].ToString();

            // Assert
            Assert.Equal(id, insertedId);
            Assert.Equal(expectedName, insertedName);
            Assert.Equal(201, insertResult.Status);
            Assert.Equal(200, replaceResult.Status);
        }

        [Theory]
        [InlineData("R1", "{ \"name\": \"A\" }")]
        [InlineData("R6", "{ \"name\": 'C' }")]
        // The service should reject inserting an object on a replace operation if the object doesn't already exist
        public async Task Upsert_fails_Not_Found(string id, string json)
        {
            IObjectService service = _fixture.CustomersService;
            var collectionName = "customers";
            var result = await service.ReplaceAsync("bookstore", collectionName, id, json);
            Assert.Equal(404, result.Status);
        }

        [Theory]
        [InlineData("41", "{ \"name\": \"A\" ")]
        [InlineData("42", " \"name\": \"A\" }")]
        [InlineData("43", " \"name\": \"A\" ")]
        [InlineData("44", " ")]
        // Disallow updating an object with malformed Json
        public async Task Replace_fails_Malformed_Json(string id, string json)
        {
            IObjectService service = _fixture.CustomersService;
            var collectionName = "customers";
            var result = await service.ReplaceAsync("bookstore", collectionName, id, json);
            Assert.Equal(400, result.Status);
        }

        [Theory]
        [InlineData("51", "{ \"title\": \"The Red Badge of Courage\" }")]
        [InlineData("52", "{ \"title\": \"Don Quixote\" }")]
        public async Task Delete_Object_by_Primitive_Id(string id, string json)
        {
            // Arrange
            IObjectService service = _fixture.CustomersService;

            var collectionName = "customers";

            // Act
            var insertResult = await service.InsertAsync("bookstore", collectionName, id, json);
            var firstGetResult = await service.GetAsync("bookstore", collectionName, id);

            Assert.Equal(200, firstGetResult.Status);

            var deleteResult = await service.DeleteAsync("bookstore", collectionName, id);

            Assert.Equal(200, deleteResult.Status);
            Assert.Equal(1, deleteResult.Value);

            var secondGetResult = await service.GetAsync("bookstore", collectionName, id);

            Assert.True(string.IsNullOrEmpty(secondGetResult.Value));
        }

        [Theory]
        [InlineData("61")]
        [InlineData("62")]
        public async Task Delete_Object_fails_Not_Found(string id)
        {
            IObjectService service = _fixture.CustomersService;
            var collectionName = "customers";

            var deleteResult = await service.DeleteAsync("bookstore", collectionName, id);
            Assert.Equal(404, deleteResult.Status);
            Assert.Equal(0, deleteResult.Value);
        }
        

        [Theory]
        [InlineData("{ pages: 288 }", 0, -1, 2)]
        [InlineData("{ pages: 288 }", 0, 1, 1)]
        [InlineData("{ pages: 288 }", 1, 1, 1)]
        [InlineData("{ pages: 289 }", 0, -1, 0)]
        [InlineData("{ pages: { $lt: 150 } }", 0, -1, 4)]
        [InlineData("{ pages: { $lt: 112 } }", 0, -1, 0)]
        [InlineData("{ pages: { $lte: 112 } }", 0, -1, 2)]
        [InlineData("{ pages: { $gt: 150 } }", 0, -1, 7)]
        [InlineData("{ pages: { $gt: 464 } }", 0, -1, 2)]
        [InlineData("{ pages: { $gte: 464 } }", 0, -1, 3)]
        [InlineData("{ title: /^(the|a)/i }", 0, -1, 6)]
        [InlineData("{ title: /^(the|of)/i }", 0, -1, 6)]
        [InlineData("{ title: /^(g)/i }", 0, -1, 1)]
        [InlineData("{ title: /^(the|of)/i, pages: { $gt: 300 } }", 0, -1, 1)]
        [InlineData("{ title: /^(the|of)/i, pages: { $lt: 500 }, author:'John Steinbeck' }", 0, -1, 2)]
        [InlineData("{ title: /^(the|of)/i, pages: { $lt: 500 }, author:\"John Steinbeck\" }", 0, -1, 2)]
        [InlineData("{ title: /^(the|of)/i, pages: { $lt: 500 }, author: /^(john)/i }", 0, -1, 2)]
        public async Task Find_Objects_in_Collection(string findExpression, int start, int limit, int expectedCount)
        {
            IObjectService service = new MongoService(_fixture.MongoClient, _fixture.Logger);
            var collectionName = "historicalbooks";

            var findResult = await service.FindAsync("bookstore", collectionName, findExpression, start, limit, "title", ListSortDirection.Ascending);
            Assert.Equal(200, findResult.Status);

            SearchResults<string> results = findResult.Value;
            Assert.Equal(expectedCount, results.Count);
        }

        [Theory]
        [InlineData("pages>464", 2)]
        [InlineData("pages>=464", 3)]
        [InlineData("pages<464", 8)]
        [InlineData("pages>=288", 5)]
        [InlineData("pages:288", 2)]
        [InlineData("pages!:288", 9)]
        [InlineData("title:Slaughterhouse-Five", 1)]
        [InlineData("title:\"Slaughterhouse-Five\"", 1)]
        [InlineData("title:\"The Red Badge of Courage\" pages>50", 1)]
        [InlineData("title:\"The Great Gatsby\" pages>250", 0)]
        [InlineData("title:\"The Great Gatsby\" pages<250", 1)]
        [InlineData("title:\"The Great Gatsby\" pages<250 author:\"F. Scott Fitzgerald\"", 1)]
        [InlineData("author:\"John Steinbeck\"", 2)]
        [InlineData("author:\"John Steinbeck\" pages<=464", 2)]
        [InlineData("author:\"John Steinbeck\" pages<464", 1)]
        [InlineData("pages<464 author:\"John Steinbeck\"", 1)]
        [InlineData("author:\"Cervantes\"", 0)]
        [InlineData("author!:\"John Steinbeck\"", 9)]
        public async Task Search_Collection(string qs, int expectedCount)
        {
            IObjectService service = new MongoService(_fixture.MongoClient, _fixture.Logger);
            var collectionName = "historicalbooks";

            var searchResult = await service.SearchAsync("bookstore", collectionName, qs, 0, -1, "title");
            Assert.Equal(200, searchResult.Status);
            Assert.Equal(expectedCount, searchResult.Value.Items.Count);
        }

        [Theory]
        [InlineData("dsfdg", "1", "{ \"title\": \"The Red Badge of Courage\" }")]
        [InlineData("qpdnv", "2", "{ \"title\": \"Don Quixote\" }")]
        public async Task Delete_Collection(string collectionName, string id, string json)
        {
            IObjectService service = new MongoService(_fixture.MongoClient, _fixture.Logger);

            // Try getting items in the collection. Collection doesn't exist yet, should get a 404
            var getFirstCollectionResult = await service.GetAllAsync("bookstore", collectionName);
            Assert.Equal(404, getFirstCollectionResult.Status);

            // Add an item to collection; Mongo auto-creates the collection            
            var insertResult = await service.InsertAsync("bookstore", collectionName, id, json);

            // // Try getting items in collection a 2nd time. Now it should return a 200.
            var getSecondCollectionResult = await service.GetAllAsync("bookstore", collectionName);
            Assert.Equal(200, getSecondCollectionResult.Status);

            // Delete the collection
            var deleteCollectionResult = await service.DeleteCollectionAsync("bookstore", collectionName);
            Assert.Equal(200, deleteCollectionResult.Status);

            // Try getting items in collection a 3rd time. It was just deleted so we should get a 404.
            var getThirdCollectionResult = await service.GetAllAsync("bookstore", collectionName);
            Assert.Equal(404, getThirdCollectionResult.Status);
        }

        [Fact]
        public async Task Get_Collection()
        {
            var collectionName = "orders2";
            IObjectService service = new MongoService(_fixture.MongoClient, _fixture.Logger);

            var items = new List<string>() 
            {
                "{ \"title\": \"The Red Badge of Courage\" }",
                "{ \"title\": \"Don Quixote\" }",
                "{ \"title\": \"The Grapes of Wrath\" }",
                "{ \"title\": \"The Catcher in the Rye\" }",
                "{ \"title\": \"Slaughterhouse-Five\" }",
                "{ \"title\": \"Of Mice and Men\" }",
                "{ \"title\": \"Gone with the Wind\" }",
                "{ \"title\": \"Fahrenheit 451\" }",
                "{ \"title\": \"The Old Man and the Sea\" }",
                "{ \"title\": \"The Great Gatsby\" }"
            };

            var insertedTitles = new Dictionary<string, string>();
            int insertedItemsCount = 0;
            foreach (var item in items)
            {
                var insertResult = await service.InsertAsync("bookstore", collectionName, item);
                if (insertResult.Status == 201)
                {
                    insertedItemsCount++;
                    JObject obj = JObject.Parse(insertResult.Value);
                    var id = obj["_id"].ToString();
                    var title = obj["title"].ToString();
                    insertedTitles.Add(id, title);
                }
                else
                {
                    Assert.True(false); // should not happen!
                }
            }

            Assert.Equal(items.Count, insertedItemsCount); // test that all inserts worked as expected

            // Try getting items in collection
            var getCollectionResult = await service.GetAllAsync("bookstore", collectionName);
            Assert.Equal(200, getCollectionResult.Status);

            Assert.Equal(items.Count, getCollectionResult.Value.Count());

            foreach (var item in getCollectionResult.Value)
            {
                JObject obj = JObject.Parse(item);
                var title = obj["title"].ToString();
                var id = obj["_id"].ToString();
                Assert.NotNull(title);
                Assert.Equal(insertedTitles[id], title);
            }
        }

        [Fact]
        public async Task Insert_Multiple_Objects()
        {
            var collectionName = "orders3";
            IObjectService service = new MongoService(_fixture.MongoClient, _fixture.Logger);

            var items = new List<string>() 
            {
                "{ \"title\": \"The Red Badge of Courage\" }",
                "{ \"title\": \"Don Quixote\" }",
                "{ \"title\": \"The Grapes of Wrath\" }",
                "{ \"title\": \"The Catcher in the Rye\" }",
                "{ \"title\": \"Slaughterhouse-Five\" }",
                "{ \"title\": \"Of Mice and Men\" }",
                "{ \"title\": \"Gone with the Wind\" }",
                "{ \"title\": \"Fahrenheit 451\" }",
                "{ \"title\": \"The Old Man and the Sea\" }",
                "{ \"title\": \"The Great Gatsby\" }"
            };

            var insertManyResult = await service.InsertManyAsync("bookstore", collectionName, items);
            Assert.Equal(201, insertManyResult.Status);
            Assert.Equal(10, insertManyResult.Value.Count());

            var ids = new HashSet<string>();
            foreach (var id in insertManyResult.Value)
            {
                ids.Add(id);
            }

            // Try getting items in collection
            var getCollectionResult = await service.GetAllAsync("bookstore", collectionName);
            Assert.Equal(200, getCollectionResult.Status);
            Assert.Equal(10, getCollectionResult.Value.Count());

            foreach (var iItem in items)
            {
                JObject iObject = JObject.Parse(iItem);
                var iTitle = iObject["title"].ToString();

                bool found = false;
                foreach (var jItem in getCollectionResult.Value)
                {
                    JObject jObject = JObject.Parse(jItem);
                    var jTitle = jObject["title"].ToString();
                    var jId = jObject["_id"]["$oid"].ToString();

                    Assert.NotNull(jTitle);

                    if (iTitle == jTitle && ids.Contains(jId))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.True(found);
            }
        }

        [Fact]
        public async Task Insert_Multiple_Objects_Fail_Malformed_Json()
        {
            var collectionName = "orders4";
            var repo = new MongoService(_fixture.MongoClient, _fixture.Logger);

            var items = new List<string>() 
            {
                "{ \"title\": \"The Red Badge of Courage\" }",
                "{ \"title\": \"Don Quixote\" }",
                "{ \"title\": \"The Grapes of Wrath\" }",
                "{ \"title\": \"The Catcher in the Rye\" }",
                " \"title\": \"Slaughterhouse-Five\" }", // bad!
                "{ \"title\": \"Of Mice and Men\" }",
                "{ \"title\": \"Gone with the Wind\" }",
                "{ \"title\": \"Fahrenheit 451\" }",
                "{ \"title\": \"The Old Man and the Sea\" }",
                "{ \"title\": \"The Great Gatsby\" }"
            };

            try 
            {
                var insertManyResult = await repo.InsertManyAsync("bookstore", collectionName, items);
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                Assert.IsType<System.FormatException>(ex);
            }

            // Try getting items in collection, ensure nothing was inserted
            var getCollectionResult = await repo.GetAllAsync("bookstore", collectionName);
            Assert.Equal(404, getCollectionResult.Status);
        }

        [Theory]
        [InlineData("historicalbooks", "[{ $match: { title: /^(the|a)/i } }]", 6)]
        [InlineData("historicalbooks", "[{ $match: { title: /^(the|a)/i, pages: { $gt: 120 } } }]", 4)]
        [InlineData("historicalbooks", "[{ $match: { title: /^(the|a)/i, pages: { $gt: 120 } } }, { $sort: { pages : -1 } }]", 4)]
        [InlineData("historicalbooks", "[{ $match: { title: /^(the|a)/i, pages: { $gt: 120 } } }, { $sort: { pages : -1 } }, { $limit: 2 }]", 2)]
        [InlineData("historicalbooks", "[{ $match: { title: /^(the|a)/i } }, { $limit: 200 }]", 6)]
        public async Task Aggregate_Match(string collectionName, string aggregateExpression, int expectedCount)
        {
            var repo = new MongoService(_fixture.MongoClient, _fixture.Logger);

            var aggregateResult = await repo.AggregateAsync("bookstore", collectionName, aggregateExpression);
            var array = JArray.Parse(aggregateResult.Value);
            Assert.Equal(200, aggregateResult.Status);            
            Assert.Equal(expectedCount, array.Count);
        }

        [Theory]
        [InlineData("historicalbooks", "[{ $match: { title: /^(the|a)/i } }, { $count: \"numberOfBooks\" }]", "numberOfBooks", 6)]
        public async Task Aggregate_Count(string collectionName, string aggregateExpression, string propertyName, int expectedCount)
        {
            IObjectService service = new MongoService(_fixture.MongoClient, _fixture.Logger);

            var aggregateResult = await service.AggregateAsync("bookstore", collectionName, aggregateExpression);
            Assert.Equal(200, aggregateResult.Status);

            JToken token = JArray.Parse(aggregateResult.Value).FirstOrDefault();
            var countValue = token[propertyName].ToString();

            Assert.Equal(expectedCount, int.Parse(countValue));
        }

        [Theory]
        [InlineData("historicalbooks", "author", "{}", 10)]
        [InlineData("historicalbooks", "title", "{}", 11)]
        [InlineData("historicalbooks", "title", "{ title: /^(the|a)/i }", 6)]
        public async Task Distinct(string collectionName, string fieldName, string findExpression, int expectedCount)
        {
            IObjectService service = new MongoService(_fixture.MongoClient, _fixture.Logger);

            var distinctResult = await service.GetDistinctAsync("bookstore", collectionName, fieldName, findExpression);
            Assert.Equal(200, distinctResult.Status);            
            Assert.Equal(expectedCount, distinctResult.Value.Count);
        }

        [Theory]
        [InlineData("historicalbooks", "{}", 11)]
        [InlineData("historicalbooks", "{ title: /^(the|a)/i }", 6)]
        [InlineData("historicalbooks", "{ title: /^(the)/i }", 5)]
        [InlineData("historicalbooks", "{ title: /^(a)/i }", 1)]
        public async Task Count(string collectionName, string findExpression, int expectedCount)
        {
            IObjectService service = new MongoService(_fixture.MongoClient, _fixture.Logger);

            var distinctResult = await service.CountAsync("bookstore", collectionName, findExpression);
            Assert.Equal(200, distinctResult.Status);            
            Assert.Equal(expectedCount, distinctResult.Value);
        }
    }

    public class MongoServiceFixture : IDisposable
    {
        internal static MongoDbRunner _runner;

        public ILogger<MongoService> Logger { get; private set; }
        public IMongoClient MongoClient { get; set; }
        public IObjectService CustomersService { get; private set; }
        public IObjectService BooksService { get; private set; }
        public List<string> Books = new List<string>();

        public MongoServiceFixture()
        {
            Logger = new Mock<ILogger<MongoService>>().Object;
            _runner = MongoDbRunner.Start();
            MongoClient = new MongoClient(_runner.ConnectionString);

            CustomersService = new MongoService(MongoClient, Logger);
            BooksService = new MongoService(MongoClient, Logger);

            LoadBooks();
        }

        private void LoadBooks()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            var path = Path.Combine(basePath, "Resources", "books");

            DirectoryInfo dir = new DirectoryInfo(path);

            var jsonFiles = dir.GetFiles("*.json");

            foreach (var jsonFile in jsonFiles)
            {
                var json = File.ReadAllText(jsonFile.FullName);
                Books.Add(json);
            }

            IObjectService service = new MongoService(MongoClient, Logger);
            var insertManyResult = service.InsertManyAsync("bookstore", "historicalbooks", Books).Result;

            Assert.Equal(201, insertManyResult.Status);
            Assert.Equal(jsonFiles.Length, insertManyResult.Value.Count());
        }

        public void Dispose()
        {
            _runner.Dispose();
        }
    }
}