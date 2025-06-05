// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace CloudNimble.WebJobs.Extensions.Amazon
{

    /// <summary>
    /// Contains constants used throughout the Amazon WebJobs extensions.
    /// </summary>
    public static class AmazonConstants
    {

        #region Extension Names

        /// <summary>
        /// The name of the SQS WebJobs extension.
        /// </summary>
        internal const string SQSExtensionName = "AmazonSQS";

        /// <summary>
        /// The configuration section name for SQS queue configuration.
        /// </summary>
        public const string SQSConfigSectionName = "Amazon:SQS:Queues";

        #endregion

        #region Configuration Sections

        /// <summary>
        /// The main AWS configuration section name.
        /// </summary>
        public const string AwsConfigSection = "AWS";

        /// <summary>
        /// The SQS configuration section name.
        /// </summary>
        public const string SqsConfigSection = "SQS";

        #endregion

        #region Configuration Keys

        /// <summary>
        /// Configuration key for AWS region.
        /// </summary>
        public const string AwsRegionKey = "AWS:Region";

        /// <summary>
        /// Configuration key for SQS service URL.
        /// </summary>
        public const string SqsServiceUrlKey = "SQS:ServiceUrl";

        /// <summary>
        /// Configuration key for AWS profile.
        /// </summary>
        public const string ProfileKey = "Profile";

        /// <summary>
        /// Configuration key for AWS region (within section).
        /// </summary>
        public const string RegionKey = "Region";

        #endregion

        #region Environment Variables

        /// <summary>
        /// Environment variable name for AWS default region.
        /// </summary>
        public const string AwsDefaultRegionEnvVar = "AWS_DEFAULT_REGION";

        /// <summary>
        /// Environment variable name for AWS access key ID.
        /// </summary>
        public const string AwsAccessKeyIdEnvVar = "AWS_ACCESS_KEY_ID";

        /// <summary>
        /// Environment variable name for AWS secret access key.
        /// </summary>
        public const string AwsSecretAccessKeyEnvVar = "AWS_SECRET_ACCESS_KEY";

        #endregion

        #region Default Values

        /// <summary>
        /// Default AWS region.
        /// </summary>
        public const string DefaultAwsRegion = "us-east-1";

        #endregion

    }
}
