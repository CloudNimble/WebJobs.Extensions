#!/usr/bin/env python3
"""
Wait for LocalStack to be ready.
Useful for CI/CD pipelines to ensure LocalStack is fully started before running tests.
"""

import time
import sys
import os
import urllib.request
import urllib.error
import json

def check_localstack_health(endpoint):
    """Check if LocalStack is healthy."""
    try:
        response = urllib.request.urlopen(f"{endpoint}/_localstack/health")
        data = json.loads(response.read().decode())
        
        # Check if services are running
        services = data.get("services", {})
        if "sqs" in services:
            return services["sqs"] == "running"
        
        # If no specific service info, just check if we got a response
        return True
    except (urllib.error.URLError, json.JSONDecodeError):
        return False

def main():
    endpoint = os.environ.get("LOCALSTACK_ENDPOINT", "http://localhost:4566")
    timeout = int(os.environ.get("LOCALSTACK_WAIT_TIMEOUT", "60"))
    interval = 2
    
    print(f"Waiting for LocalStack at {endpoint} (timeout: {timeout}s)...")
    
    start_time = time.time()
    while time.time() - start_time < timeout:
        if check_localstack_health(endpoint):
            print("✅ LocalStack is ready!")
            return 0
        
        print(".", end="", flush=True)
        time.sleep(interval)
    
    print("\n❌ Timeout waiting for LocalStack to be ready")
    return 1

if __name__ == "__main__":
    sys.exit(main())