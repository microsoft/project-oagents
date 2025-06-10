#!/bin/bash

# Script to provision minimal Azure resources for local development

set -e

echo "=========================================="
echo "Local Development Provisioning Script"
echo "=========================================="
echo ""

# Check if azd is installed
if ! command -v azd &> /dev/null; then
    echo "Error: Azure Developer CLI (azd) is not installed."
    echo "Please install azd first: https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd"
    echo ""
    echo "Installation instructions:"
    echo "  https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd"
    exit 1
fi

# Check if we're in the right directory
if [ ! -f "azure.yaml" ]; then
  echo "Error: azure.yaml not found. Please run this script from the gh-flow sample directory."
  exit 1
fi

echo "This script will provision minimal Azure resources for local development."
echo "It only creates a storage account instead of the full infrastructure."
echo ""

# Get environment name
if [ -z "$1" ]; then
  echo "Usage: $0 <environment-name>"
  echo ""
  echo "Example: $0 my-local-dev"
  exit 1
fi

ENVIRONMENT_NAME=$1

echo "Environment: $ENVIRONMENT_NAME"
echo ""

# Create environment if it doesn't exist
echo "Creating/selecting azd environment..."
azd env new $ENVIRONMENT_NAME 2>/dev/null || azd env select $ENVIRONMENT_NAME

# Override the bicep template to use the local version
echo "Configuring for local development (using main.local.bicep)..."

# Create a temporary azure.yaml that uses the local bicep
cat > azure.local.tmp.yaml << EOF
# yaml-language-server: \$schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

name: ai-dev-team-local
infra:
  provider: bicep
  path: infra
  module: main.local
  parameters: main.local.parameters.json
EOF

# Use the temporary configuration
export AZD_CONFIG_FILE="azure.local.tmp.yaml"

# Provision the infrastructure
echo "Running azd provision with minimal configuration..."
echo ""
azd provision

# Clean up temp file
rm -f azure.local.tmp.yaml

echo ""
echo "=========================================="
echo "Local development setup complete!"
echo "=========================================="
echo ""
echo "To get your configuration values, run:"
echo "  azd env get-values -e $ENVIRONMENT_NAME"
echo ""
echo "You'll need to configure your appsettings.json with values from azd env get-values."
echo ""
echo "For detailed configuration instructions, see LOCAL-DEVELOPMENT.md"