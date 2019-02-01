using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.Sdk.Security
{
    /// <summary>Class for disabling scope-based authorization by forcing an always-succeed result.</summary>
    public class AlwaysAllowHandler : AuthorizationHandler<HasScopeRequirement>
    {
        /// <summary>Handles the authorization requirement in an always-succeed mode.</summary>
        /// <remarks> Do not use this in production except for situations where the API does not need to be secured</remarks>
        /// <param name="context">AuthorizationHandlerContext</param>
        /// <param name="requirement">The scope requirement</param>
        /// <returns>Always succeed</returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}