#!/bin/bash
# Unified LocalStack setup script for all platforms
# This script detects the environment and uses the appropriate method to start LocalStack

# Default values
METHOD="auto"  # auto, cli, docker
DETACH=true
SKIP_WAIT=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --method)
            METHOD="$2"
            shift 2
            ;;
        --no-detach)
            DETACH=false
            shift
            ;;
        --skip-wait)
            SKIP_WAIT=true
            shift
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  --method [auto|cli|docker]  Specify method to use (default: auto)"
            echo "  --no-detach                 Run in foreground"
            echo "  --skip-wait                 Don't wait for LocalStack to be ready"
            echo "  --help                      Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

LOCALSTACK_ENDPOINT="${LOCALSTACK_ENDPOINT:-http://localhost:4566}"

echo -e "${CYAN}LocalStack Setup Script${NC}"
echo -e "${CYAN}======================${NC}"
echo -e "${CYAN}Endpoint: $LOCALSTACK_ENDPOINT${NC}"
echo ""

# Function to check if LocalStack is running
check_localstack() {
    if curl -s -o /dev/null -w "%{http_code}" "$LOCALSTACK_ENDPOINT/_localstack/health" | grep -q "200"; then
        return 0
    else
        return 1
    fi
}

# Function to wait for LocalStack to be ready
wait_for_localstack() {
    local timeout=${1:-60}
    local elapsed=0
    
    echo -e "${CYAN}Waiting for LocalStack to be ready...${NC}"
    
    while [ $elapsed -lt $timeout ]; do
        if check_localstack; then
            echo -e "\n${GREEN}✅ LocalStack is ready!${NC}"
            return 0
        fi
        echo -n "."
        sleep 2
        elapsed=$((elapsed + 2))
    done
    
    echo -e "\n${RED}❌ Timeout waiting for LocalStack to start${NC}"
    return 1
}

# Check if LocalStack is already running
if check_localstack; then
    echo -e "${GREEN}✅ LocalStack is already running!${NC}"
    exit 0
fi

# Detect available methods
HAS_DOCKER=false
HAS_LOCALSTACK_CLI=false

if command -v docker &> /dev/null; then
    HAS_DOCKER=true
fi

if command -v localstack &> /dev/null; then
    HAS_LOCALSTACK_CLI=true
fi

echo -e "${CYAN}Detected environment:${NC}"
echo -e "${CYAN}  Docker: $([ "$HAS_DOCKER" = true ] && echo "${GREEN}✅ Available${NC}" || echo "${RED}❌ Not found${NC}")${NC}"
echo -e "${CYAN}  LocalStack CLI: $([ "$HAS_LOCALSTACK_CLI" = true ] && echo "${GREEN}✅ Available${NC}" || echo "${RED}❌ Not found${NC}")${NC}"
echo ""

# Function to install Homebrew
install_homebrew() {
    echo -e "${CYAN}Installing Homebrew...${NC}"
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    
    # Detect OS and set up PATH accordingly
    OS="$(uname -s)"
    if [ "$OS" = "Linux" ]; then
        # Linux - Homebrew installs to /home/linuxbrew/.linuxbrew
        test -d ~/.linuxbrew && eval "$(~/.linuxbrew/bin/brew shellenv)"
        test -d /home/linuxbrew/.linuxbrew && eval "$(/home/linuxbrew/.linuxbrew/bin/brew shellenv)"
        echo "eval \"\$($(brew --prefix)/bin/brew shellenv)\"" >> ~/.bashrc
    elif [ "$OS" = "Darwin" ]; then
        # macOS - Apple Silicon uses /opt/homebrew, Intel uses /usr/local
        if [[ -f "/opt/homebrew/bin/brew" ]]; then
            eval "$(/opt/homebrew/bin/brew shellenv)"
            echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zprofile
        fi
    fi
}

# Function to install LocalStack CLI
install_localstack_cli() {
    echo -e "${CYAN}Installing LocalStack CLI...${NC}"
    
    # Check for Homebrew
    if ! command -v brew &> /dev/null; then
        echo -e "${YELLOW}Homebrew not found. Would you like to install it? (y/n)${NC}"
        read -r response
        if [[ "$response" =~ ^[Yy]$ ]]; then
            install_homebrew
            
            # Verify brew is now available
            if ! command -v brew &> /dev/null; then
                echo -e "${RED}❌ Failed to install Homebrew${NC}"
                echo "Please install Homebrew manually from: https://brew.sh"
                return 1
            fi
        else
            echo -e "${RED}❌ Homebrew is required to install LocalStack CLI${NC}"
            echo "Please install Homebrew from: https://brew.sh"
            echo "Then run: brew install localstack/tap/localstack-cli"
            return 1
        fi
    fi
    
    echo -e "${CYAN}Installing LocalStack CLI using Homebrew...${NC}"
    brew install localstack/tap/localstack-cli
    
    # Verify installation
    if command -v localstack &> /dev/null; then
        echo -e "${GREEN}✅ LocalStack CLI installed successfully!${NC}"
        HAS_LOCALSTACK_CLI=true
        return 0
    else
        echo -e "${YELLOW}⚠️  LocalStack CLI installed but not found in PATH${NC}"
        echo "You may need to restart your terminal"
        return 1
    fi
}

# Determine which method to use
USE_METHOD="$METHOD"
if [ "$METHOD" = "auto" ]; then
    if [ "$HAS_LOCALSTACK_CLI" = true ]; then
        USE_METHOD="cli"
    elif [ "$HAS_DOCKER" = true ]; then
        USE_METHOD="docker"
    else
        echo -e "${RED}❌ Neither Docker nor LocalStack CLI is available!${NC}"
        echo ""
        echo -e "${YELLOW}Would you like to install LocalStack CLI? (y/n)${NC}"
        read -r response
        if [[ "$response" =~ ^[Yy]$ ]]; then
            if install_localstack_cli; then
                USE_METHOD="cli"
            else
                echo -e "${RED}❌ Failed to install LocalStack CLI${NC}"
                echo ""
                echo "Please install one of the following manually:"
                echo "  - LocalStack CLI: brew install localstack/tap/localstack-cli"
                echo "  - Docker: https://docs.docker.com/get-docker/"
                exit 1
            fi
        else
            echo ""
            echo "Please install one of the following:"
            echo "  - LocalStack CLI (recommended): brew install localstack/tap/localstack-cli"
            echo "  - Docker: https://docs.docker.com/get-docker/"
            exit 1
        fi
    fi
fi

echo -e "${CYAN}Using method: $USE_METHOD${NC}"
echo ""

# Start LocalStack
case "$USE_METHOD" in
    cli)
        if [ "$HAS_LOCALSTACK_CLI" != true ]; then
            echo -e "${RED}❌ LocalStack CLI is not installed!${NC}"
            echo -e "${YELLOW}Would you like to install it? (y/n)${NC}"
            read -r response
            if [[ "$response" =~ ^[Yy]$ ]]; then
                if ! install_localstack_cli; then
                    exit 1
                fi
            else
                exit 1
            fi
        fi
        
        echo -e "${CYAN}Starting LocalStack using CLI...${NC}"
        if [ "$DETACH" = true ]; then
            SERVICES=sqs localstack start -d
        else
            SERVICES=sqs localstack start
        fi
        ;;
        
    docker)
        if [ "$HAS_DOCKER" != true ]; then
            echo -e "${RED}❌ Docker is not installed!${NC}"
            echo "Install from: https://docs.docker.com/get-docker/"
            exit 1
        fi
        
        echo -e "${CYAN}Starting LocalStack using Docker...${NC}"
        
        # Check if container exists
        if docker ps -a --format "{{.Names}}" | grep -q "^localstack$"; then
            echo -e "${CYAN}Removing existing LocalStack container...${NC}"
            docker rm -f localstack >/dev/null 2>&1
        fi
        
        # Run LocalStack
        DOCKER_ARGS=(
            "run"
            "--name" "localstack"
            "-p" "4566:4566"
            "-e" "SERVICES=sqs"
            "-e" "DEBUG=0"
        )
        
        if [ "$DETACH" = true ]; then
            DOCKER_ARGS+=("-d")
        fi
        
        DOCKER_ARGS+=("localstack/localstack:latest")
        
        docker "${DOCKER_ARGS[@]}"
        ;;
        
    *)
        echo -e "${RED}❌ Unknown method: $USE_METHOD${NC}"
        exit 1
        ;;
esac

# Wait for LocalStack to be ready
if [ "$SKIP_WAIT" != true ]; then
    if wait_for_localstack; then
        echo ""
        echo -e "${GREEN}🚀 LocalStack is ready for testing!${NC}"
        echo ""
        echo -e "${CYAN}To run integration tests:${NC}"
        echo "  dotnet test --filter TestCategory=Integration"
        echo ""
        echo -e "${CYAN}To stop LocalStack:${NC}"
        if [ "$USE_METHOD" = "cli" ]; then
            echo "  localstack stop"
        else
            echo "  docker stop localstack"
        fi
    else
        exit 1
    fi
fi