using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Foundation.Sdk.Security
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public static class TokenAuthExtensions
    {
        public static AuthenticationBuilder AddTokenAuth(this AuthenticationBuilder builder, Action<TokenAuthOptions> configureOptions)
        {
            return builder.AddScheme<TokenAuthOptions, TokenAuthHandler>("FDNS Token Scheme", "FDNS Token Scheme", configureOptions);
        }
    }
#pragma warning restore 1591
}