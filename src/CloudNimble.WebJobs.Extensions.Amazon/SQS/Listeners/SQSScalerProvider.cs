// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners
{

    /// <summary>
    /// Provides scaling capabilities for SQS queues.
    /// </summary>
    internal class SQSScalerProvider : IScaleMonitorProvider, ITargetScalerProvider
    {
        private readonly TriggerMetadata _triggerMetadata;
        private readonly IOptionsMonitor<QueuesOptionsBase> _queuesOptions;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IQueueClient _queueClient;
        private readonly IQueueRequestExceptionClassifier _exceptionClassifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSScalerProvider"/> class.
        /// </summary>
        /// <param name="triggerMetadata">The trigger metadata.</param>
        /// <param name="exceptionClassifier">The exception classifier.</param>
        /// <param name="queueClient"></param>
        /// <param name="queuesOptions">The queue options.</param>
        /// <param name="loggerFactory"></param>
        public SQSScalerProvider(
            TriggerMetadata triggerMetadata, 
            IQueueRequestExceptionClassifier exceptionClassifier,
            IQueueClient queueClient,
            IOptionsMonitor<QueuesOptionsBase> queuesOptions,
            ILoggerFactory loggerFactory)
        {
            _triggerMetadata = triggerMetadata;
            _exceptionClassifier = exceptionClassifier;
            _queueClient = queueClient;
            _queuesOptions = queuesOptions;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Gets the scale monitor.
        /// </summary>
        /// <returns>The scale monitor.</returns>
        public IScaleMonitor GetMonitor()
        {
            return new QueueScaleMonitor(
                _triggerMetadata.FunctionName,
                _queueClient,
                _exceptionClassifier,
                _loggerFactory);
        }

        /// <summary>
        /// Gets the target scaler.
        /// </summary>
        /// <returns>The target scaler.</returns>
        public ITargetScaler GetTargetScaler()
        {
            return new QueueTargetScaler(
                _triggerMetadata.FunctionName,
                _queueClient,
                _queuesOptions.CurrentValue,
                _exceptionClassifier,
                _loggerFactory);
        }

        /// <summary>
        /// Represents metadata for a queue.
        /// </summary>
        internal class QueueMetadata
        {
            /// <summary>
            /// Gets or sets the connection string.
            /// </summary>
            [JsonProperty]
            public string Connection { get; set; }

            /// <summary>
            /// Gets or sets the queue name.
            /// </summary>
            [JsonProperty]
            public string QueueName { get; set; }

            /// <summary>
            /// Resolves properties using the specified name resolver.
            /// </summary>
            /// <param name="resolver">The name resolver.</param>
            public void ResolveProperties(INameResolver resolver)
            {
                if (resolver is not null)
                {
                    QueueName = resolver.ResolveWholeString(QueueName);
                }
            }
        }

    }

}
