Prerequisites

You should have a valid Azure Subscription

You should have a running Azure Function

If you are not sure about how to create an Azure Function App, 

You should have a valid Azure Storage Account

Using the code

Configure the Dependency Injection in Azure Function
As we are going to inject our dependency via constructor we need to configure the same by creating a Startup.cs class in our solution. Let’s do that first. To configure, make sure that you had installed the Nuget Package Microsoft.Azure.Functions.Extensions.

Now create a new class and name it as Startup.cs and write the code as preceding.

using Microsoft.Azure.Functions.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

using Backup.Functions.Interfaces;

using Backup.Functions.Services;

[assembly: FunctionsStartup(typeof(Backup.Functions.Startup))]
namespace Backup.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IBlobService, BlobService>();
        }
    }
}

Here we are adding a singleton service for our IBlobService. Do not forget to inherit your Startup class from FunctionsStartup. But we have to create azure function first for timer trigger



Write the Azure Function in the portal

Delete.cs

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Backup.Functions.Helpers;
using Backup.Functions.Interfaces;
using System.Threading.Tasks;

namespace Backup.Functions
{
    public class DeleteDailyBlobs
    {
        private readonly IBlobService _blobService;
        public DeleteDailyBlobs(IBlobService blobService)
        {
            _blobService = blobService;
        }
        
        [FunctionName("DeleteDailyBlobs")]
        public async Task Run([TimerTrigger("0 0 4 * * 1")]TimerInfo myTimer, ILogger log)
        {
            if (await _blobService.PerformTasks(BlobContainers.Daily))
            {
                log.LogInformation(SuccessMessages.FunctionExecutedSuccessfully);
            }
            else
            {
                log.LogError(ErrorMessages.SomethingBadHappened);
            }
        }
    }
}

Here we are making the Function to run on every Monday at 4 AM using the CRON expression. Make sure to check my previous post to see more about the CRON expression.

Below are the blob container names I have in my Azure Blob Storage.

Blob.cs

public static class BlobContainers
{
   public static readonly string Daily = "daily";
   public static readonly string Weekly = "weekly";
   public static readonly string Monthly = "monthly";
}

New interface for our service

public interface IBlobService
{
 Task<bool> PerformTasks(string containerName);
}



And then create a service BlobService.cs

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

Here you can see that in the PerformTasks function we are getting the blob container reference and then get all the blobs using ListBlobsSegmentedAsync and then cast it as ICloudBlob so that we can easily delete the blobs.

Make sure to add the AzureWebJobsStorage in your local.settings.json file and in the Azure Function Configuration in the portal.

we have learned,

about Azure Function and setting up the same

about Time Trigger in Azure Function

about CRON expressions in Azure Function

how to set up dependency injection in Azure Function

how to delete Azure blobs from Azure Blob Containers using Azure Function






