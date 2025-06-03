#!/bin/bash
# Script to check if LocalStack is running and accessible

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

LOCALSTACK_ENDPOINT="${LOCALSTACK_ENDPOINT:-http://localhost:4566}"
echo "Checking LocalStack at: $LOCALSTACK_ENDPOINT"

# Check if LocalStack is reachable
if curl -s -o /dev/null -w "%{http_code}" "$LOCALSTACK_ENDPOINT/_localstack/health" | grep -q "200"; then
    echo -e "${GREEN}✅ LocalStack is running and healthy!${NC}"
    
    # Check SQS service
    if aws --endpoint-url="$LOCALSTACK_ENDPOINT" sqs list-queues --region us-east-1 >/dev/null 2>&1; then
        echo -e "${GREEN}✅ SQS service is available!${NC}"
    else
        echo -e "${YELLOW}⚠️  SQS service check failed (AWS CLI may not be installed)${NC}"
    fi
else
    echo -e "${RED}❌ LocalStack is not running or not accessible at $LOCALSTACK_ENDPOINT${NC}"
    echo ""
    
    # Check if LocalStack CLI is available
    if command -v localstack &> /dev/null; then
        echo "LocalStack CLI is installed. To start LocalStack:"
        echo "  localstack start -d"
        echo ""
        echo "Or with specific services:"
        echo "  SERVICES=sqs localstack start -d"
    else
        echo "To start LocalStack using Docker:"
        echo "  docker run -d --name localstack -p 4566:4566 localstack/localstack"
        echo ""
        echo "Or install LocalStack CLI:"
        echo "  pip install localstack"
    fi
    exit 1
fi