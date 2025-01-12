// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CloudNimble.WebJobs.Extensions.Common
{

    /// <summary>
    /// Represents a command that sends notifications.
    /// </summary>
    public interface INotificationCommand
    {

        /// <summary>
        /// Sends a notification.
        /// </summary>
        void Notify();

    }

}
