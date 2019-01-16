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

namespace Foundation.Sdk.Services
{
    /// <summary>
    /// Class for interacting with the Indexing Foundation Service (see https://github.com/CDCgov/fdns-ms-indexing) over HTTP
    /// </summary>
    public sealed class HttpIndexingService : IIndexingService
    {
        private readonly Regex _regexHostName = new Regex(@"^[a-zA-Z0-9:\.\-/]*$");
        private readonly Regex _regexCollectionName = new Regex(@"^[a-zA-Z0-9s]*$");

        private const string MEDIA_TYPE = "application/json";
        private readonly HttpClient _client = null;
        private readonly ILogger<HttpIndexingService> _logger = null;
        private JsonSerializerSettings JsonSerializerSettings { get; }
        private string SendingServiceName { get; } = string.Empty;

        public HttpIndexingService(IHttpClientFactory clientFactory, ILogger<HttpIndexingService> logger, string appName)
        {
            #region Input Validation
            if (clientFactory == null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentNullException(nameof(appName));
            }
            #endregion // Input Validation

            _client = clientFactory.CreateClient($"{appName}-{Common.INDEXING_SERVICE_NAME}");
            _logger = logger;
            SendingServiceName = appName;
            JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }

        public HttpIndexingService(IHttpClientFactory clientFactory, ILogger<HttpIndexingService> logger, string appName, JsonSerializerSettings jsonSerializerSettings) : this(clientFactory, logger, appName)
        {
            JsonSerializerSettings = jsonSerializerSettings;
        }

        /// <summary>
        /// Retrieves an index configuration by name
        /// </summary>
        /// <param name="configName">The name of the config to retrieve</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> GetConfigAsync(string configName, Dictionary<string, string> headers = null)
        {
            var url = $"config/{configName}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.INDEXING_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get config completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Creates or updates rules for a specific config
        /// </summary>
        /// <param name="configName">The name of the config</param>
        /// <param name="config">The config to create or update</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> CreateConfigAsync(string configName, string config, Dictionary<string, string> headers = null)
        {
            var url = $"config/{configName}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, config);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.INDEXING_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Create config completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Create config failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Registers a config with the search engine
        /// </summary>
        /// <param name="configName">The name of the config</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> RegisterConfigAsync(string configName, Dictionary<string, string> headers = null)
        {
            var url = $"index/{configName}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Put, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.INDEXING_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Register config completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Register config failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Deletes a config
        /// </summary>
        /// <param name="configName">The name of the config</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> DeleteConfigAsync(string configName, Dictionary<string, string> headers = null)
        {
            var url = $"config/{configName}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Delete, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.INDEXING_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete config completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete config failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Indexes a single object by its MongoDB id value
        /// </summary>
        /// <param name="configName">The name of the config</param>
        /// <param name="objectId">The id of the object (as stored in MongoDB) to index</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> IndexAsync(string configName, string objectId, Dictionary<string, string> headers = null)
        {
            var url = $"index/{configName}/{objectId}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.INDEXING_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Index one object completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Index one object failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Indexes all MongoDB objects
        /// </summary>
        /// <param name="configName">The name of the config</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> IndexAllAsync(string configName, Dictionary<string, string> headers = null)
        {
            var url = $"index/all/{configName}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Put, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.INDEXING_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Index all objects completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Index all objects failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Indexes multiple objects by their MongoDB id values
        /// </summary>
        /// <param name="configName">The name of the config</param>
        /// <param name="objectIds">The ids of the objects (as stored in MongoDB) to index</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> IndexManyAsync(string configName, List<string> objectIds, Dictionary<string, string> headers = null)
        {
            var url = $"index/bulk/{configName}";
            try
            {
                var ids = JsonConvert.SerializeObject(objectIds);
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, ids);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.INDEXING_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Index many objects completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Index many objects failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Gets a single object by its id
        /// </summary>
        /// <param name="configName">The name of the config</param>
        /// <param name="objectId">The id of the object (as stored in Elasticsearch) to index</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> GetAsync(string configName, string objectId, Dictionary<string, string> headers = null)
        {
            var url = $"get/{configName}/{objectId}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.INDEXING_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get one object completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get one object failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Searches for objects
        /// </summary>
        /// <param name="configName">The name of the config</param>
        /// <param name="query">Google-like search query</param>
        /// <param name="hydrate">Whether to hydrate as part of this operation or not</param>
        /// <param name="from">Starting point for the results</param>
        /// <param name="size">The number of items to limit the result set to</param>
        /// <param name="scroll">Scroll live time (e.g. "1m")</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> SearchAsync(string configName, string query, bool hydrate, int from, int size, string scroll, Dictionary<string, string> headers = null)
        {
            var url = $"search/{configName}?query={query}&hydrate={hydrate}&from={from}&size={size}&scroll={scroll}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.INDEXING_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get one object completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.INDEXING_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get one object failed on {_client.BaseAddress}{url}");
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
    }
}