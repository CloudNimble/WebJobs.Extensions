using Microsoft.Extensions.Options;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.AI
{

    /// <summary>
    /// Provides configuration options specific to Amazon SQS integration.
    /// </summary>
    public sealed class SqsOptions : IOptions<SqsOptions>
    {

        /// <summary>
        /// Gets or sets the base URL for the SQS service.
        /// </summary>
        public string AccountUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether automatic scaling is enabled.
        /// </summary>
        public bool EnableScaling { get; set; } = true;

        /// <summary>
        /// Gets the current options instance.
        /// </summary>
        public SqsOptions Value => this;

    }

}