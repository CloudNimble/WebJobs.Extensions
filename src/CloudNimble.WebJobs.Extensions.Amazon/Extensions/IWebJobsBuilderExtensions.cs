using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Config;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel;
using System.Linq;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Azure.WebJobs
#pragma warning restore IDE0130 // Namespace does not match folder structure
{

    /// <summary>
    /// Extension methods for Amazon SQS integration.
    /// </summary>
    public static class IWebJobsBuilderExtensions
    {

        /// <summary>
        /// Adds the Amazon SQS extension to the provided <see cref="IWebJobsBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
        /// <param name="configureQueues">Optional. An action to configure <see cref="QueuesOptionsBase"/>.</param>
        public static IWebJobsBuilder AddAmazonSQS(this IWebJobsBuilder builder, Action<QueuesOptionsBase> configureQueues = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // RWM: Add basic Amazon SQS support here


            // RWM: Map the WebJobs Amazon SQS support the same way Azure Storage Queues are mapped
            builder.Services.TryAddSingleton<SharedQueueWatcher>();

            builder.Services.TryAddSingleton<QueueMessageCausalityManager>();

            builder.Services.TryAddSingleton<IContextSetter<IMessageEnqueuedWatcher>>((p) => new ContextAccessor<IMessageEnqueuedWatcher>());
            builder.Services.TryAddSingleton((p) => p.GetService<IContextSetter<IMessageEnqueuedWatcher>>() as IContextGetter<IMessageEnqueuedWatcher>);

            builder.Services.TryAddSingleton<SQSTriggerAttributeBindingProvider>();

            builder.AddExtension<SQSExtensionConfigProvider>()
                .BindOptions<QueuesOptionsBase>();
            if (configureQueues != null)
            {
                builder.Services.Configure<QueuesOptionsBase>(configureQueues);
            }

            builder.Services.TryAddSingleton<IQueueProcessorFactory, DefaultQueueProcessorFactory>();

            builder.Services.AddOptions<QueuesOptionsBase>()
                .Configure<IHostingEnvironment>((options, env) =>
                {
                    if (env.IsDevelopment() && options.MaxPollingInterval == QueuePollingIntervals.DefaultMaximum)
                    {
                        options.MaxPollingInterval = TimeSpan.FromSeconds(2);
                    }
                });

            //RWM: Now add the Amazon SQS-specific implementations
            builder.Services.TryAddSingleton<IQueueRequestExceptionClassifier, SQSRequestExceptionClassifier>();

            return builder;

        }

        /// <summary>
        /// Adds the Storage Queues extension to the provided <see cref="IWebJobsBuilder"/>.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="triggerMetadata">Trigger metadata.</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IWebJobsBuilder AddAzureStorageQueuesScaleForTrigger(this IWebJobsBuilder builder, TriggerMetadata triggerMetadata)
        {
            // We need to register an instance of QueueScalerProvider in the DI container and then map it to the interfaces IScaleMonitorProvider and ITargetScalerProvider.
            // Since there can be more than one instance of QueueScalerProvider, we have to store a reference to the created instance to filter it out later.
            SQSScalerProvider queueScalerProvider = null;
            builder.Services.AddSingleton(sp =>
            {
                queueScalerProvider = new SQSScalerProvider(triggerMetadata, sp.GetRequiredService<IQueueRequestExceptionClassifier>(), sp.GetRequiredService<IOptionsMonitor<QueuesOptionsBase>>());
                return queueScalerProvider;
            });
            builder.Services.AddSingleton<IScaleMonitorProvider>(sp => sp.GetServices<SQSScalerProvider>().Single(x => x == queueScalerProvider));
            builder.Services.AddSingleton<ITargetScalerProvider>(serviceProvider => serviceProvider.GetServices<SQSScalerProvider>().Single(x => x == queueScalerProvider));

            return builder;
        }

    }

}