using Amazon.SQS;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.AI
{

    /// <summary>
    /// Processes messages from an SQS queue using WebJobs queue processing patterns.
    /// </summary>
    public sealed class SqsQueueProcessor : IQueueProcessorFactory
    {

        private readonly ILogger _logger;
        private readonly QueuesOptions _queueOptions;
        private readonly IAmazonSQS _sqsClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqsQueueProcessor"/> class.
        /// </summary>
        /// <param name="sqsClient">The Amazon SQS client to use for queue operations.</param>
        /// <param name="queueOptions">The options for queue processing.</param>
        /// <param name="logger">The logger to use for diagnostic information.</param>
        public SqsQueueProcessor(
            IAmazonSQS sqsClient,
            IOptions<QueuesOptions> queueOptions,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(sqsClient);
            ArgumentNullException.ThrowIfNull(queueOptions);
            ArgumentNullException.ThrowIfNull(logger);

            _sqsClient = sqsClient;
            _queueOptions = queueOptions.Value;
            _logger = logger;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueProcessorOptions"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public QueueProcessor Create(QueueProcessorOptions queueProcessorOptions)
        {
            return new QueueProcessor(queueProcessorOptions);
        }

        /// <summary>
        /// Processes a message from the queue using the specified processing function.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <param name="processAsync">The function to process the message.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public async Task ProcessMessageAsync(
            SQSMessage message,
            Func<SQSMessage, Task> processAsync,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(processAsync);

            var queueProcessor = _queueProcessorFactory.Create(message.QueueUrl);
            var context = new QueueProcessorContext
            {
                Queue = message.QueueUrl,
                Message = message,
                DequeueCount = message.DequeueCount,
                MaxDequeueCount = _queueOptions.MaxDequeueCount
            };

            try
            {
                await queueProcessor.BeginProcessingMessageAsync(context, cancellationToken);

                try
                {
                    await processAsync(message);
                    await queueProcessor.CompleteProcessingMessageAsync(context, null, cancellationToken);
                }
                catch (Exception ex)
                {
                    await queueProcessor.CompleteProcessingMessageAsync(context, ex, cancellationToken);
                    throw;
                }
            }
            catch (Exception ex) when (context.DequeueCount >= _queueOptions.MaxDequeueCount)
            {
                await _sqsClient.MoveToPoisonQueueAsync(message.Original, message.QueueUrl, ex, _logger, cancellationToken);
                throw;
            }
        }

    }

}