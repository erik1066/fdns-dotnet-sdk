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
    public class SearchResultsTests
    {

        [Fact]
        public void Get_Typed_Results()
        {
            var searchResults = new SearchResults();
            searchResults.Items.Add("{ \"name\" : \"John\", \"age\" : 15 }");
            searchResults.Items.Add("{ \"name\" : \"Jane\", \"age\" : 25 }");
            searchResults.Items.Add("{ \"name\" : \"Jose\", \"age\" : 35 }");

            List<Foo> people = searchResults.GetTypedItems<Foo>();

            var person1 = people[0];
            var person2 = people[1];
            var person3 = people[2];

            Assert.True(person1.Name.Equals("John"));
            Assert.True(person1.Age.Equals(15));

            Assert.True(person2.Name.Equals("Jane"));
            Assert.True(person2.Age.Equals(25));

            Assert.True(person3.Name.Equals("Jose"));
            Assert.True(person3.Age.Equals(35));
        }

        [Fact]
        public void Get_Stringified_Results()
        {
            var searchResults = new SearchResults();
            searchResults.Items.Add("{ \"name\" : \"John\", \"age\" : 15 }");
            searchResults.Items.Add("{ \"name\" : \"Jane\", \"age\" : 25 }");
            searchResults.Items.Add("{ \"name\" : \"Jose\", \"age\" : 35 }");

            string people = searchResults.StringifyItems();

            Assert.Equal("[ { \"name\" : \"John\", \"age\" : 15 },{ \"name\" : \"Jane\", \"age\" : 25 },{ \"name\" : \"Jose\", \"age\" : 35 } ]", people);
        }

        private class Foo
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }
    }
}