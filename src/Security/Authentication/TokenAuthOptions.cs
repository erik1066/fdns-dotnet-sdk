using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.Sdk.Security
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public sealed class TokenAuthOptions: AuthenticationSchemeOptions
    {
        public TokenAuthOptions()
        { }
    }
#pragma warning restore 1591
}