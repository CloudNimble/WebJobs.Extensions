// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides argument binding for user-defined types in queue trigger parameters.
    /// </summary>
    public class UserTypeArgumentBindingProvider : IQueueTriggerArgumentBindingProvider
    {

        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTypeArgumentBindingProvider"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        public UserTypeArgumentBindingProvider(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Tries to create an argument binding for the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter information.</param>
        /// <returns>An instance of <see cref="ITriggerDataArgumentBinding{IQueueMessage}"/> if the binding can be created; otherwise, null.</returns>
        public ITriggerDataArgumentBinding<IQueueMessage> TryCreate(ParameterInfo parameter)
        {
            // At indexing time, attempt to bind all types.
            // (Whether or not actual binding is possible depends on the message shape at runtime.)
            return new UserTypeArgumentBinding(parameter.ParameterType, _loggerFactory);
        }

    }

}
