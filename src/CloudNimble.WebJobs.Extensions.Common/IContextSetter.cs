// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CloudNimble.WebJobs.Extensions.Common
{

    /// <summary>
    /// Defines a method to set a value in a specific context.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to be set.</typeparam>
    public interface IContextSetter<TValue>
    {

        /// <summary>
        /// Sets the specified value in the context.
        /// </summary>
        /// <param name="value">The value to set.</param>
        void SetValue(TValue value);

    }

}
