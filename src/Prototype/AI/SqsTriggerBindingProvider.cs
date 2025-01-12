using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.AI
{

    /// <summary>
    /// Provides trigger binding capabilities for SQS triggers.
    /// </summary>
    public sealed class SqsTriggerBindingProvider : ITriggerBindingProvider
    {

        private readonly ILogger _logger;
        private readonly IOptions<QueuesOptions> _queuesOptions;
        private readonly IQueueProcessorFactory _processorFactory;
        private readonly IAmazonSQS _sqsClient;
        private readonly SqsOptions _sqsOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSTriggerBindingProvider"/> class.
        /// </summary>
        /// <param name="sqsClient">The Amazon SQS client to use for queue operations.</param>
        /// <param name="sqsOptions">The options for SQS configuration.</param>
        /// <param name="queueOptions">The options for queue processing.</param>
        /// <param name="processorFactory">The factory for creating queue processors.</param>
        /// <param name="logger">The logger to use for diagnostic information.</param>
        public SqsTriggerBindingProvider(
            IAmazonSQS sqsClient,
            IOptions<SqsOptions> sqsOptions,
            IOptions<QueuesOptions> queueOptions,
            IQueueProcessorFactory processorFactory,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(sqsClient);
            ArgumentNullException.ThrowIfNull(sqsOptions);
            ArgumentNullException.ThrowIfNull(queueOptions);
            ArgumentNullException.ThrowIfNull(processorFactory);
            ArgumentNullException.ThrowIfNull(logger);

            _sqsClient = sqsClient;
            _sqsOptions = sqsOptions.Value;
            _queuesOptions = queueOptions;
            _processorFactory = processorFactory;
            _logger = logger;
        }

        /// <summary>
        /// Tries to create a binding for an SQS trigger.
        /// </summary>
        /// <param name="context">The context for the binding creation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created trigger binding, or null if no binding could be created.</returns>
        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<SqsTriggerAttribute>(false);

            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            string queueUrl = ResolveQueueUrl(attribute);

            var binding = new SqsTriggerBinding(
                queueUrl,
                _sqsClient,
                parameter.ParameterType,
                _queuesOptions,
                _processorFactory,
                _logger);

            return Task.FromResult<ITriggerBinding>(binding);
        }

        /// <summary>
        /// Resolves the full queue URL from the trigger attribute.
        /// </summary>
        /// <param name="attribute">The trigger attribute containing the queue information.</param>
        /// <returns>The fully resolved queue URL.</returns>
        private string ResolveQueueUrl(SqsTriggerAttribute attribute) =>
            $"{_sqsOptions.AccountUrl}/{attribute.QueueName}";

    }

}