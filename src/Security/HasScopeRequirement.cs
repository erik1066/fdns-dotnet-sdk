using System;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.Sdk.Security
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class HasScopeRequirement : IAuthorizationRequirement
    {
        public string Issuer { get; }
        public string Scope { get; }

        public HasScopeRequirement(string scope, string issuer)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }
    }
#pragma warning restore 1591
}