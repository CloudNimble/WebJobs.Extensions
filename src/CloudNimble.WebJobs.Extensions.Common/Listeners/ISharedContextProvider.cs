// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CloudNimble.WebJobs.Extensions.Common.Listeners
{

    /// <summary>
    /// Interface for providing host-level state persistence.
    /// </summary>
    /// <remarks>
    /// A new instance of the provider is created when a host is constructed.
    /// </remarks>
    public interface ISharedContextProvider
    {

        /// <summary>
        /// Tries to get a value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the object associated with the specified key, if the key is found; otherwise, null.</param>
        /// <returns>true if the key was found; otherwise, false.</returns>
        bool TryGetValue(string key, out object value);

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        void SetValue(string key, object value);

        /// <summary>
        /// Gets an existing instance of the specified type or creates a new one using the provided factory.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to get or create.</typeparam>
        /// <param name="factory">The factory to use for creating a new instance if one does not already exist.</param>
        /// <returns>The existing or newly created instance of the specified type.</returns>
        TValue GetOrCreateInstance<TValue>(IFactory<TValue> factory);

    }

}
