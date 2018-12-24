using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Foundation.Sdk.Tests.Models
{
    public sealed class Book
    {
        [JsonProperty("_id")]
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string Editor { get; set; } = string.Empty;
        public int Edition { get; set; } = 1;
        public string Isbn13 { get; set; } = string.Empty;
        public int Pages { get; set; } = 1;
        public int Chapters { get; set; } = 1;
    }
}