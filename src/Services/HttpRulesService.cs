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
    /// Class for interacting with the Rules Foundation Service (see https://github.com/CDCgov/fdns-ms-rules) over HTTP.
    /// See https://github.com/CDCgov/fdns-rules-engine/blob/master/README.md for documentation on how to build a rules
    /// Json profile. See https://github.com/CDCgov/fdns-ms-rules/blob/master/src/test/resources/junit/rules.json for an
    /// examples of a profile.
    /// </summary>
    public sealed class HttpRulesService : IRulesService
    {
        private readonly Regex _regexHostName = new Regex(@"^[a-zA-Z0-9:\.\-/]*$");
        private readonly Regex _regexCollectionName = new Regex(@"^[a-zA-Z0-9s]*$");

        private const string MEDIA_TYPE = "application/json";
        private readonly HttpClient _client = null;
        private readonly ILogger<HttpRulesService> _logger = null;
        private JsonSerializerSettings JsonSerializerSettings { get; }
        private string SendingServiceName { get; } = string.Empty;

        public HttpRulesService(IHttpClientFactory clientFactory, ILogger<HttpRulesService> logger, string appName)
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

            _client = clientFactory.CreateClient($"{appName}-{Common.RULES_SERVICE_NAME}");
            _logger = logger;
            SendingServiceName = appName;
            JsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }

        public HttpRulesService(IHttpClientFactory clientFactory, ILogger<HttpRulesService> logger, string appName, JsonSerializerSettings jsonSerializerSettings) : this(clientFactory, logger, appName)
        {
            JsonSerializerSettings = jsonSerializerSettings;
        }

        /// <summary>
        /// Retrieves a validation profile
        /// </summary>
        /// <param name="profileName">The name of the profile to retrieve</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> GetProfileAsync(string profileName, Dictionary<string, string> headers = null)
        {
            var url = $"{profileName}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Get, url, string.Empty, headers);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.RULES_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.RULES_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get profile completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.RULES_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Get profile on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Creates a validation profile
        /// </summary>
        /// <param name="profileName">The name of the profile</param>
        /// <param name="payload">The profile to create or update</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> CreateProfileAsync(string profileName, string payload, Dictionary<string, string> headers = null)
        {
            var url = $"{profileName}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.RULES_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.RULES_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Create profile completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.RULES_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Create profile failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Upserts a validation profile
        /// </summary>
        /// <param name="profileName">The name of the profile</param>
        /// <param name="payload">The profile to upsert</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> UpsertProfileAsync(string profileName, string payload, Dictionary<string, string> headers = null)
        {
            var url = $"{profileName}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Put, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.RULES_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.RULES_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Upsert profile completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.RULES_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Upsert profile failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        /// <summary>
        /// Validates Json data based on a validation profile
        /// </summary>
        /// <param name="profileName">The name of the profile</param>
        /// <param name="payload">The data to be validated</param>
        /// <param name="explain">Whether to explain the validation errors or not</param>
        /// <param name="headers">Optional custom headers to pass through to this request, such as for authorization tokens or correlation Ids</param>
        /// <returns>ServiceResult of string</returns>
        public async Task<ServiceResult<string>> ValidateAsync(string profileName, string payload, bool explain, Dictionary<string, string> headers = null)
        {
            var url = $"validate/{profileName}?explain={explain}";
            try
            {
                headers = Common.NormalizeHeaders(headers);
                ServiceResult<string> result = null;
                HttpRequestMessage requestMessage = BuildHttpRequestMessage(HttpMethod.Post, url, Common.MEDIA_TYPE_APPLICATION_JSON, headers, payload);
                using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                {
                    result = await Common.GetHttpResultAsServiceResultAsync<string>(response, Common.RULES_SERVICE_NAME, url, headers);
                }
                _logger.LogInformation($"{Common.GetLogPrefix(Common.RULES_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Validate completed on {_client.BaseAddress}{url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Common.GetLogPrefix(Common.RULES_SERVICE_NAME, Common.GetCorrelationIdFromHeaders(headers))}: Validate failed on {_client.BaseAddress}{url}");
                throw ex;
            }
        }

        private HttpRequestMessage BuildHttpRequestMessage(HttpMethod method, string url, string mediaType, Dictionary<string, string> headers, string payload = null)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(method, url);
            Common.AddHttpRequestHeaders(requestMessage, SendingServiceName, Common.RULES_SERVICE_NAME, headers);
            if (payload != null && method != HttpMethod.Get)
            {
                requestMessage.Content = new StringContent(payload, System.Text.Encoding.UTF8, mediaType);
            }
            return requestMessage;
        }
    }
}