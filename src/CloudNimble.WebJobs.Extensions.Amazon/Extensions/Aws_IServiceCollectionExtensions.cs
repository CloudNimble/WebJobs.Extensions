using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace CloudNimble.WebJobs.Extensions.AWS.Extensions
{

    /// <summary>
    /// 
    /// </summary>
    public static class Aws_IServiceCollectionExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection AddAmazonSQS(this IServiceCollection services, Action<QueuesOptionsBase> configure)
        {
            services.Configure(configure);
            services.AddSingleton<AmazonSQSClient>();
            services.AddSingleton<AmazonSQSConfig>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<QueuesOptionsBase>>().Value;
                return new AmazonSQSConfig
                {
                    //ServiceURL = options.AccountUrl
                };
            });
            return services;
        }


    }

}
