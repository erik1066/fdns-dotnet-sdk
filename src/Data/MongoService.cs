using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using MongoDB.Driver.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Foundation.Sdk.Data
{
    /// <summary>
    /// Class representing a MongoDB service for arbitrary, untyped Json objects
    /// </summary>
    public sealed class MongoService : IObjectService
    {
        private readonly IMongoClient _client = null;
        private static readonly JsonSerializerSettings _jsonSerializersettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        private static readonly JsonWriterSettings _jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
        private readonly ILogger<MongoService> _logger;
        private const string ID_PROPERTY_NAME = "_id";
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly string _databaseName = string.Empty;
        private readonly string _collectionName = string.Empty;
        private readonly string _serviceName = string.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">MongoDB client</param>
        /// <param name="databaseName">Name of the database to use</param>
        /// <param name="collectionName">Name of the collection to use</param>
        /// <param name="logger">Logger</param>
        public MongoService(IMongoClient client, string databaseName, string collectionName, ILogger<MongoService> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
            _logger = logger;
            _databaseName = databaseName;
            _collectionName = collectionName;
            _database = GetDatabase(_databaseName);
            _collection = GetCollection(_database, _collectionName);
            _serviceName = this.GetType().Name;
        }

        /// <summary>
        /// Gets a single object
        /// </summary>
        /// <param name="id">The id of the object to get</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>The object matching the specified id</returns>
        public async Task<ServiceResult<string>> GetAsync(object id, Dictionary<string, string> headers = null)
        {
            try
            {   
                (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());
                BsonDocument findDocument = isObjectId == true ? new BsonDocument(ID_PROPERTY_NAME, objectId) : new BsonDocument(ID_PROPERTY_NAME, id.ToString());
                var json = StringifyDocument(await _collection.Find(findDocument).FirstOrDefaultAsync());

                var result = new ServiceResult<string>(value: json, status: json == null ? 404 : 200, correlationId: Common.GetCorrelationIdFromHeaders(headers), servicename: _serviceName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_serviceName}: Get failed on {_databaseName}/{_collectionName}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Gets all objects in a collection
        /// </summary>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>All objects in the collection</returns>
        public async Task<ServiceResult<IEnumerable<string>>> GetAllAsync(Dictionary<string, string> headers = null)
        {
            try
            {
                if (await DoesCollectionExist() == false) 
                {
                    _logger.LogInformation($"{_serviceName}: Get all failed on {_database}/{_collection}: The collection does not exist");
                    var notFoundResult = GetNotFoundResult(correlationId: Common.GetCorrelationIdFromHeaders(headers), message: $"Collection '{_collectionName}' does not exist in database '{_databaseName}'");
                    var copiedResult = ServiceResult<IEnumerable<string>>.CreateNewUsingDetailsFrom<string>(null, notFoundResult);
                    return copiedResult;
                }

                var documents = await _collection.Find(_ => true).ToListAsync();
                var items = new List<string>();
                foreach (var document in documents)
                {
                    items.Add(document.ToJson(_jsonWriterSettings));
                }

                var result = new ServiceResult<IEnumerable<string>>(value: items, status: 200, correlationId: Common.GetCorrelationIdFromHeaders(headers), servicename: _serviceName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_serviceName}: Get all failed on {_database}/{_collection}");
                throw;
            }
        }

        /// <summary>
        /// Inserts a single object into the given database and collection. An ID is auto-generated for the object.
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>The object that was inserted</returns>
        public async Task<ServiceResult<string>> InsertAsync(string entity, Dictionary<string, string> headers = null) => await InsertAsync(id: null, entity: entity, headers: headers);

        /// <summary>
        /// Inserts a single object into the given database and collection
        /// </summary>
        /// <param name="id">The id of the object</param>
        /// <param name="entity">The entity to insert</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>The object that was inserted</returns>
        public async Task<ServiceResult<string>> InsertAsync(object id, string entity, Dictionary<string, string> headers = null)
        {
            if (id is ObjectId && (ObjectId)id == ObjectId.Empty)
            {
                id = null;
            }

            try
            {
                var document = BsonDocument.Parse(entity);

                if (document.Contains("_id") && document["_id"].GetType() != typeof(BsonObjectId) && document["_id"].GetType() != typeof(BsonString))
                {
                    return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process this object due to invalid _id format.");
                }

                if (id != null)
                {
                    (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());                    
                    if (isObjectId)
                    {
                        document.Set("_id", objectId);
                    }
                    else
                    {
                        document.Set("_id", id.ToString());
                    }
                }
                
                await _collection.InsertOneAsync(document);
                id = document.GetValue("_id");
                var result = await GetAsync(id, headers);
                if (result.IsSuccess)
                {
                    result.Status = (int)HttpStatusCode.Created;
                }
                return result;
            }
            catch (System.FormatException)
            {
                return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process this object due to malformed object structure.");
            }
            catch (MongoDB.Driver.MongoWriteException ex) when (ex.Message.Contains("E11000"))
            {
                return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process this object. An object with this key already exists.");
            }
            catch (MongoDB.Bson.BsonSerializationException)
            {
                return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process this object due to malformed object structure or invalid Json.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_serviceName}: Insert failed on {_database}/{_collection}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Updates a single object in the given database and collection
        /// </summary>
        /// <param name="id">The id of the object</param>
        /// <param name="entity">The entity</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>The object that was updated</returns>
        public async Task<ServiceResult<string>> ReplaceAsync(object id, string entity, Dictionary<string, string> headers = null)
        {
            try
            {
                var document = BsonDocument.Parse(entity);
                (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());
                BsonDocument findDocument = isObjectId == true ? new BsonDocument(ID_PROPERTY_NAME, objectId) : new BsonDocument(ID_PROPERTY_NAME, id.ToString());
                var replaceOneResult = await _collection.ReplaceOneAsync(findDocument, document);

                if (replaceOneResult.IsAcknowledged && replaceOneResult.ModifiedCount == 1)
                {
                    return await GetAsync(id, headers);
                }
                else if (replaceOneResult.IsAcknowledged && replaceOneResult.ModifiedCount == 0)
                {
                    return GetNotFoundResult(Common.GetCorrelationIdFromHeaders(headers));
                }
                else
                {
                    throw new InvalidOperationException("The replace operation was not acknowledged by MongoDB");
                }
            }
            catch (System.FormatException)
            {
                return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process this object due to malformed object structure.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_serviceName}: Replace failed on {_database}/{_collection}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a single object in the given database and collection
        /// </summary>
        /// <param name="id">The id of the object</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>Whether the deletion was successful</returns>
        public async Task<ServiceResult<int>> DeleteAsync(object id, Dictionary<string, string> headers = null)
        {
            try
            {
                (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());                
                BsonDocument findDocument = isObjectId == true ? new BsonDocument(ID_PROPERTY_NAME, objectId) : new BsonDocument(ID_PROPERTY_NAME, id.ToString());
                var deleteOneResult = await _collection.DeleteOneAsync(findDocument);

                if (deleteOneResult.IsAcknowledged && deleteOneResult.DeletedCount == 1)
                {
                    return new ServiceResult<int>(value: (int)deleteOneResult.DeletedCount, status: 200, correlationId: Common.GetCorrelationIdFromHeaders(headers));
                }
                else if (deleteOneResult.IsAcknowledged && deleteOneResult.DeletedCount == 0)
                {
                    return new ServiceResult<int>(value: 0, status: 404, correlationId: Common.GetCorrelationIdFromHeaders(headers));
                }
                else
                {
                    throw new InvalidOperationException("The delete operation was not acknowledged by MongoDB");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_serviceName}: Delete failed on {_database}/{_collection}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Finds a set of objects that match the specified find criteria
        /// </summary>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <param name="start">The index within the find results at which to start filtering</param>
        /// <param name="size">The number of items within the find results to limit the result set to</param>
        /// <param name="sortFieldName">The Json property name of the object on which to sort</param>
        /// <param name="sortDirection">The sort direction</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>A collection of objects that match the find criteria</returns>
        public async Task<ServiceResult<SearchResults<string>>> FindAsync(string findExpression, int start, int limit, string sortFieldName, ListSortDirection sortDirection = ListSortDirection.Descending, Dictionary<string, string> headers = null)
        {
            try
            {
                var regexFind = GetRegularExpressionQuery(findExpression, start, limit, sortFieldName, sortDirection);
                var document = await regexFind.ToListAsync();
                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict, Indent = false, NewLineChars = string.Empty };
                var json = document.ToJson(jsonWriterSettings);
                var items = new List<string>();

                JArray array = JArray.Parse(json);
                foreach (var jObject in array)
                {
                    var item = jObject.ToString();
                    items.Add(item);
                }

                SearchResults<string> searchResults = new SearchResults<string>()
                {
                    Items = items,
                    From = start,
                    Total = items.Count
                };

                var result = new ServiceResult<SearchResults<string>>(value: searchResults, status: 200, correlationId: Common.GetCorrelationIdFromHeaders(headers), servicename: _serviceName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_serviceName}: Find failed on {_database}/{_collection} with arguments start={start}, limit={limit}, sortFieldName={sortFieldName}");
                throw;
            }
        }


        /// <summary>
        /// Searches for a set of objects that match the specified query syntax
        /// </summary>
        /// <param name="searchExpression">The Google-like query syntax</param>
        /// <param name="start">The index within the find results at which to start filtering</param>
        /// <param name="size">The number of items within the find results to limit the result set to</param>
        /// <param name="sortFieldName">The Json property name of the object on which to sort</param>
        /// <param name="sortDirection">The sort direction</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>A collection of objects that match the search criteria</returns>
        public async Task<ServiceResult<SearchResults<string>>> SearchAsync(string searchExpression, int start, int limit, string sortFieldName, ListSortDirection sortDirection = ListSortDirection.Descending, Dictionary<string, string> headers = null)
        {
            string convertedExpression = SearchStringConverter.BuildQuery(searchExpression);
            return await FindAsync(findExpression: convertedExpression, start: start, limit: limit, sortFieldName: sortFieldName, sortDirection: sortDirection, headers: headers);
        }

        /// <summary>
        /// Deletes a collection
        /// </summary>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>Whether the deletion was successful</returns>
        public async Task<ServiceResult<int>> DeleteCollectionAsync(Dictionary<string, string> headers = null)
        {
            bool collectionExists = await DoesCollectionExist();

            if (collectionExists) 
            {
                await _database.DropCollectionAsync(_collectionName);
                return new ServiceResult<int>(value: 1, status: 200, correlationId: Common.GetCorrelationIdFromHeaders(headers), servicename: _serviceName);
            }
            else
            {
                return new ServiceResult<int>(value: 0, status: 404, correlationId: Common.GetCorrelationIdFromHeaders(headers), servicename: _serviceName);
            }
        }

        /// <summary>
        /// Inserts multiple objects and auto-generates their ids
        /// </summary>
        /// <param name="entities">The entities to be inserted</param>
        /// <returns>List of ids that were generated for the inserted objects</returns>
        public async Task<ServiceResult<IEnumerable<string>>> InsertManyAsync(IEnumerable<string> entities, Dictionary<string, string> headers = null)
        {
            var jsonArray = SerializeEntities(entities);
            var documents = new List<BsonDocument>();

            JArray array = JArray.Parse(jsonArray);
            foreach(JToken o in array.Children<JToken>())
            {
                var json = o.ToString();
                BsonDocument document = BsonDocument.Parse(json);
                documents.Add(document);
            }
            
            await _collection.InsertManyAsync(documents);

            List<string> ids = new List<string>();
            foreach (var document in documents)
            {
                ids.Add(document.GetValue("_id").ToString());
            }

            return new ServiceResult<IEnumerable<string>>(value: ids, status: (int)HttpStatusCode.Created, correlationId: Common.GetCorrelationIdFromHeaders(headers));
        }

        /// <summary>
        /// Counts the number of objects that match the specified count criteria
        /// </summary>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>Number of matching objects</returns>
        public async Task<ServiceResult<long>> CountAsync(string findExpression, Dictionary<string, string> headers = null)
        {
            try
            {
                var regexFind = GetRegularExpressionQuery(findExpression, 0, Int32.MaxValue, string.Empty, ListSortDirection.Ascending);
                var documentCount = await regexFind.CountDocumentsAsync();
                return new ServiceResult<long>(value: documentCount, status: 200, correlationId: Common.GetCorrelationIdFromHeaders(headers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_serviceName}: Count failed on {_databaseName}/{_collectionName}");
                throw;
            }
        }

        /// <summary>
        /// Gets a list of distinct values for a given field
        /// </summary>
        /// <param name="fieldName">The field name</param>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>List of distinct values</returns>
        public async Task<ServiceResult<List<string>>> GetDistinctAsync(string fieldName, string findExpression, Dictionary<string, string> headers = null)
        {
            try
            {
                BsonDocument bsonDocument = BsonDocument.Parse(findExpression);
                FilterDefinition<BsonDocument> filterDefinition = bsonDocument;

                var distinctResults = await _collection.DistinctAsync<string>(fieldName, filterDefinition, null);
                
                var items = distinctResults.ToList();
                return new ServiceResult<List<string>>(value: items, status: 200, correlationId: Common.GetCorrelationIdFromHeaders(headers), servicename: _serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_serviceName}: Distinct failed on {_databaseName}/{_collectionName}/distinct/{fieldName}");
                throw;
            }
        }

        /// <summary>
        /// Aggregates data via an aggregation pipeline and returns an array of objects
        /// </summary>
        /// <param name="aggregationExpression">The MongoDB-style aggregation expression; see https://docs.mongodb.com/manual/aggregation/</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>List of matching and/or transformed objects</returns>
        public async Task<ServiceResult<string>> AggregateAsync(string aggregationExpression, Dictionary<string, string> headers = null)
        {
            var pipeline = new List<BsonDocument>();

            var pipelineOperations = ParseJsonArray(aggregationExpression);
            foreach(var operation in pipelineOperations)
            {
                BsonDocument document = BsonDocument.Parse(operation);
                pipeline.Add(document);
            }

            var result = (await _collection.AggregateAsync<BsonDocument> (pipeline)).ToList();
            var stringifiedDocument = result.ToJson(_jsonWriterSettings);
            return new ServiceResult<string>(value: stringifiedDocument, status: 200, correlationId: Common.GetCorrelationIdFromHeaders(headers), servicename: _serviceName);
        }

        #region Private helper methods

        /// <summary>
        /// Parses a Json array into plain strings
        /// </summary>
        /// <remarks>
        /// This method is necessary because the JArray and other Json.Net APIs will throw exceptions when
        /// presented with invalid Json, e.g. MongoDB's find and aggregate syntax. While unfortunate, this
        /// method does work around the problem.
        /// </remarks>
        /// <param name="jsonArray">Json array to parse</param>
        /// <returns>List of string</returns>
        private List<string> ParseJsonArray(string jsonArray)
        {
            string array = jsonArray.Trim();
            var objects = new List<string>();

            if (!array.StartsWith("[") || !array.EndsWith("]"))
            {
                throw new ArgumentException("Json array must start and end with brackets", nameof(jsonArray));
            }

            var preparedArray = array.Substring(array.IndexOf('{')).TrimEnd(']').Trim(' ');

            int level = 0;
            int lastIndex = 0;

            for (int i = 0; i < preparedArray.Length; i++)
            {
                char character = preparedArray[i];

                if (character.Equals('{'))
                {
                    level++;
                }
                else if (character.Equals('}'))
                {
                    level--;

                    if (level == 0)
                    {
                        var obj = preparedArray.Substring(lastIndex, i - lastIndex + 1).Trim(',').Trim();
                        objects.Add(obj);
                        lastIndex = i + 1;
                    }
                }
            }

            return objects;
        }

        private async Task<bool> DoesCollectionExist()
        {
            var filter = new BsonDocument("name", _collectionName);
            var collectionCursor = await _database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
            var exists = await collectionCursor.AnyAsync();
            return exists;
        }

        private ServiceResult<string> GetNotFoundResult(string correlationId, string message = "") => new ServiceResult<string>(
                value: string.Empty,
                status: (int)HttpStatusCode.NotFound,
                correlationId: correlationId,
                servicename: _serviceName,
                message: !string.IsNullOrEmpty(message) ? message : "Object not found");

        private ServiceResult<string> GetBadRequestResult(string correlationId, string message = "") => new ServiceResult<string>(
                value: string.Empty,
                status: (int)HttpStatusCode.BadRequest,
                correlationId: correlationId,
                servicename: _serviceName,
                message: !string.IsNullOrEmpty(message) ? message : "Invalid inputs");


        private IMongoDatabase GetDatabase(string databaseName) => _client.GetDatabase(databaseName);

        private IMongoCollection<BsonDocument> GetCollection(IMongoDatabase database, string collectionName) => database.GetCollection<BsonDocument>(collectionName);

        private string StringifyDocument(BsonDocument document) => document == null ? null : document.ToJson(_jsonWriterSettings);

        private string StringifyDocuments(List<BsonDocument> documents) => documents.ToJson(_jsonWriterSettings);

        private (bool, ObjectId) IsObjectId(string id)
        {
            bool isObjectId = ObjectId.TryParse(id.ToString(), out ObjectId objectId);
            return (isObjectId, objectId);
        }

        private IFindFluent<BsonDocument, BsonDocument> GetRegularExpressionQuery(string findExpression, int start, int size, string sortFieldName, ListSortDirection sortDirection)
        {
            if (size <= -1)
            {
                size = Int32.MaxValue;
            }

            BsonDocument bsonDocument = BsonDocument.Parse(findExpression);
            var regexFind = _collection
                .Find(bsonDocument)
                .Skip(start)
                .Limit(size);

            if (!string.IsNullOrEmpty(sortFieldName))
            {
                if (sortDirection == ListSortDirection.Ascending)
                {
                    regexFind.SortBy(bson => bson[sortFieldName]);
                }
                else
                {
                    regexFind.SortByDescending(bson => bson[sortFieldName]);
                }
            }

            return regexFind;
        }

        private string SerializeEntity(string entity) => entity.ToString();

        private string SerializeEntities(IEnumerable<string> entity) => Newtonsoft.Json.JsonConvert.SerializeObject(entity, _jsonSerializersettings);

        #endregion // Private helper methods
    }
}