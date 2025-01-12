// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides argument binding for user-defined types in queue trigger parameters.
    /// </summary>
    internal class UserTypeArgumentBinding : ITriggerDataArgumentBinding<IQueueMessage>
    {
        private readonly Type _valueType;
        private readonly IBindingDataProvider _bindingDataProvider;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTypeArgumentBinding"/> class.
        /// </summary>
        /// <param name="valueType">The type of the value.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public UserTypeArgumentBinding(Type valueType, ILoggerFactory loggerFactory)
        {
            _valueType = valueType;
            _bindingDataProvider = BindingDataProvider.FromType(_valueType);
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Gets the type of the trigger value.
        /// </summary>
        public Type ValueType => _valueType;

        /// <summary>
        /// Gets the binding data contract.
        /// </summary>
        public IReadOnlyDictionary<string, Type> BindingDataContract => _bindingDataProvider?.Contract;

        /// <summary>
        /// Binds to the specified trigger value.
        /// </summary>
        /// <param name="value">The value to bind to.</param>
        /// <param name="context">The binding context.</param>
        /// <returns>A task that returns the <see cref="ITriggerData"/> for the binding.</returns>
        public Task<ITriggerData> BindAsync(IQueueMessage value, ValueBindingContext context)
        {
            Ensure.ArgumentNotNull(value, nameof(value));

            object convertedValue;
            try
            {
                convertedValue = JsonSerializer.Deserialize(value.Body, ValueType, JsonSerialization.Options);
            }
            catch (JsonException e)
            {
                // Easy to have the queue payload not deserialize properly. So give a useful error.
                string msg = string.Format(CultureInfo.CurrentCulture,
                    @"Binding parameters to complex objects (such as '{0}') uses Json.NET serialization. 
                        1. Bind the parameter type as 'string' instead of '{0}' to get the raw values and avoid JSON deserialization, or
                        2. Change the queue payload to be valid json. The JSON parser failed: {1}
                        ", _valueType.Name, e.Message);
                throw new InvalidOperationException(msg);
            }

            var provider = new QueueMessageValueProvider(value, convertedValue, ValueType, _loggerFactory);
            var bindingData = _bindingDataProvider?.GetBindingData(convertedValue);

            return Task.FromResult<ITriggerData>(new TriggerData(provider, bindingData));
        }

    }
}