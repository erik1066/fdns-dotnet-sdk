using System;
using System.Collections.Generic;
using System.Net;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing a result from calling one of the Foundation Services
    /// </summary>
    public sealed class ServiceResult<T>
    {
        public string URI { get; } = string.Empty;
        public T Response { get; }
        public string ServiceName { get; } = string.Empty;
        public bool IsSuccess { get; }
        public HttpStatusCode Code { get; }
        public TimeSpan Elapsed { get; }
        public string CorrelationId { get; }
        public string Message { get; } = string.Empty;

        public ServiceResult(string uri, TimeSpan elapsed, T response, string serviceName, bool isSuccess, HttpStatusCode code, string correlationId, string message = "")
        {
            #region Input Validation
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentNullException(nameof(uri));
            }
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }
            if (correlationId == null)
            {
                throw new ArgumentNullException(nameof(correlationId));
            }
            if (elapsed.TotalMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            #endregion // Input Validation

            URI = uri;
            Response = response;
            ServiceName = serviceName;
            IsSuccess = isSuccess;
            Code = code;
            Elapsed = elapsed;
            CorrelationId = correlationId;
            Message = message;
        }
    }
}