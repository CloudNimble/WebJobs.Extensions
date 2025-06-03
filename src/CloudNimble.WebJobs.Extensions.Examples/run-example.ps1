# PowerShell script to run the WebJobs SQS example

Write-Host "=== CloudNimble WebJobs SQS Example ===" -ForegroundColor Cyan
Write-Host ""

# Check if AWS credentials are configured
$hasEnvCreds = $env:AWS_ACCESS_KEY_ID -ne $null
$hasAwsCli = Get-Command aws -ErrorAction SilentlyContinue

if (-not $hasEnvCreds -and $hasAwsCli) {
    try {
        aws sts get-caller-identity | Out-Null
        $hasAwsCreds = $true
    }
    catch {
        $hasAwsCreds = $false
    }
}
else {
    $hasAwsCreds = $hasEnvCreds
}

if (-not $hasAwsCreds) {
    Write-Host "❌ AWS credentials not configured!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please configure AWS credentials using one of these methods:"
    Write-Host "1. Set environment variables:" -ForegroundColor Yellow
    Write-Host "   `$env:AWS_ACCESS_KEY_ID='your-key'"
    Write-Host "   `$env:AWS_SECRET_ACCESS_KEY='your-secret'"
    Write-Host "   `$env:AWS_DEFAULT_REGION='us-east-1'"
    Write-Host ""
    Write-Host "2. Use AWS CLI profile:" -ForegroundColor Yellow
    Write-Host "   aws configure --profile webjobs-example"
    Write-Host ""
    Write-Host "3. Use dotnet user-secrets (for development):" -ForegroundColor Yellow
    Write-Host "   dotnet user-secrets set `"AWS:AccessKey`" `"your-key`""
    Write-Host "   dotnet user-secrets set `"AWS:SecretKey`" `"your-secret`""
    exit 1
}

# Set default queue name if not provided
if (-not $env:SQS_QUEUE_NAME) {
    $env:SQS_QUEUE_NAME = "webjobs-example-queue"
}

Write-Host "✅ AWS credentials configured" -ForegroundColor Green
Write-Host "📦 Using queue: $($env:SQS_QUEUE_NAME)" -ForegroundColor Green
Write-Host ""
Write-Host "Starting WebJobs host..."
Write-Host "- Timer will publish a message every 30 seconds" -ForegroundColor Cyan
Write-Host "- SQS trigger will process messages from the queue" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

# Run the application
dotnet run --project CloudNimble.WebJobs.Extensions.Examples.csproj