using Amazon;
using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Config;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Azure.WebJobs.Host.Timers;
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
        /// <param name="configureQueues">Optional action to configure <see cref="SQSOptions"/> for queue processing behavior and AWS settings.</param>
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
        ///     options.Region = "us-east-1";
        ///     options.ServiceUrl = "http://localhost:4566"; // For LocalStack
        ///     options.UseFifo = true;
        ///     options.MessageGroupId = "my-group";
        /// });
        /// </code>
        /// </example>
        public static IWebJobsBuilder AddAmazonSQS(this IWebJobsBuilder builder, Action<SQSOptions> configureQueues = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Register SQSOptions as the primary configuration type
            if (configureQueues is not null)
            {
                builder.Services.Configure<SQSOptions>(configureQueues);
            }

           // Register both SQSOptions and QueuesOptionsBase to return the same instance
            // This allows existing code that depends on QueuesOptionsBase to continue working
            // while new code can use the enhanced SQSOptions
            builder.Services.TryAddSingleton<IOptions<SQSOptions>>(serviceProvider =>
            {
                var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<SQSOptions>>();
                return Options.Create(monitor.CurrentValue);
            });
            
            builder.Services.TryAddSingleton(serviceProvider =>
            {
                var sqsOptions = serviceProvider.GetRequiredService<IOptions<SQSOptions>>();
                return Options.Create<QueuesOptionsBase>(sqsOptions.Value);
            });

            builder.Services.TryAddSingleton<IOptionsMonitor<QueuesOptionsBase>>(serviceProvider =>
            {
                var sqsOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<SQSOptions>>();
                return new OptionsMonitorWrapper<SQSOptions, QueuesOptionsBase>(sqsOptionsMonitor);
            });

            // Register AWS SQS Client with enhanced configuration support
            builder.Services.TryAddSingleton<IAmazonSQS>(serviceProvider =>
            {
                var configuration = serviceProvider.GetService<IConfiguration>();
                var sqsOptions = serviceProvider.GetService<IOptions<SQSOptions>>()?.Value;
                var config = new AmazonSQSConfig();

                // Prioritize SQSOptions settings, fall back to configuration, then defaults
                var serviceUrl = sqsOptions?.ServiceUrl ?? configuration?.GetValue<string>("AWS:SQS:ServiceURL");
                if (!string.IsNullOrWhiteSpace(serviceUrl))
                {
                    config.ServiceURL = serviceUrl;
                    config.UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
                }

                // Configure region from options or configuration
                var region = sqsOptions?.Region ?? configuration?.GetValue<string>("AWS:Region");
                if (!string.IsNullOrWhiteSpace(region))
                {
                    config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
                }

                // Create client with explicit credentials if provided
                if (!string.IsNullOrWhiteSpace(sqsOptions?.AccessKey) && !string.IsNullOrWhiteSpace(sqsOptions?.SecretKey))
                {
                    return new AmazonSQSClient(sqsOptions.AccessKey, sqsOptions.SecretKey, config);
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
            builder.Services.TryAddSingleton(serviceProvider =>
                new SQSTriggerAttributeBindingProvider(
                    serviceProvider.GetService<INameResolver>(),
                    serviceProvider.GetRequiredService<IOptions<QueuesOptionsBase>>(),
                    serviceProvider.GetRequiredService<IWebJobsExceptionHandler>(),
                    serviceProvider.GetRequiredService<SharedQueueWatcher>(),
                    serviceProvider.GetRequiredService<ILoggerFactory>(),
                    serviceProvider.GetRequiredService<IQueueProcessorFactory>(),
                    serviceProvider.GetRequiredService<QueueMessageCausalityManager>(),
                    serviceProvider.GetRequiredService<IQueueRequestExceptionClassifier>(),
                    serviceProvider.GetRequiredService<ConcurrencyManager>(),
                    serviceProvider.GetRequiredService<IDrainModeManager>(),
                    serviceProvider.GetRequiredService<IAmazonSQS>(),
                    serviceProvider.GetRequiredService<IOptions<SQSOptions>>()));
            builder.Services.TryAddSingleton<IQueueRequestExceptionClassifier, SQSRequestExceptionClassifier>();
            builder.Services.TryAddSingleton<IQueueProcessorFactory, DefaultQueueProcessorFactory>();

            // Register the extension config provider
            builder.AddExtension<SQSExtensionConfigProvider>()
                .BindOptions<SQSOptions>();

            // Configure development-friendly defaults
            builder.Services.AddOptions<SQSOptions>()
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
        /// Adds the Amazon SQS extension with base queue options configuration for backward compatibility.
        /// This overload allows configuration using the base <see cref="QueuesOptionsBase"/> type.
        /// </summary>
        /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
        /// <param name="configureQueues">Action to configure <see cref="QueuesOptionsBase"/> for queue processing behavior.</param>
        /// <returns>The <see cref="IWebJobsBuilder"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        /// <remarks>
        /// This overload provides backward compatibility for existing code that uses <see cref="QueuesOptionsBase"/>.
        /// For new implementations, consider using the overload that accepts <see cref="SQSOptions"/> to access
        /// SQS-specific configuration options.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Backward compatibility usage
        /// builder.AddAmazonSQS((QueuesOptionsBase options) =>
        /// {
        ///     options.BatchSize = 10;
        ///     options.MaxDequeueCount = 3;
        /// });
        /// </code>
        /// </example>
        public static IWebJobsBuilder AddAmazonSQS(this IWebJobsBuilder builder, Action<QueuesOptionsBase> configureQueues)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configureQueues);

            return builder.AddAmazonSQS((SQSOptions options) => configureQueues(options));
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

    /// <summary>
    /// Wrapper class that adapts an IOptionsMonitor of one type to another compatible type.
    /// This enables the DI container to provide the same underlying options instance
    /// for both SQSOptions and QueuesOptionsBase registrations.
    /// </summary>
    /// <typeparam name="TSource">The source options type.</typeparam>
    /// <typeparam name="TTarget">The target options type that TSource must be assignable to.</typeparam>
    internal class OptionsMonitorWrapper<TSource, TTarget> : IOptionsMonitor<TTarget>
        where TSource : class, TTarget
        where TTarget : class
    {
        private readonly IOptionsMonitor<TSource> _sourceMonitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsMonitorWrapper{TSource, TTarget}"/> class.
        /// </summary>
        /// <param name="sourceMonitor">The source options monitor to wrap.</param>
        public OptionsMonitorWrapper(IOptionsMonitor<TSource> sourceMonitor)
        {
            _sourceMonitor = sourceMonitor;
        }

        /// <summary>
        /// Gets the current options value.
        /// </summary>
        public TTarget CurrentValue => _sourceMonitor.CurrentValue;

        /// <summary>
        /// Gets the named options value.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <returns>The options value.</returns>
        public TTarget Get(string name) => _sourceMonitor.Get(name);

        /// <summary>
        /// Registers a listener to be called whenever a named TOptions changes.
        /// </summary>
        /// <param name="listener">The action to be invoked when TOptions has changed.</param>
        /// <returns>An IDisposable which should be disposed to stop listening for changes.</returns>
        public IDisposable OnChange(Action<TTarget, string> listener)
        {
            return _sourceMonitor.OnChange((source, name) => listener(source, name));
        }
    }

}