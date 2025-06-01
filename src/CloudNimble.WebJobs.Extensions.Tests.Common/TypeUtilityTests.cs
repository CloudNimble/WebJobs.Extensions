// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace CloudNimble.WebJobs.Extensions.Tests.Common
{

    /// <summary>
    /// Unit tests for the <see cref="TypeUtility"/> class.
    /// </summary>
    [TestClass]
    public class TypeUtilityTests
    {

        #region GetHierarchicalAttributeOrNull Tests - ParameterInfo

        [TestMethod]
        [TestCategory("Unit")]
        public void GetHierarchicalAttributeOrNull_WithNullParameter_ShouldReturnNull()
        {
            // Arrange
            ParameterInfo parameter = null;

            // Act
            var result = TypeUtility.GetHierarchicalAttributeOrNull(parameter, typeof(TestAttribute));

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetHierarchicalAttributeOrNull_WithAttributeOnParameter_ShouldReturnAttribute()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithParameterAttribute));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetHierarchicalAttributeOrNull(parameter, typeof(TestAttribute));

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<TestAttribute>();
            ((TestAttribute)result).Value.Should().Be("ParameterValue");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetHierarchicalAttributeOrNull_WithAttributeOnMethod_ShouldReturnAttribute()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithMethodAttribute));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetHierarchicalAttributeOrNull(parameter, typeof(TestAttribute));

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<TestAttribute>();
            ((TestAttribute)result).Value.Should().Be("MethodValue");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetHierarchicalAttributeOrNull_WithAttributeOnClass_ShouldReturnAttribute()
        {
            // Arrange
            var method = typeof(TestClassWithAttribute).GetMethod(nameof(TestClassWithAttribute.SimpleMethod));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetHierarchicalAttributeOrNull(parameter, typeof(TestAttribute));

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<TestAttribute>();
            ((TestAttribute)result).Value.Should().Be("ClassValue");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetHierarchicalAttributeOrNull_WithNoAttribute_ShouldReturnNull()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithoutAttribute));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetHierarchicalAttributeOrNull(parameter, typeof(TestAttribute));

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetHierarchicalAttributeOrNull_PriorityOrder_ParameterOverMethod()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithBothAttributes));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetHierarchicalAttributeOrNull(parameter, typeof(TestAttribute));

            // Assert
            result.Should().NotBeNull();
            ((TestAttribute)result).Value.Should().Be("ParameterOverride");
        }

        #endregion

        #region GetHierarchicalAttributeOrNull Tests - MethodInfo

        [TestMethod]
        [TestCategory("Unit")]
        public void GetHierarchicalAttributeOrNull_Method_WithAttributeOnMethod_ShouldReturnAttribute()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithMethodAttribute));

            // Act
            var result = TypeUtility.GetHierarchicalAttributeOrNull(method, typeof(TestAttribute));

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<TestAttribute>();
            ((TestAttribute)result).Value.Should().Be("MethodValue");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetHierarchicalAttributeOrNull_Method_WithAttributeOnClass_ShouldReturnAttribute()
        {
            // Arrange
            var method = typeof(TestClassWithAttribute).GetMethod(nameof(TestClassWithAttribute.SimpleMethod));

            // Act
            var result = TypeUtility.GetHierarchicalAttributeOrNull(method, typeof(TestAttribute));

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<TestAttribute>();
            ((TestAttribute)result).Value.Should().Be("ClassValue");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetHierarchicalAttributeOrNull_Method_PriorityOrder_MethodOverClass()
        {
            // Arrange
            var method = typeof(TestClassWithAttribute).GetMethod(nameof(TestClassWithAttribute.MethodWithOverride));

            // Act
            var result = TypeUtility.GetHierarchicalAttributeOrNull(method, typeof(TestAttribute));

            // Assert
            result.Should().NotBeNull();
            ((TestAttribute)result).Value.Should().Be("MethodOverride");
        }

        #endregion

        #region GetResolvedAttribute Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void GetResolvedAttribute_WithSimpleAttribute_ShouldReturnAttribute()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithParameterAttribute));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetResolvedAttribute<TestAttribute>(parameter);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().Be("ParameterValue");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetResolvedAttribute_WithNoAttribute_ShouldReturnNull()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithoutAttribute));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetResolvedAttribute<TestAttribute>(parameter);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetResolvedAttribute_WithConnectionProvider_EmptyConnection_ShouldResolveFromHierarchy()
        {
            // Arrange
            var method = typeof(TestConnectionClass).GetMethod(nameof(TestConnectionClass.MethodWithEmptyConnection));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetResolvedAttribute<TestConnectionAttribute>(parameter);

            // Assert
            result.Should().NotBeNull();
            result.Connection.Should().Be("ClassConnection");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetResolvedAttribute_WithConnectionProvider_ExplicitConnection_ShouldNotOverride()
        {
            // Arrange
            var method = typeof(TestConnectionClass).GetMethod(nameof(TestConnectionClass.MethodWithExplicitConnection));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetResolvedAttribute<TestConnectionAttribute>(parameter);

            // Assert
            result.Should().NotBeNull();
            result.Connection.Should().Be("ExplicitConnection");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetResolvedAttribute_WithConnectionProvider_NoHierarchicalOverride_ShouldKeepEmpty()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithConnectionAttribute));
            var parameter = method.GetParameters()[0];

            // Act
            var result = TypeUtility.GetResolvedAttribute<TestConnectionAttribute>(parameter);

            // Assert
            result.Should().NotBeNull();
            result.Connection.Should().BeNull();
        }

        #endregion

        #region Test Classes and Attributes

        private class TestAttribute : Attribute
        {
            public string Value { get; }
            public TestAttribute(string value)
            {
                Value = value;
            }
        }

        [ConnectionProvider(typeof(TestConnectionProviderAttribute))]
        private class TestConnectionAttribute : Attribute, IConnectionProvider
        {
            public string Connection { get; set; }
        }

        private class TestConnectionProviderAttribute : Attribute, IConnectionProvider
        {
            public string Connection { get; set; }
            public TestConnectionProviderAttribute(string connection = null)
            {
                Connection = connection;
            }
        }

        private class TestClass
        {
            public void MethodWithParameterAttribute([TestAttribute("ParameterValue")] string param) { }

            [TestAttribute("MethodValue")]
            public void MethodWithMethodAttribute(string param) { }

            public void MethodWithoutAttribute(string param) { }

            [TestAttribute("MethodValue")]
            public void MethodWithBothAttributes([TestAttribute("ParameterOverride")] string param) { }

            public void MethodWithConnectionAttribute([TestConnectionAttribute] string param) { }
        }

        [TestAttribute("ClassValue")]
        private class TestClassWithAttribute
        {
            public void SimpleMethod(string param) { }

            [TestAttribute("MethodOverride")]
            public void MethodWithOverride(string param) { }
        }

        [TestConnectionProviderAttribute("ClassConnection")]
        private class TestConnectionClass
        {
            public void MethodWithEmptyConnection([TestConnectionAttribute] string param) { }

            public void MethodWithExplicitConnection([TestConnectionAttribute(Connection = "ExplicitConnection")] string param) { }
        }

        #endregion

    }

}