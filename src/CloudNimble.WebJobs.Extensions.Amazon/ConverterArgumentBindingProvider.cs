// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS;
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
            return parameter.ParameterType == typeof(T) ? new ConverterTriggerDataArgumentBinding<T>((IConverter<IQueueMessage, T>)_converter, _loggerFactory) : null;
        }

        #endregion

    }

}
