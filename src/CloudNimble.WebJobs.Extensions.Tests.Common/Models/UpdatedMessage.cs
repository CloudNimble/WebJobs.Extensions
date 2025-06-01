// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{
    /// <summary>
    /// Represents an updated message for testing tracking.
    /// </summary>
    public class UpdatedMessage
    {
        public string Id { get; set; }
        public string PopReceipt { get; set; }
        public TimeSpan VisibilityTimeout { get; set; }
    }

}