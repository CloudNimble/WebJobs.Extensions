// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// Event argument class for when poison messages
    /// are added to a poison queue.
    /// </summary>
    internal class PoisonMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="message">The poison message</param>
        /// <param name="poisonQueue">The poison queue</param>
        public PoisonMessageEventArgs(SQSMessage message, SQSQueue poisonQueue)
        {
            Message = message;
            PoisonQueue = poisonQueue;
        }

        /// <summary>
        /// The poison message
        /// </summary>
        public SQSMessage Message { get; private set; }

        /// <summary>
        /// The poison queue
        /// </summary>
        public SQSQueue PoisonQueue { get; private set; }

    }

}
