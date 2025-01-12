// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides a binding provider for queue trigger arguments that uses a converter to convert the queue message to the target type.
    /// </summary>
    /// <typeparam name="T">The target type to convert the queue message to.</typeparam>
    public class ConverterQueueTriggerArgumentBindingProvider<T> : IQueueTriggerArgumentBindingProvider
    {
        private readonly IConverter<IQueueMessage, T> _converter;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterQueueTriggerArgumentBindingProvider{T}"/> class.
        /// </summary>
        /// <param name="converter">The converter to use for converting the queue message.</param>
        /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
        public ConverterQueueTriggerArgumentBindingProvider(IConverter<IQueueMessage, T> converter, ILoggerFactory loggerFactory)
        {
            _converter = converter;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Tries to create a trigger data argument binding for the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter to create the binding for.</param>
        /// <returns>The trigger data argument binding if the parameter type matches the target type; otherwise, null.</returns>
        public ITriggerDataArgumentBinding<IQueueMessage> TryCreate(ParameterInfo parameter)
        {
            if (parameter.ParameterType != typeof(T))
            {
                return null;
            }

            return new ConverterTriggerDataArgumentBinding<T>(_converter, _loggerFactory);
        }

    }

}
