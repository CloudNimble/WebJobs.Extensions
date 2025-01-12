// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using System;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// Represents an attribute used to bind a parameter to an Amazon SQS queue trigger.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public sealed class SQSTriggerAttribute : Attribute, IConnectionProvider
    {

        #region Properties

        /// <summary>
        /// Gets or sets the connection string name for the SQS queue.
        /// </summary>
        [ConnectionString]
        public string Connection { get; set; } = string.Empty;

        /// <summary>
        /// Gets the name of the queue to monitor.
        /// </summary>
        [AutoResolve]
        public string QueueName { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSTriggerAttribute"/> class.
        /// </summary>
        /// <param name="queueName">The name of the queue to monitor.</param>
        public SQSTriggerAttribute(string queueName)
        {
            QueueName = queueName;
        }

        #endregion

    }

}