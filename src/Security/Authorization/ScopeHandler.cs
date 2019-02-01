using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.Sdk.Security
{
    /// <summary>
    /// Class for handling scope requirements specific to the foundation services scoping authorization model
    /// </summary>
    public abstract class ScopeHandler : AuthorizationHandler<HasScopeRequirement>
    {
        /// <summary>
        /// Constant representing the word 'scope'
        /// </summary>
        protected const string SCOPE = "scope";
        private static Regex _regex = new Regex(@"^[a-zA-Z0-9_\.]*$");

        /// <summary>
        /// Gets/sets the system name
        /// </summary>
        protected string SystemName { get; private set; } = string.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="systemName">The name of the system to which the verifying service belongs</param>
        protected ScopeHandler(string systemName)
        {
            #region Input validation
            if (string.IsNullOrEmpty(systemName))
            {
                throw new ArgumentNullException(nameof(systemName));
            }
            if (string.IsNullOrEmpty(systemName.Trim()))
            {
                throw new ArgumentException(nameof(systemName));
            }
            if (!_regex.IsMatch(systemName))
            {
                throw new ArgumentException(nameof(systemName));
            }
            #endregion // Input validation

            SystemName = systemName;
        }

        /// <summary>
        /// Determines whether the required scope is present in an OAuth2 scope string
        /// </summary>
        /// <param name="requiredScope">The scope that is required for the request to be successful</param>
        /// <param name="tokenScopes">The space-delimited set of scopes, e.g. 'fdns.object.bookstore fdns.object.coffeshop'</param>
        /// <returns>Whether the required scope is present in the token scopes</returns>
        protected bool HasScope(string requiredScope, string[] tokenScopes)
        {
            // Succeed if the scope array contains the required scope
            if (tokenScopes.Any(s => s == requiredScope))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the required scope from the route
        /// </summary>
        /// <param name="resource">Resource from the authorization context</param>
        /// <returns>The scope associated with the specified route</returns>
        protected (string Scope, string[] ScopeParts) GetScopeFromRoute(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext resource)
        {
            string service = resource.RouteData.Values.ElementAt(1).Value.ToString().ToLower();
            string db = resource.RouteData.Values.ElementAt(2).Value.ToString();
            string collection = resource.RouteData.Values.ElementAt(3).Value.ToString();

            var scope = $"{SystemName}.{service}.{db}.{collection}";
            var scopes = new string[4] { SystemName, service, db, collection };
            return (scope, scopes);
        }
    }
}