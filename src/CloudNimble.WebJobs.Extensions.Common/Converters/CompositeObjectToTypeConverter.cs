// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Common.Converters
{
    /// <summary>
    /// An object converter that encapsulates a set of inner converters.
    /// </summary>
    /// <typeparam name="T">The output <see cref="Type"/></typeparam>
    public class CompositeObjectToTypeConverter<T> : IObjectToTypeConverter<T>
    {
        private readonly IEnumerable<IObjectToTypeConverter<T>> _converters;

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="converters">The set of converters to encapsulate.</param>
        public CompositeObjectToTypeConverter(IEnumerable<IObjectToTypeConverter<T>> converters)
        {
            Ensure.ArgumentNotNull(converters, nameof(converters));
            _converters = converters;
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="converters">The set of converters to encapsulate.</param>
        public CompositeObjectToTypeConverter(params IObjectToTypeConverter<T>[] converters)
            : this((IEnumerable<IObjectToTypeConverter<T>>)converters)
        {
        }

        /// <summary>
        /// Try to perform a conversion by attempting each inner converter in order
        /// until one succeeds, or all fail.
        /// </summary>
        /// <param name="input">The value to convert.</param>
        /// <param name="output">The converted value if successful.</param>
        /// <returns>True if the conversion was successful, false otherwise.</returns>
        public bool TryConvert(object input, out T output)
        {
            foreach (IObjectToTypeConverter<T> converter in _converters)
            {

                if (converter.TryConvert(input, out T possibleConverted))
                {
                    output = possibleConverted;
                    return true;
                }
            }

            output = default;
            return false;
        }
    }
}
