// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// A watcher that gets notified when a message is enqueued in a queue and triggers registered notification commands.
    /// </summary>
    public class SharedQueueWatcher : IMessageEnqueuedWatcher
    {

        /// <summary>
        /// A dictionary that holds the queue names and their corresponding notification commands.
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentBag<INotificationCommand>> _registrations =
            new();

        /// <summary>
        /// Notifies all registered notification commands for the specified queue.
        /// </summary>
        /// <param name="enqueuedInQueueName">The name of the queue where the message was enqueued.</param>
        public void Notify(string enqueuedInQueueName)
        {
            if (_registrations.TryGetValue(enqueuedInQueueName, out ConcurrentBag<INotificationCommand> queueRegistrations))
            {
                foreach (INotificationCommand registration in queueRegistrations.ToArray())
                {
                    registration.Notify();
                }
            }
        }

        /// <summary>
        /// Registers a notification command for a specified queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to register the notification command for.</param>
        /// <param name="notification">The notification command to register.</param>
        public void Register(string queueName, INotificationCommand notification)
        {
            _registrations.AddOrUpdate(
                queueName,
                [.. new INotificationCommand[] { notification }],
                (i, existing) => 
                {
                    existing.Add(notification);
                    return existing;
                });
        }
    }
}
