// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{
    /// <summary>
    /// Test implementation of IQueueRequestExceptionClassifier for testing purposes.
    /// </summary>
    public class TestQueueRequestExceptionClassifier : IQueueRequestExceptionClassifier
    {
        public bool IsServerSideExceptionResult { get; set; } = false;
        public bool IsPopReceiptMismatchResult { get; set; } = false;
        public bool IsNotFoundExceptionResult { get; set; } = false;
        public bool IsConflictExceptionResult { get; set; } = false;

        public bool IsServerSideException(Exception exception) => IsServerSideExceptionResult;
        public bool IsPopReceiptMismatch(Exception exception) => IsPopReceiptMismatchResult;
        public bool IsNotFoundException(Exception exception) => IsNotFoundExceptionResult;
        public bool IsConflictException(Exception exception) => IsConflictExceptionResult;
    }

}