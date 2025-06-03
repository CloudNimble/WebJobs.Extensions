# Running Integration Tests

This directory contains integration tests for AWS-dependent classes using LocalStack.

## Quick Start

### Option 1: Using LocalStack CLI (Recommended)

#### 1. Install and Start LocalStack

```bash
# Install LocalStack CLI
pip install localstack

# Start LocalStack
localstack start -d

# Or start with only SQS service
SERVICES=sqs localstack start -d

# Verify LocalStack is running
localstack status

# Check LocalStack health
curl http://localhost:4566/_localstack/health
```

#### 2. Run Integration Tests

```bash
# Run all integration tests
dotnet test --filter "TestCategory=Integration"

# Run specific integration test class
dotnet test --filter "FullyQualifiedName~SQSQueueIntegrationTests"

# Run with detailed output
dotnet test --filter "TestCategory=Integration" --logger "console;verbosity=detailed"
```

#### 3. Stop LocalStack

```bash
localstack stop
```

### Option 2: Using Docker

#### 1. Start LocalStack

From the `CloudNimble.WebJobs.Extensions.Tests.Amazon` directory:

```bash
# Start LocalStack
docker-compose up -d

# Verify LocalStack is running
docker-compose ps

# Check LocalStack health
curl http://localhost:4566/_localstack/health
```

#### 2. Run Integration Tests

Same as above.

#### 3. Stop LocalStack

```bash
# Stop LocalStack
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

## Test Categories

- **Integration**: All integration tests that require LocalStack
- **Unit**: Unit tests that don't require external services

## Environment Variables

You can override default settings using environment variables:

```bash
# Windows PowerShell
$env:LOCALSTACK_ENDPOINT="http://localhost:4566"
$env:AWS_DEFAULT_REGION="us-east-1"

# Linux/macOS
export LOCALSTACK_ENDPOINT="http://localhost:4566"
export AWS_DEFAULT_REGION="us-east-1"
```

## Troubleshooting

### Tests are skipped with "LocalStack is not available"

1. Ensure Docker is running
2. Start LocalStack: `docker-compose up -d`
3. Wait for health check: `docker-compose ps` (should show "healthy")

### Connection refused errors

1. Check if LocalStack is running on port 4566
2. Try using `127.0.0.1` instead of `localhost`
3. Check Docker logs: `docker-compose logs localstack`

### Tests fail with authentication errors

LocalStack accepts any credentials, but they must be provided. The tests use "test" for both access key and secret key.

## Writing New Integration Tests

1. Inherit from `LocalStackTestBase`
2. Add `[TestCategory("Integration")]` to your test class
3. Call `await SkipIfLocalStackNotAvailable()` in your test setup
4. Use the helper methods for queue operations

Example:

```csharp
[TestClass]
[TestCategory("Integration")]
public class MyIntegrationTests : LocalStackTestBase
{
    [TestMethod]
    public async Task MyTest()
    {
        await SkipIfLocalStackNotAvailable();
        
        var queueUrl = await CreateTestQueueAsync("my-test");
        // Your test logic here
    }
}
```

## CI/CD Integration

For CI/CD pipelines, see the GitHub Actions and Azure DevOps examples in [LocalStack.md](../LocalStack.md).