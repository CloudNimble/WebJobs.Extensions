# CloudNimble WebJobs Extensions Memory

## Project Overview
This project provides Azure WebJobs extensions for various cloud services, starting with Amazon SQS. It follows Azure WebJobs SDK patterns while providing cloud-agnostic queue processing capabilities.

## Current State (June 2025)

### Completed Work
1. **Dependency Injection Testing Framework**
   - Migrated from TestFactories to DI-based testing using Breakdance
   - Created WebJobsTestBase inheriting from BreakdanceMSTestBase
   - Pattern: Each test calls Setup(), TestHostBuilder configures services, TestSetup() builds host

2. **Amazon SQS Extension**
   - Full implementation of SQS triggers and bindings
   - Support for multiple message types (SQSMessage, string, custom types)
   - Integration with WebJobs scaling and concurrency management
   - Poison message handling and retry logic

3. **Unit Tests (104 passing, 6 skipped)**
   - Comprehensive tests for all non-AWS dependent classes
   - Tests for converters, options, attributes, metrics, etc.

4. **Integration Tests with LocalStack**
   - LocalStack setup documentation and scripts
   - Support for both LocalStack CLI and Docker
   - Integration tests for SQSQueue operations
   - CI/CD pipeline configurations for Azure DevOps and GitHub Actions

5. **Examples Project**
   - Timer-triggered message publisher (publishes every 30 seconds)
   - SQS-triggered message processors (3 patterns: raw, string, typed)
   - Full AWS configuration support
   - Scripts for running examples (run-example.sh, run-example.ps1)

### Key Technical Fixes Applied
1. **Converter Architecture Fix**
   - Created QueueMessageConverterAdapter to handle IConverter<SQSMessage, T> to IConverter<IQueueMessage, T> conversion
   - Created SQSMessageArgumentBindingAdapter for ITriggerDataArgumentBinding adaptation
   - Fixed variance issues in C# generic type system

2. **Dependency Registration**
   - Fixed SQSExtensionConfigProvider expecting AmazonSQSClient instead of IAmazonSQS
   - Register both concrete type and interface in DI container

3. **Configuration Updates**
   - Removed AzureStorageCoreServices dependency (not needed for SQS-only scenarios)
   - Added proper package references for all required dependencies

### Project Structure
- CloudNimble.WebJobs.Extensions.Common - Base classes and interfaces
- CloudNimble.WebJobs.Extensions.Amazon - Amazon SQS implementation
- CloudNimble.WebJobs.Extensions.Examples - Working examples with timer and SQS triggers
- CloudNimble.WebJobs.Extensions.Tests.Common - Common test infrastructure
- CloudNimble.WebJobs.Extensions.Tests.Amazon - Amazon-specific tests

### Current Issues
1. Examples project needs AWS credentials configured to run against real AWS
2. Queue listeners try to poll before queue exists (needs graceful handling)
3. Some XML documentation comments are incomplete

### Next Steps
1. Add graceful queue creation in listeners
2. Complete EventBridge and SNS implementations
3. Add more cloud providers (Azure Service Bus, Google Cloud Pub/Sub)
4. Enhance examples with more real-world scenarios

### Important Files
- Program.cs in Examples - Shows full DI setup and AWS configuration
- SQSTriggerAttributeBindingProvider.cs - Core trigger binding logic
- QueueMessageConverterAdapter.cs - Key fix for converter variance
- LocalStack.md - Comprehensive LocalStack setup guide
- Integration tests in Tests.Amazon/Integration/

### Running the Examples
1. Configure AWS credentials (environment vars, profile, or user secrets)
2. Or use LocalStack: set ServiceUrl to http://localhost:4566
3. Run: dotnet run --project CloudNimble.WebJobs.Extensions.Examples
4. Timer publishes messages every 30 seconds at :00 and :30
5. SQS triggers process messages immediately

### Testing
- Unit tests: dotnet test --filter \TestCategory!=Integration\
- Integration tests (requires LocalStack): dotnet test --filter \TestCategory=Integration\
- All tests: dotnet test

## Key Patterns and Decisions
1. Following Azure WebJobs SDK patterns for consistency
2. Using interfaces (IQueueMessage, IQueueClient) for abstraction
3. Comprehensive converter system for message type flexibility
4. LocalStack-first development for cost-effective testing
5. MSTest v3 with FluentAssertions for testing
6. Breakdance for DI-based test infrastructure
