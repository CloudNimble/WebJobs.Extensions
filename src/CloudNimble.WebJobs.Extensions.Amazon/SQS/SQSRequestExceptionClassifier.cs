// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using System;
using System.Collections.Generic;
using System.Net;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// Classifies exceptions thrown by Amazon SQS operations to determine appropriate handling strategies.
    /// Provides comprehensive mapping of all SQS exception types to the common queue interface categories.
    /// </summary>
    internal class SQSRequestExceptionClassifier : IQueueRequestExceptionClassifier
    {

        #region Private Fields

        /// <summary>
        /// Set of SQS error codes that indicate server-side transient issues that should be retried.
        /// </summary>
        private static readonly HashSet<string> ServerSideErrorCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "InternalError",
            "InternalFailure", 
            "ServiceUnavailable",
            "Throttling",
            "ThrottlingException",
            "RequestTimeout",
            "SlowDown",
            "TooManyRequestsException"
        };

        /// <summary>
        /// Set of SQS error codes that indicate the requested resource was not found.
        /// </summary>
        private static readonly HashSet<string> NotFoundErrorCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "NonExistentQueue",
            "QueueDoesNotExist",
            "AWS.SimpleQueueService.NonExistentQueue"
        };

        /// <summary>
        /// Set of SQS error codes that indicate conflicts with the current state of resources.
        /// </summary>
        private static readonly HashSet<string> ConflictErrorCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "QueueDeletedRecently",
            "QueueNameExists",
            "PurgeQueueInProgress"
        };

        /// <summary>
        /// Set of SQS error codes that indicate issues with message receipt handles or ownership.
        /// </summary>
        private static readonly HashSet<string> PopReceiptMismatchErrorCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "ReceiptHandleIsInvalid",
            "InvalidReceiptHandle", 
            "MessageNotInflight"
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines if the given exception represents a conflict with the current state of SQS resources.
        /// </summary>
        /// <param name="exception">The exception to classify.</param>
        /// <returns>True if the exception is a conflict exception; otherwise, false.</returns>
        /// <remarks>
        /// Conflict exceptions typically indicate:
        /// - Queue was recently deleted and cannot be recreated yet
        /// - Queue name already exists when trying to create
        /// - Queue purge operation is already in progress
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     await sqsClient.CreateQueueAsync(request);
        /// }
        /// catch (Exception ex) when (classifier.IsConflictException(ex))
        /// {
        ///     // Wait and retry - queue may have been recently deleted
        ///     await Task.Delay(TimeSpan.FromSeconds(60));
        ///     // Retry creation...
        /// }
        /// </code>
        /// </example>
        public bool IsConflictException(Exception exception)
        {
            return exception switch
            {
                // Typed exceptions for conflicts
                QueueDeletedRecentlyException => true,
                QueueNameExistsException => true,
                PurgeQueueInProgressException => true,

                // HTTP status code based classification
                AmazonSQSException sqsException when sqsException.StatusCode == HttpStatusCode.Conflict => true,

                // Error code based classification for untyped exceptions
                AmazonSQSException sqsException when ConflictErrorCodes.Contains(sqsException.ErrorCode) => true,

                _ => false
            };
        }

        /// <summary>
        /// Determines if the given exception represents a resource not found error.
        /// </summary>
        /// <param name="exception">The exception to classify.</param>
        /// <returns>True if the exception is a not found exception; otherwise, false.</returns>
        /// <remarks>
        /// Not found exceptions typically indicate:
        /// - Queue does not exist
        /// - Attempting to access a non-existent queue
        /// - Queue was deleted and no longer available
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     await sqsClient.GetQueueAttributesAsync(queueUrl);
        /// }
        /// catch (Exception ex) when (classifier.IsNotFoundException(ex))
        /// {
        ///     // Queue doesn't exist - create it first
        ///     await sqsClient.CreateQueueAsync(queueName);
        /// }
        /// </code>
        /// </example>
        public bool IsNotFoundException(Exception exception)
        {
            return exception switch
            {
                // Typed exceptions for not found
                QueueDoesNotExistException => true,

                // HTTP status code based classification  
                AmazonSQSException sqsException when sqsException.StatusCode == HttpStatusCode.NotFound => true,

                // Error code based classification for untyped exceptions
                AmazonSQSException sqsException when NotFoundErrorCodes.Contains(sqsException.ErrorCode) => true,

                _ => false
            };
        }

        /// <summary>
        /// Determines if the given exception represents a pop receipt mismatch or invalid message handle.
        /// </summary>
        /// <param name="exception">The exception to classify.</param>
        /// <returns>True if the exception is a pop receipt mismatch; otherwise, false.</returns>
        /// <remarks>
        /// Pop receipt mismatch exceptions typically indicate:
        /// - Message receipt handle is invalid or expired
        /// - Another consumer has already processed the message
        /// - Message is not currently in-flight (not invisible)
        /// - Attempting to delete/modify a message with wrong receipt handle
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     await sqsClient.DeleteMessageAsync(queueUrl, receiptHandle);
        /// }
        /// catch (Exception ex) when (classifier.IsPopReceiptMismatch(ex))
        /// {
        ///     // Message was already processed by another consumer
        ///     // No action needed - message is already gone
        ///     logger.LogDebug("Message already processed by another consumer");
        /// }
        /// </code>
        /// </example>
        public bool IsPopReceiptMismatch(Exception exception)
        {
            return exception switch
            {
                // Typed exceptions for receipt handle issues
                ReceiptHandleIsInvalidException => true,
                MessageNotInflightException => true,

                // Error code based classification for untyped exceptions
                AmazonSQSException sqsException when PopReceiptMismatchErrorCodes.Contains(sqsException.ErrorCode) => true,

                _ => false
            };
        }

        /// <summary>
        /// Determines if the given exception represents a server-side transient error that should be retried.
        /// </summary>
        /// <param name="exception">The exception to classify.</param>
        /// <returns>True if the exception is a server-side exception; otherwise, false.</returns>
        /// <remarks>
        /// Server-side exceptions typically indicate:
        /// - Internal AWS service errors (should retry)
        /// - Service unavailable (temporary capacity issues)
        /// - Throttling (rate limiting - should backoff and retry)
        /// - Request timeouts (network or service issues)
        /// - Over limit errors (temporary resource constraints)
        /// - Batch processing limits (retry with smaller batches)
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     await sqsClient.ReceiveMessageAsync(request);
        /// }
        /// catch (Exception ex) when (classifier.IsServerSideException(ex))
        /// {
        ///     // Transient error - implement exponential backoff retry
        ///     await Task.Delay(retryDelay);
        ///     // Retry the operation...
        /// }
        /// </code>
        /// </example>
        public bool IsServerSideException(Exception exception)
        {
            return exception switch
            {
                // Typed exceptions for server-side issues
                OverLimitException => true,

                // Batch processing related exceptions (server limits)
                BatchEntryIdsNotDistinctException => true,
                BatchRequestTooLongException => true,
                EmptyBatchRequestException => true,
                InvalidBatchEntryIdException => true,
                TooManyEntriesInBatchRequestException => true,

                // HTTP status code based classification
                AmazonSQSException sqsException when IsServerSideStatusCode(sqsException.StatusCode) => true,

                // Error code based classification for untyped exceptions
                AmazonSQSException sqsException when ServerSideErrorCodes.Contains(sqsException.ErrorCode) => true,

                _ => false
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines if an HTTP status code represents a server-side issue.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns>True if the status code indicates a server-side issue; otherwise, false.</returns>
        private static bool IsServerSideStatusCode(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.InternalServerError => true,      // 500
                HttpStatusCode.BadGateway => true,               // 502  
                HttpStatusCode.ServiceUnavailable => true,      // 503
                HttpStatusCode.GatewayTimeout => true,          // 504
                HttpStatusCode.RequestTimeout => true,          // 408
                HttpStatusCode.TooManyRequests => true,         // 429 (throttling)

                // Some 403 errors are server-side (like OverLimit)
                HttpStatusCode.Forbidden => true,

                _ => false
            };
        }

        #endregion

    }

}