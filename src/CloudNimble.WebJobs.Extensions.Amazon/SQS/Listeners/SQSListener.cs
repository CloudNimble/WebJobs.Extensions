// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners
{

    /// <summary>
    /// Listener implementation for Amazon SQS queues that extends the common queue listener functionality.
    /// </summary>
    internal sealed partial class SQSListener : QueueListener, IListener
    {

        #region Private Fields

        /// <summary>
        /// SQS exception error codes that should be treated as fatal and stop the poller.
        /// These are the subset of SQS errors that indicate configuration or permission issues
        /// rather than transient service problems.
        /// </summary>
        /// <remarks>
        /// These are the subset of https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_ReceiveMessage.html#API_ReceiveMessage_Errors
        /// that aren't modeled as typed exceptions and indicate fatal configuration issues.
        /// </remarks>
        private static readonly HashSet<string> _fatalSQSErrorCodes =
        [
            "AccessDenied", // Returned due to insufficient IAM permissions to read from the configured queue
        ];

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets the action to execute when a message visibility timeout is updated.
        /// For SQS, this typically doesn't require special handling as the base class manages the visibility extension.
        /// </summary>
        protected override Action<IQueueMessage, QueueMessageUpdateReceipt> OnUpdateReceipt =>
            (message, updateReceipt) =>
            {
                // For SQS, we don't need to do anything special on receipt update
                // The base class handles the visibility timeout extension
                // Just log for debugging if needed
                if (message is SQSMessage sqsMessage)
                {
                    // Could add logging here if needed for debugging
                    // Logger.LogDebug($"Updated visibility for message {sqsMessage.Id} until {updateReceipt.NextVisibleOn}");
                }
            };

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSListener"/> class.
        /// </summary>
        /// <param name="queue">The SQS queue to listen to for messages.</param>
        /// <param name="poisonQueue">The poison queue for messages that fail processing repeatedly.</param>
        /// <param name="triggerExecutor">The executor for processing triggered functions.</param>
        /// <param name="exceptionHandler">The handler for managing unhandled exceptions.</param>
        /// <param name="loggerFactory">The factory for creating loggers.</param>
        /// <param name="messageEnqueuedWatcherSetter">The watcher for message enqueue notifications.</param>
        /// <param name="queueOptions">The queue processing options and configuration.</param>
        /// <param name="queueProcessor">The processor for handling queue message lifecycle.</param>
        /// <param name="descriptor">The function descriptor providing metadata about the triggered function.</param>
        /// <param name="exceptionClassifier">The classifier for determining exception types and handling strategies.</param>
        /// <param name="concurrencyManager">The manager for controlling function execution concurrency.</param>
        /// <param name="drainModeManager">The manager for handling graceful shutdown and drain mode operations.</param>
        /// <example>
        /// <code>
        /// // This listener is typically created by the SQSListenerFactory:
        /// var listener = new SQSListener(
        ///     queue,
        ///     poisonQueue,
        ///     triggerExecutor,
        ///     exceptionHandler,
        ///     loggerFactory,
        ///     messageEnqueuedWatcher,
        ///     queueOptions,
        ///     queueProcessor,
        ///     descriptor,
        ///     exceptionClassifier,
        ///     concurrencyManager,
        ///     drainModeManager);
        /// </code>
        /// </example>
        public SQSListener(
            SQSQueue queue,
            SQSQueue poisonQueue,
            SQSTriggerExecutor triggerExecutor,
            IWebJobsExceptionHandler exceptionHandler,
            ILoggerFactory loggerFactory,
            SharedQueueWatcher messageEnqueuedWatcherSetter,
            QueuesOptionsBase queueOptions,
            QueueProcessor queueProcessor,
            FunctionDescriptor descriptor,
            IQueueRequestExceptionClassifier exceptionClassifier,
            ConcurrencyManager concurrencyManager,
            IDrainModeManager drainModeManager) : base(
                queue,                    // IQueueClient
                poisonQueue,             // IQueueClient 
                triggerExecutor,         // ITriggerExecutor<IQueueMessage>
                exceptionHandler,        // IWebJobsExceptionHandler
                loggerFactory,           // ILoggerFactory
                messageEnqueuedWatcherSetter,  // SharedQueueWatcher
                queueOptions,            // QueuesOptionsBase
                queueProcessor,          // QueueProcessor
                descriptor,              // FunctionDescriptor  
                exceptionClassifier,     // IQueueRequestExceptionClassifier
                concurrencyManager,      // ConcurrencyManager
                drainModeManager: drainModeManager) // IDrainModeManager
        {
            // Constructor body can be empty - base class handles everything
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Determines if a given exception should be treated as fatal and rethrown to stop the SQS poller.
        /// </summary>
        /// <param name="exception">The exception to evaluate for fatality.</param>
        /// <returns>True if the exception should stop the poller; false if polling should continue after handling the exception.</returns>
        /// <remarks>
        /// This treats exceptions related to queue or KMS permissions or the framework configuration as fatal.
        /// Exceptions related to deserializing and handling of a specific message are not fatal and allow continued polling.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Fatal exceptions that will stop the listener:
        /// // - QueueDoesNotExistException (queue was deleted)
        /// // - KmsAccessDeniedException (insufficient KMS permissions)
        /// // - InvalidSecurityException (invalid AWS credentials)
        /// 
        /// // Non-fatal exceptions that allow continued polling:
        /// // - Message deserialization errors
        /// // - Transient network issues
        /// // - Throttling exceptions
        /// </code>
        /// </example>
        protected override bool IsExceptionFatal(Exception exception)
        {
            return exception switch
            {
                // Modeled SQS exceptions that should be treated as fatal
                QueueDoesNotExistException or
                UnsupportedOperationException or
                InvalidAddressException or
                InvalidSecurityException or
                KmsAccessDeniedException or
                KmsInvalidKeyUsageException or
                KmsInvalidStateException or
                KmsNotFoundException or
                KmsOptInRequiredException => true,

                // For unmodeled SQS exceptions that don't have a corresponding .NET type, check the error code
                AmazonSQSException sqsException => _fatalSQSErrorCodes.Contains(sqsException.ErrorCode),

                // All other exceptions are not fatal - allow continued polling

                // RWM: This is from old code. Double-check with the AI if this is still legit.
                // AWSMessagingExceptions thrown by the framework that should be treated as fatal
                //case FailedToFindAWSServiceClientException:     // Failed to resolve AWS service clients from DI
                //case InvalidAppSettingsConfigurationException:  // Failed to find the handler type from what was specified in settings
                //case InvalidMessageHandlerSignatureException:   // A subscriber mapping was registered, but failed to invoke the handler
                //return true;

                _ => false,
            };
        }

        #endregion

    }

}