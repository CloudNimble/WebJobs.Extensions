// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using System;
using System.Reflection;

namespace CloudNimble.WebJobs.Extensions.Common
{


    /// <summary>
    /// Provides utility methods for working with types and attributes in the context of Azure WebJobs.
    /// </summary>
    public static class TypeUtility
    {

        /// <summary>
        /// Walks from the parameter up to the containing type, looking for an instance
        /// of the specified attribute type, returning it if found.
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        /// <param name="attributeType">The attribute type to look for.</param>
        /// <returns>The found attribute, or null if not found.</returns>
        public static Attribute GetHierarchicalAttributeOrNull(ParameterInfo parameter, Type attributeType)
        {
            if (parameter is null) return null;

            var attribute = parameter.GetCustomAttribute(attributeType);
            if (attribute is not null)
            {
                return attribute;
            }

            if (parameter.Member is MethodInfo method)
            {
                return GetHierarchicalAttributeOrNull(method, attributeType);
            }

            return null;
        }

        /// <summary>
        /// Walks from the method up to the containing type, looking for an instance
        /// of the specified attribute type, returning it if found.
        /// </summary>
        /// <param name="method">The method to check.</param>
        /// <param name="type">The attribute type to look for.</param>
        /// <returns>The found attribute, or null if not found.</returns>
        public static Attribute GetHierarchicalAttributeOrNull(MethodInfo method, Type type)
        {
            return method.GetCustomAttribute(type) ??
                   method.DeclaringType.GetCustomAttribute(type);
        }

        /// <summary>
        /// Resolves the specified attribute for the given parameter, potentially walking up the hierarchy
        /// to find an override specified via another attribute.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute to resolve.</typeparam>
        /// <param name="parameter">The parameter to check.</param>
        /// <returns>The resolved attribute, or null if not found.</returns>
        public static TAttribute GetResolvedAttribute<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute
        {
            var attribute = parameter.GetCustomAttribute<TAttribute>();

            if (attribute is IConnectionProvider attributeConnectionProvider && string.IsNullOrEmpty(attributeConnectionProvider.Connection))
            {
                // if the attribute doesn't specify an explicit connection, walk up
                // the hierarchy looking for an override specified via attribute
                if (attribute.GetType().GetCustomAttribute<ConnectionProviderAttribute>() is { ProviderType: not null } connectionProviderAttribute)
                {
                    if (GetHierarchicalAttributeOrNull(parameter, connectionProviderAttribute.ProviderType) is IConnectionProvider connectionOverrideProvider &&
                        !string.IsNullOrEmpty(connectionOverrideProvider.Connection))
                    {
                        attributeConnectionProvider.Connection = connectionOverrideProvider.Connection;
                    }
                }
            }

            return attribute;
        }

    }

}
