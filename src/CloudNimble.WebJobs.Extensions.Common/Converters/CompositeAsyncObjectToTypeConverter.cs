// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Converters
{

    /// <summary>
    /// A composite converter that attempts to convert an object to a specified type using a collection of converters.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    public class CompositeAsyncObjectToTypeConverter<T> : IAsyncObjectToTypeConverter<T>
    {
        private readonly IEnumerable<IAsyncObjectToTypeConverter<T>> _converters;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAsyncObjectToTypeConverter{T}"/> class with a collection of converters.
        /// </summary>
        /// <param name="converters">The collection of converters to use.</param>
        public CompositeAsyncObjectToTypeConverter(IEnumerable<IAsyncObjectToTypeConverter<T>> converters)
        {
            _converters = converters;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAsyncObjectToTypeConverter{T}"/> class with an array of converters.
        /// </summary>
        /// <param name="converters">The array of converters to use.</param>
        public CompositeAsyncObjectToTypeConverter(params IAsyncObjectToTypeConverter<T>[] converters)
            : this((IEnumerable<IAsyncObjectToTypeConverter<T>>)converters)
        {
        }

        /// <summary>
        /// Attempts to convert the specified value to the target type using the collection of converters.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns>A task that returns the conversion result.</returns>
        public async Task<ConversionResult<T>> TryConvertAsync(object value, CancellationToken cancellationToken)
        {
            foreach (IAsyncObjectToTypeConverter<T> converter in _converters)
            {
                var result = await converter.TryConvertAsync(value, cancellationToken).ConfigureAwait(false);

                if (result.Succeeded)
                {
                    return result;
                }
            }

            return new ConversionResult<T>
            {
                Succeeded = false,
                Result = default
            };
        }
    }
}
