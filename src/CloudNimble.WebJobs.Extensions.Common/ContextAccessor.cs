// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CloudNimble.WebJobs.Extensions.Common
{
    /// <summary>
    /// Provides a mechanism to set and get a context value of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the context value.</typeparam>
    public class ContextAccessor<TValue> : IContextGetter<TValue>, IContextSetter<TValue>
    {
        private TValue _value;

        /// <summary>
        /// Gets the context value.
        /// </summary>
        public TValue Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Sets the specified value in the context.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetValue(TValue value)
        {
            _value = value;
        }
    }
}
