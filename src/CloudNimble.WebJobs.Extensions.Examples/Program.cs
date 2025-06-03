using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace CloudNimble.WebJobs.Extensions.Examples
{
    /// <summary>
    /// Main entry point for the WebJobs example application.
    /// Demonstrates using Amazon SQS with Azure WebJobs.
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();

                using (host)
                {
                    Console.WriteLine("Starting WebJobs host with Amazon SQS support...");
                    Console.WriteLine("Press Ctrl+C to exit.");
                    host.Run();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Host terminated unexpectedly: {ex.Message}");
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                .ConfigureHostConfiguration(configBuilder =>
                {
                    configBuilder.SetBasePath(Directory.GetCurrentDirectory());
                    configBuilder.AddEnvironmentVariables(prefix: "WEBJOBS_");
                })
                .ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);

                    // Add user secrets in development
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        configBuilder.AddUserSecrets<Program>(optional: true);
                    }
                })
                .ConfigureWebJobs((context, webJobsBuilder) =>
                {
                    // Add WebJobs services
                    webJobsBuilder.AddTimers();
                    
                    // Add Amazon SQS support
                    webJobsBuilder.AddAmazonSQS();
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Configure AWS SDK
                    ConfigureAwsServices(services, configuration);

                    // Configure SQS options
                    services.Configure<SQSOptions>(options =>
                    {
                        var sqsConfig = configuration.GetSection("SQS");
                        sqsConfig.Bind(options);
                    });

                    // Add function classes as services
                    services.AddScoped<Functions.MessagePublisherFunction>();
                    services.AddScoped<Functions.MessageProcessorFunction>();
                })
                .ConfigureLogging((context, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));
                    loggingBuilder.AddConsole();
                    
                    // Add debug logging in development
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        loggingBuilder.AddDebug();
                    }

                    // Configure log levels
                    loggingBuilder.SetMinimumLevel(LogLevel.Information);
                    loggingBuilder.AddFilter("System", LogLevel.Warning);
                    loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
                    loggingBuilder.AddFilter("Function", LogLevel.Information);
                })
                .UseConsoleLifetime();

        private static void ConfigureAwsServices(IServiceCollection services, IConfiguration configuration)
        {
            // Get AWS configuration
            var awsConfig = configuration.GetSection("AWS");
            var region = awsConfig["Region"] ?? "us-east-1";
            var profile = awsConfig["Profile"];

            // Create AWS credentials
            AWSCredentials credentials;
            
            // Check for explicit credentials in environment or config
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? awsConfig["AccessKey"];
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? awsConfig["SecretKey"];
            
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                credentials = new BasicAWSCredentials(accessKey, secretKey);
            }
            else if (!string.IsNullOrEmpty(profile))
            {
                // Use AWS profile with modern credential management
                var chain = new CredentialProfileStoreChain();
                if (chain.TryGetAWSCredentials(profile, out var profileCredentials))
                {
                    credentials = profileCredentials;
                }
                else
                {
                    throw new InvalidOperationException($"AWS profile '{profile}' not found.");
                }
            }
            else
            {
                // Use default credential chain (IAM role, etc.)
                credentials = FallbackCredentialsFactory.GetCredentials();
            }

            // Register SQS client - both as interface and concrete type
            services.AddSingleton<AmazonSQSClient>(provider =>
            {
                var sqsConfig = new AmazonSQSConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(region)
                };

                // Override service URL if specified (e.g., for LocalStack)
                var serviceUrl = configuration["SQS:ServiceUrl"];
                if (!string.IsNullOrEmpty(serviceUrl))
                {
                    sqsConfig.ServiceURL = serviceUrl;
                }

                return new AmazonSQSClient(credentials, sqsConfig);
            });
            
            // Also register as interface
            services.AddSingleton<IAmazonSQS>(provider => provider.GetRequiredService<AmazonSQSClient>());
        }
    }
}
