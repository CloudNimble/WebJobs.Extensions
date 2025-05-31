// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.EasyAF.Core;
using Microsoft.Azure.WebJobs;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
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
