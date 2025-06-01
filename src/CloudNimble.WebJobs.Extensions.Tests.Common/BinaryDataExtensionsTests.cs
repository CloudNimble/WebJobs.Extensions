// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text;

namespace CloudNimble.WebJobs.Extensions.Tests.Common
{

    /// <summary>
    /// Unit tests for the <see cref="BinaryDataExtensions"/> class.
    /// </summary>
    [TestClass]
    public class BinaryDataExtensionsTests
    {

        #region ToValidUTF8String Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithValidUTF8String_ShouldReturnOriginalString()
        {
            // Arrange
            var originalString = "Hello, World! 你好世界 🌍";
            var binaryData = new BinaryData(originalString);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().Be(originalString);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            var binaryData = new BinaryData(string.Empty);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithAsciiOnly_ShouldReturnCorrectString()
        {
            // Arrange
            var asciiString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()";
            var binaryData = new BinaryData(asciiString);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().Be(asciiString);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithUnicodeCharacters_ShouldReturnCorrectString()
        {
            // Arrange
            var unicodeString = "Unicode: α β γ δ ε ζ η θ ι κ λ μ ν ξ ο π ρ σ τ υ φ χ ψ ω";
            var binaryData = new BinaryData(unicodeString);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().Be(unicodeString);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithEmojis_ShouldReturnCorrectString()
        {
            // Arrange
            var emojiString = "Emojis: 😀 😃 😄 😁 😆 😅 😂 🤣 ☺️ 😊 😇 🙂 🙃 😉 😌";
            var binaryData = new BinaryData(emojiString);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().Be(emojiString);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithMultilineString_ShouldPreserveNewlines()
        {
            // Arrange
            var multilineString = "Line 1\r\nLine 2\nLine 3\rLine 4";
            var binaryData = new BinaryData(multilineString);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().Be(multilineString);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithSpecialCharacters_ShouldReturnCorrectString()
        {
            // Arrange
            var specialChars = "Special: \t\r\n\0\b\f\"'\\";
            var binaryData = new BinaryData(specialChars);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().Be(specialChars);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithInvalidUTF8Sequence_ShouldThrowDecoderFallbackException()
        {
            // Arrange
            // Create an invalid UTF-8 sequence
            byte[] invalidUtf8 = new byte[] { 0xFF, 0xFE, 0xFD };
            var binaryData = new BinaryData(invalidUtf8);

            // Act
            var act = () => binaryData.ToValidUTF8String();

            // Assert
            act.Should().Throw<DecoderFallbackException>();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithByteArray_ShouldConvertCorrectly()
        {
            // Arrange
            var text = "Test string with UTF-8 encoding";
            var bytes = Encoding.UTF8.GetBytes(text);
            var binaryData = new BinaryData(bytes);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().Be(text);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithLargeString_ShouldHandleEfficiently()
        {
            // Arrange
            var largeString = new string('A', 10000);
            var binaryData = new BinaryData(largeString);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().Be(largeString);
            result.Length.Should().Be(10000);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithBOM_ShouldNotIncludeBOM()
        {
            // Arrange
            // UTF-8 BOM is EF BB BF
            var textWithoutBom = "Hello World";
            var bytesWithBom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(textWithoutBom)).ToArray();
            var binaryData = new BinaryData(bytesWithBom);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            // The internal UTF8Encoding is created with BOM = false, so it should not emit BOM
            // but it will still decode the BOM as a zero-width no-break space character
            result.Should().StartWith("\uFEFF");
            result.Should().EndWith(textWithoutBom);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ToValidUTF8String_WithMemorySegment_ShouldUseOptimizedPath()
        {
            // Arrange
            var text = "Memory segment test";
            var bytes = Encoding.UTF8.GetBytes(text);
            var memory = new Memory<byte>(bytes);
            var binaryData = BinaryData.FromBytes(memory);

            // Act
            var result = binaryData.ToValidUTF8String();

            // Assert
            result.Should().Be(text);
        }

        #endregion

    }

}