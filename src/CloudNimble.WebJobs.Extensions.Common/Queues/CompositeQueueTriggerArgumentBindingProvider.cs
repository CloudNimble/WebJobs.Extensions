// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.WebJobs.Extensions.Common.Triggers;
using System.Collections.Generic;
using System.Reflection;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides a composite implementation of <see cref="IQueueTriggerArgumentBindingProvider"/> that delegates to multiple providers.
    /// </summary>
    public class CompositeQueueTriggerArgumentBindingProvider : IQueueTriggerArgumentBindingProvider
    {
        private readonly IEnumerable<IQueueTriggerArgumentBindingProvider> _providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeQueueTriggerArgumentBindingProvider"/> class.
        /// </summary>
        /// <param name="providers">The collection of <see cref="IQueueTriggerArgumentBindingProvider"/> to delegate to.</param>
        public CompositeQueueTriggerArgumentBindingProvider(params IQueueTriggerArgumentBindingProvider[] providers)
        {
            _providers = providers;
        }

        /// <summary>
        /// Tries to create an argument binding for the specified parameter by delegating to the contained providers.
        /// </summary>
        /// <param name="parameter">The parameter information.</param>
        /// <returns>An instance of <see cref="ITriggerDataArgumentBinding{IQueueMessage}"/> if a binding can be created; otherwise, null.</returns>
        public ITriggerDataArgumentBinding<IQueueMessage> TryCreate(ParameterInfo parameter)
        {
            foreach (var provider in _providers)
            {
               var binding = provider.TryCreate(parameter);

                if (binding is not null)
                {
                    return binding;
                }
            }

            return null;
        }

    }

}
