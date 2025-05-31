using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloudNimble.WebJobs.Extensions.Examples
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureWebJobs(webJobsBuilder =>
                {
                    //webJobsBuilder.AddAzureStorageCoreServices();
                    webJobsBuilder.AddAmazonSQS();
                    // Add other extensions if needed
                })
                .ConfigureLogging((context, loggingBuilder) =>
                {
                    //loggingBuilder.AddConsole();
                })
                .Build();

            using (host)
            {
                host.Run();
            }
        }
    }
}
