// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Common.Listeners
{

    /// <summary>
    /// Provides host-level state persistence by managing shared context instances and items.
    /// </summary>
    public class SharedContextProvider : ISharedContextProvider
    {

        #region Private Members

        private readonly Dictionary<Type, object> _instances = [];
        private readonly Dictionary<string, object> _items = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets an existing instance of the specified type or creates a new one using the provided factory.
        /// </summary>
        /// <typeparam name="T">The type of the value to get or create.</typeparam>
        /// <param name="factory">The factory to use for creating a new instance if one does not already exist.</param>
        /// <returns>The existing or newly created instance of the specified type.</returns>
        public T GetOrCreateInstance<T>(IFactory<T> factory)
        {
            var factoryItemType = typeof(T);

            if (_instances.TryGetValue(factoryItemType, out object value))
            {
                return (T)value;
            }
            else
            {
                T listener = factory.Create();
                _instances.Add(factoryItemType, listener);
                return listener;
            }
        }

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue(string key, object value)
        {
            _items[key] = value;
        }

        /// <summary>
        /// Tries to get a value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the object associated with the specified key, if the key is found; otherwise, null.</param>
        /// <returns>true if the key was found; otherwise, false.</returns>
        public bool TryGetValue(string key, out object value)
        {
            return _items.TryGetValue(key, out value);
        }

        #endregion

    }

}
