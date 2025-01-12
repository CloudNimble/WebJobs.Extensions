using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.AWS;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.AI
{

    /// <summary>
    /// Provides binding capabilities for SQS triggers in WebJobs.
    /// </summary>
    public sealed class SqsTriggerBinding : ITriggerBinding
    {

        private readonly Type _parameterType;
        private readonly ILogger _logger;
        private readonly IQueueProcessorFactory _processorFactory;
        private readonly QueuesOptions _queuesOptions;
        private readonly string _queueUrl;
        private readonly IAmazonSQS _sqsClient;

        /// <summary>
        /// Gets the contract for binding data.
        /// </summary>
        public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>();

        /// <summary>
        /// Gets the type of the trigger value.
        /// </summary>
        public Type TriggerValueType => typeof(SQSMessage);

        /// <summary>
        /// Initializes a new instance of the <see cref="SqsTriggerBinding"/> class.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to bind to.</param>
        /// <param name="sqsClient">The Amazon SQS client to use for queue operations.</param>
        /// <param name="parameterType">The type of the parameter to bind to.</param>
        /// <param name="queuesOptions">The options for queue processing.</param>
        /// <param name="processorFactory">The factory for creating queue processors.</param>
        /// <param name="logger">The logger to use for diagnostic information.</param>
        public SqsTriggerBinding(
            string queueUrl,
            IAmazonSQS sqsClient,
            Type parameterType,
            IOptions<QueuesOptions> queuesOptions,
            IQueueProcessorFactory processorFactory,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(queueUrl);
            ArgumentNullException.ThrowIfNull(sqsClient);
            ArgumentNullException.ThrowIfNull(parameterType);
            ArgumentNullException.ThrowIfNull(queuesOptions);
            ArgumentNullException.ThrowIfNull(processorFactory);
            ArgumentNullException.ThrowIfNull(logger);

            _queueUrl = queueUrl;
            _sqsClient = sqsClient;
            _parameterType = parameterType;
            _queuesOptions = queuesOptions.Value;
            _processorFactory = processorFactory;
            _logger = logger;
        }

        /// <summary>
        /// Binds the trigger value to the function parameter.
        /// </summary>
        /// <param name="value">The trigger value to bind.</param>
        /// <param name="context">The context for the binding operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the trigger data.</returns>
        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            ArgumentNullException.ThrowIfNull(value);

            var message = value as SQSMessage;
            var bindingData = new Dictionary<string, object>();
            var valueProvider = new SqsValueProvider(message);

            return Task.FromResult<ITriggerData>(new TriggerData(valueProvider, bindingData));
        }

        /// <summary>
        /// Creates a listener for the SQS trigger.
        /// </summary>
        /// <param name="context">The context for creating the listener.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created listener.</returns>
        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var processor = new SqsQueueProcessor(_sqsClient, _processorFactory, _queuesOptions, _logger);
            var listener = new SqsListener(_queueUrl, _sqsClient, context.Executor, processor, _queuesOptions, _logger);

            return Task.FromResult<IListener>(listener);
        }

        /// <summary>
        /// Creates a parameter descriptor for the SQS trigger.
        /// </summary>
        /// <returns>A parameter descriptor for the SQS trigger.</returns>
        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor
            {
                Name = "SqsTrigger",
                DisplayHints = new ParameterDisplayHints
                {
                    Description = $"SQS trigger fired from queue: {_queueUrl}",
                    Prompt = "SQS Message"
                }
            };
        }

    }

}