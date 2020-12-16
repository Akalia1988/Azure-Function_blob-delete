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