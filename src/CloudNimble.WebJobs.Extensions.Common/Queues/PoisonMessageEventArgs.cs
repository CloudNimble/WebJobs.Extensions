// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Event argument class for when poison messages
    /// are added to a poison queue.
    /// </summary>
    public class PoisonMessageEventArgs : EventArgs
    {

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="message">The poison message</param>
        /// <param name="poisonQueue">The poison queue</param>
        public PoisonMessageEventArgs(IQueueMessage message, IQueueClient poisonQueue)
        {
            Message = message;
            PoisonQueue = poisonQueue;
        }

        /// <summary>
        /// The poison message
        /// </summary>
        public IQueueMessage Message { get; private set; }

        /// <summary>
        /// The poison queue
        /// </summary>
        public IQueueClient PoisonQueue { get; private set; }

    }

}
