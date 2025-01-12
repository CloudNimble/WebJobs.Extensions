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
    /// 
    /// </summary>
    internal sealed partial class SQSListener : QueueListener<SQSMessage>, IListener
    {

        /// <summary>
        /// <see cref="AmazonSQSException"/> error codes that should be treated as fatal and stop the poller
        /// </summary>
        /// <remarks>
        /// These are the subset of https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_ReceiveMessage.html#API_ReceiveMessage_Errors
        /// that aren't modeled as typed exceptions
        /// </remarks>
        private static readonly HashSet<string> _fatalSQSErrorCodes =
        [
            "AccessDenied", // Returned due to insufficient IAM permissions to read from the configured queue
        ];


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
                queue,
                poisonQueue,
                triggerExecutor,
                exceptionHandler,
                loggerFactory,
                messageEnqueuedWatcherSetter,
                queueOptions,
                queueProcessor,
                descriptor,
                exceptionClassifier,
                concurrencyManager,
                drainModeManager: drainModeManager)
        {
        }

        protected override Action<IQueueMessage, QueueMessageUpdateReceipt> OnUpdateReceipt => (message, updateReceipt) =>
        {
            //var sqsMessage = (SQSMessage)message;
            //var receiptHandle = sqsMessage.Message.ReceiptHandle;
            //var visibilityTimeout = _visibilityTimeout;
            //// Update the message visibility timeout to keep the message invisible to other consumers
            //// while the function is processing it.
            //var request = new ChangeMessageVisibilityRequest
            //{
            //    QueueUrl = _queue.QueueUrl,
            //    ReceiptHandle = receiptHandle,
            //    VisibilityTimeout = (int)visibilityTimeout.TotalSeconds
            //};
            //_queue.Client.ChangeMessageVisibilityAsync(request).GetAwaiter().GetResult();
        };

        /// <summary>
        /// Default logic that determines if a given exception should be treated as fatal and rethrown to stop the SQS poller.
        /// </summary>
        /// <remarks>
        /// This treats exceptions related to queue or KMS permissions or the framework configuration as fatal.
        /// Exceptions related to the deserializing and handling of a specific message are not fatal.</remarks>
        /// <param name="exception">Exception to determine if it's fatal</param>
        /// <returns>True to stop the SQS poller if the exception is caught, false otherwise</returns>
        protected override bool IsExceptionFatal(Exception exception)
        {
            return exception switch
            {
                // Modeled SQS exceptions that should be treated as fatal
                // Queue URL doesn't exist
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

                // AWSMessagingExceptions thrown by the framework that should be treated as fatal
                //case FailedToFindAWSServiceClientException:     // Failed to resolve AWS service clients from DI
                //case InvalidAppSettingsConfigurationException:  // Failed to find the handler type from what was specified in settings
                //case InvalidMessageHandlerSignatureException:   // A subscriber mapping was registered, but failed to invoke the handler
                //return true;

                _ => false,
            };
        }
    }

}
