using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.Sdk.Security
{
    /// <summary>
    /// Class for handling scope requirements specific to the foundation services scoping authorization model
    /// </summary>
    public sealed class JwtHasScopeHandler : ScopeHandler
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="systemName">The name of the system to which the verifying service belongs</param>
        public JwtHasScopeHandler(string systemName) 
            : base(systemName)
        { }

        /// <summary>
        /// Determine if the user's scope claim (if any) matches the URL and HTTP operation they are attempting to carry out
        /// </summary>
        /// <param name="context">Contains authorization information used by Microsoft.AspNetCore.Authorization.IAuthorizationHandler</param>
        /// <param name="requirement">Information about what the requirement for this HTTP operation are</param>
        /// <returns>Task</returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            // Let's see if the resource is an auth filter. If not, exit
            var resource = (context.Resource as Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext);
            if (resource == null || !resource.RouteData.Values.Keys.Contains("db") || !resource.RouteData.Values.Keys.Contains("collection"))
            {
                return Task.CompletedTask;
            }

            var parts = requirement.GetScopeParts();
            string systemName = parts.SystemName;
            string serviceName = parts.ServiceName;
            string permission = parts.Permission;

            /* We need to get the dot-separated path to the collection, such as fdns.object.bookstore.customer. This dot-separated
             * path is mapped to an HTTP route: "object" is the name of the servce (the Object microservice), "bookstore" is
             * the database name, and "customer" is the collection, e.g. /api/1.0/bookstore/customer. Before we can authorize
             * the user we have to build that dot-separated list so we can compare it to one of the scopes that was passed in
             * via the OAuth2 token. The first step is to get the dot-separated list from the URL, and then we add the
             * create/read/update/delete/etc portion at the end, per the requirement passed into the method call.
             */
            var scopeFromRoute = GetScopeFromRoute(resource);
            string requiredScope = $"{scopeFromRoute.Scope}.{permission}";

            if (!serviceName.Equals(scopeFromRoute.ScopeParts[1]) || !SystemName.Equals(scopeFromRoute.ScopeParts[0]))
            {
                // the scope doesn't include the proper service name or system name
                context.Fail();
                return Task.CompletedTask;
            }

            // Just a check to see if the user identity object has a scope claim. If not, something is wrong and exit
            if (!context.User.HasClaim(c => c.Type == SCOPE && c.Issuer == requirement.Issuer))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            /* Let's figure out all the scopes the user has been authorized to. These came from the OAuth2 token and have been
             * parsed by the ASP.NET Core middleware. We just an array of strings for simplicity's sake.
             */
            var tokenScopes = context.User.FindFirst(c => c.Type == SCOPE && c.Issuer == requirement.Issuer).Value.Split(' ');

            // Succeed if the scope array contains the required scope
            if (HasScope(requiredScope, tokenScopes))
            {
                context.Succeed(requirement);
            }
            else 
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}