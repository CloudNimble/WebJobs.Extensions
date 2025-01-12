using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Defines a client for interacting with a queue.
    /// </summary>
    public interface IQueueClient//<TQueueMessage>
        //where TQueueMessage : IQueueMessage
    {

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public string AccountName { get; }

        /// <summary>
        /// Validates a queue name.
        /// </summary>
        /// <param name="queueName">The queue name to validate.</param>
        /// <param name="errorMessage">The error message if the queue name is invalid.</param>
        /// <returns>True if the queue name is valid, otherwise false.</returns>
        static bool IsValidQueueName(string queueName, out string errorMessage)
        {
            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Validates the specified queue name.
        /// </summary>
        /// <param name="name">The name of the queue to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the queue name is invalid.</exception>

        static void ValidateQueueName(string name)
        {
        }

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddMessageAndCreateIfNotExistsAsync(string body, CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="popReceipt"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteMessageAsync(string id, string popReceipt, CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool?> ExistsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<QueueProperties> GetPropertiesAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        Task<QueueResponse<TQueueMessage>> PeekMessagesAsync<TQueueMessage>(int v)
            where TQueueMessage : IQueueMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numMessagesToReceive"></param>
        /// <param name="visibilityTimeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<QueueResponse<TQueueMessage>> ReceiveMessagesAsync<TQueueMessage>(int numMessagesToReceive, TimeSpan visibilityTimeout, CancellationToken token)
            where TQueueMessage : IQueueMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="popReceipt"></param>
        /// <param name="visibilityTimeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<QueueMessageUpdateReceipt> UpdateMessageAsync(string id, string popReceipt, TimeSpan visibilityTimeout, CancellationToken cancellationToken);

        #endregion

    }

}
