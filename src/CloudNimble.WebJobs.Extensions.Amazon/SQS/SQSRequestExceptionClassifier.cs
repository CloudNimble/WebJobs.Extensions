using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using System;
using System.Net;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// Classifies exceptions thrown by Amazon SQS.
    /// </summary>
    internal class SQSRequestExceptionClassifier : IQueueRequestExceptionClassifier
    {

        /// <inheritdoc />
        public bool IsConflictException(Exception exception)
        {
            if (exception is AmazonSQSException sqsException)
            {
                return sqsException.StatusCode == HttpStatusCode.Conflict ||
                       exception is QueueDeletedRecentlyException ||
                       exception is QueueNameExistsException;
            }
            return false;
        }

        /// <inheritdoc />
        public bool IsNotFoundException(Exception exception)
        {
            if (exception is AmazonSQSException sqsException)
            {
                return sqsException.StatusCode == HttpStatusCode.NotFound ||
                       exception is QueueDoesNotExistException;
            }
            return false;
        }

        /// <inheritdoc />
        public bool IsPopReceiptMismatch(Exception exception)
        {
            if (exception is ReceiptHandleIsInvalidException)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public bool IsServerSideException(Exception exception)
        {
            if (exception is AmazonSQSException sqsException)
            {
                return sqsException.StatusCode == HttpStatusCode.InternalServerError ||
                       sqsException.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       exception is OverLimitException ||
                       exception is BatchEntryIdsNotDistinctException ||
                       exception is BatchRequestTooLongException ||
                       exception is EmptyBatchRequestException ||
                       exception is InvalidBatchEntryIdException ||
                       exception is TooManyEntriesInBatchRequestException;
            }
            return false;
        }

    }

}
