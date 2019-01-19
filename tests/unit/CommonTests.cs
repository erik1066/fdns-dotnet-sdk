using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Xunit;

using Foundation.Sdk;

namespace Foundation.Sdk.Tests
{
    public class CommonTests
    {
        [Theory]
        [InlineData("x23gd4")]
        [InlineData("")]
        [InlineData("()U*(Y@UIHijt23")]
        [InlineData("5090cd2ddea9738aeb27af2bff73f0f741f0495c")]
        public void Get_Correlation_Id_From_Headers(string expectedCorrelationId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "X-App-Id", "1234" },
                { "X-Correlation-Id", expectedCorrelationId }
            };

            var actualCorrelationId = Common.GetCorrelationIdFromHeaders(headers);

            Assert.Equal(expectedCorrelationId, actualCorrelationId);
        }

        [Fact]
        public void Get_Correlation_Id_From_Headers_Null()
        {
            Dictionary<string, string> headers = null;

            var actualCorrelationId = Common.GetCorrelationIdFromHeaders(headers);

            Assert.Equal(string.Empty, actualCorrelationId);
        }

        [Fact]
        public void Get_Correlation_Id_From_Headers_NotPresent()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "X-App-Id", "1234" }
            };

            var actualCorrelationId = Common.GetCorrelationIdFromHeaders(headers);

            Assert.Equal(string.Empty, actualCorrelationId);
        }

        [Fact]
        public void Get_Correlation_Id_From_Headers_Empty()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            var actualCorrelationId = Common.GetCorrelationIdFromHeaders(headers);

            Assert.Equal(string.Empty, actualCorrelationId);
        }
    }
}