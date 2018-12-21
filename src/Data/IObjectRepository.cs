using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Foundation.Sdk.Data
{
    /// <summary>
    /// Interface for interacting with the Object Foundation Service (see https://github.com/CDCgov/fdns-ms-object)
    /// </summary>
    public interface IObjectRepository<T>
    {
        Task<ServiceResult<T>> GetAsync(object id, Dictionary<string, string> headers = null);

        Task<ServiceResult<SearchResults<T>>> FindAsync(int from, int size, string sortFieldName, string payload, bool sortDescending = true, Dictionary<string, string> headers = null);

        Task<ServiceResult<T>> ReplaceAsync(object id, T entity, Dictionary<string, string> headers = null);

        Task<ServiceResult<int>> GetCountAsync(string payload, Dictionary<string, string> headers = null);

        Task<ServiceResult<DeleteResult>> DeleteAsync(object id, Dictionary<string, string> headers = null);

        Task<ServiceResult<T>> InsertAsync(object id, T entity, Dictionary<string, string> headers = null);

        Task<ServiceResult<bool>> DeleteCollectionAsync(Dictionary<string, string> headers = null);

        Task<ServiceResult<List<string>>> GetDistinctAsync(string fieldName, string payload, Dictionary<string, string> headers = null);
    }
}