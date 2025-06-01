// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Converters
{
    /// <summary>
    /// An asynchronous converter that delegates to an inner synchronous converter.
    /// </summary>
    /// <typeparam name="TInput">The type to convert from.</typeparam>
    /// <typeparam name="TOutput">The type to convert to.</typeparam>
    public class AsyncConverter<TInput, TOutput> : IAsyncConverter<TInput, TOutput>
    {

        #region Fields

        private readonly IConverter<TInput, TOutput> _innerConverter;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="innerConverter">The inner converter to delegate to.</param>
        public AsyncConverter(IConverter<TInput, TOutput> innerConverter)
        {
            ArgumentNullException.ThrowIfNull(innerConverter);
            _innerConverter = innerConverter;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public Task<TOutput> ConvertAsync(TInput input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = _innerConverter.Convert(input);
            return Task.FromResult(result);
        }

        #endregion

    }

}