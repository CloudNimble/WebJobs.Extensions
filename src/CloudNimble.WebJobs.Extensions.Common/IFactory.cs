// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CloudNimble.WebJobs.Extensions.Common
{

    /// <summary>
    /// Defines a factory interface for creating instances of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of object that the factory creates.</typeparam>
    public interface IFactory<T>
    {

        /// <summary>
        /// Creates an instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns>A new instance of type <typeparamref name="T"/>.</returns>
        T Create();

    }

}
