# PowerShell script to check if LocalStack is running and accessible

$LocalStackEndpoint = if ($env:LOCALSTACK_ENDPOINT) { $env:LOCALSTACK_ENDPOINT } else { "http://localhost:4566" }
Write-Host "Checking LocalStack at: $LocalStackEndpoint"

# Function to check if a command exists
function Test-CommandExists {
    param($Command)
    $null = Get-Command $Command -ErrorAction SilentlyContinue
    return $?
}

# Check if LocalStack is reachable
try {
    $response = Invoke-WebRequest -Uri "$LocalStackEndpoint/_localstack/health" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ LocalStack is running and healthy!" -ForegroundColor Green
        
        # Check SQS service if AWS CLI is available
        if (Test-CommandExists "aws") {
            try {
                $null = & aws sqs list-queues --endpoint-url $LocalStackEndpoint --region us-east-1 2>$null
                Write-Host "✅ SQS service is available!" -ForegroundColor Green
            }
            catch {
                Write-Host "⚠️  SQS service check failed" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "⚠️  AWS CLI not installed, skipping SQS service check" -ForegroundColor Yellow
        }
        
        Write-Host ""
        Write-Host "LocalStack is ready for testing!" -ForegroundColor Green
        exit 0
    }
}
catch {
    Write-Host "❌ LocalStack is not running or not accessible at $LocalStackEndpoint" -ForegroundColor Red
    Write-Host ""
    
    # Check if LocalStack CLI is available
    if (Test-CommandExists "localstack") {
        Write-Host "LocalStack CLI is installed. To start LocalStack:"
        Write-Host "  localstack start -d"
        Write-Host ""
        Write-Host "Or with specific services:"
        Write-Host "  `$env:SERVICES='sqs'; localstack start -d"
    }
    else {
        Write-Host "To start LocalStack using Docker:"
        Write-Host "  docker run -d --name localstack -p 4566:4566 localstack/localstack"
        Write-Host ""
        Write-Host "Or install LocalStack CLI:"
        Write-Host "  pip install localstack"
    }
    exit 1
}