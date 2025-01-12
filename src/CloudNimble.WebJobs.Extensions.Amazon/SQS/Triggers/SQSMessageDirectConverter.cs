// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.Storage.Queues.Triggers
{
    internal class SQSMessageDirectConverter : IConverter<SQSMessage, SQSMessage>
    {
        public SQSMessage Convert(SQSMessage input)
        {
            Ensure.ArgumentNotNull(input, nameof(input));

            return input;
        }
    }
}
