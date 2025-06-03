#!/bin/bash
# Script to check if LocalStack CLI is installed and running

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

LOCALSTACK_ENDPOINT="${LOCALSTACK_ENDPOINT:-http://localhost:4566}"
echo "Checking LocalStack at: $LOCALSTACK_ENDPOINT"

# Check if LocalStack CLI is installed
if ! command -v localstack &> /dev/null; then
    echo -e "${RED}❌ LocalStack CLI is not installed!${NC}"
    echo ""
    echo "To install LocalStack CLI:"
    echo "  pip install localstack"
    echo "  # or"
    echo "  brew install localstack"
    exit 1
fi

echo -e "${GREEN}✅ LocalStack CLI is installed${NC}"
localstack --version

# Check if LocalStack is running
if localstack status | grep -q "running"; then
    echo -e "${GREEN}✅ LocalStack is running!${NC}"
    
    # Check health endpoint
    if curl -s -o /dev/null -w "%{http_code}" "$LOCALSTACK_ENDPOINT/_localstack/health" | grep -q "200"; then
        echo -e "${GREEN}✅ LocalStack health check passed!${NC}"
        
        # Check SQS service
        if aws --endpoint-url="$LOCALSTACK_ENDPOINT" sqs list-queues --region us-east-1 >/dev/null 2>&1; then
            echo -e "${GREEN}✅ SQS service is available!${NC}"
        else
            echo -e "${YELLOW}⚠️  SQS service check failed (AWS CLI may not be installed)${NC}"
        fi
    else
        echo -e "${RED}❌ LocalStack health check failed${NC}"
        exit 1
    fi
else
    echo -e "${RED}❌ LocalStack is not running${NC}"
    echo ""
    echo "To start LocalStack:"
    echo "  localstack start -d"
    echo ""
    echo "To start with specific services:"
    echo "  SERVICES=sqs localstack start -d"
    exit 1
fi

echo ""
echo -e "${GREEN}LocalStack is ready for testing!${NC}"