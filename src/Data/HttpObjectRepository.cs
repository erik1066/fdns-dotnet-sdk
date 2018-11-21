using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Foundation.Sdk.Data
{
    /// <summary>
    /// Class for interacting with the FDNS Object microservice (see https://github.com/CDCgov/fdns-ms-object) over HTTP using strongly-typed objects
    /// </summary>
    public sealed class HttpObjectRepository<T> : IObjectRepository<T>
    {
        #region Members
        private readonly Regex _regexHostName = new Regex(@"^[a-zA-Z0-9:\.\-/]*$");
        private readonly Regex _regexCollectionName = new Regex(@"^[a-zA-Z0-9\.]*$");
        private readonly HttpClient _client = null;
        private readonly ILogger<HttpObjectRepository<T>> _logger;

        private string SendingServiceName { get; } = string.Empty;
        private JsonSerializerSettings JsonSerializerSettings { get; }
        #endregion // Members

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientFactory">The Http client factory to use for creating Http clients</param>
        /// <param name="logger">The logger to use</param>
        /// <param name="appName">Name of the service that is using this class to make requests to the Http Object service.</param>
        public HttpObjectRepository(IHttpClientFactory clientFactory, ILogger<HttpObjectRepository<T>> logger, string appName)
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
            #endregion // Input Validation

            _client = clientFactory.CreateClient($"{appName}-{Common.OBJECT_SERVICE_NAME}");
            _logger = logger;
            SendingServiceName = appName;
            JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientFactory">The Http client factory to use for creating Http clients</param>
        /// <param name="logger">The logger to use</param>
        /// <param name="appName">Name of the service that is using this class to make requests to the Http Object service.</param>
        /// <param name="jsonSerializerSettings">Customer Json serializer</param>
        public HttpObjectRepository(IHttpClientFactory clientFactory, ILogger<HttpObjectRepository<T>> logger, string appName, JsonSerializerSettings jsonSerializerSettings) : this(clientFactory, logger, appName)
        {
            JsonSerializerSettings = jsonSerializerSettings;
        }

        /// <summary>
        /// Retrieves an object by id
        /// </summary>
        /// <param name="id">The id of the object to retrieve. This parameter must match a property on the object with a key of "id" (all lowercase).</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of T</returns>
        public async Task<ServiceResult<T>> GetAsync(string id, Dictionary<string, string> headers = null)
        {
            var url = GetStandardItemUrl(id);
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<T> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<T>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get completed on {_client.BaseAddress}{url} in {result.Elapsed.TotalMilliseconds.ToString("N0")}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Finds one or more objects based on the specified find criteria
        /// </summary>
        /// <param name="from">The index on which to start retrieving objects</param>
        /// <param name="size">The maximum size of the result set that should be returned</param>
        /// <param name="sortFieldName">The field name on which to sort the result set</param>
        /// <param name="payload">The search payload in MongoDB find syntax format; for more information see https://docs.mongodb.com/manual/reference/method/db.collection.find/</param>
        /// <param name="sortDescending">Whether to sort in descending order or not</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of T</returns>
        public async Task<ServiceResult<SearchResults<T>>> FindAsync(int from, int size, string sortFieldName, string payload, bool sortDescending = true, Dictionary<string, string> headers = null)
        {
            #region Input Validation
            if (from < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(from));
            }
            if (size < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
            if (!_regexCollectionName.IsMatch(sortFieldName))
            {
                throw new ArgumentException(nameof(sortFieldName));
            }
            #endregion // Input Validation

            int sort = sortDescending ? 1 : -1;
            var url = $"{GetStandardCollectionUrl()}/find?from={from}&size={size}&sort={sortFieldName}&order={sort}";

            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<SearchResults<T>> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<SearchResults<T>>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Find completed on {_client.BaseAddress}{url} in {result.Elapsed.TotalMilliseconds.ToString("N0")}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Find failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Carries out a wholesale replacement of the object with the specified id
        /// </summary>
        /// <param name="id">The id of the object. This parameter must match a property on the object with a key of "id" (all lowercase).</param>
        /// <param name="entity">The entity that will replace the object with the specified id</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of T</returns>
        public async Task<ServiceResult<T>> ReplaceAsync(string id, T entity, Dictionary<string, string> headers = null)
        {
            var url = GetStandardItemUrl(id);
            try
            {
                var payload = SerializeEntity(entity);
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<T> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Put, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<T>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Update completed on {_client.BaseAddress}{url} in {result.Elapsed.TotalMilliseconds.ToString("N0")}");
                return result;
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
        public async Task<ServiceResult<int>> GetCountAsync(string payload, Dictionary<string, string> headers = null)
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

                var dictionary = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(result.Response);
                var kv = dictionary.FirstOrDefault(k => k.Key.Equals("count", StringComparison.OrdinalIgnoreCase));
                int.TryParse(kv.Value, out int count);
                var typedResult = new ServiceResult<int>(url, result.Elapsed, count, Common.OBJECT_SERVICE_NAME, result.IsSuccess, result.Code, Common.GetCorrelationIdFromHeaders(headers));
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get count completed on {_client.BaseAddress} in {result.Elapsed.TotalMilliseconds.ToString("N0")}");
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
        /// <returns>ServiceResult of bool</returns>
        public async Task<ServiceResult<DeleteResult>> DeleteAsync(string id, Dictionary<string, string> headers = null)
        {
            var url = GetStandardItemUrl(id);
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<DeleteResult> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Delete, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<DeleteResult>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete completed on {_client.BaseAddress}{url} in {result.Elapsed.TotalMilliseconds.ToString("N0")}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Inserts an object by id
        /// </summary>
        /// <param name="id">The id of the object. This parameter must match a property on the object with a key of "id" (all lowercase).</param>
        /// <param name="entity">The entity to insert</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of T</returns>
        public async Task<ServiceResult<T>> InsertAsync(string id, T entity, Dictionary<string, string> headers = null)
        {
            var url = GetStandardItemUrl(id);
            try
            {
                var payload = SerializeEntity(entity);
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<T> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<T>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Insert completed on {_client.BaseAddress}{url} in {result.Elapsed.TotalMilliseconds.ToString("N0")}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Insert failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Deletes an entire collection
        /// </summary>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of bool</returns>
        public async Task<ServiceResult<bool>> DeleteCollectionAsync(Dictionary<string, string> headers = null)
        {
            var url = GetStandardCollectionUrl();
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<bool> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Delete, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<bool>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete collection completed on {_client.BaseAddress} in {result.Elapsed.TotalMilliseconds.ToString("N0")}");
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
                var url = $"distinct/{fieldName}";
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<List<string>> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<List<string>>(response, Common.OBJECT_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.OBJECT_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get distinct completed on {_client.BaseAddress} with field={fieldName} in {result.Elapsed.TotalMilliseconds.ToString("N0")}");
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

        private string SerializeEntity(T entity) 
        {
            if (typeof(T) == typeof(String))
            {
                return entity.ToString();
            }  
            else 
            {
                return JsonConvert.SerializeObject(entity, JsonSerializerSettings);
            }
        }

        private string GetStandardItemUrl(string id) => $"{id}";

        private string GetStandardCollectionUrl() => $"";
    }
}