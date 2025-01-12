using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// 
    /// </summary>
    public class QueueResponse<TQueueMessage>
        where TQueueMessage : IQueueMessage
    {

        /// <summary>
        /// 
        /// </summary>
        public string ClientRequestId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<TQueueMessage> Value { get; set; }

    }

}
