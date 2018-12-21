using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class MongoRepositoryTests : IClassFixture<MongoRepositoryFixture>
    {
        MongoRepositoryFixture _fixture;

        public MongoRepositoryTests(MongoRepositoryFixture fixture)
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
            var repo = _fixture.MongoRepository;

            // Act
            var insertResult = await repo.InsertAsync(id, insertedJson);
            var getResult = await repo.GetAsync(id);
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, getResult.Code);
            Assert.Equal(expectedJson, getResult.Value);
        }

        [Theory]
        [InlineData("X1")]
        [InlineData("X2")]
        [InlineData("XABCD")]
        public async Task Get_Object_fail_Not_Found(string id)
        {
            var repo = _fixture.MongoRepository;
            var getResult = await repo.GetAsync(id);
            Assert.Equal(HttpStatusCode.NotFound, getResult.Code);
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
            var repo = _fixture.MongoRepository;

            // Act
            var result = await repo.InsertAsync(id, json);
            
            JObject jsonObject = JObject.Parse(result.Value);
            
            var insertedId = jsonObject["_id"].ToString();
            var insertedName = jsonObject["name"].ToString();

            // Assert
            Assert.Equal(id, insertedId);
            Assert.Equal(expectedName, insertedName);
            Assert.Equal(HttpStatusCode.Created, result.Code);
        }

        [Theory]
        [InlineData("XFS1", "{ \"name\": \"A\" }")]
        [InlineData("XQP2", "{ \"name\": \"A\" }")]
        public async Task Insert_fails_Duplicate_Ids(string id, string json)
        {
            var repo = _fixture.MongoRepository;
            var result1 = await repo.InsertAsync(id, json);

            Assert.Equal(HttpStatusCode.Created, result1.Code);

            var result2 = await repo.InsertAsync(id, json);
            Assert.Equal(HttpStatusCode.BadRequest, result2.Code);
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" ")]
        [InlineData("2", " \"name\": \"A\" }")]
        [InlineData("3", " \"name\": \"A\" ")]
        [InlineData("4", " ")]
        // Disallow inserting an object with malformed Json
        public async Task Insert_fails_Malformed_Json(string id, string json)
        {
            var repo = _fixture.MongoRepository;
            var result = await repo.InsertAsync(id, json);
            Assert.Equal(HttpStatusCode.BadRequest, result.Code);
        }

        [Theory]
        [InlineData("31", "{ \"name\": \"A\" }", "{ \"name\": \"B\" }", "B")]
        [InlineData("36", "{ \"name\": 'B' }", "{ \"name\": 'C' }", "C")]
        [InlineData("37", "{ 'name': 'C' }", "{ 'name': 'D' }", "D")]
        [InlineData("38", "{ name: 'D' }", "{ 'name': 'E' }", "E")]
        public async Task Replace_with_Primitive_Id(string id, string json1, string json2, string expectedName)
        {
            // Arrange
            var repo = _fixture.MongoRepository;

            // Act
            var insertResult = await repo.InsertAsync(id, json1);

            var replaceResult = await repo.ReplaceAsync(id, json2);

            JObject jsonObject = JObject.Parse(replaceResult.Value);
            
            var insertedId = jsonObject["_id"].ToString();
            var insertedName = jsonObject["name"].ToString();

            // Assert
            Assert.Equal(id, insertedId);
            Assert.Equal(expectedName, insertedName);
            Assert.Equal(HttpStatusCode.Created, insertResult.Code);
            Assert.Equal(HttpStatusCode.OK, replaceResult.Code);
        }

        [Theory]
        [InlineData("1", "{ \"name\": \"A\" }")]
        [InlineData("6", "{ \"name\": 'C' }")]
        // The service should reject inserting an object on a replace operation if the object doesn't already exist
        public async Task Upsert_fails_Not_Found(string id, string json)
        {
            var repo = _fixture.MongoRepository;
            var result = await repo.ReplaceAsync(id, json);
            Assert.Equal(HttpStatusCode.NotFound, result.Code);
        }

        [Theory]
        [InlineData("41", "{ \"name\": \"A\" ")]
        [InlineData("42", " \"name\": \"A\" }")]
        [InlineData("43", " \"name\": \"A\" ")]
        [InlineData("44", " ")]
        // Disallow updating an object with malformed Json
        public async Task Replace_fails_Malformed_Json(string id, string json)
        {
            var repo = _fixture.MongoRepository;
            var result = await repo.ReplaceAsync(id, json);
            Assert.Equal(HttpStatusCode.BadRequest, result.Code);
        }

        [Theory]
        [InlineData("51", "{ \"title\": \"The Red Badge of Courage\" }")]
        [InlineData("52", "{ \"title\": \"Don Quixote\" }")]
        public async Task Delete_Object_by_Primitive_Id(string id, string json)
        {
            // Arrange
            var repo = _fixture.MongoRepository;

            // Act
            var insertResult = await repo.InsertAsync(id, json);
            var firstGetResult = await repo.GetAsync(id);

            Assert.Equal(HttpStatusCode.OK, firstGetResult.Code);

            var deleteResult = await repo.DeleteAsync(id);

            Assert.Equal(HttpStatusCode.OK, deleteResult.Code);
            Assert.Equal(1, deleteResult.Value);

            var secondGetResult = await repo.GetAsync(id);

            Assert.True(string.IsNullOrEmpty(secondGetResult.Value));
        }

        [Theory]
        [InlineData("61")]
        [InlineData("62")]
        public async Task Delete_Object_fails_Not_Found(string id)
        {
            var repo = _fixture.MongoRepository;
            var deleteResult = await repo.DeleteAsync(id);
            Assert.Equal(HttpStatusCode.NotFound, deleteResult.Code);
            Assert.Equal(0, deleteResult.Value);
        }
        

        // [Theory]
        // [InlineData("books201", "{ pages: 288 }", 0, -1, 2)]
        // [InlineData("books202", "{ pages: 288 }", 0, 1, 1)]
        // [InlineData("books203", "{ pages: 288 }", 1, 1, 1)]
        // [InlineData("books204", "{ pages: 289 }", 0, -1, 0)]
        // [InlineData("books205", "{ pages: { $lt: 150 } }", 0, -1, 3)]
        // [InlineData("books206", "{ pages: { $lt: 112 } }", 0, -1, 0)]
        // [InlineData("books207", "{ pages: { $lte: 112 } }", 0, -1, 2)]
        // [InlineData("books208", "{ pages: { $gt: 150 } }", 0, -1, 7)]
        // [InlineData("books209", "{ pages: { $gt: 464 } }", 0, -1, 2)]
        // [InlineData("books210", "{ pages: { $gte: 464 } }", 0, -1, 3)]
        // [InlineData("books211", "{ title: /^(the|a)/i }", 0, -1, 5)]
        // [InlineData("books212", "{ title: /^(the|of)/i }", 0, -1, 6)]
        // [InlineData("books213", "{ title: /^(g)/i }", 0, -1, 1)]
        // [InlineData("books214", "{ title: /^(the|of)/i, pages: { $gt: 300 } }", 0, -1, 1)]
        // [InlineData("books215", "{ title: /^(the|of)/i, pages: { $lt: 500 }, author:'John Steinbeck' }", 0, -1, 2)]
        // [InlineData("books216", "{ title: /^(the|of)/i, pages: { $lt: 500 }, author:\"John Steinbeck\" }", 0, -1, 2)]
        // [InlineData("books217", "{ title: /^(the|of)/i, pages: { $lt: 500 }, author: /^(john)/i }", 0, -1, 2)]
        // public async Task Find_Objects_in_Collection(string collectionName, string findExpression, int start, int limit, int expectedCount)
        // {
        //     var repo = _fixture.MongoRepository;

        //     var items = new List<string>() 
        //     {
        //         "{ 'title': 'The Red Badge of Courage', 'author': 'Stephen Crane', 'pages': 112, 'isbn': { 'isbn-10' : '0486264653', 'isbn-13' : '978-0486264653' } }",
        //         "{ 'title': 'Don Quixote', 'author': 'Miguel De Cervantes', 'pages': 992, 'isbn': { 'isbn-10' : '0060934344', 'isbn-13' : '978-0060934347' } }",
        //         "{ 'title': 'The Grapes of Wrath', 'author': 'John Steinbeck', 'pages': 464, 'isbn': { 'isbn-10' : '0143039431', 'isbn-13' : '978-0143039433' } }",
        //         "{ 'title': 'The Catcher in the Rye', 'author': 'J. D. Salinger', 'pages': 288, 'isbn': { 'isbn-10' : '9780316769174', 'isbn-13' : '978-0316769174' } }",
        //         "{ 'title': 'Slaughterhouse-Five', 'author': 'Kurt Vonnegut', 'pages': 288, 'isbn': { 'isbn-10' : '0812988523', 'isbn-13' : '978-0812988529' } }",
        //         "{ 'title': 'Of Mice and Men', 'author': 'John Steinbeck', 'pages': 112, 'isbn': { 'isbn-10' : '0140177396', 'isbn-13' : '978-0140177398' } }",
        //         "{ 'title': 'Gone with the Wind', 'author': 'Margaret Mitchell', 'pages': 960, 'isbn': { 'isbn-10' : '1451635621', 'isbn-13' : '978-1451635621' } }",
        //         "{ 'title': 'Fahrenheit 451', 'author': 'Ray Bradbury', 'pages': 249, 'isbn': { 'isbn-10' : '9781451673319', 'isbn-13' : '978-1451673319' } }",
        //         "{ 'title': 'The Old Man and the Sea', 'author': 'Ernest Hemingway', 'pages': 128, 'isbn': { 'isbn-10' : '0684801221', 'isbn-13' : '978-0684801223' } }",
        //         "{ 'title': 'The Great Gatsby', 'author': 'F. Scott Fitzgerald', 'pages': 180, 'isbn': { 'isbn-10' : '9780743273565', 'isbn-13' : '978-0743273565' } }",
        //     };

        //     // var payload = "[" + string.Join(',', items) + "]";
        //     var insertManyResult = await repo.InsertManyAsync(items);
        //     Assert.Equal(HttpStatusCode.OK, insertManyResult.Code);

        //     var findResult = await repo.FindAsync(findExpression, start, limit, "title", ListSortDirection.Ascending);
        //     Assert.Equal(HttpStatusCode.OK, findResult.Code);

        //     var array = JArray.Parse(findResult.Value.ToString());
        //     Assert.Equal(expectedCount, array.Count);

        //     // Delete the collection
        //     var deleteCollectionResult = await repo.DeleteCollectionAsync();
        //     Assert.Equal(HttpStatusCode.OK, deleteCollectionResult.Code);
        // }

    }

    public class MongoRepositoryFixture : IDisposable
    {
        internal static MongoDbRunner _runner;

        public ILogger<MongoRepository<string>> Logger { get; private set; }
        private IMongoClient MongoClient { get; set; }
        public MongoRepository<string> MongoRepository { get; private set; }

        public MongoRepositoryFixture()
        {
            Logger = new Mock<ILogger<MongoRepository<string>>>().Object;
            _runner = MongoDbRunner.Start();
            MongoClient = new MongoClient(_runner.ConnectionString);

            MongoRepository = new MongoRepository<string>(MongoClient, "bookstore", "customer", Logger);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }
    }
}