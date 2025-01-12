// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// This class defines a strategy used for processing queue messages.
    /// </summary>
    /// <remarks>
    /// Custom <see cref="QueueProcessor"/> implementations can be registered by implementing
    /// a custom <see cref="IQueueProcessorFactory"/>.
    /// </remarks>
    public class QueueProcessor
    {

        private readonly IQueueClient _queue;
        private readonly IQueueClient _poisonQueue;
        private readonly ILogger _logger;
        private readonly IQueueRequestExceptionClassifier _exceptionClassifier;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="queueProcessorOptions">The options.</param>
        /// <param name="exceptionClassifier"></param>
        internal protected QueueProcessor(QueueProcessorOptions queueProcessorOptions, IQueueRequestExceptionClassifier exceptionClassifier)
        {
            Ensure.ArgumentNotNull(queueProcessorOptions, nameof(queueProcessorOptions));

            _queue = queueProcessorOptions.Queue;
            _poisonQueue = queueProcessorOptions.PoisonQueue;
            _logger = queueProcessorOptions.Logger;

            QueuesOptions = queueProcessorOptions.Options;
            _exceptionClassifier = exceptionClassifier;
        }

        /// <summary>
        /// Event raised when a message is added to the poison queue.
        /// </summary>
        public event Func<QueueProcessor, PoisonMessageEventArgs, Task> MessageAddedToPoisonQueueAsync;

        internal QueuesOptionsBase QueuesOptions { get; private set; }

        /// <summary>
        /// This method is called when there is a new message to process, before the job function is invoked.
        /// This allows any preprocessing to take place on the message before processing begins.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use</param>
        /// <returns>True if the message processing should continue, false otherwise.</returns>
        internal protected virtual async Task<bool> BeginProcessingMessageAsync(IQueueMessage message, CancellationToken cancellationToken)
        {
            if (message.DequeueCount > QueuesOptions.MaxDequeueCount)
            {
                await HandlePoisonMessageAsync(message, cancellationToken).ConfigureAwait(false);
                return await Task.FromResult(false).ConfigureAwait(false);
            }
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        /// <summary>
        /// This method completes processing of the specified message, after the job function has been invoked.
        /// </summary>
        /// <remarks>
        /// If the message was processed successfully, the message should be deleted. If message processing failed, the
        /// message should be release back to the queue, or if the maximum dequeue count has been exceeded, the message
        /// should be moved to the poison queue (if poison queue handling is configured for the queue).
        /// </remarks>
        /// <param name="message">The message to complete processing for.</param>
        /// <param name="result">The <see cref="FunctionResult"/> from the job invocation.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns></returns>
        internal protected virtual async Task CompleteProcessingMessageAsync(IQueueMessage message, FunctionResult result, CancellationToken cancellationToken)
        {
            if (result.Succeeded)
            {
                await DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
            else if (_poisonQueue is not null)
            {
                if (message.DequeueCount >= QueuesOptions.MaxDequeueCount)
                {
                    await HandlePoisonMessageAsync(message, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ReleaseMessageAsync(message, result, QueuesOptions.VisibilityTimeout, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                // For queues without a corresponding poison queue, leave the message invisible when processing
                // fails to prevent a fast infinite loop.
                // Specifically, don't call ReleaseMessage(message)
            }
        }

        internal async Task HandlePoisonMessageAsync(IQueueMessage message, CancellationToken cancellationToken)
        {
            if (_poisonQueue is not null)
            {
                await CopyMessageToPoisonQueueAsync(message, _poisonQueue, cancellationToken).ConfigureAwait(false);
                await DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Moves the specified message to the poison queue.
        /// </summary>
        /// <param name="message">The poison message.</param>
        /// <param name="poisonQueue">The poison queue to copy the message to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns></returns>
        protected virtual async Task CopyMessageToPoisonQueueAsync(IQueueMessage message, IQueueClient poisonQueue, CancellationToken cancellationToken)
        {
            string msg = string.Format(CultureInfo.InvariantCulture, "Message has reached MaxDequeueCount of {0}. Moving message to queue '{1}'.", QueuesOptions.MaxDequeueCount, poisonQueue.Name);
            _logger?.LogWarning(msg);

            await poisonQueue.AddMessageAndCreateIfNotExistsAsync(message.Body, cancellationToken).ConfigureAwait(false);

            var eventArgs = new PoisonMessageEventArgs(message, poisonQueue);
            await OnMessageAddedToPoisonQueueAsync(eventArgs).ConfigureAwait(false);
        }

        /// <summary>
        /// Release the specified failed message back to the queue.
        /// </summary>
        /// <param name="message">The message to release</param>
        /// <param name="result">The <see cref="FunctionResult"/> from the job invocation.</param>
        /// <param name="visibilityTimeout">The visibility timeout to set for the message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns></returns>
        protected virtual async Task ReleaseMessageAsync(IQueueMessage message, FunctionResult result, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            try
            {
                // We couldn't process the message. Let someone else try.
                await _queue.UpdateMessageAsync(message.Id, message.PopReceipt, visibilityTimeout: visibilityTimeout, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (_exceptionClassifier.IsPopReceiptMismatch(exception))
                {
                    // Someone else already took over the message; no need to do anything.
                    return;
                }
                else if (_exceptionClassifier.IsNotFoundException(exception) ||
                         _exceptionClassifier.IsConflictException(exception))
                {
                    // The message or queue is gone, or the queue is down; no need to release the message.
                    return;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Delete the specified message.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns></returns>
        protected virtual async Task DeleteMessageAsync(IQueueMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await _queue.DeleteMessageAsync(message.Id, message.PopReceipt, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // For consistency, the exceptions handled here should match UpdateQueueMessageVisibilityCommand.
                if (_exceptionClassifier.IsPopReceiptMismatch(exception))
                {
                    // If someone else took over the message; let them delete it.
                    string msg = $"Unable to delete queue message '{message.Id}' because the {nameof(IQueueMessage.PopReceipt)} did not match. This could indicate that the function has modified the message and may be expected.";
                    _logger.LogDebug(msg);
                    return;
                }
                else if (_exceptionClassifier.IsNotFoundException(exception))
                {
                    string msg = $"Unable to delete queue message '{message.Id}' because either the message or the queue '{_queue.Name}' was not found.";
                    _logger.LogDebug(msg);
                }
                else if (_exceptionClassifier.IsConflictException(exception))
                {
                    // The message or queue is gone, or the queue is down; no need to delete the message.
                    string msg = $"Unable to delete queue message '{message.Id}' because the queue `{_queue.Name}` is either disabled or being deleted.";
                    _logger.LogDebug(msg);
                    return;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Called to raise the MessageAddedToPoisonQueue event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected internal virtual Task OnMessageAddedToPoisonQueueAsync(PoisonMessageEventArgs e)
        {
            return MessageAddedToPoisonQueueAsync?.Invoke(this, e) ?? Task.CompletedTask;
        }

    }

}
