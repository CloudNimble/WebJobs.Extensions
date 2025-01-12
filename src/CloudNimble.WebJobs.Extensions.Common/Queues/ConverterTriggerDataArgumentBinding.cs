// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides a trigger data argument binding that uses a converter to convert the queue message to the target type.
    /// </summary>
    internal class ConverterTriggerDataArgumentBinding<T> : ITriggerDataArgumentBinding<IQueueMessage>
    {
        private readonly IConverter<IQueueMessage, T> _converter;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterTriggerDataArgumentBinding{T}"/> class.
        /// </summary>
        /// <param name="converter">The converter to use for converting the queue message.</param>
        /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
        public ConverterTriggerDataArgumentBinding(IConverter<IQueueMessage, T> converter, ILoggerFactory loggerFactory)
        {
            _converter = converter;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Gets the type of the trigger value.
        /// </summary>
        public Type ValueType => typeof(T);

        /// <summary>
        /// Gets the binding data contract.
        /// </summary>
        public IReadOnlyDictionary<string, Type> BindingDataContract
        {
            get { return null; }
        }

        /// <summary>
        /// Binds to the specified trigger value.
        /// </summary>
        /// <param name="value">The value to bind to.</param>
        /// <param name="context">The binding context.</param>
        /// <returns>A task that returns the trigger data for the binding.</returns>
        public Task<ITriggerData> BindAsync(IQueueMessage value, ValueBindingContext context)
        {
            var provider = new QueueMessageValueProvider(value, _converter.Convert(value), typeof(T), _loggerFactory);
            return Task.FromResult<ITriggerData>(new TriggerData(provider, null));
        }

    }

}
