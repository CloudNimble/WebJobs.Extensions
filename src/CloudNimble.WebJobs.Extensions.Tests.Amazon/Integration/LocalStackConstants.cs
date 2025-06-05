// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.Integration
{

    /// <summary>
    /// Contains constants used for LocalStack integration testing.
    /// </summary>
    public static class LocalStackConstants
    {

        #region Environment Variables

        /// <summary>
        /// Environment variable name for LocalStack endpoint.
        /// </summary>
        public const string LocalStackEndpointEnvVar = "LOCALSTACK_ENDPOINT";

        /// <summary>
        /// Environment variable name for SQS endpoint URL.
        /// </summary>
        public const string SqsEndpointUrlEnvVar = "SQS_ENDPOINT_URL";

        #endregion

        #region Default Values

        /// <summary>
        /// Default LocalStack endpoint URL.
        /// </summary>
        public const string DefaultLocalStackEndpoint = "http://localhost:4566";

        /// <summary>
        /// Default test credentials for LocalStack (access key).
        /// </summary>
        public const string DefaultTestAccessKey = "test";

        /// <summary>
        /// Default test credentials for LocalStack (secret key).
        /// </summary>
        public const string DefaultTestSecretKey = "test";

        #endregion

    }

}