// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;

namespace Microsoft.Azure.WebJobs.Extensions.Storage.Queues.Triggers
{
    internal class SQSMessageToStringConverter : IConverter<SQSMessage, string>
    {
        public string Convert(SQSMessage input)
        {
            Ensure.ArgumentNotNull(input, nameof(input));

            return input.Body;
        }
    }
}
