// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{
    /// <summary>
    /// Test implementation of ILogger&lt;T&gt; for testing purposes.
    /// </summary>
    public class TestLogger<T> : TestLogger, ILogger<T>
    {
    }

}