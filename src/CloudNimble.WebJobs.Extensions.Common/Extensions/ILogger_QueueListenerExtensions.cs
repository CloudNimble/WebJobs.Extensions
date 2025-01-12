// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides logging functionality for queue listener operations.
    /// </summary>
    internal static class ILogger_QueueListenerExtensions
    {

        #region Public Methods

        /// <summary>
        /// Logs the retrieval of messages from the queue.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="functionName">The name of the function.</param>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="clientRequestId">The client request ID.</param>
        /// <param name="messageCount">The number of messages retrieved.</param>
        /// <param name="pollLatency">The latency of the poll in milliseconds.</param>
        public static void GetMessages<TQueueMessage>(this ILogger<QueueListener<TQueueMessage>> logger, string functionName, string queueName, string clientRequestId, int messageCount, long pollLatency)
            where TQueueMessage : IQueueMessage
            => _getMessages(logger, functionName, queueName, clientRequestId, messageCount, pollLatency, null);

        /// <summary>
        /// Logs the backoff delay before the next poll.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="functionName">The name of the function.</param>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="pollDelay">The delay before the next poll in milliseconds.</param>
        public static void BackoffDelay<TQueueMessage>(this ILogger<QueueListener<TQueueMessage>> logger, string functionName, string queueName, double pollDelay)
            where TQueueMessage : IQueueMessage
            => _backoffDelay(logger, functionName, pollDelay, queueName, null);

        /// <summary>
        /// Logs the handling of a storage exception during queue polling.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="functionName">The name of the function.</param>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="clientRequestId">The client request ID.</param>
        /// <param name="pollLatency">The latency of the poll in milliseconds.</param>
        /// <param name="exception">The exception that was thrown.</param>
        public static void HandlingStorageException<TQueueMessage>(this ILogger<QueueListener<TQueueMessage>> logger, string functionName, string queueName, string clientRequestId, long pollLatency, Exception exception)
            where TQueueMessage : IQueueMessage
            => _handlingStorageException(logger, functionName, queueName, clientRequestId, exception.GetType().Name, pollLatency, exception.Message, null);

        #endregion

        #region Private Methods

        private static readonly Action<ILogger, string, string, string, int, long, Exception> _getMessages =
            LoggerMessage.Define<string, string, string, int, long>(LogLevel.Debug, new EventId(1, nameof(GetMessages)),
                "Poll for function '{functionName}' on queue '{queueName}' with ClientRequestId '{clientRequestId}' found {messageCount} messages in {pollLatency} ms.");

        private static readonly Action<ILogger, string, double, string, Exception> _backoffDelay =
            LoggerMessage.Define<string, double, string>(LogLevel.Debug, new EventId(2, nameof(BackoffDelay)),
                "Function '{functionName}' will wait {pollDelay} ms before polling queue '{queueName}'.");

        private static readonly Action<ILogger, string, string, string, string, long, string, Exception> _handlingStorageException =
           LoggerMessage.Define<string, string, string, string, long, string>(LogLevel.Debug, new EventId(3, nameof(HandlingStorageException)),
               "Poll for function '{functionName}' on queue '{queueName}' with ClientRequestId '{clientRequestId}' threw a {exceptionType} in {pollLatency} ms. " +
               "This exception is handled and queue polling will resume after a delay. Message: '{exceptionMessage}'");

        #endregion

    }

}
