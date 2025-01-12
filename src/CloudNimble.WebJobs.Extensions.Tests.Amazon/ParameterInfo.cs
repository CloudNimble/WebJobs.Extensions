// SqsListenerTests.cs
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.AWS
{

    // Helper Classes
    internal class ParameterInfo
    {
        private readonly Attribute _attribute;

        public ParameterInfo(Attribute attribute = null)
        {
            _attribute = attribute;
        }

        public T GetCustomAttribute<T>(bool inherit) where T : Attribute
        {
            return _attribute as T;
        }

        public Type ParameterType => typeof(string);
    }
}