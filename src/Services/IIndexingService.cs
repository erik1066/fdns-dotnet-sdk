using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Foundation.Sdk.Services
{
    /// <summary>
    /// Interface for interacting with the Indexing Foundation Service (see https://github.com/CDCgov/fdns-ms-indexing)
    /// </summary>
    public interface IIndexingService
    {
        Task<ServiceResult<string>> GetConfigAsync(string configName, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> CreateConfigAsync(string configName, string config, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> RegisterConfigAsync(string configName, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> DeleteConfigAsync(string configName, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> IndexAsync(string configName, string objectId, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> IndexManyAsync(string configName, List<string> objectIds, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> IndexAllAsync(string configName, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> GetAsync(string configName, string objectId, Dictionary<string, string> headers = null);

        Task<ServiceResult<string>> SearchAsync(string configName, string query, bool hydrate, int from, int size, string scroll, Dictionary<string, string> headers = null);
    }
}