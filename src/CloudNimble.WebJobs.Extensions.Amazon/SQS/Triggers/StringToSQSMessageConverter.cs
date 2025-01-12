// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.WebJobs.Extensions.Amazon.SQS;

namespace Microsoft.Azure.WebJobs.Extensions.Storage.Queues.Triggers
{
    internal class StringToSQSMessageConverter : IConverter<string, SQSMessage>
    {

        public StringToSQSMessageConverter()
        {
            //if (queue == null)
            //{
            //    throw new ArgumentNullException(nameof(queue));
            //}

            //_queue = queue;
        }

        public SQSMessage Convert(string input)
        {
            return new SQSMessage(input);
        }
    }
}
