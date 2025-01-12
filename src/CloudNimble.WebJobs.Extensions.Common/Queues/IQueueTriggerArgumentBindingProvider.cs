// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.WebJobs.Extensions.Common.Triggers;
using System.Reflection;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Interface for providing argument binding for queue trigger parameters.
    /// </summary>
    public interface IQueueTriggerArgumentBindingProvider
    {

        /// <summary>
        /// Tries to create an argument binding for the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter information.</param>
        /// <returns>An instance of <see cref="ITriggerDataArgumentBinding{IQueueMessage}"/> if the binding can be created; otherwise, null.</returns>
        ITriggerDataArgumentBinding<IQueueMessage> TryCreate(ParameterInfo parameter);

    }

}
