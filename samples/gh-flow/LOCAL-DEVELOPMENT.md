# Local Development Provisioning

This guide explains how to provision minimal Azure resources for local development of the gh-flow sample.

## Overview

When developing locally, you don't need all the Azure resources that are required for a full production deployment. The local provisioning option creates only the essential Azure resources while using containerized services for everything else.

## What gets provisioned

### Full deployment (azure.yaml + main.bicep)
- Azure Storage Account
- Application Insights & Log Analytics
- Container Apps Environment & Registry
- Qdrant deployed to Azure Container Apps
- Cosmos DB
- gh-flow Container App service

### Local development (main.local.bicep)
- Azure Storage Account (for file shares needed by Azure Container Instances)

### What you use locally instead
- **Qdrant**: Uses the containerized Qdrant running in devcontainer (`http://qdrant:6333`)
- **Application**: Runs locally via `dotnet run` or VS Code debugging
- **Database**: Can use Cosmos DB emulator or skip database-dependent features
- **Monitoring**: Local logging instead of Application Insights

## How to use

1. **Provision minimal resources:**
   ```bash
   azd auth login
   ./provision-local.sh your-env-name
   ```

2. **Configure your appsettings.json:**
   The script will output the exact values you need to add to your `appsettings.json` file.

3. **Complete your configuration:**
   You still need to configure:
   - GitHub App settings (AppKey, AppId, InstallationId, WebhookSecret)
   - OpenAI settings (Endpoint, ApiKey, DeploymentId, etc.)

## Benefits

- **Faster provisioning**: Only creates one storage account instead of 10+ resources
- **Lower cost**: No Container Apps, Cosmos DB, or other expensive services running in Azure
- **Easier cleanup**: Minimal resources to clean up when done
- **Better for development**: Use local debugging tools and faster iteration

## When to use each option

**Use local provisioning when:**
- Developing and testing the application locally
- Working on agent logic and GitHub integration
- Don't need Azure hosting or full production environment
- Want to minimize Azure costs during development

**Use full provisioning when:**
- Deploying to production or staging
- Testing the full Azure integration
- Need the complete Container Apps deployment
- Sharing the application with others via public URL