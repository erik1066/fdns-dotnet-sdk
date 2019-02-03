using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Xunit;

using Foundation.Sdk;

namespace Foundation.Sdk.Tests
{
    public class ServiceResultTests
    {
        [Theory]
        [InlineData(200, "https://tools.ietf.org/html/rfc7231#section-6.3.1", "OK")]
        [InlineData(201, "https://tools.ietf.org/html/rfc7231#section-6.3.2", "Created")]
        [InlineData(202, "https://tools.ietf.org/html/rfc7231#section-6.3.3", "Accepted")]
        [InlineData(203, "https://tools.ietf.org/html/rfc7231#section-6.3.4", "NonAuthoritativeInformation")]
        [InlineData(204, "https://tools.ietf.org/html/rfc7231#section-6.3.5", "NoContent")]
        [InlineData(400, "https://tools.ietf.org/html/rfc7231#section-6.5.1", "BadRequest")]
        [InlineData(403, "https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden")]
        [InlineData(404, "https://tools.ietf.org/html/rfc7231#section-6.5.4", "NotFound")]
        [InlineData(405, "https://tools.ietf.org/html/rfc7231#section-6.5.5", "MethodNotAllowed")]
        [InlineData(408, "https://tools.ietf.org/html/rfc7231#section-6.5.7", "RequestTimeout")]
        [InlineData(409, "https://tools.ietf.org/html/rfc7231#section-6.5.8", "Conflict")]
        [InlineData(413, "https://tools.ietf.org/html/rfc7231#section-6.5.11", "RequestEntityTooLarge")]
        [InlineData(414, "https://tools.ietf.org/html/rfc7231#section-6.5.12", "RequestUriTooLong")]
        [InlineData(415, "https://tools.ietf.org/html/rfc7231#section-6.5.13", "UnsupportedMediaType")]
        [InlineData(500, "https://tools.ietf.org/html/rfc7231#section-6.6", "InternalServerError")]
        [InlineData(499, "", "499")]
        public void CheckResultDetails(int status, string type, string title)
        {
            var serviceResult = new ServiceResult<string>("value", status, "3kfnv", "unittests", "message");

            Assert.Equal(status, serviceResult.Details.Status);
            Assert.Equal(status, serviceResult.Status);
            Assert.Equal(type, serviceResult.Details.Type);
            Assert.Equal(title, serviceResult.Details.Title);
            Assert.Equal("value", serviceResult.Value);
            Assert.Equal("3kfnv", serviceResult.CorrelationId);
            Assert.Equal("unittests", serviceResult.ServiceName);
            Assert.Equal("message", serviceResult.Details.Detail);

            if (status >= 200 && status <= 299) 
            {
                Assert.True(serviceResult.IsSuccess);
            }
            else
            {
                Assert.False(serviceResult.IsSuccess);
            }
        }

        [Fact]
        public void Construct_Fail_Null_Message()
        {
            Assert.Throws<ArgumentNullException>(() => 
            {
                var serviceResult = new ServiceResult<string>("value", 200, "3kfnv", "unittests", null);
            });
        }

        [Fact]
        public void Construct_Fail_Null_Correlation()
        {
            Assert.Throws<ArgumentNullException>(() => 
            {
                var serviceResult = new ServiceResult<string>("value", 200, null, "unittests", "message");
            });
        }

        [Fact]
        public void Construct_Fail_Null_ServiceName()
        {
            Assert.Throws<ArgumentNullException>(() => 
            {
                var serviceResult = new ServiceResult<string>("value", 200, "3kfnv", null, "message");
            });
        }

        [Fact]
        public void Get_Typed_Result()
        {
            var serviceResult = new ServiceResult<string>("{ \"name\" : \"John\", \"age\" : 15 }", 200, "3kfnv", "object", "message");
            var foo = serviceResult.GetTypedItem<Foo>();

            Assert.True(foo.Name.Equals("John"));
            Assert.True(foo.Age.Equals(15));
        }

        private class Foo
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }
    }
}