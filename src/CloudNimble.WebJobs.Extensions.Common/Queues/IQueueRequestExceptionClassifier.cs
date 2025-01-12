using System;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Interface for classifying different types of queue request exceptions.
    /// </summary>
    public interface IQueueRequestExceptionClassifier
    {

        /// <summary>
        /// Determines if the given exception is a server-side exception.
        /// </summary>
        /// <param name="exception">The exception to classify.</param>
        /// <returns>True if the exception is a server-side exception; otherwise, false.</returns>
        public bool IsServerSideException(Exception exception);

        /// <summary>
        /// Determines if the given exception is due to a pop receipt mismatch.
        /// </summary>
        /// <param name="exception">The exception to classify.</param>
        /// <returns>True if the exception is a pop receipt mismatch; otherwise, false.</returns>
        public bool IsPopReceiptMismatch(Exception exception);

        /// <summary>
        /// Determines if the given exception is a not found exception.
        /// </summary>
        /// <param name="exception">The exception to classify.</param>
        /// <returns>True if the exception is a not found exception; otherwise, false.</returns>
        public bool IsNotFoundException(Exception exception);

        /// <summary>
        /// Determines if the given exception is a conflict exception.
        /// </summary>
        /// <param name="exception">The exception to classify.</param>
        /// <returns>True if the exception is a conflict exception; otherwise, false.</returns>
        public bool IsConflictException(Exception exception);

    }

}
