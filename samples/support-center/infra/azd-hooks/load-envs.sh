#!/bin/bash

pushd src/frontend || exit 1

# Check if the variable is set  
if [ -z "${AZURE_BACKEND_ENDPOINT}" ]; then  
    echo "AZURE_BACKEND_ENDPOINT is not set. Exiting."  
    popd  
    exit 1  
fi
echo "AZURE_BACKEND_ENDPOINT is set to: ${AZURE_BACKEND_ENDPOINT}"
echo "Replacing <AZURE_BACKEND_ENDPOINT> with ${AZURE_BACKEND_ENDPOINT}"
  
# Use sed to replace the placeholder
sed -i "s|<AZURE_BACKEND_ENDPOINT>|${AZURE_BACKEND_ENDPOINT}|g" .env.azureConfig
  
# Verify the replacement
echo "Contents of .env.azureConfig after replacement:"
cat .env.azureConfig

popd || exit 1