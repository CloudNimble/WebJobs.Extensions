// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Listeners;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Host.Executors;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{

    /// <summary>
    /// Trigger executor for SQS messages that properly implements the common interface for queue message execution.
    /// </summary>
    internal class SQSTriggerExecutor : ITriggerExecutor<IQueueMessage>
    {

        #region Private Fields

        private readonly QueueMessageCausalityManager _causalityManager;
        private readonly ITriggeredFunctionExecutor _innerExecutor;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSTriggerExecutor"/> class.
        /// </summary>
        /// <param name="innerExecutor">The inner function executor that will handle the actual function invocation.</param>
        /// <param name="causalityManager">The manager for tracking message causality and parent relationships.</param>
        /// <exception cref="ArgumentNullException">Thrown when innerExecutor is null.</exception>
        public SQSTriggerExecutor(ITriggeredFunctionExecutor innerExecutor, QueueMessageCausalityManager causalityManager)
        {
            ArgumentNullException.ThrowIfNull(innerExecutor);

            _innerExecutor = innerExecutor;
            _causalityManager = causalityManager;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the trigger function with the specified queue message.
        /// </summary>
        /// <param name="value">The queue message that triggered the function execution.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the function execution result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        /// <exception cref="ArgumentException">Thrown when value is not an SQSMessage instance.</exception>
        /// <example>
        /// <code>
        /// // This method is called automatically by the WebJobs runtime when a message is received:
        /// [FunctionName("ProcessSQSMessage")]
        /// public static async Task ProcessMessage([SQSTrigger("my-queue")] string message)
        /// {
        ///     // Function logic here
        /// }
        /// </code>
        /// </example>
        public async Task<FunctionResult> ExecuteAsync(IQueueMessage value, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new FunctionResult(false);
            }

            ArgumentNullException.ThrowIfNull(value);

            // Cast to SQSMessage since we know that's what we're working with
            if (value is not SQSMessage sqsMessage)
            {
                throw new ArgumentException($"Expected SQSMessage but received {value.GetType().Name}", nameof(value));
            }

            return await ExecuteSQSMessageAsync(sqsMessage, cancellationToken);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Internal method to handle the SQS-specific execution logic.
        /// </summary>
        /// <param name="sqsMessage">The SQS message to process.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the function execution result.</returns>
        private async Task<FunctionResult> ExecuteSQSMessageAsync(SQSMessage sqsMessage, CancellationToken cancellationToken)
        {
            var parentId = _causalityManager?.GetOwner(sqsMessage);
            var triggerDetails = new Dictionary<string, string>()
            {
                { "MessageId", sqsMessage.Id },
                { "DequeueCount", sqsMessage.DequeueCount.ToString() },
                { "InsertionTime", sqsMessage.DateInserted?.ToString(Constants.DateTimeFormatString) }
            };

            if (sqsMessage.Original.Attributes.TryGetValue("MessageGroupId", out var messageGroupId))
            {
                triggerDetails.Add("MessageGroupId", messageGroupId);
            }

            if (sqsMessage.Original.Attributes.TryGetValue("MessageDeduplicationId", out var messageDeduplicationId))
            {
                triggerDetails.Add("MessageDeduplicationId", messageDeduplicationId);
            }

            var input = new TriggeredFunctionData
            {
                ParentId = parentId,
                TriggerValue = sqsMessage,
                TriggerDetails = triggerDetails
            };

            return await _innerExecutor.TryExecuteAsync(input, cancellationToken);
        }

        #endregion

    }

}