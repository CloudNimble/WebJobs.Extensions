// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Timers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Timers
{

    /// <summary>
    /// Unit tests for the <see cref="TaskSeriesCommandResult"/> struct.
    /// </summary>
    [TestClass]
    public class TaskSeriesCommandResultTests
    {

        #region Constructor Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithCompletedTask_ShouldSetWaitProperty()
        {
            // Arrange
            var task = Task.CompletedTask;

            // Act
            var result = new TaskSeriesCommandResult(task);

            // Assert
            result.Wait.Should().BeSameAs(task);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithDelayTask_ShouldSetWaitProperty()
        {
            // Arrange
            var task = Task.Delay(100);

            // Act
            var result = new TaskSeriesCommandResult(task);

            // Assert
            result.Wait.Should().BeSameAs(task);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNullTask_ShouldSetWaitToNull()
        {
            // Arrange
            Task task = null;

            // Act
            var result = new TaskSeriesCommandResult(task);

            // Assert
            result.Wait.Should().BeNull();
        }

        #endregion

        #region Wait Property Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void Wait_ShouldReturnSameTaskAsConstructor()
        {
            // Arrange
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;

            // Act
            var result = new TaskSeriesCommandResult(task);

            // Assert
            result.Wait.Should().BeSameAs(task);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Wait_WithDefaultConstructor_ShouldReturnNull()
        {
            // Arrange & Act
            var result = default(TaskSeriesCommandResult);

            // Assert
            result.Wait.Should().BeNull();
        }

        #endregion

        #region Struct Behavior Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void TaskSeriesCommandResult_ShouldBeValueType()
        {
            // Arrange & Act
            var type = typeof(TaskSeriesCommandResult);

            // Assert
            type.IsValueType.Should().BeTrue();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void TaskSeriesCommandResult_Equality_ShouldCompareByValue()
        {
            // Arrange
            var task = Task.CompletedTask;
            var result1 = new TaskSeriesCommandResult(task);
            var result2 = new TaskSeriesCommandResult(task);

            // Act & Assert
            result1.Should().Be(result2);
            result1.Equals(result2).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void TaskSeriesCommandResult_WithDifferentTasks_ShouldNotBeEqual()
        {
            // Arrange
            var task1 = Task.Delay(100);
            var task2 = Task.Delay(100);
            var result1 = new TaskSeriesCommandResult(task1);
            var result2 = new TaskSeriesCommandResult(task2);

            // Act & Assert
            result1.Should().NotBe(result2);
            result1.Equals(result2).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void TaskSeriesCommandResult_Copy_ShouldCreateIndependentInstance()
        {
            // Arrange
            var task = Task.CompletedTask;
            var original = new TaskSeriesCommandResult(task);

            // Act
            var copy = original; // Value type copy

            // Assert
            copy.Wait.Should().BeSameAs(original.Wait);
        }

        #endregion

    }

}