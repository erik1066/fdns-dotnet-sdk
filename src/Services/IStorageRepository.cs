using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Foundation.Sdk.Services
{
    /// <summary>
    /// Interface for interacting with the Storage Foundation Service (see https://github.com/CDCgov/fdns-ms-storage)
    /// </summary>
    public interface IStorageRepository
    {
        Task<ServiceResult<List<StorageMetadata>>> GetAllDrawersAsync(Dictionary<string, string> headers = null);

        Task<ServiceResult<DrawerResult>> CreateDrawerAsync(Dictionary<string, string> headers = null);

        Task<ServiceResult<DrawerResult>> GetDrawerAsync(Dictionary<string, string> headers = null);

        Task<ServiceResult<DrawerResult>> DeleteDrawerAsync(Dictionary<string, string> headers = null);

        Task<ServiceResult<List<StorageMetadata>>> GetAllNodesAsync(Dictionary<string, string> headers = null);

        Task<ServiceResult<StorageMetadata>> GetNodeAsync(string id, Dictionary<string, string> headers = null);

        Task<ServiceResult<byte[]>> DownloadNodeAsync(string id, Dictionary<string, string> headers = null);

        Task<ServiceResult<StorageMetadata>> CreateNodeAsync(string id, string filename, byte[] data, Dictionary<string, string> headers = null);

        Task<ServiceResult<DrawerResult>> DeleteNodeAsync(string id, Dictionary<string, string> headers = null);
    }
}