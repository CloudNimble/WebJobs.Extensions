// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CloudNimble.WebJobs.Extensions.Amazon
{

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ConverterArgumentBindingProvider<T> : IQueueTriggerArgumentBindingProvider
    {

        #region Private Members

        private readonly IConverter<SQSMessage, T> _converter;
        private readonly ILoggerFactory _loggerFactory;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="converter"></param>
        /// <param name="loggerFactory"></param>
        public ConverterArgumentBindingProvider(IConverter<SQSMessage, T> converter, ILoggerFactory loggerFactory)
        {
            _converter = converter;
            _loggerFactory = loggerFactory;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public ITriggerDataArgumentBinding<IQueueMessage> TryCreate(ParameterInfo parameter)
        {
            if (parameter.ParameterType != typeof(T))
            {
                return null;
            }

            // Use the adapter to convert IConverter<SQSMessage, T> to IConverter<IQueueMessage, T>
            var adaptedConverter = new QueueMessageConverterAdapter<T>(_converter);
            return new ConverterTriggerDataArgumentBinding<T>(adaptedConverter, _loggerFactory);
        }

        #endregion

    }

}
