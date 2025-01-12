// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CloudNimble.WebJobs.Extensions.Common
{

    /// <summary>
    /// Defines a contract for a watcher that gets notified when a message is enqueued in a queue.
    /// </summary>
    public interface IMessageEnqueuedWatcher
    {

        /// <summary>
        /// Notifies the watcher that a message has been enqueued in the specified queue.
        /// </summary>
        /// <param name="enqueuedInQueueName">The name of the queue where the message was enqueued.</param>
        void Notify(string enqueuedInQueueName);

    }

}
