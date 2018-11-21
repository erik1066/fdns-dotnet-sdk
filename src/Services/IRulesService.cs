using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Foundation.Sdk.Services
{
    /// <summary>
    /// Interface for interacting with the Business Rules Foundation Service (see https://github.com/CDCgov/fdns-ms-rules)
    /// </summary>
    public interface IRulesService
    {
        Task<ServiceResult<string>> GetProfileAsync(string profileName, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> CreateProfileAsync(string profileName, string payload, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> UpsertProfileAsync(string profileName, string payload, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> ValidateAsync(string profileName, string payload, bool explain, Dictionary<string, string> headers = null);
    }
}