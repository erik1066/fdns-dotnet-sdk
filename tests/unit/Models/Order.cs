using System;
using System.Collections.Generic;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace Foundation.Sdk.Tests.Models
{
    public sealed class Order
    {
        [JsonProperty("_id")]
        public ObjectId Id { get; set; }
        public DateTime Date { get; set; }
        public Customer Customer { get; set; }
        public List<Book> Books { get; set; } = new List<Book>();
        public decimal Amount { get; set; }
    }
}