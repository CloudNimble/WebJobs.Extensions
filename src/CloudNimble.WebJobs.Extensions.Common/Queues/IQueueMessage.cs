using System;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Marker interface for a queue message.
    /// </summary>
    public interface IQueueMessage
    {

        /// <summary>
        /// 
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int DequeueCount { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PopReceipt { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTimeOffset? DateInserted { get; set; }

    }

}
