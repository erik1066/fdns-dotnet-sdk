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
    /// Class representing a MongoDB repository for arbitrary, untyped Json objects
    /// </summary>
    public sealed class MongoRepository<T> //: IObjectRepository<T>
    {
        private static JsonSerializerSettings _settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        private readonly IMongoClient _client = null;
        private readonly JsonWriterSettings _jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
        private readonly ILogger<MongoRepository<T>> _logger;
        private const string ID_PROPERTY_NAME = "_id";
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly string _databaseName = string.Empty;
        private readonly string _collectionName = string.Empty;
        private const string SERVICE_NAME = "Mongo";
        private readonly bool _isStringType = typeof(T) == typeof(String);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">MongoDB client</param>
        /// <param name="logger">Logger</param>
        public MongoRepository(IMongoClient client, string databaseName, string collectionName, ILogger<MongoRepository<T>> logger)
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
        }

        /// <summary>
        /// Gets a single object
        /// </summary>
        /// <param name="id">The id of the object to get</param>
        /// <returns>The object matching the specified id</returns>
        public async Task<ServiceResult<T>> GetAsync(object id, Dictionary<string, string> headers = null)
        {
            try
            {   
                var sw = new Stopwatch();
                sw.Start();
                
                (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());

                BsonDocument findDocument = isObjectId == true ? new BsonDocument(ID_PROPERTY_NAME, objectId) : new BsonDocument(ID_PROPERTY_NAME, id.ToString());
                var json = StringifyDocument(await _collection.Find(findDocument).FirstOrDefaultAsync());
                T objectValue = GetObjectFromJson(json);

                sw.Stop();

                var result = new ServiceResult<T>(
                    uri: $"{_database}/{_collection}/{id}",
                    elapsed: sw.Elapsed,
                    value: objectValue,
                    serviceName: SERVICE_NAME,
                    code: json == null ? HttpStatusCode.NotFound : HttpStatusCode.OK,
                    correlationId: GetCorrelationId(headers),
                    message: string.Empty);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get failed on {_databaseName}/{_collectionName}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Gets all objects in a collection
        /// </summary>
        /// <returns>All objects in the collection</returns>
        public async Task<ServiceResult<T>> GetAllAsync(Dictionary<string, string> headers = null)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var documents = await _collection.Find(_ => true).ToListAsync();
                var json = StringifyDocuments(documents);
                
                T objectValue = GetObjectFromJson(json);
                sw.Stop();
                var correlationId = GetCorrelationId(headers);

                var result = new ServiceResult<T>($"{_database}/{_collection}", sw.Elapsed, objectValue, SERVICE_NAME, HttpStatusCode.OK, correlationId, string.Empty);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get all failed on {_database}/{_collection}");
                throw;
            }
        }

        /// <summary>
        /// Inserts a single object into the given database and collection
        /// </summary>
        /// <param name="id">The id of the object</param>
        /// <param name="json">The Json that represents the object</param>
        /// <returns>The object that was inserted</returns>
        public async Task<ServiceResult<T>> InsertAsync(object id, T entity, Dictionary<string, string> headers = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var json = SerializeEntity(entity);
                var document = BsonDocument.Parse(json);

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
                result.Code = HttpStatusCode.Created;
                return result;
            }
            catch (System.FormatException)
            {
                return GetBadRequestResult($"{_database}/{_collection}/{id}", sw.Elapsed, GetCorrelationId(headers), "Unable to process this object due to malformed object structure.");
            }
            catch (MongoDB.Driver.MongoWriteException ex) when (ex.Message.Contains("E11000"))
            {
                return GetBadRequestResult($"{_database}/{_collection}/{id}", sw.Elapsed, GetCorrelationId(headers), "Unable to process this object. An object with this key already exists.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Insert failed on {_database}/{_collection}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Updates a single object in the given database and collection
        /// </summary>
        /// <param name="id">The id of the object</param>
        /// <param name="json">The Json that represents the object</param>
        /// <returns>The object that was updated</returns>
        public async Task<ServiceResult<T>> ReplaceAsync(object id, T entity, Dictionary<string, string> headers = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var json = SerializeEntity(entity);
                var document = BsonDocument.Parse(json);

                (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());
                BsonDocument findDocument = isObjectId == true ? new BsonDocument(ID_PROPERTY_NAME, objectId) : new BsonDocument(ID_PROPERTY_NAME, id.ToString());
                var replaceOneResult = await _collection.ReplaceOneAsync(findDocument, document);

                if (replaceOneResult.IsAcknowledged && replaceOneResult.ModifiedCount == 1)
                {
                    return await GetAsync(id, headers);
                }
                else if (replaceOneResult.IsAcknowledged && replaceOneResult.ModifiedCount == 0)
                {
                    return GetNotFoundResult($"{_database}/{_collection}/{id}", sw.Elapsed, GetCorrelationId(headers));
                }
                else
                {
                    throw new InvalidOperationException("The replace operation was not acknowledged by MongoDB");
                }
            }
            catch (System.FormatException)
            {
                return GetBadRequestResult($"{_database}/{_collection}/{id}", sw.Elapsed, GetCorrelationId(headers), "Unable to process this object due to malformed object structure.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Replace failed on {_database}/{_collection}/{id}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a single object in the given database and collection
        /// </summary>
        /// <param name="id">The id of the object</param>
        /// <returns>Whether the deletion was successful</returns>
        public async Task<ServiceResult<int>> DeleteAsync(object id, Dictionary<string, string> headers = null)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var uri = $"{_database}/{_collection}/{id}";

                (var isObjectId, ObjectId objectId) = IsObjectId(id.ToString());
                
                BsonDocument findDocument = isObjectId == true ? new BsonDocument(ID_PROPERTY_NAME, objectId) : new BsonDocument(ID_PROPERTY_NAME, id.ToString());
                var deleteOneResult = await _collection.DeleteOneAsync(findDocument);

                if (deleteOneResult.IsAcknowledged && deleteOneResult.DeletedCount == 1)
                {
                    return new ServiceResult<int>(uri: uri, elapsed: sw.Elapsed, value: (int)deleteOneResult.DeletedCount, serviceName: SERVICE_NAME, code: HttpStatusCode.OK, correlationId: GetCorrelationId(headers));
                }
                else if (deleteOneResult.IsAcknowledged && deleteOneResult.DeletedCount == 0)
                {
                    return new ServiceResult<int>(uri: uri, elapsed: sw.Elapsed, value: 0, serviceName: SERVICE_NAME, code: HttpStatusCode.NotFound, correlationId: GetCorrelationId(headers));
                }
                else
                {
                    throw new InvalidOperationException("The delete operation was not acknowledged by MongoDB");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Delete failed on {_database}/{_collection}/{id}");
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
        /// <returns>A collection of objects that match the find criteria</returns>
        public async Task<ServiceResult<SearchResults<T>>> FindAsync(string findExpression, int start, int size, string sortFieldName, ListSortDirection sortDirection, Dictionary<string, string> headers = null)
        {
            try
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                var regexFind = GetRegularExpressionQuery(findExpression, start, size, sortFieldName, sortDirection);
                var document = await regexFind.ToListAsync();
                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                var json = document.ToJson(jsonWriterSettings);
                SearchResults<T> searchResults = null;

                if (_isStringType)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    List<T> items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(json);
                    searchResults = new SearchResults<T>()
                    {
                        Items = items,
                        Size = size,
                        From = start,
                        Total = items.Count
                    };
                }

                sw.Stop();

                var result = new ServiceResult<SearchResults<T>>($"{_database}/{_collection}/find", sw.Elapsed, searchResults, SERVICE_NAME, HttpStatusCode.OK, GetCorrelationId(headers), string.Empty);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Find failed on {_database}/{_collection} with arguments start={start}, size={size}, sortFieldName={sortFieldName}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a collection
        /// </summary>
        /// <returns>Whether the deletion was successful</returns>
        public async Task<ServiceResult<int>> DeleteCollectionAsync(Dictionary<string, string> headers = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var uri = $"{_database}/{_collection}";

            bool collectionExists = await DoesCollectionExist();

            if (collectionExists) 
            {
                await _database.DropCollectionAsync(_collectionName);
                return new ServiceResult<int>(uri: uri, elapsed: sw.Elapsed, value: 1, serviceName: SERVICE_NAME, code: HttpStatusCode.OK, correlationId: GetCorrelationId(headers));
            }
            else
            {
                _logger.LogWarning($"Delete collection attempted on {_databaseName}/{_collectionName}, but the collection does not exist");
                return new ServiceResult<int>(uri: uri, elapsed: sw.Elapsed, value: 0, serviceName: SERVICE_NAME, code: HttpStatusCode.NotFound, correlationId: GetCorrelationId(headers));
            }
        }

        /// <summary>
        /// Inserts multiple objects and auto-generates their ids
        /// </summary>
        /// <param name="jsonArray">The Json array that contains the objects to be inserted</param>
        /// <returns>List of ids that were generated for the inserted objects</returns>
        public async Task<ServiceResult<string[]>> InsertManyAsync(IEnumerable<T> entities, Dictionary<string, string> headers = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var uri = $"multi/{_database}/{_collection}";

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

            return new ServiceResult<string[]>(uri: uri, elapsed: sw.Elapsed, value: ids.ToArray(), serviceName: SERVICE_NAME, code: HttpStatusCode.OK, correlationId: GetCorrelationId(headers));
        }

        #region Private helper methods

        private async Task<bool> DoesCollectionExist()
        {
            var filter = new BsonDocument("name", _collectionName);
            var collectionCursor = await _database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
            var exists = await collectionCursor.AnyAsync();
            return exists;
        }

        private string GetCorrelationId(Dictionary<string, string> headers = null)
        {
            if (headers != null && headers.Count > 0)
            {
                if (headers.TryGetValue(Common.CORRELATION_ID_HEADER, out string correlationId))
                {
                    return correlationId;
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        private ServiceResult<T> GetNotFoundResult(string url, TimeSpan elapsed, string correlationId, string message = "") => new ServiceResult<T>(
                url,
                elapsed,
                default(T),
                SERVICE_NAME,
                HttpStatusCode.NotFound,
                correlationId,
                !string.IsNullOrEmpty(message) ? message : "Object not found");

        private ServiceResult<T> GetBadRequestResult(string url, TimeSpan elapsed, string correlationId, string message = "") => new ServiceResult<T>(
                url,
                elapsed,
                default(T),
                SERVICE_NAME,
                HttpStatusCode.BadRequest,
                correlationId,
                !string.IsNullOrEmpty(message) ? message : "Invalid inputs");

        private T GetObjectFromJson(string json)
        {
            T objectValue = default(T);

            if (typeof(T) == typeof(String))
            {
                if (!string.IsNullOrEmpty(json))
                {
                    objectValue = (T)(object)json;
                }
                else
                {
                    objectValue = (T)(object)string.Empty;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(json))
                {
                    objectValue = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                }
            }

            return objectValue;
        }

        private IMongoDatabase GetDatabase(string databaseName)
        {
            #region Input Validation
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            #endregion // Input Validation
            return _client.GetDatabase(databaseName);
        }

        private IMongoCollection<BsonDocument> GetCollection(IMongoDatabase database, string collectionName)
        {
            #region Input Validation
            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ArgumentNullException(nameof(collectionName));
            }
            #endregion // Input Validation
            return database.GetCollection<BsonDocument>(collectionName);
        }

        private string StringifyDocument(BsonDocument document)
        {
            if (document == null)
            {
                return null;
            }
            return document.ToJson(_jsonWriterSettings);
        }

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

        private string SerializeEntity(T entity) 
        {
            if (typeof(T) == typeof(String))
            {
                return entity.ToString();
            }  
            else 
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(entity, _settings);
            }
        }

        private string SerializeEntities(IEnumerable<T> entity) => Newtonsoft.Json.JsonConvert.SerializeObject(entity, _settings);

        #endregion // Private helper methods
    }
}

//         // /// <summary>
//         // /// Counts the number of objects that match the specified count criteria
//         // /// </summary>
//         // /// <param name="databaseName">The database name</param>
//         // /// <param name="collectionName">The collection name</param>
//         // /// <param name="findExpression">The MongoDB-style find syntax</param>
//         // /// <returns>Number of matching objects</returns>
//         // public async Task<long> CountAsync(string databaseName, string collectionName, string findExpression)
//         // {
//         //     try
//         //     {
//         //         var regexFind = GetRegularExpressionQuery(databaseName, collectionName, findExpression, 0, Int32.MaxValue, string.Empty, ListSortDirection.Ascending);
//         //         var documentCount = await regexFind.CountDocumentsAsync();
//         //         return documentCount;
//         //     }
//         //     catch (Exception ex)
//         //     {
//         //         _logger.LogError(ex, $"Count failed on {databaseName}/{collectionName}");
//         //         throw;
//         //     }
//         // }

//         // /// <summary>
//         // /// Gets a list of distinct values for a given field
//         // /// </summary>
//         // /// <param name="databaseName">The database name</param>
//         // /// <param name="collectionName">The collection name</param>
//         // /// <param name="fieldName">The field name</param>
//         // /// <param name="findExpression">The MongoDB-style find syntax</param>
//         // /// <returns>List of distinct values</returns>
//         // public async Task<string> GetDistinctAsync(string databaseName, string collectionName, string fieldName, string findExpression)
//         // {
//         //     try
//         //     {
//         //         var database = GetDatabase(databaseName);
//         //         var collection = GetCollection(database, collectionName);

//         //         BsonDocument bsonDocument = BsonDocument.Parse(findExpression);
//         //         FilterDefinition<BsonDocument> filterDefinition = bsonDocument;

//         //         var distinctResults = await collection.DistinctAsync<string>(fieldName, filterDefinition, null);
                
//         //         var items = distinctResults.ToList();
//         //         var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
//         //         var stringifiedDocument = items.ToJson(jsonWriterSettings);

//         //         return stringifiedDocument;
//         //     }
//         //     catch (Exception ex)
//         //     {
//         //         _logger.LogError(ex, $"Distinct failed on {databaseName}/{collectionName}/distinct/{fieldName}");
//         //         throw;
//         //     }
//         // }

//         // /// <summary>
//         // /// Aggregates data via an aggregation pipeline and returns an array of objects
//         // /// </summary>
//         // /// <param name="databaseName">The database name</param>
//         // /// <param name="collectionName">The collection name</param>
//         // /// <param name="aggregationExpression">The MongoDB-style aggregation expression; see https://docs.mongodb.com/manual/aggregation/</param>
//         // /// <returns>List of matching objects</returns>
//         // public async Task<string> AggregateAsync(string databaseName, string collectionName, string aggregationExpression)
//         // {
//         //     var database = GetDatabase(databaseName);
//         //     var collection = GetCollection(database, collectionName);
//         //     var pipeline = new List<BsonDocument>();

//         //     var pipelineOperations = ParseJsonArray(aggregationExpression);
//         //     foreach(var operation in pipelineOperations)
//         //     {
//         //         BsonDocument document = BsonDocument.Parse(operation);
//         //         pipeline.Add(document);
//         //     }

//         //     var result = (await collection.AggregateAsync<BsonDocument> (pipeline)).ToList();

//         //     var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
//         //     var stringifiedDocument = result.ToJson(jsonWriterSettings);

//         //     return stringifiedDocument;
//         // }

//         // /// <summary>
//         // /// Inserts multiple objects and auto-generates their ids
//         // /// </summary>
//         // /// <param name="databaseName">The database name</param>
//         // /// <param name="collectionName">The collection name</param>
//         // /// <param name="jsonArray">The Json array that contains the objects to be inserted</param>
//         // /// <returns>List of ids that were generated for the inserted objects</returns>
//         // public async Task<string[]> InsertManyAsync(string databaseName, string collectionName, string jsonArray)
//         // {
//         //     var database = GetDatabase(databaseName);
//         //     var collection = GetCollection(database, collectionName);

//         //     var documents = new List<BsonDocument>();

//         //     JArray array = JArray.Parse(jsonArray);
//         //     foreach(JObject o in array.Children<JObject>())
//         //     {
//         //         var json = o.ToString();
//         //         BsonDocument document = BsonDocument.Parse(json);
//         //         documents.Add(document);
//         //     }
            
//         //     await collection.InsertManyAsync(documents);

//         //     List<string> ids = new List<string>();
//         //     foreach (var document in documents)
//         //     {
//         //         ids.Add(document.GetValue("_id").ToString());
//         //     }

//         //     return ids.ToArray();
//         // }

//         // /// <summary>
//         // /// Parses a Json array into plain strings
//         // /// </summary>
//         // /// <remarks>
//         // /// This method is necessary because the JArray and other Json.Net APIs will throw exceptions when
//         // /// presented with invalid Json, e.g. MongoDB's find and aggregate syntax. While unfortunate, this
//         // /// method does work around the problem.
//         // /// </remarks>
//         // /// <param name="jsonArray">Json array to parse</param>
//         // /// <returns>List of string</returns>
//         // private List<string> ParseJsonArray(string jsonArray)
//         // {
//         //     string array = jsonArray.Trim();
//         //     var objects = new List<string>();

//         //     if (!array.StartsWith("[") || !array.EndsWith("]"))
//         //     {
//         //         throw new ArgumentException("Json array must start and end with brackets", nameof(jsonArray));
//         //     }

//         //     var preparedArray = array.Substring(array.IndexOf('{')).TrimEnd(']').Trim(' ');

//         //     int level = 0;
//         //     int lastIndex = 0;

//         //     for (int i = 0; i < preparedArray.Length; i++)
//         //     {
//         //         char character = preparedArray[i];

//         //         if (character.Equals('{'))
//         //         {
//         //             level++;
//         //         }
//         //         else if (character.Equals('}'))
//         //         {
//         //             level--;

//         //             if (level == 0)
//         //             {
//         //                 var obj = preparedArray.Substring(lastIndex, i - lastIndex + 1).Trim(',').Trim();
//         //                 objects.Add(obj);
//         //                 lastIndex = i + 1;
//         //             }
//         //         }
//         //     }

//         //     return objects;
//         // }

//         // /// <summary>
//         // /// Returns whether or not the collection exists in the specified 
//         // /// </summary>
//         // /// <param name="databaseName">Name of the database that owns the specified collection</param>
//         // /// <param name="collectionName">Name of the collection to check</param>
//         // /// <returns>bool; whether or not the collection eixsts</returns>
//         // public async Task<bool> DoesCollectionExist(string databaseName, string collectionName)
//         // {
//         //     var database = GetDatabase(databaseName);
//         //     return await DoesCollectionExist(database, collectionName);
//         // }

//         // private async Task<bool> DoesCollectionExist(IMongoDatabase database, string collectionName)
//         // {
//         //     var filter = new BsonDocument("name", collectionName);
//         //     var collectionCursor = await database.ListCollectionsAsync(new ListCollectionsOptions {Filter = filter});
//         //     var exists = await collectionCursor.AnyAsync();
//         //     return exists;
//         // }

//         // /// <summary>
//         // /// Forces an ID property into a JSON object
//         // /// </summary>
//         // /// <param name="id">The ID value to force into the object's 'id' property</param>
//         // /// <param name="json">The Json that should contain the ID key and value</param>
//         // /// <returns>The Json object with an 'id' property and the specified id value</returns>
//         // private string ForceAddIdToJsonObject(object id, string json)
//         // {
//         //     var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
//         //     if (values.ContainsKey(ID_PROPERTY_NAME))
//         //     {
//         //         values[ID_PROPERTY_NAME] = id;
//         //     }
//         //     else
//         //     {
//         //         values.Add(ID_PROPERTY_NAME, id);
//         //     }
//         //     string checkedJson = Newtonsoft.Json.JsonConvert.SerializeObject(values, Formatting.Indented);
//         //     return checkedJson;
//         // }

//         // private IMongoDatabase GetDatabase(string databaseName)
//         // {
//         //     #region Input Validation
//         //     if (string.IsNullOrEmpty(databaseName))
//         //     {
//         //         throw new ArgumentNullException(nameof(databaseName));
//         //     }
//         //     #endregion // Input Validation
//         //     return _client.GetDatabase(databaseName);
//         // }

//         // private IMongoCollection<BsonDocument> GetCollection(IMongoDatabase database, string collectionName)
//         // {
//         //     #region Input Validation
//         //     if (string.IsNullOrEmpty(collectionName))
//         //     {
//         //         throw new ArgumentNullException(nameof(collectionName));
//         //     }
//         //     #endregion // Input Validation
//         //     return database.GetCollection<BsonDocument>(collectionName);
//         // }

//         // private string StringifyDocument(BsonDocument document)
//         // {
//         //     if (document == null)
//         //     {
//         //         return null;
//         //     }
//         //     return document.ToJson(_jsonWriterSettings);
//         // }

//         // private string StringifyDocuments(List<BsonDocument> documents) => documents.ToJson(_jsonWriterSettings);

//         // private (bool, ObjectId) IsObjectId(string id)
//         // {
//         //     bool isObjectId = ObjectId.TryParse(id.ToString(), out ObjectId objectId);
//         //     return (isObjectId, objectId);
//         // }
//     }
// }