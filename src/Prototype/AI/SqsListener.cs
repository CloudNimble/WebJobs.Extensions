using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Config;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.AI
{

    /// <summary>
    /// Provides a listener for Amazon SQS queues that integrates with the WebJobs SDK.
    /// </summary>
    public sealed class SqsListener : IListener
    {

        private readonly ITriggeredFunctionExecutor _executor;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _maxConcurrencySemaphore;
        private readonly SQSOptions _queuesOptions;
        private readonly string _queueUrl;
        private readonly SqsQueueProcessor _queueProcessor;
        private readonly IAmazonSQS _sqsClient;
        private Task _executingTask;
        private CancellationTokenSource _stoppingCts;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqsListener"/> class.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to monitor.</param>
        /// <param name="sqsClient">The Amazon SQS client to use for queue operations.</param>
        /// <param name="executor">The executor for triggered functions.</param>
        /// <param name="queueProcessor">The processor for handling queue messages.</param>
        /// <param name="queuesOptions">The options for queue processing.</param>
        /// <param name="logger">The logger to use for diagnostic information.</param>
        public SqsListener(
            string queueUrl,
            IAmazonSQS sqsClient,
            ITriggeredFunctionExecutor executor,
            SqsQueueProcessor queueProcessor,
            IOptions<SQSOptions> queuesOptions,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(queueUrl);
            ArgumentNullException.ThrowIfNull(sqsClient);
            ArgumentNullException.ThrowIfNull(executor);
            ArgumentNullException.ThrowIfNull(queueProcessor);
            ArgumentNullException.ThrowIfNull(queuesOptions);
            ArgumentNullException.ThrowIfNull(logger);

            _queueUrl = queueUrl;
            _sqsClient = sqsClient;
            _executor = executor;
            _queueProcessor = queueProcessor;
            _queuesOptions = queuesOptions.Value;
            _logger = logger;
            _maxConcurrencySemaphore = new(_queuesOptions.BatchSize);
        }

        /// <summary>
        /// Cancels the listener operation.
        /// </summary>
        public void Cancel() => _stoppingCts?.Cancel();

        /// <summary>
        /// Releases the resources used by the listener.
        /// </summary>
        public void Dispose()
        {
            _stoppingCts?.Cancel();
            _stoppingCts?.Dispose();
            _maxConcurrencySemaphore?.Dispose();
        }

        /// <summary>
        /// Starts the listener operation.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = PollQueue(_stoppingCts.Token);
            return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        /// <summary>
        /// Stops the listener operation.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask is null) return;

            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }

        /// <summary>
        /// Continuously polls the SQS queue for messages.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task PollQueue(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MaxNumberOfMessages = _queuesOptions.BatchSize,
                        WaitTimeSeconds = (int)_queuesOptions.MaxPollingInterval.TotalSeconds,
                        VisibilityTimeout = (int)_queuesOptions.VisibilityTimeout.TotalSeconds,
                        MessageAttributeNames = ["All"]
                    };

                    var response = await _sqsClient.ReceiveMessageAsync(request, cancellationToken);

                    var processingTasks = response.Messages.Select(async message =>
                    {
                        await _maxConcurrencySemaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var sqsMessage = new SQSMessage(message, _queueUrl);

                            // Directly execute the function without Azure-specific queue processor
                            var result = await _executor.TryExecuteAsync(
                                new TriggeredFunctionData { TriggerValue = sqsMessage },
                                cancellationToken);

                            // Always delete message after successful processing
                            if (result.Succeeded)
                            {
                                await _sqsClient.DeleteMessageAsync(_queueUrl, sqsMessage.PopReceipt, cancellationToken);
                            }
                            else
                            {
                                // Handle message failure, e.g., move to poison queue
                                await _sqsClient.MoveToPoisonQueueAsync(
                                    message, _queueUrl, result.Exception, _logger, cancellationToken);
                            }
                        }
                        finally
                        {
                            _maxConcurrencySemaphore.Release();
                        }
                    });

                    await Task.WhenAll(processingTasks);

                    if (response.Messages.Count == 0)
                    {
                        await Task.Delay(_queuesOptions.MaxPollingInterval, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling SQS queue {QueueUrl}", _queueUrl);
                    await Task.Delay(_queuesOptions.MaxPollingInterval, cancellationToken);
                }
            }
        }

    }

}