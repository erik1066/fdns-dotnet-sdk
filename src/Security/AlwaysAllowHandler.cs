using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.Sdk.Security
{
    /// <summary>
    /// Class used only for disabling authorization for debugging and demonstration purposes. Do not use in production.
    /// </summary>
    public class AlwaysAllowHandler : AuthorizationHandler<HasScopeRequirement>
    {
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
#pragma warning restore 1591
}