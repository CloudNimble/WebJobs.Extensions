using Amazon;
using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Config;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel;
using System.Linq;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Azure.WebJobs
#pragma warning restore IDE0130 // Namespace does not match folder structure
{

    /// <summary>
    /// Extension methods for Amazon SQS integration with Azure WebJobs SDK.
    /// Provides methods to configure and register Amazon SQS services for queue processing.
    /// </summary>
    public static class Aws_IWebJobsBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Adds the Amazon SQS extension to the provided <see cref="IWebJobsBuilder"/>.
        /// This method registers all necessary services for SQS queue processing including triggers, bindings, and scaling.
        /// </summary>
        /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
        /// <param name="configureQueues">Optional action to configure <see cref="QueuesOptionsBase"/> for queue processing behavior.</param>
        /// <returns>The <see cref="IWebJobsBuilder"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <example>
        /// <code>
        /// // Basic registration
        /// builder.AddAmazonSQS();
        /// 
        /// // With custom configuration
        /// builder.AddAmazonSQS(options =>
        /// {
        ///     options.BatchSize = 10;
        ///     options.MaxDequeueCount = 3;
        ///     options.VisibilityTimeout = TimeSpan.FromMinutes(5);
        /// });
        /// </code>
        /// </example>
        public static IWebJobsBuilder AddAmazonSQS(this IWebJobsBuilder builder, Action<QueuesOptionsBase> configureQueues = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Register AWS SQS Client with configuration support
            builder.Services.TryAddSingleton<IAmazonSQS>(serviceProvider =>
            {
                var configuration = serviceProvider.GetService<IConfiguration>();
                var config = new AmazonSQSConfig();

                // Check for LocalStack or custom endpoint
                var endpoint = configuration?.GetValue<string>("AWS:SQS:ServiceURL");
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    config.ServiceURL = endpoint;
                    config.UseHttp = endpoint.StartsWith("http://");
                }

                // Allow region override
                var region = configuration?.GetValue<string>("AWS:Region");
                if (!string.IsNullOrWhiteSpace(region))
                {
                    config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
                }

                return new AmazonSQSClient(config);
            });

            // Register shared queue watcher for cross-queue notifications
            builder.Services.TryAddSingleton<SharedQueueWatcher>();

            // Register causality manager for tracking message relationships
            builder.Services.TryAddSingleton<QueueMessageCausalityManager>();

            // Register context accessors for message enqueue watching
            builder.Services.TryAddSingleton<IContextSetter<IMessageEnqueuedWatcher>>(p =>
                new ContextAccessor<IMessageEnqueuedWatcher>());
            builder.Services.TryAddSingleton(p =>
                p.GetService<IContextSetter<IMessageEnqueuedWatcher>>() as IContextGetter<IMessageEnqueuedWatcher>);

            // Register SQS-specific services
            builder.Services.TryAddSingleton<SQSTriggerAttributeBindingProvider>();
            builder.Services.TryAddSingleton<IQueueRequestExceptionClassifier, SQSRequestExceptionClassifier>();
            builder.Services.TryAddSingleton<IQueueProcessorFactory, DefaultQueueProcessorFactory>();

            // Register the extension config provider
            builder.AddExtension<SQSExtensionConfigProvider>()
                .BindOptions<QueuesOptionsBase>();

            if (configureQueues is not null)
            {
                builder.Services.Configure<QueuesOptionsBase>(configureQueues);
            }

            // Configure development-friendly defaults
            builder.Services.AddOptions<QueuesOptionsBase>()
                .Configure<IHostingEnvironment>((options, env) =>
                {
                    if (env.IsDevelopment() && options.MaxPollingInterval == QueuePollingIntervals.DefaultMaximum)
                    {
                        options.MaxPollingInterval = TimeSpan.FromSeconds(2);
                    }
                });

            return builder;
        }

        /// <summary>
        /// Adds Amazon SQS scaling support for a specific trigger.
        /// This method registers scale monitoring and target scaling capabilities for the specified trigger.
        /// </summary>
        /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
        /// <param name="triggerMetadata">The metadata for the trigger that needs scaling support.</param>
        /// <returns>The <see cref="IWebJobsBuilder"/> for method chaining.</returns>
        /// <remarks>
        /// This method is typically called internally by the WebJobs runtime during function discovery.
        /// It registers scaling providers that monitor queue length and determine appropriate instance counts.
        /// </remarks>
        /// <example>
        /// <code>
        /// // This is typically called automatically by the runtime, but can be used manually:
        /// var triggerMetadata = new TriggerMetadata { FunctionName = "ProcessSQSMessages" };
        /// builder.AddAmazonSQSScaleForTrigger(triggerMetadata);
        /// </code>
        /// </example>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IWebJobsBuilder AddAmazonSQSScaleForTrigger(this IWebJobsBuilder builder, TriggerMetadata triggerMetadata)
        {
            // We need to register an instance of SQSScalerProvider in the DI container and then map it to the interfaces IScaleMonitorProvider and ITargetScalerProvider.
            // Since there can be more than one instance of SQSScalerProvider, we have to store a reference to the created instance to filter it out later.
            SQSScalerProvider queueScalerProvider = null;
            builder.Services.AddSingleton(sp =>
            {
                queueScalerProvider = new SQSScalerProvider(
                    triggerMetadata,
                    sp.GetRequiredService<IQueueRequestExceptionClassifier>(),
                    sp.GetRequiredService<IQueueClient>(),
                    sp.GetRequiredService<IOptionsMonitor<QueuesOptionsBase>>(),
                    sp.GetRequiredService<ILoggerFactory>());
                return queueScalerProvider;
            });
            builder.Services.AddSingleton<IScaleMonitorProvider>(sp => sp.GetServices<SQSScalerProvider>().Single(x => x == queueScalerProvider));
            builder.Services.AddSingleton<ITargetScalerProvider>(serviceProvider => serviceProvider.GetServices<SQSScalerProvider>().Single(x => x == queueScalerProvider));

            return builder;
        }

        #endregion

    }

}