using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Foundation.Sdk.Security
{
    /// <summary>
    /// Class representing an introspection response from an OAuth2 introspection endpoint
    /// </summary>
    public sealed class IntrospectionResponse
    {
        /// <summary>
        /// This is a boolean value of whether or not the presented token is currently active. The value should be “true” if the token has been issued by this authorization server, has not been revoked by the user, and has not expired.
        /// </summary>
        public bool Active { get; set; } = false;

        /// <summary>
        /// The client identifier for the OAuth 2.0 client that the token was issued to.
        /// </summary>
        [JsonProperty("client_id")]
        public string ClientId { get; set; } = null;

        /// <summary>
        /// Subject - Identifier for the End-User at the IssuerURL.
        /// </summary>
        public string Sub { get; set; } = null;

        /// <summary>
        /// The unix timestamp (integer timestamp, number of seconds since January 1, 1970 UTC) indicating when this token will expire.
        /// </summary>
        public long? Exp { get; set; } = null;

        /// <summary>
        /// The unix timestamp (integer timestamp, number of seconds since January 1, 1970 UTC) indicating when this token was issued
        /// </summary>
        public long? Iat { get; set; } = null;

        /// <summary>
        /// Issuer of the token
        /// </summary>
        [JsonProperty("iss")]
        public string Issuer { get; set; } = null;

        /// <summary>
        /// Type of token
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType { get; set; } = "None";

        /// <summary>
        /// A JSON string containing a space-separated list of scopes associated with this token.
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// A human-readable identifier for the user who authorized this token.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Array of scopes
        /// </summary>
        public string[] Scopes
        {
            get 
            {
                if (string.IsNullOrEmpty(Scope)) return new string[0];

                var scopeArray = Scope.Split(' ');
                return scopeArray;
            }
        }
    }
}