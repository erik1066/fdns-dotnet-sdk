using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Foundation.Sdk.Services
{
    /// <summary>
    /// Class for interacting with the Storage Foundation Service (see https://github.com/CDCgov/fdns-ms-storage) over HTTP
    /// </summary>
    public sealed class HttpStorageRepository : IStorageRepository
    {
        private readonly HttpClient _client = null;
        private readonly ILogger<HttpStorageRepository> _logger;
        private string Drawer { get; } = string.Empty;

        private string SendingServiceName { get; }
        private JsonSerializerSettings JsonSerializerSettings { get; }

        public HttpStorageRepository(IHttpClientFactory clientFactory, ILogger<HttpStorageRepository> logger, string appName, string drawer)
        {
            #region Input Validation
            if (clientFactory == null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }
            if (appName == null)
            {
                throw new ArgumentNullException(nameof(appName));
            }
            #endregion // Input Validation

            _client = clientFactory.CreateClient($"{appName}-{Common.STORAGE_SERVICE_NAME}");
            _logger = logger;
            SendingServiceName = appName;
            Drawer = drawer;
            JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }

        public HttpStorageRepository(IHttpClientFactory clientFactory, ILogger<HttpStorageRepository> logger, string appName, string drawer, JsonSerializerSettings jsonSerializerSettings) : this(clientFactory, logger, appName, drawer)
        {
            JsonSerializerSettings = jsonSerializerSettings;
        }

        public async Task<ServiceResult<List<StorageMetadata>>> GetAllDrawersAsync(Dictionary<string, string> headers = null)
        {
            try
            {
                var url = $"drawer";
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<List<StorageMetadata>> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<List<StorageMetadata>>(response, Common.STORAGE_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get all drawers completed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get all drawers failed");
                throw;
            }
        }

        public async Task<ServiceResult<DrawerResult>> CreateDrawerAsync(Dictionary<string, string> headers = null)
        {
            try
            {
                var url = $"drawer/{Drawer}";
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<DrawerResult> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Put, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<DrawerResult>(response, Common.STORAGE_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Create drawer completed on {_client.BaseAddress}/{Drawer}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Create drawer failed on {_client.BaseAddress}/{Drawer}");
                throw;
            }
        }

        public async Task<ServiceResult<DrawerResult>> DeleteDrawerAsync(Dictionary<string, string> headers = null)
        {
            try
            {
                var url = $"drawer/{Drawer}";
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<DrawerResult> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Delete, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<DrawerResult>(response, Common.STORAGE_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete drawer completed on {_client.BaseAddress}/{Drawer}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Delete drawer failed on {_client.BaseAddress}/{Drawer}");
                throw;
            }
        }

        public async Task<ServiceResult<DrawerResult>> GetDrawerAsync(Dictionary<string, string> headers = null)
        {
            try
            {
                var url = $"drawer/{Drawer}";
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<DrawerResult> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<DrawerResult>(response, Common.STORAGE_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get drawer completed on {_client.BaseAddress}/{Drawer}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get drawer failed on {_client.BaseAddress}/{Drawer}");
                throw;
            }
        }

        public async Task<ServiceResult<List<StorageMetadata>>> GetAllNodesAsync(Dictionary<string, string> headers = null)
        {
            var url = $"drawer/nodes/{Drawer}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<List<StorageMetadata>> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<List<StorageMetadata>>(response, Common.STORAGE_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get all nodes on drawer completed on {_client.BaseAddress}/{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get all nodes on drawer failed on {_client.BaseAddress}/{url}");
                throw;
            }
        }

        public async Task<ServiceResult<StorageMetadata>> GetNodeAsync(string id, Dictionary<string, string> headers = null)
        {
            try
            {
                var url = $"node/{Drawer}?id={id}";
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<StorageMetadata> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<StorageMetadata>(response, Common.STORAGE_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get node completed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get node failed");
                throw;
            }
        }

        public async Task<ServiceResult<byte[]>> DownloadNodeAsync(string id, Dictionary<string, string> headers = null)
        {
            var url = $"node/{Drawer}/dl?id={id}";

            try
            {
                ServiceResult<byte[]> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<byte[]>(response, Common.STORAGE_SERVICE_NAME, url, headers);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.STORAGE_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get failed on {url}");
                throw;
            }
        }

        public async Task<ServiceResult<StorageMetadata>> CreateNodeAsync(string id, string filename, byte[] data, Dictionary<string, string> headers = null)
        {
            var url = $"node/{Drawer}?id={id}&generateStruct=false&generateId=false&replace=false";

            ServiceResult<StorageMetadata> result = null;
            HttpRequestMessage requestMessage = BuildMultipartHttpRequestMessage(HttpMethod.Post, url, headers, data, filename);
            using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
            {
                result = await Common.GetHttpResultAsServiceResultAsync<StorageMetadata>(response, Common.STORAGE_SERVICE_NAME, url, headers);
            }
            MultipartFormDataContent content = requestMessage.Content as MultipartFormDataContent;
            if (content != null)
            {
                content.Dispose();
            }
            return result;
        }

        public async Task<ServiceResult<DrawerResult>> DeleteNodeAsync(string id, Dictionary<string, string> headers = null)
        {
            var url = $"node/{Drawer}?id={id}";

            ServiceResult<DrawerResult> result = null;
            HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Delete, url, string.Empty, headers);
            using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
            {
                result = await Common.GetHttpResultAsServiceResultAsync<DrawerResult>(response, Common.STORAGE_SERVICE_NAME, url, headers);
            }
            return result;
        }

        private HttpRequestMessage BuildHttpRequestMessage(HttpMethod method, string url, string mediaType, Dictionary<string, string> headers, string payload = null)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(method, url);
            Common.AddHttpRequestHeaders(requestMessage, SendingServiceName, Common.STORAGE_SERVICE_NAME, headers);
            if (payload != null && method != HttpMethod.Get)
            {
                requestMessage.Content = new StringContent(payload, System.Text.Encoding.UTF8, mediaType);
            }
            return requestMessage;
        }

        private HttpRequestMessage BuildMultipartHttpRequestMessage(HttpMethod method, string url, Dictionary<string, string> headers, byte[] data, string filename)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(method, url);
            Common.AddHttpRequestHeaders(requestMessage, SendingServiceName, Common.STORAGE_SERVICE_NAME, headers);
            if (data != null && method != HttpMethod.Get)
            {
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(new System.IO.MemoryStream(data)), "file", filename);
                requestMessage.Content = content;
            }
            return requestMessage;
        }
    }
}