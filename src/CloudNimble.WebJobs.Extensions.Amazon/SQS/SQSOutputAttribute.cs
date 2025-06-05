// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// Attribute used to bind a parameter to an Amazon SQS Queue for output operations.
    /// </summary>
    /// <remarks>
    /// The method parameter type can be one of the following:
    /// <list type="bullet">
    /// <item><description><see cref="ICollector{T}"/> of SQSMessage, string, byte[], or user-defined types (to enqueue multiple messages)</description></item>
    /// <item><description><see cref="IAsyncCollector{T}"/> of SQSMessage, string, byte[], or user-defined types (to enqueue multiple messages)</description></item>
    /// <item><description>out SQSMessage</description></item>
    /// <item><description>out string</description></item>
    /// <item><description>out byte[]</description></item>
    /// <item><description>out T (where T is a user-defined type, serialized as JSON)</description></item>
    /// </list>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    [Binding]
    public class SQSOutputAttribute : Attribute, IConnectionProvider
    {
        private readonly string _queueName;

        /// <summary>Initializes a new instance of the <see cref="SQSOutputAttribute"/> class.</summary>
        /// <param name="queueName">The name of the queue to which to bind.</param>
        public SQSOutputAttribute(string queueName)
        {
            _queueName = queueName;
        }

        /// <summary>
        /// Gets the name of the queue to which to bind.
        /// </summary>
        [AutoResolve]
        public string QueueName => _queueName;

        /// <summary>
        /// Gets or sets the app setting name that contains the AWS connection configuration.
        /// </summary>
        public string Connection { get; set; }

        private string GetDebuggerDisplay()
        {
            return _queueName;
        }
    }

}