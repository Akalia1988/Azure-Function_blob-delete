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