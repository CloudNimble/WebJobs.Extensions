# LocalStack Setup Guide

This guide provides instructions for setting up LocalStack to run integration tests for AWS-dependent classes in the CloudNimble.WebJobs.Extensions.Amazon project.

## What is LocalStack?

LocalStack is a cloud service emulator that runs in a single container on your local machine. It provides a fully functional local AWS cloud stack that allows you to develop and test AWS cloud applications offline.

## Prerequisites

- Python 3.8 or later (for LocalStack CLI)
- .NET SDK 8.0 or later
- AWS CLI (optional, for manual testing)
- Docker (optional, if not using LocalStack CLI)

## Installation

### Option 1: LocalStack CLI (Recommended for CI/CD)

#### Windows

1. **Install Python** (if not already installed)
   ```powershell
   # Using Chocolatey
   choco install python
   
   # Or download from: https://www.python.org/downloads/
   ```

2. **Install LocalStack CLI**
   ```powershell
   pip install localstack
   ```

3. **Verify Installation**
   ```powershell
   localstack --version
   ```

#### Linux/macOS

1. **Install LocalStack CLI**
   ```bash
   # Using pip
   pip install localstack
   
   # Using pipx (recommended)
   pipx install localstack
   
   # Using Homebrew (macOS/Linux)
   brew install localstack
   ```

2. **Verify Installation**
   ```bash
   localstack --version
   ```

### Option 2: Docker (Alternative)

#### Windows - Using Docker Desktop

1. **Install Docker Desktop**
   ```powershell
   # Using Chocolatey
   choco install docker-desktop
   
   # Or download from: https://www.docker.com/products/docker-desktop
   ```

2. **Start Docker Desktop**
   - Launch Docker Desktop from the Start Menu
   - Wait for Docker to start (system tray icon will be stable)

3. **Pull LocalStack Docker Image**
   ```powershell
   docker pull localstack/localstack:latest
   ```

4. **Run LocalStack**
   ```powershell
   # Basic setup
   docker run -d `
     --name localstack `
     -p 4566:4566 `
     -p 4510-4559:4510-4559 `
     -e SERVICES=sqs `
     -e DEBUG=1 `
     -e DATA_DIR=/tmp/localstack/data `
     -v ${env:USERPROFILE}\.localstack:/tmp/localstack `
     -v /var/run/docker.sock:/var/run/docker.sock `
     localstack/localstack
   ```

#### Option 2: Using Docker Compose

1. **Create `docker-compose.yml`** in the test project directory:
   ```yaml
   version: '3.8'
   services:
     localstack:
       image: localstack/localstack:latest
       container_name: localstack-sqs
       ports:
         - "4566:4566"
         - "4510-4559:4510-4559"
       environment:
         - SERVICES=sqs
         - DEBUG=1
         - DATA_DIR=/tmp/localstack/data
         - DOCKER_HOST=unix:///var/run/docker.sock
       volumes:
         - "${USERPROFILE}/.localstack:/tmp/localstack"
         - "/var/run/docker.sock:/var/run/docker.sock"
   ```

2. **Start LocalStack**
   ```powershell
   docker-compose up -d
   ```

### Linux

#### Option 1: Using Docker

1. **Install Docker**
   ```bash
   # Ubuntu/Debian
   sudo apt-get update
   sudo apt-get install docker.io docker-compose
   
   # RHEL/CentOS/Fedora
   sudo yum install docker docker-compose
   
   # Arch
   sudo pacman -S docker docker-compose
   ```

2. **Start Docker Service**
   ```bash
   sudo systemctl start docker
   sudo systemctl enable docker
   
   # Add your user to the docker group (logout/login required)
   sudo usermod -aG docker $USER
   ```

3. **Pull LocalStack Docker Image**
   ```bash
   docker pull localstack/localstack:latest
   ```

4. **Run LocalStack**
   ```bash
   # Basic setup
   docker run -d \
     --name localstack \
     -p 4566:4566 \
     -p 4510-4559:4510-4559 \
     -e SERVICES=sqs \
     -e DEBUG=1 \
     -e DATA_DIR=/tmp/localstack/data \
     -v ~/.localstack:/tmp/localstack \
     -v /var/run/docker.sock:/var/run/docker.sock \
     localstack/localstack
   ```

## Starting LocalStack

### Using LocalStack CLI (Recommended)

1. **Start LocalStack**
   ```bash
   # Start in detached mode
   localstack start -d
   
   # Or start with specific services only
   SERVICES=sqs localstack start -d
   
   # Start with custom port
   GATEWAY_LISTEN=0.0.0.0:4566 localstack start -d
   ```

2. **Check Status**
   ```bash
   localstack status
   ```

3. **View Logs**
   ```bash
   localstack logs
   ```

4. **Stop LocalStack**
   ```bash
   localstack stop
   ```

### Using Docker (Alternative)

## Configuration

### Environment Variables

Create a `.env` file in your test project directory:

```env
# LocalStack Configuration
LOCALSTACK_ENDPOINT=http://localhost:4566
AWS_ACCESS_KEY_ID=test
AWS_SECRET_ACCESS_KEY=test
AWS_DEFAULT_REGION=us-east-1

# SQS Specific
SQS_ENDPOINT_URL=http://localhost:4566
```

### Test Configuration

Add the following to your test project's `appsettings.Test.json`:

```json
{
  "AWS": {
    "ServiceURL": "http://localhost:4566",
    "Region": "us-east-1",
    "AccessKey": "test",
    "SecretKey": "test",
    "UseFakeCredentials": true
  },
  "SQS": {
    "ServiceUrl": "http://localhost:4566",
    "Region": "us-east-1",
    "AccessKey": "test",
    "SecretKey": "test"
  }
}
```

## Verifying LocalStack Setup

### Using Docker

1. **Check if LocalStack is running**
   ```bash
   docker ps | grep localstack
   ```

2. **Check LocalStack health**
   ```bash
   curl http://localhost:4566/_localstack/health
   ```

3. **View LocalStack logs**
   ```bash
   docker logs localstack
   ```

### Using AWS CLI

1. **Configure AWS CLI for LocalStack**
   ```bash
   # Windows PowerShell
   $env:AWS_ACCESS_KEY_ID="test"
   $env:AWS_SECRET_ACCESS_KEY="test"
   $env:AWS_DEFAULT_REGION="us-east-1"
   
   # Linux/macOS
   export AWS_ACCESS_KEY_ID="test"
   export AWS_SECRET_ACCESS_KEY="test"
   export AWS_DEFAULT_REGION="us-east-1"
   ```

2. **Create a test queue**
   ```bash
   aws sqs create-queue \
     --queue-name test-queue \
     --endpoint-url http://localhost:4566
   ```

3. **List queues**
   ```bash
   aws sqs list-queues \
     --endpoint-url http://localhost:4566
   ```

## Running Tests

### From Command Line

```bash
# Set environment variable for LocalStack endpoint
# Windows PowerShell
$env:SQS_ENDPOINT_URL="http://localhost:4566"

# Linux/macOS
export SQS_ENDPOINT_URL="http://localhost:4566"

# Run integration tests
dotnet test --filter "Category=Integration"
```

### From Visual Studio

1. Set environment variables in `launchSettings.json`:
   ```json
   {
     "profiles": {
       "LocalStack Tests": {
         "commandName": "Project",
         "environmentVariables": {
           "SQS_ENDPOINT_URL": "http://localhost:4566",
           "AWS_ACCESS_KEY_ID": "test",
           "AWS_SECRET_ACCESS_KEY": "test",
           "AWS_DEFAULT_REGION": "us-east-1"
         }
       }
     }
   }
   ```

2. Select the "LocalStack Tests" profile and run tests

## Troubleshooting

### Common Issues

1. **LocalStack not starting**
   - Ensure Docker is running
   - Check port 4566 is not in use: `netstat -an | grep 4566`
   - Check Docker logs: `docker logs localstack`

2. **Connection refused errors**
   - Verify LocalStack is running: `docker ps`
   - Check health endpoint: `curl http://localhost:4566/_localstack/health`
   - Ensure firewall is not blocking port 4566

3. **Authentication errors**
   - LocalStack accepts any credentials, but they must be provided
   - Ensure AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY are set

4. **WSL2 issues (Windows)**
   - Use `host.docker.internal` instead of `localhost` in WSL2
   - Or use the WSL2 IP address: `ip addr show eth0 | grep inet`

### Cleanup

```bash
# Stop LocalStack
docker stop localstack

# Remove LocalStack container
docker rm localstack

# Clean up volumes
docker volume prune
```

## CI/CD Integration

### Azure DevOps Pipeline

```yaml
trigger:
- main
- dev

pool:
  vmImage: 'ubuntu-latest'

variables:
  - name: SERVICES
    value: sqs
  - name: DEBUG
    value: 1
  - name: AWS_ACCESS_KEY_ID
    value: test
  - name: AWS_SECRET_ACCESS_KEY
    value: test
  - name: AWS_DEFAULT_REGION
    value: us-east-1
  - name: LOCALSTACK_ENDPOINT
    value: http://localhost:4566

steps:
- task: UsePythonVersion@0
  inputs:
    versionSpec: '3.x'
    addToPath: true
  displayName: 'Use Python 3.x'

- script: |
    pip install localstack awscli
  displayName: 'Install LocalStack CLI and AWS CLI'

- script: |
    localstack start -d
    # Wait for LocalStack to be ready
    localstack wait -t 30
  displayName: 'Start LocalStack'

- script: |
    # Verify LocalStack is running
    localstack status
    # Test SQS connectivity
    aws sqs list-queues --endpoint-url $LOCALSTACK_ENDPOINT
  displayName: 'Verify LocalStack'

- task: DotNetCoreCLI@2
  inputs:
    command: 'build'
    projects: '**/*.csproj'
  displayName: 'Build Projects'

- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: '**/CloudNimble.WebJobs.Extensions.Tests.Amazon.csproj'
    arguments: '--filter "TestCategory=Integration" --logger trx --results-directory $(Agent.TempDirectory)'
  displayName: 'Run Integration Tests'
  env:
    SQS_ENDPOINT_URL: $(LOCALSTACK_ENDPOINT)

- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '$(Agent.TempDirectory)/*.trx'
  displayName: 'Publish Test Results'
  condition: succeededOrFailed()

- script: |
    localstack stop
  displayName: 'Stop LocalStack'
  condition: always()
```

### GitHub Actions

```yaml
name: Integration Tests

on:
  push:
    branches: [ main, dev ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    env:
      SERVICES: sqs
      DEBUG: 1
      AWS_ACCESS_KEY_ID: test
      AWS_SECRET_ACCESS_KEY: test
      AWS_DEFAULT_REGION: us-east-1
      LOCALSTACK_ENDPOINT: http://localhost:4566
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup Python
      uses: actions/setup-python@v4
      with:
        python-version: '3.x'
    
    - name: Install LocalStack
      run: |
        pip install localstack awscli
        localstack --version
    
    - name: Start LocalStack
      run: |
        localstack start -d
        localstack wait -t 30
    
    - name: Verify LocalStack
      run: |
        localstack status
        aws sqs list-queues --endpoint-url $LOCALSTACK_ENDPOINT
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build
      run: dotnet build
    
    - name: Run Tests
      run: dotnet test --filter "TestCategory=Integration" --logger trx
      env:
        SQS_ENDPOINT_URL: ${{ env.LOCALSTACK_ENDPOINT }}
    
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: '**/*.trx'
    
    - name: Stop LocalStack
      if: always()
      run: localstack stop
```

## Best Practices

1. **Test Isolation**
   - Create unique queue names for each test
   - Clean up resources after tests
   - Use test-specific prefixes

2. **Performance**
   - Reuse LocalStack container between test runs
   - Use `PERSISTENCE=1` for data persistence
   - Configure only required services

3. **Debugging**
   - Enable DEBUG mode for detailed logs
   - Use AWS CLI to inspect resources
   - Check LocalStack dashboard (if Pro version)

## Additional Resources

- [LocalStack Documentation](https://docs.localstack.cloud/overview/)
- [LocalStack GitHub](https://github.com/localstack/localstack)
- [AWS SDK for .NET Documentation](https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/welcome.html)
- [LocalStack SQS Documentation](https://docs.localstack.cloud/user-guide/aws/sqs/)