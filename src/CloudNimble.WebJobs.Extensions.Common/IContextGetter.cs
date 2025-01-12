// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CloudNimble.WebJobs.Extensions.Common
{
    /// <summary>
    /// Defines a mechanism for retrieving a context value of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the context value.</typeparam>
    public interface IContextGetter<TValue>
    {
        /// <summary>
        /// Gets the context value.
        /// </summary>
        TValue Value { get; }
    }
}
