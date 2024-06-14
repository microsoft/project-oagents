@description('That name is the name of our application. It has to be unique.Type a name followed by your resource group name. (<name>-<resourceGroupName>)')
param documentIntelligenceName string = 'agent-cog-fr-${uniqueString(resourceGroup().id)}'

@description('Location for all resources.')
param location string = resourceGroup().location
param tags object = {}

@allowed([
  'F0'
  'S0'
])
param sku string = 'F0'

resource cognitiveService 'Microsoft.CognitiveServices/accounts@2021-10-01' = {
  name: documentIntelligenceName
  location: location
  tags: tags
  sku: {
    name: sku
  }
  kind: 'FormRecognizer'
  properties: {
    apiProperties: {
      statisticsEnabled: false
    }
  }
}

output id string = cognitiveService.id
output endpoint string = 'https://${documentIntelligenceName}.cognitiveservices.azure.com/'
output name string = cognitiveService.name
//output key string = cognitiveService.listKeys().key1
