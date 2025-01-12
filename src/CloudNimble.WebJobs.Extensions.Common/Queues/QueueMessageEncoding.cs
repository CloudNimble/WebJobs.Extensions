// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Determines how <see cref="IQueueMessage.Body"/> is represented in HTTP requests and responses.
    /// </summary>
    public enum QueueMessageEncoding
    {

        /// <summary>
        /// The <see cref="IQueueMessage.Body"/> is represented verbatim in HTTP requests and responses. I.e. message is not transformed.
        /// </summary>
        None = 0,

        /// <summary>
        /// The <see cref="IQueueMessage.Body"/> is represented as Base64 encoded string in HTTP requests and responses.
        /// </summary>
        Base64 = 1,

    }

}
