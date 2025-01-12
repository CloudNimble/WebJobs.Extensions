// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Common.Timers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common
{

    /// <summary>
    /// Represents a command to update the visibility timeout of a queue message.
    /// </summary>
    public class UpdateQueueMessageVisibilityCommand<TQueueMessage> : ITaskSeriesCommand
        where TQueueMessage : IQueueMessage
    {
        private readonly IQueueClient<TQueueMessage> _queue;
        private volatile IQueueMessage _message;
        private readonly TimeSpan _visibilityTimeout;
        private readonly IQueueRequestExceptionClassifier _classifier;
        private readonly IDelayStrategy _speedupStrategy;
        private readonly Action<IQueueMessage, QueueMessageUpdateReceipt> _onUpdateReceipt;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateQueueMessageVisibilityCommand{TQueueMessage}"/> class.
        /// </summary>
        /// <param name="queue">The queue client to interact with the queue.</param>
        /// <param name="message">The queue message whose visibility timeout is to be updated.</param>
        /// <param name="visibilityTimeout">The new visibility timeout for the message.</param>
        /// <param name="classifier"></param>
        /// <param name="speedupStrategy">The strategy to determine the delay before the next execution attempt.</param>
        /// <param name="onUpdateReceipt">The action to perform when the message update receipt is received.</param>
        public UpdateQueueMessageVisibilityCommand(IQueueClient<TQueueMessage> queue, IQueueMessage message,
            TimeSpan visibilityTimeout, IQueueRequestExceptionClassifier classifier, IDelayStrategy speedupStrategy, 
            Action<IQueueMessage, QueueMessageUpdateReceipt> onUpdateReceipt)
        {
            Ensure.ArgumentNotNull(queue, nameof(queue));
            Ensure.ArgumentNotNull(message, nameof(message));
            Ensure.ArgumentNotNull(speedupStrategy, nameof(speedupStrategy));

            _queue = queue;
            _message = message;
            _visibilityTimeout = visibilityTimeout;
            _classifier = classifier;
            _speedupStrategy = speedupStrategy;
            _onUpdateReceipt = onUpdateReceipt;
        }

        /// <summary>
        /// Executes the command to update the visibility timeout of the queue message.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of the command execution.</returns>
        public async Task<TaskSeriesCommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            TimeSpan delay;

            try
            {
                var updateReceipt = await _queue.UpdateMessageAsync(_message.Id, _message.PopReceipt, _visibilityTimeout, cancellationToken).ConfigureAwait(false);
                _onUpdateReceipt?.Invoke(_message, updateReceipt);
                // The next execution should occur after a normal delay.
                delay = _speedupStrategy.GetNextDelay(true);
            }
            catch (Exception ex)
            {
                // For consistency, the exceptions handled here should match PollQueueCommand.DeleteMessageAsync.
                if (_classifier.IsServerSideException(ex))
                {
                    // The next execution should occur more quickly (try to update the visibility before it expires).
                    delay = _speedupStrategy.GetNextDelay(false);
                }
                else if (_classifier.IsPopReceiptMismatch(ex) || _classifier.IsNotFoundException(ex) || _classifier.IsConflictException(ex))
                {
                    // There's no point to executing again. Once the pop receipt doesn't match, we've permanently lost ownership.
                    // For queue disabled, in theory it's possible the queue could be re-enabled, but the scenarios here
                    // are currently unclear.
                    delay = Timeout.InfiniteTimeSpan;
                }
                else
                {
                    throw;
                }
            }

            return new TaskSeriesCommandResult(Task.Delay(delay, cancellationToken));
        }

    }
}
