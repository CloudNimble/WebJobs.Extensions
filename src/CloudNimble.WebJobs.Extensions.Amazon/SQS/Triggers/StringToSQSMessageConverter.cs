// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{

    /// <summary>
    /// Converts string message bodies to SQS message instances for queue processing.
    /// </summary>
    internal class StringToSQSMessageConverter : IConverter<string, SQSMessage>
    {

        #region Public Methods

        /// <summary>
        /// Converts a string message body to an SQSMessage instance.
        /// </summary>
        /// <param name="input">The string content to convert to an SQS message.</param>
        /// <returns>An SQSMessage instance containing the input as the message body.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        /// <example>
        /// <code>
        /// var converter = new StringToSQSMessageConverter();
        /// var message = converter.Convert("Hello, World!");
        /// Console.WriteLine(message.Body); // Outputs: Hello, World!
        /// </code>
        /// </example>
        public SQSMessage Convert(string input)
        {
            ArgumentNullException.ThrowIfNull(input);

            // Create a minimal SQS message from string content
            // We don't have a real AWS Message object, so we'll create a synthetic one
            var message = new Message
            {
                Body = input,
                MessageId = Guid.NewGuid().ToString(),
                ReceiptHandle = Guid.NewGuid().ToString(),
                Attributes = new Dictionary<string, string>
                {
                    ["SentTimestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                    ["ApproximateReceiveCount"] = "1"
                }
            };

            return new SQSMessage(message, string.Empty); // Empty queue URL for synthetic messages
        }

        #endregion

    }

}