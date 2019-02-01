using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using Newtonsoft.Json;

namespace Foundation.Sdk.Security
{
    /// <summary>
    /// Class for handling scope requirements specific to the foundation services scoping authorization model
    /// </summary>
    public sealed class TokenHasScopeHandler : ScopeHandler
    {
        private readonly string _introspectionUri;
        private readonly HttpClient _client = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="systemName">The name of the system to which the verifying service belongs</param>
        /// <param name="introspectionUri">The Uri to use for token introspection</param>
        /// <param name="httpClientFactory">HttpClient factory to use for the introspection request</param>
        public TokenHasScopeHandler(string systemName, string introspectionUri, IHttpClientFactory httpClientFactory) 
            : base(systemName)
        {
            _client = httpClientFactory.CreateClient($"oauth2-provider");
            _introspectionUri = introspectionUri;
        }

        /// <summary>
        /// Determine if the user's scope claim (if any) matches the URL and HTTP operation they are attempting to carry out
        /// </summary>
        /// <param name="context">Contains authorization information used by Microsoft.AspNetCore.Authorization.IAuthorizationHandler</param>
        /// <param name="requirement">Information about what the requirement for this HTTP operation are</param>
        /// <returns>Task</returns>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            // Let's see if the resource is an auth filter. If not, exit
            var resource = (context.Resource as Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext);
            if (resource == null || !resource.RouteData.Values.Keys.Contains("db") || !resource.RouteData.Values.Keys.Contains("collection"))
            {
                return;
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
                return;
            }

            string responseBody = string.Empty;
            string token = resource.HttpContext.Request.Headers["Authorization"];

            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, _introspectionUri))
            {
                var formData = new Dictionary<string, string>();
                formData.Add("token", token);
                
                message.Content = new FormUrlEncodedContent(formData);

                using (var response = await _client.SendAsync(message)) 
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                }
            }

            IntrospectionResponse introspectionResponse = JsonConvert.DeserializeObject<IntrospectionResponse>(responseBody);

            if (!introspectionResponse.Active || !introspectionResponse.Exp.HasValue)
            {
                context.Fail();
                return;
            }

            DateTimeOffset expirationTimeOffset = DateTimeOffset.FromUnixTimeSeconds(introspectionResponse.Exp.Value);
            DateTime expirationTime = expirationTimeOffset.UtcDateTime;
            DateTime now = DateTime.Now;

            if (now > expirationTime)
            {
                // fail due to expiration
                context.Fail();
                return;
            }

            if (HasScope(requiredScope, introspectionResponse.Scopes))
            {
                context.Succeed(requirement);
                return;
            }

            context.Fail();
        }
    }
}