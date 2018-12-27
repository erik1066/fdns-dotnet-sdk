using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Foundation.Sdk.Data
{
    /// <summary>
    /// Interface for interacting with the Object Foundation Service (see https://github.com/CDCgov/fdns-ms-object)
    /// </summary>
    public interface IObjectService
    {
        Task<ServiceResult<string>> GetAsync(string databaseName, string collectionName, object id, Dictionary<string, string> headers = null);
        Task<ServiceResult<IEnumerable<string>>> GetAllAsync(string databaseName, string collectionName, Dictionary<string, string> headers = null);
        Task<ServiceResult<string>> InsertAsync(string databaseName, string collectionName, object id, string entity, Dictionary<string, string> headers = null);
        Task<ServiceResult<string>> InsertAsync(string databaseName, string collectionName, string entity, Dictionary<string, string> headers = null);
        Task<ServiceResult<string>> ReplaceAsync(string databaseName, string collectionName, object id, string entity, Dictionary<string, string> headers = null);
        Task<ServiceResult<int>> DeleteAsync(string databaseName, string collectionName, object id, Dictionary<string, string> headers = null);
        Task<ServiceResult<SearchResults<string>>> FindAsync(string databaseName, string collectionName, string findExpression, int start, int limit, string sortFieldName, ListSortDirection sortDirection = ListSortDirection.Descending, Dictionary<string, string> headers = null);
        Task<ServiceResult<SearchResults<string>>> SearchAsync(string databaseName, string collectionName, string searchExpression, int start, int limit, string sortFieldName, ListSortDirection sortDirection = ListSortDirection.Descending, Dictionary<string, string> headers = null);
        Task<ServiceResult<int>> DeleteCollectionAsync(string databaseName, string collectionName, Dictionary<string, string> headers = null);
        Task<ServiceResult<IEnumerable<string>>> InsertManyAsync(string databaseName, string collectionName, IEnumerable<string> entities, Dictionary<string, string> headers = null);
        Task<ServiceResult<long>> CountAsync(string databaseName, string collectionName, string payload, Dictionary<string, string> headers = null);
        Task<ServiceResult<List<string>>> GetDistinctAsync(string databaseName, string collectionName, string fieldName, string payload, Dictionary<string, string> headers = null);
        Task<ServiceResult<string>> AggregateAsync(string databaseName, string collectionName, string aggregationExpression, Dictionary<string, string> headers = null);
    }
}