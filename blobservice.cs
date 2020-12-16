using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Backup.Functions.Helpers;
using Backup.Functions.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Backup.Functions.Services
{
    public class BlobService : IBlobService
    {
        private readonly ILogger<IBlobService> _logger;
        private static readonly string _storageConnectionStringName = "AzureWebJobsStorage";
        public static string StorageConnectionString
        {
            get
            {
                var connectionString = Environment.GetEnvironmentVariable(_storageConnectionStringName);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new CustomExceptions(ErrorMessages.NoStorageConnectionStringAvailable);
                }
                return connectionString;
            }
        }

        public BlobService(ILogger<IBlobService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> PerformTasks(string containerName)
        {
            try
            {
                if (CloudStorageAccount.TryParse(StorageConnectionString, out CloudStorageAccount cloudStorageAccount))
                {
                    var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                    var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                    if (await cloudBlobContainer.ExistsAsync())
                    {
                        BlobContinuationToken blobContinuationToken = null;
                        var blobList = await cloudBlobContainer.ListBlobsSegmentedAsync(blobContinuationToken);
                        var cloudBlobList = blobList.Results.Select(blb => blb as ICloudBlob);
                        foreach (var item in cloudBlobList)
                        {
                            await item.DeleteIfExistsAsync();
                        }
                        return true;
                    }
                    else
                    {
                        _logger.LogError(ErrorMessages.NoBlobContainerAvailable);
                    }
                }
                else
                {
                    _logger.LogError(ErrorMessages.NoStorageConnectionStringAvailable);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return false;
        }
    }
}
