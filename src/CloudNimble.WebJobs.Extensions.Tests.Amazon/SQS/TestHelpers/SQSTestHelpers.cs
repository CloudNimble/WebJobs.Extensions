// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.TestHelpers
{

    /// <summary>
    /// Helper methods for creating test SQS messages and related objects.
    /// </summary>
    internal static class SQSTestHelpers
    {

        /// <summary>
        /// Creates a test SQS message with the specified body.
        /// </summary>
        public static SQSMessage CreateTestMessage(
            string body = "test message body",
            string messageId = "test-message-id",
            string receiptHandle = "test-receipt-handle",
            string queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
            int? dequeueCount = null,
            DateTimeOffset? sentTimestamp = null)
        {
            var awsMessage = new Message
            {
                Body = body,
                MessageId = messageId,
                ReceiptHandle = receiptHandle,
                Attributes = new Dictionary<string, string>()
            };

            if (dequeueCount.HasValue)
            {
                awsMessage.Attributes["ApproximateReceiveCount"] = dequeueCount.Value.ToString();
            }

            if (sentTimestamp.HasValue)
            {
                awsMessage.Attributes["SentTimestamp"] = sentTimestamp.Value.ToUnixTimeMilliseconds().ToString();
            }

            return new SQSMessage(awsMessage, queueUrl);
        }

        /// <summary>
        /// Creates a synthetic test message (as would be created by converters).
        /// </summary>
        public static SQSMessage CreateSyntheticMessage(string body = "synthetic message")
        {
            return new SQSMessage(body);
        }

    }

}