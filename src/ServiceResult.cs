using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.AspNetCore.Mvc;

namespace Foundation.Sdk
{
    /// <summary>
    /// Class representing a result from calling one of the Foundation Services
    /// </summary>
    public sealed class ServiceResult<T>
    {
        public ProblemDetails Details { get; private set; }
        public string URI { get; set; } = string.Empty;
        public T Value { get; }
        public string ServiceName { get; set; } = string.Empty;
        public bool IsSuccess 
        {
            get 
            {
                if (Details.Status >= 200 && Details.Status <= 299) return true;
                else return false;
            }
        }
        public string CorrelationId { get; set; }
        public int Status 
        {
            get
            {
                return Details.Status ?? -1;
            }
            set
            {
                Details.Status = value;
            }
        }

        public static ServiceResult<T> CreateNewUsingDetailsFrom<K>(T value, ServiceResult<K> source) => new ServiceResult<T>(value: value, status: source.Status, correlationId: source.CorrelationId, servicename: source.ServiceName, message: source.Details.Detail);

        public ServiceResult(T value, int status, string correlationId = "", string servicename = "", string message = "")
        {
            #region Input Validation
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (correlationId == null)
            {
                throw new ArgumentNullException(nameof(correlationId));
            }
            if (servicename == null)
            {
                throw new ArgumentNullException(nameof(servicename));
            }
            #endregion // Input Validation

            Value = value;
            CorrelationId = correlationId;
            ServiceName = servicename;

            Details = new ProblemDetails()
            {
                Detail = message,
                Status = status,
                Title = GetTitleForHttpStatus(status),
                Type = GetTypeForHttpStatus(status)
            };
        }

        private static string GetTitleForHttpStatus(int status)
        {
            HttpStatusCode statusCode = (HttpStatusCode)status;
            return statusCode.ToString();
        }

        private static string GetTypeForHttpStatus(int status)
        {
            if (status >= 500)
            {
                return "https://tools.ietf.org/html/rfc7231#section-6.6";
            }
            switch (status)
            {
                case 200:
                    return "https://tools.ietf.org/html/rfc7231#section-6.3.1";
                case 201:
                    return "https://tools.ietf.org/html/rfc7231#section-6.3.2";
                case 202:
                    return "https://tools.ietf.org/html/rfc7231#section-6.3.3";
                case 203:
                    return "https://tools.ietf.org/html/rfc7231#section-6.3.4";
                case 204:
                    return "https://tools.ietf.org/html/rfc7231#section-6.3.5";
                case 400:
                    return "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                case 403:
                    return "https://tools.ietf.org/html/rfc7231#section-6.5.3";
                case 404:
                    return "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                case 405:
                    return "https://tools.ietf.org/html/rfc7231#section-6.5.5";
                case 408:
                    return "https://tools.ietf.org/html/rfc7231#section-6.5.7";
                case 409:
                    return "https://tools.ietf.org/html/rfc7231#section-6.5.8";
                case 413:
                    return "https://tools.ietf.org/html/rfc7231#section-6.5.11";
                case 414:
                    return "https://tools.ietf.org/html/rfc7231#section-6.5.12";
                case 415:
                    return "https://tools.ietf.org/html/rfc7231#section-6.5.13";
                default:
                    return string.Empty;                    
            }
        }
    }
}