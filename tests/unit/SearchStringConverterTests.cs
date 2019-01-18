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
    public class SearchStringConverterTests
    {
        [Theory]
        [InlineData("pages>400", "{\"pages\":{\"$gt\":400.0}}")]
        [InlineData("pages>=400", "{\"pages\":{\"$gte\":400.0}}")]
        [InlineData("pages<400", "{\"pages\":{\"$lt\":400.0}}")]
        [InlineData("pages<=400", "{\"pages\":{\"$lte\":400.0}}")]
        [InlineData("pages:400", "{\"pages\":400.0}")]
        [InlineData("pages!:400", "{\"pages\":{\"$ne\":400.0}}")]
        [InlineData("pages:400 authorCount>5", "{\"pages\":400.0,\"authorCount\":{\"$gt\":5.0}}")]
        [InlineData("pages:400 authorCount>1 chapters<=50", "{\"pages\":400.0,\"authorCount\":{\"$gt\":1.0},\"chapters\":{\"$lte\":50.0}}")]
        public void ConvertNumber(string queryString, string expectedFindExpression)
        {
            string actualFindExpression = SearchStringConverter.BuildQuery(queryString);

            Assert.Equal(expectedFindExpression, actualFindExpression);
        }

        [Theory]
        [InlineData("title:\"Engineering\"", "{\"title\":\"Engineering\"}")]
        [InlineData("title!:\"Engineering\"", "{\"title\":{\"$ne\":\"Engineering\"}}")]
        [InlineData("title:\"The Red Badge of Courage\"", "{\"title\":\"The Red Badge of Courage\"}")]
        [InlineData("title:\"Engineering\" author:\"John Doe\"", "{\"title\":\"Engineering\",\"author\":\"John Doe\"}")]
        public void ConvertText(string queryString, string expectedFindExpression)
        {
            string actualFindExpression = SearchStringConverter.BuildQuery(queryString);

            Assert.Equal(expectedFindExpression, actualFindExpression);
        }

        [Theory]
        [InlineData("isValid:true", "{\"isValid\":true}")]
        [InlineData("isValid:false", "{\"isValid\":false}")]
        [InlineData("isValid!:true", "{\"isValid\":{\"$ne\":true}}")]
        [InlineData("isValid!:false", "{\"isValid\":{\"$ne\":false}}")]
        public void ConvertBoolean(string queryString, string expectedFindExpression)
        {
            string actualFindExpression = SearchStringConverter.BuildQuery(queryString);

            Assert.Equal(expectedFindExpression, actualFindExpression);
        }

        [Theory]
        [InlineData("pages > 400")]
        [InlineData("pages> 400")]
        public void ConvertNumber_Fail(string queryString)
        {
            var expectedFindExpression = "{}";
            string actualFindExpression = SearchStringConverter.BuildQuery(queryString);

            Assert.Equal(expectedFindExpression, actualFindExpression);
        }
    }
}