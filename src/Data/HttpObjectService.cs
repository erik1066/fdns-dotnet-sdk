using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using MongoDB.Bson;

namespace Foundation.Sdk.Data
{
    /// <summary>
    /// Class for interacting with the FDNS Object microservice (see https://github.com/CDCgov/fdns-ms-object) over HTTP using strongly-typed objects
    /// </summary>
    public sealed class HttpObjectService : IObjectService
    {
        #region Members
        private readonly Regex _regexHostName = new Regex(@"^[a-zA-Z0-9:\.\-/]*$");
        private readonly Regex _regexCollectionName = new Regex(@"^[a-zA-Z0-9\.]*$");
        private readonly HttpClient _client = null;
        private readonly ILogger<HttpObjectService> _logger;
        private const string ID_PROPERTY_NAME = "_id";
        private readonly string _databaseName = string.Empty;
        private readonly string _collectionName = string.Empty;

        private string SendingServiceName { get; } = string.Empty;
        private JsonSerializerSettings JsonSerializerSettings { get; }
        #endregion // Members

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientFactory">The Http client factory to use for creating Http clients</param>
        /// <param name="logger">The logger to use</param>
        /// <param name="appName">Name of the service that is using this class to make requests to the Http Object service.</param>
        /// <param name="databaseName">Name of the database</param>
        /// <param name="collectionName">Name of the collection within the database</param>
        public HttpObjectService(string appName, string databaseName, string collectionName, IHttpClientFactory clientFactory, ILogger<HttpObjectService> logger)
        {
            #region Input Validation
            if (clientFactory == null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (appName == null)
            {
                throw new ArgumentNullException(nameof(appName));
            }
            if (databaseName == null)
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            if (collectionName == null)
            {
                throw new ArgumentNullException(nameof(collectionName));
            }
            if (!string.IsNullOrEmpty(databaseName) && !_regexCollectionName.IsMatch(databaseName))
            {
                throw new ArgumentException(nameof(databaseName));
            }
            if (!string.IsNullOrEmpty(collectionName) && !_regexCollectionName.IsMatch(collectionName))
            {
                throw new ArgumentException(nameof(collectionName));
            }
            #endregion // Input Validation

            _client = clientFactory.CreateClient($"{appName}-{Common.OBJECT_SERVICE_NAME}");
            _logger = logger;
            _databaseName = databaseName;
            _collectionName = collectionName;
            SendingServiceName = appName;
            JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientFactory">The Http client factory to use for creating Http clients</param>
        /// <param name="logger">The logger to use</param>
        /// <param name="appName">Name of the service that is using this class to make requests to the Http Object service.</param>
        /// <param name="routePrefix">Optional route parts to use</param>
        /// <param name="jsonSerializerSettings">Customer Json serializer</param>
        public HttpObjectService(string appName, string databaseName, string collectionName, IHttpClientFactory clientFactory, ILogger<HttpObjectService> logger, JsonSerializerSettings jsonSerializerSettings) 
            : this(appName, databaseName, collectionName, clientFactory, logger)
        {
            JsonSerializerSettings = jsonSerializerSettings;
        }

        /// <summary>
        /// Retrieves an object by id
        /// </summary>
        /// <param name="id">The id of the object to retrieve. This parameter must match a property on the object with a key of "id" (all lowercase).</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of T</returns>
        public async Task<ServiceResult<string>> GetAsync(object id, Dictionary<string, string> headers = null)
        {
            var url = GetStandardItemUrl(id.ToString());

            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        public async Task<ServiceResult<IEnumerable<string>>> GetAllAsync(Dictionary<string, string> headers = null)
        {
            var url = GetStandardCollectionUrl();
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<IEnumerable<string>> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<IEnumerable<string>>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get all completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get all failed on {_client.BaseAddress}{url}");
                throw ex;
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
            #region Input Validation
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (limit < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }
            if (!_regexCollectionName.IsMatch(sortFieldName))
            {
                throw new ArgumentException(nameof(sortFieldName));
            }
            #endregion // Input Validation

            int sort = sortDirection == ListSortDirection.Descending ? 1 : -1;
            var url = $"{GetStandardCollectionUrl()}/find?from={start}&size={limit}&sort={sortFieldName}&order={sort}";

            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<SearchResults<string>> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_TEXT_PLAIN, headers, findExpression);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultOfSearchResultsAsync(response, Common.OBJECT_SERVICE_NAME, url, headers, start);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Find completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Find failed on {_client.BaseAddress}{url}");
                throw ex;
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
        /// Carries out a wholesale replacement of the object with the specified id
        /// </summary>
        /// <param name="id">The id of the object. This parameter must match a property on the object with a key of "id" (all lowercase).</param>
        /// <param name="entity">The entity that will replace the object with the specified id</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of T</returns>
        public async Task<ServiceResult<string>> ReplaceAsync(object id, string entity, Dictionary<string, string> headers = null)
        {
            var url = GetStandardItemUrl(id.ToString());
            try
            {
                if (string.IsNullOrEmpty(entity))
                {
                    throw new ArgumentNullException(nameof(entity));
                }
                var payload = SerializeEntity(entity);
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Put, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Update completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex) when (ex is Newtonsoft.Json.JsonSerializationException || ex is System.FormatException)
            {
                return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process this object due to malformed object structure.");
            }
            catch (ArgumentNullException ex) when (ex.Message.Equals(nameof(entity)))
            {
                return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process an empty object.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Update failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Gets a count of objects that match the specified find criteria
        /// </summary>
        /// <param name="payload">The search payload in MongoDB find syntax format; for more information see https://docs.mongodb.com/manual/reference/method/db.collection.find/</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of integer</returns>
        public async Task<ServiceResult<long>> CountAsync(string payload, Dictionary<string, string> headers = null)
        {
            try
            {
                var url = $"{GetStandardCollectionUrl()}/count";
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;

                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_TEXT_PLAIN, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }

                var dictionary = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(result.Value);
                var kv = dictionary.FirstOrDefault(k => k.Key.Equals("count", StringComparison.OrdinalIgnoreCase));
                long.TryParse(kv.Value, out long count);
                var typedResult = new ServiceResult<long>(value: count, status: (int)result.Status, correlationId: Common.GetCorrelationIdFromHeaders(headers)) { ServiceName = Common.OBJECT_SERVICE_NAME };
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get count completed on {_client.BaseAddress}");
                return typedResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get count failed on {_client.BaseAddress}");
                throw ex;
            }
        }

        /// <summary>
        /// Deletes an object by id
        /// </summary>
        /// <param name="id">The id of the object. This parameter must match a property on the object with a key of "id" (all lowercase).</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of int</returns>
        public async Task<ServiceResult<int>> DeleteAsync(object id, Dictionary<string, string> headers = null)
        {
            var url = GetStandardItemUrl(id.ToString());
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<int> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Delete, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    var deleteResult = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                    result = ServiceResult<int>.CreateNewUsingDetailsFrom<string>(1, deleteResult);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Inserts an object with no specified Id. An ID is assigned by the database.
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of T</returns>
        public async Task<ServiceResult<string>> InsertAsync(string entity, Dictionary<string, string> headers = null) => await InsertAsync(id: null, entity: entity, headers: headers);

        /// <summary>
        /// Inserts an object by id
        /// </summary>
        /// <param name="id">The id of the object. This parameter must match a property on the object with a key of "_id" (all lowercase).</param>
        /// <param name="entity">The entity to insert</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of T</returns>
        public async Task<ServiceResult<string>> InsertAsync(object id, string entity, Dictionary<string, string> headers = null)
        {
            string url = (id != null) ? GetStandardItemUrl(id.ToString()) : GetStandardCollectionUrl();
            
            try
            {
                if (string.IsNullOrEmpty(entity))
                {
                    throw new ArgumentNullException(nameof(entity));
                }

                entity = ForceAddIdToJsonObject(id, entity);

                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, entity);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Insert completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (ArgumentNullException ex) when (ex.ParamName.Equals(nameof(entity)))
            {
                return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process an empty object.");
            }
            catch (Exception ex) when (ex is Newtonsoft.Json.JsonReaderException || ex is Newtonsoft.Json.JsonSerializationException || ex is System.FormatException)
            {
                return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process this object due to malformed object structure.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Insert failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Inserts many objects at once. IDs for the objects will be auto-generated.
        /// </summary>
        /// <param name="entities">The entities to insert</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of the IDs of the objects that were inserted</returns>
        public async Task<ServiceResult<IEnumerable<string>>> InsertManyAsync(IEnumerable<string> entities, Dictionary<string, string> headers = null)
        {
            var url = GetCollectionOperationUrl("multi");
            try
            {
                var payload = SerializeEntities(entities);
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<IEnumerable<string>> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    var insertManyResult = await Common.GetHttpResultAsServiceResultAsync<InsertManyResult>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                    result = ServiceResult<IEnumerable<string>>.CreateNewUsingDetailsFrom<InsertManyResult>(insertManyResult.Value.Ids, insertManyResult);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Insert completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Insert failed on {_client.BaseAddress}{url}");
                throw ex;
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
            var url = $"{GetStandardCollectionUrl()}/aggregate";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, aggregationExpression);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Aggregate completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex) when (ex is Newtonsoft.Json.JsonSerializationException || ex is System.FormatException)
            {
                return GetBadRequestResult(Common.GetCorrelationIdFromHeaders(headers), "Unable to process this object due to malformed object structure.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Aggregate failed on {_client.BaseAddress}{url}");
                throw ex;
            }            
        }

        /// <summary>
        /// Deletes an entire collection
        /// </summary>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of int</returns>
        public async Task<ServiceResult<int>> DeleteCollectionAsync(Dictionary<string, string> headers = null)
        {
            var url = GetStandardCollectionUrl();
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<int> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Delete, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    var deleteResult = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                    result = ServiceResult<int>.CreateNewUsingDetailsFrom<string>(1, deleteResult);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete collection completed on {_client.BaseAddress}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete collection failed on {_client.BaseAddress}");
                throw ex;
            }
        }

        /// <summary>
        /// Gets the distinct values for a given field in this collection
        /// </summary>
        /// <param name="fieldName">Name of the field to use for determining distinctness</param>
        /// <param name="payload">The search payload in MongoDB find syntax format; for more information see https://docs.mongodb.com/manual/reference/method/db.collection.find/</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of strings</returns>
        public async Task<ServiceResult<List<string>>> GetDistinctAsync(string fieldName, string payload, Dictionary<string, string> headers = null)
        {
            #region Input Validation
            if (!_regexCollectionName.IsMatch(fieldName))
            {
                throw new ArgumentException(nameof(fieldName));
            }
            #endregion // Input Validation

            try
            {
                var url = $"{GetStandardUrl("distinct")}/{fieldName}";
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<List<string>> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<List<string>>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get distinct completed on {_client.BaseAddress} with field={fieldName}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get distinct failed on {_client.BaseAddress} with field={fieldName}");
                throw ex;
            }
        }

        private HttpRequestMessage BuildHttpRequestMessage(HttpMethod method, string url, string mediaType, Dictionary<string, string> headers, string payload = null)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(method, url);
            Common.AddHttpRequestHeaders(requestMessage, SendingServiceName, Common.OBJECT_SERVICE_NAME, headers);
            if (payload != null && method != HttpMethod.Get)
            {
                requestMessage.Content = new StringContent(payload, System.Text.Encoding.UTF8, mediaType);
            }
            return requestMessage;
        }

        private string SerializeEntity(string entity) => entity.ToString();

        private string SerializeEntities(IEnumerable<string> entity) => Newtonsoft.Json.JsonConvert.SerializeObject(entity, JsonSerializerSettings);

        private string GetStandardItemUrl(string id) => $"{_databaseName}/{_collectionName}/{id}";

        private string GetStandardCollectionUrl() => $"{_databaseName}/{_collectionName}";

        private string GetStandardUrl(string routePart) => $"{_databaseName}/{_collectionName}/{routePart}";

        private string GetCollectionOperationUrl(string operationName) => $"{operationName}/{_databaseName}/{_collectionName}";

        /// <summary>
        /// Forces an ID property into a JSON object
        /// </summary>
        /// <param name="id">The ID value to force into the object's 'id' property</param>
        /// <param name="json">The Json that should contain the ID key and value</param>
        /// <returns>The Json object with an 'id' property and the specified id value</returns>
        private string ForceAddIdToJsonObject(object id, string json)
        {
            var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (values.ContainsKey(ID_PROPERTY_NAME))
            {
                if (id != null)
                {
                    values[ID_PROPERTY_NAME] = id;
                }
                else if (id == null)
                {
                    var idValue = values[ID_PROPERTY_NAME];
                    if (idValue is string)
                    {
                        values.Remove(ID_PROPERTY_NAME);
                    }
                }
            }
            else if (id != null)
            {
                values.Add(ID_PROPERTY_NAME, id);
            }

            string checkedJson = Newtonsoft.Json.JsonConvert.SerializeObject(values, Formatting.Indented);
            return checkedJson;
        }

        private (bool, ObjectId) IsObjectId(string id)
        {
            bool isObjectId = ObjectId.TryParse(id.ToString(), out ObjectId objectId);
            return (isObjectId, objectId);
        }

        private ServiceResult<string> GetBadRequestResult(string correlationId, string message = "") => new ServiceResult<string>(
                value: string.Empty,
                status: (int)HttpStatusCode.BadRequest,
                correlationId: correlationId,
                servicename: Common.OBJECT_SERVICE_NAME,
                message: !string.IsNullOrEmpty(message) ? message : "Invalid inputs");
    }
}