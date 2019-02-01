using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Foundation.Sdk.Security
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    internal class TokenAuthHandler : AuthenticationHandler<TokenAuthOptions>
    {
        public TokenAuthHandler(IOptionsMonitor<TokenAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            // store custom services here...
        }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // build the claims and put them in "Context"; you need to import the Microsoft.AspNetCore.Authentication package
            return await Task.Run(() => AuthenticateResult.NoResult());
        }
    }
#pragma warning restore 1591
}