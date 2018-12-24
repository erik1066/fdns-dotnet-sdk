using System;
using System.Collections.Generic;

namespace Foundation.Sdk.Tests.Models
{
    public sealed class Customer
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public List<int> Favorites { get; set; } = new List<int>();
        public List<string> Aliases { get; set; } = new List<string>();
        public List<Book> Books { get; set; } = new List<Book>();
    }
}