using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.Sdk.Security
{
    /// <summary>
    /// Class representing a set of scopes that an HTTP request must be authorized for
    /// </summary>
    public sealed class HasScopeRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// The issuer
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// The scope(s)
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scope">The scope(s) to use</param>
        /// <param name="issuer">The issuer</param>
        public HasScopeRequirement(string scope, string issuer)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }
        
        /// <summary>
        /// Gets the named pieces of the dot-delimted scope
        /// </summary>
        /// <returns>Strings representing each of the scope parts</returns>
        public (string SystemName, string ServiceName, string Outer, string Inner, string Permission) GetScopeParts()
        {
            var parts = this.Scope.Split('.');
            return (parts[0], parts[1], parts.Length > 2 ? parts[2] : "", parts.Length > 3 ? parts[3] : "", parts.Last());
        }
    }
}