# Import the necessary modules  
# Import-Module Az  
# Import-Module Az.CosmosDB
# Import-Module CosmosDB

# Authenticate to Azure (if not already authenticated)  
Write-Host "Authenticating to Azure..."  
Connect-AzAccount -Identity
Set-AzContext -Subscription "636154ff-066d-4e3d-a87c-a6407841b716"

$resourceGroupName = "rg-oagents-support-center"
$cosmosDbAccountName = "cosmos-gxecy3vbydlss"
$databaseName = "customer-support"
$containerName = "items"
$partitionKey = "/id"

Write-Host "Resource Group: $resourceGroupName"  
Write-Host "Cosmos DB Account: $cosmosDbAccountName"  
Write-Host "Database Name: $databaseName"  
Write-Host "Container Name: $containerName"  
Write-Host "Partition Key Path: $partitionKeyPath" 

$cosmosDbContext = New-CosmosDbContext -Account $cosmosDbAccountName -Database $databaseName -ResourceGroup $resourceGroupName    

$document = @"
{
    "id": "1234",
    "Name": "User",  
    "Email": "user@test.com",  
    "Phone": "+1123456789",  
    "Address": "Contoso Address 123, CN."        
}
"@

New-CosmosDbDocument -Context $cosmosDbContext -CollectionId $containerName -DocumentBody $document -PartitionKey $partitionkey