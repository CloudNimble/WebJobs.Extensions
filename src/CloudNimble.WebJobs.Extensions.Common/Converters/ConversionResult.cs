// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CloudNimble.WebJobs.Extensions.Common.Converters
{

    /// <summary>
    /// Represents the result of a conversion.
    /// </summary>
    /// <typeparam name="TResult">The <see cref="System.Type"/> of the conversion result.</typeparam>
    public struct ConversionResult<TResult>
    {

        /// <summary>
        /// Gets a value indicating whether the conversion succeeded.
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Gets the conversion result.
        /// </summary>
        public TResult Result { get; set; }

    }

}
