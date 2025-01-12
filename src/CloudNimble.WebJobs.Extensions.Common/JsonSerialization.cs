// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudNimble.WebJobs.Extensions.Common
{

    /// <summary>
    /// Provides the standard <see cref="JsonSerializerOptions"/> used by protocol data.
    /// </summary>
    public static class JsonSerialization
    {

        #region Private Members

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            // The default value, DateParseHandling.DateTime, drops time zone information from DateTimeOffets.
            // This value appears to work well with both DateTimes (without time zone information) and DateTimeOffsets.
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        #endregion

        #region Static Properties

        /// <summary>
        /// Gets the standard <see cref="JsonSerializerOptions"/> used by protocol data.
        /// </summary>
        public static JsonSerializerOptions Options => JsonSerializerOptions;

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="input">
        /// The object to serialize.
        /// </param>
        /// <returns>
        /// A <see cref="bool"/> indicating whether the input is a JSON object.
        /// </returns>
        public static bool IsJsonObject(string input)
        {
            if (input is null or { Length: 0 })
            {
                return false;
            }

            var trimmedInput = input.AsSpan().Trim();
            return trimmedInput.StartsWith("{", StringComparison.OrdinalIgnoreCase) && trimmedInput.EndsWith("}", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Creates a new instance of <see cref="Utf8JsonReader"/> for the provided stream.
        /// </summary>
        /// <param name="stream">The stream to read JSON data from.</param>
        /// <returns>A new instance of <see cref="Utf8JsonReader"/>.</returns>
        internal static Utf8JsonReader CreateJsonTextReader(Stream stream)
        {
            return new Utf8JsonReader(new ReadOnlySpan<byte>(new byte[stream.Length]));
        }

        /// <summary>
        /// Creates a new instance of <see cref="Utf8JsonWriter"/> for the provided stream.
        /// </summary>
        /// <param name="stream">The stream to write JSON data to.</param>
        /// <returns>A new instance of <see cref="Utf8JsonWriter"/>.</returns>
        internal static Utf8JsonWriter CreateJsonTextWriter(Stream stream)
        {
            return new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = JsonSerializerOptions.WriteIndented });
        }

        /// <summary>
        /// Parses the provided JSON string into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <returns>A <see cref="JsonDocument"/> representing the parsed JSON object, or null if the input is not a valid JSON object.</returns>
        internal static JsonDocument ParseJsonObject(string json)
        {
            Ensure.ArgumentNotNull(json, nameof(json));

            if (!IsJsonObject(json)) return null;

            return JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        }

        #endregion

    }

}
