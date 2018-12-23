using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Foundation.Sdk.Data
{
    /// <summary>
    /// Interface for interacting with the Object Foundation Service (see https://github.com/CDCgov/fdns-ms-object)
    /// </summary>
    public interface IObjectService<T>
    {
        Task<ServiceResult<T>> GetAsync(object id, Dictionary<string, string> headers = null);
        Task<ServiceResult<T>> InsertAsync(object id, T entity, Dictionary<string, string> headers = null);
        Task<ServiceResult<T>> ReplaceAsync(object id, T entity, Dictionary<string, string> headers = null);
        Task<ServiceResult<int>> DeleteAsync(object id, Dictionary<string, string> headers = null);
        Task<ServiceResult<SearchResults<T>>> FindAsync(string findExpression, int start, int limit, string sortFieldName, ListSortDirection sortDirection = ListSortDirection.Descending, Dictionary<string, string> headers = null);
        Task<ServiceResult<SearchResults<T>>> SearchAsync(string searchExpression, int start, int limit, string sortFieldName, ListSortDirection sortDirection = ListSortDirection.Descending, Dictionary<string, string> headers = null);
        Task<ServiceResult<int>> DeleteCollectionAsync(Dictionary<string, string> headers = null);
        Task<ServiceResult<IEnumerable<string>>> InsertManyAsync(IEnumerable<T> entities, Dictionary<string, string> headers = null);
        Task<ServiceResult<long>> CountAsync(string payload, Dictionary<string, string> headers = null);
        Task<ServiceResult<List<string>>> GetDistinctAsync(string fieldName, string payload, Dictionary<string, string> headers = null);
        Task<ServiceResult<string>> AggregateAsync(string aggregationExpression, Dictionary<string, string> headers = null);
    }
}