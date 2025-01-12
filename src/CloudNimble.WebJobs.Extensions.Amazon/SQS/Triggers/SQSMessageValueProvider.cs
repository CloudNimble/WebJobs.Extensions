// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using System;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{

    /// <summary>
    /// 
    /// </summary>
    internal class SQSMessageValueProvider : IValueProvider
    {
        private readonly SQSMessage _message;
        private readonly object _value;
        private readonly Type _valueType;

        public SQSMessageValueProvider(SQSMessage message, object value, Type valueType)
        {
            if (value is not null && !valueType.IsAssignableFrom(value.GetType()))
            {
                throw new InvalidOperationException("value is not of the correct type.");
            }

            _message = message;
            _value = value;
            _valueType = valueType;
        }

        public Type Type => _valueType;

        public Task<object> GetValueAsync() => Task.FromResult(_value);

        public string ToInvokeString()
        {
            return _message.Body;
        }

    }

}
