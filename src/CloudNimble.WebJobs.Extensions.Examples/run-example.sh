#!/bin/bash
# Script to run the WebJobs SQS example

echo "=== CloudNimble WebJobs SQS Example ==="
echo ""

# Check if AWS credentials are configured
if [ -z "$AWS_ACCESS_KEY_ID" ] && ! aws sts get-caller-identity >/dev/null 2>&1; then
    echo "❌ AWS credentials not configured!"
    echo ""
    echo "Please configure AWS credentials using one of these methods:"
    echo "1. Set environment variables:"
    echo "   export AWS_ACCESS_KEY_ID=your-key"
    echo "   export AWS_SECRET_ACCESS_KEY=your-secret"
    echo "   export AWS_DEFAULT_REGION=us-east-1"
    echo ""
    echo "2. Use AWS CLI profile:"
    echo "   aws configure --profile webjobs-example"
    echo ""
    echo "3. Use dotnet user-secrets (for development):"
    echo "   dotnet user-secrets set \"AWS:AccessKey\" \"your-key\""
    echo "   dotnet user-secrets set \"AWS:SecretKey\" \"your-secret\""
    exit 1
fi

# Set default queue name if not provided
if [ -z "$SQS_QUEUE_NAME" ]; then
    export SQS_QUEUE_NAME="webjobs-example-queue"
fi

echo "✅ AWS credentials configured"
echo "📦 Using queue: $SQS_QUEUE_NAME"
echo ""
echo "Starting WebJobs host..."
echo "- Timer will publish a message every 30 seconds"
echo "- SQS trigger will process messages from the queue"
echo ""
echo "Press Ctrl+C to stop"
echo ""

# Run the application
dotnet run --project CloudNimble.WebJobs.Extensions.Examples.csproj