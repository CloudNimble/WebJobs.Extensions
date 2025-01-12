// SqsListenerTests.cs
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;


// SqsQueueProcessorTests.cs
namespace CloudNimble.WebJobs.Extensions.AWS.Tests
{
    // SqsValueProviderTests.cs
    [TestClass]
    public class SqsValueProviderTests
    {

        [TestMethod]
        public void Constructor_WhenValueIsNull_ShouldHandleNullValue()
        {
            // Act
            var provider = new SqsValueProvider(null);

            // Assert
            provider.Type.Should().Be(typeof(object));
        }

        [TestMethod]
        public async Task GetValueAsync_WhenCalled_ShouldReturnOriginalValue()
        {
            // Arrange
            var testValue = "test value";
            var provider = new SqsValueProvider(testValue);

            // Act
            var result = await provider.GetValueAsync();

            // Assert
            result.Should().Be(testValue);
        }

        [TestMethod]
        public void ToInvokeString_WhenValueExists_ShouldReturnStringRepresentation()
        {
            // Arrange
            var testValue = "test value";
            var provider = new SqsValueProvider(testValue);

            // Act
            var result = provider.ToInvokeString();

            // Assert
            result.Should().Be(testValue);
        }

        [TestMethod]
        public void ToInvokeString_WhenValueIsNull_ShouldReturnEmptyString()
        {
            // Arrange
            var provider = new SqsValueProvider(null);

            // Act
            var result = provider.ToInvokeString();

            // Assert
            result.Should().BeEmpty();
        }
    }
}