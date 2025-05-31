// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Converters;
using Microsoft.Azure.WebJobs;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{

    internal class OutputConverter<TInput> : IObjectToTypeConverter<SQSMessage>
        where TInput : class
    {
        private readonly IConverter<TInput, SQSMessage> _innerConverter;

        public OutputConverter(IConverter<TInput, SQSMessage> innerConverter)
        {
            _innerConverter = innerConverter;
        }

        public bool TryConvert(object input, out SQSMessage output)
        {
            if (input is not TInput typedInput)
            {
                output = null;
                return false;
            }

            output = _innerConverter.Convert(typedInput);
            return true;
        }
    }

}
