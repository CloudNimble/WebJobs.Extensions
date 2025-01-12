// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using Microsoft.Azure.WebJobs;
using System;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{
    internal class SQSMessageToParameterBindingDataConverter : IConverter<SQSMessage, ParameterBindingData>
    {
        public ParameterBindingData Convert(SQSMessage input)
        {
            Ensure.ArgumentNotNull(input, nameof(input));

            var content = new BinaryData(input);
            return new ParameterBindingData("1.0", AmazonConstants.SQSExtensionName, content, "application/json");
        }
    }
}
