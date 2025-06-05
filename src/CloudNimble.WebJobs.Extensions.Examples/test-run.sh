#!/bin/bash
echo "Starting WebJobs Examples test run..."
echo "Timestamp: $(date)"
echo "=================================="

# Run the app for 90 seconds and capture all output
timeout 90s dotnet run --no-build 2>&1 | tee test-output.log

echo "=================================="
echo "Test run completed at: $(date)"
echo "Exit code: $?"

# Look for specific patterns in the output
echo ""
echo "Checking for queue name resolution..."
grep -i "queuename" test-output.log || echo "No queue name references found"

echo ""
echo "Checking for SQSTrigger logs..."
grep -i "sqstrigger" test-output.log || echo "No SQSTrigger logs found"

echo ""
echo "Checking for errors..."
grep -i "error\|exception" test-output.log || echo "No errors found"