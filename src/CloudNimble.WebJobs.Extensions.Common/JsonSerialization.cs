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
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static readonly JsonReaderOptions JsonReaderOptions = new()
        {
            CommentHandling = JsonCommentHandling.Skip
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
        /// Determines whether the specified string represents a valid JSON object.
        /// </summary>
        /// <param name="input">The string to validate.</param>
        /// <returns>
        /// <c>true</c> if the input is a valid JSON object; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method performs both syntactic validation (proper JSON format) and semantic validation 
        /// (ensures the root element is a JSON object, not an array, string, or other value type).
        /// Uses efficient UTF-8 parsing without allocating intermediate objects.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool isValid1 = JsonSerialization.IsJsonObject("{\"name\":\"value\"}"); // true
        /// bool isValid2 = JsonSerialization.IsJsonObject("{invalid}");           // false
        /// bool isValid3 = JsonSerialization.IsJsonObject("[1,2,3]");             // false (array)
        /// bool isValid4 = JsonSerialization.IsJsonObject("\"string\"");          // false (string)
        /// </code>
        /// </example>
        public static bool IsJsonObject(string input)
        {
            if (input is null or { Length: 0 })
            {
                return false;
            }

            var trimmedInput = input.AsSpan().Trim();
            
            // Quick check for basic object format - must start with { and end with }
            if (!trimmedInput.StartsWith("{") || !trimmedInput.EndsWith("}"))
            {
                return false;
            }

            // Validate JSON syntax and ensure root element is an object
            return ValidateJsonObject(input);
        }

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Creates a new instance of <see cref="Utf8JsonReader"/> for the provided stream.
        /// </summary>
        /// <param name="stream">The stream to read JSON data from.</param>
        /// <returns>A new instance of <see cref="Utf8JsonReader"/>.</returns>
        /// <remarks>
        /// The caller is responsible for managing the lifetime of the stream and any allocated buffers.
        /// For efficient memory usage, consider using <see cref="JsonDocument.Parse(Stream, JsonDocumentOptions)"/> directly for most scenarios.
        /// </remarks>
        internal static Utf8JsonReader CreateJsonTextReader(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            // Read stream content to buffer for Utf8JsonReader
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var buffer = memoryStream.ToArray();
            
            return new Utf8JsonReader(buffer, JsonReaderOptions);
        }

        /// <summary>
        /// Creates a new instance of <see cref="Utf8JsonWriter"/> for the provided stream.
        /// </summary>
        /// <param name="stream">The stream to write JSON data to.</param>
        /// <returns>A new instance of <see cref="Utf8JsonWriter"/>.</returns>
        internal static Utf8JsonWriter CreateJsonTextWriter(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            
            return new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = JsonSerializerOptions.WriteIndented });
        }

        /// <summary>
        /// Parses the provided JSON string into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <returns>A <see cref="JsonDocument"/> representing the parsed JSON object, or null if the input is not a valid JSON object.</returns>
        /// <remarks>
        /// This method performs validation and parsing in a single operation for efficiency.
        /// The caller is responsible for disposing the returned <see cref="JsonDocument"/>.
        /// </remarks>
        internal static JsonDocument ParseJsonObject(string json)
        {
            Ensure.ArgumentNotNull(json, nameof(json));

            try
            {
                var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
                
                // Check if it's actually an object
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return document;
                }
                
                // Not an object, dispose and return null
                document.Dispose();
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Efficiently validates if the input is a valid JSON object using JsonDocument.
        /// </summary>
        /// <param name="input">The JSON string to validate.</param>
        /// <returns>True if the input is a valid JSON object; otherwise, false.</returns>
        private static bool ValidateJsonObject(string input)
        {
            try
            {
                using var document = JsonDocument.Parse(input, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
                return document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        #endregion

    }

}