using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Foundation.Sdk.HealthChecks
{
    /// <summary>
    /// Class for conducting health checks over HTTP
    /// </summary>
    public sealed class HttpHealthCheck : IHealthCheck
    {
        private readonly string _url;
        private readonly int _degradationThreshold;
        private readonly string _description;
        private readonly HttpClient _client;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="description">Short, one-word description of the health check</param>
        /// <param name="url">The HTTP URL to use for the check</param>
        /// <param name="client">The HTTP client to use for the check</param>
        /// <param name="degradationThreshold">The threshold in milliseconds after which to consider the service degraded</param>
        /// <param name="cancellationThreshold">The threshold in milliseconds after which to cancel the check and consider the service unavailable</param>
        public HttpHealthCheck(string description, string url, HttpClient client, int degradationThreshold = 1000, int cancellationThreshold = 2000)
        {
            #region Input validation
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            if (degradationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(degradationThreshold));
            }
            if (cancellationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cancellationThreshold));
            }
            #endregion // Input validation

            _description = description;
            _url = url;
            _degradationThreshold = degradationThreshold;
            _client = client;
            _client.Timeout = new TimeSpan(0, 0, 0, cancellationThreshold);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="description">Description of the health check</param>
        /// <param name="url">The HTTP URL to use for the check</param>
        /// <param name="clientFactory">The HTTP client factory to use for the check</param>
        /// <param name="degradationThreshold">The threshold in milliseconds after which to consider the service degraded</param>
        /// <param name="cancellationThreshold">The threshold in milliseconds after which to cancel the check and consider the service unavailable</param>
        public HttpHealthCheck(string description, string url, IHttpClientFactory clientFactory, int degradationThreshold = 1000, int cancellationThreshold = 2000)
            : this (description, url, clientFactory.CreateClient(description), degradationThreshold, cancellationThreshold)
        { }

        /// <summary>
        /// Checks a URL using the provided HttpClient
        /// </summary>
        /// <param name="context">HealthCheckContext</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HealthCheckResult</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            HealthCheckResult checkResult;

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, _url))
            {
                var sw = new Stopwatch();
                sw.Start();

                try 
                {
                    HttpStatusCode status;
                    bool isSuccessCode = false;

                    using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                    {
                        status = response.StatusCode;
                        isSuccessCode = response.IsSuccessStatusCode;
                    }

                    sw.Stop();
                    var elapsed = sw.Elapsed.TotalMilliseconds.ToString("N0");

                    var data = new Dictionary<string, object> 
                    { 
                        ["elapsed"] = elapsed,
                        ["httpStatusCode"] = (int)status 
                    };

                    if (!isSuccessCode)
                    {
                        checkResult = HealthCheckResult.Unhealthy(
                            data: data,
                            description: $"{_description} probe failed: HTTP {status}");
                    }
                    else if (sw.Elapsed.TotalMilliseconds > _degradationThreshold)
                    {
                        checkResult = HealthCheckResult.Degraded(
                            data: data,
                            description: $"{_description} probe took more than {_degradationThreshold} ms");
                    }
                    else 
                    {
                        checkResult = HealthCheckResult.Healthy(
                            data: data,
                            description: $"{_description} probe completed in {elapsed} ms");
                    }
                }
                catch (Exception ex)
                {
                    checkResult = HealthCheckResult.Unhealthy(
                        data: new Dictionary<string, object> { ["exceptionType"] = ex.GetType().ToString() },
                        description: $"{_description} probe failed due to exception");
                }
                finally
                {
                    sw.Stop();
                }
            }

            return checkResult;
        }
    }
}