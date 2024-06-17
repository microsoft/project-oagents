@description('Location for all resources.')
param location string = resourceGroup().location
param tags object = {}

@allowed([
  'F0'
  'S0'
])
param sku string = 'F0'

param documentIntelligenceName string = ''
// Because name is optional in main.bicep, we make sure the name is set here.
var defaultName = 'doc-intelligence-${uniqueString(resourceGroup().id)}'
var actualname = !empty(documentIntelligenceName) ? documentIntelligenceName : defaultName

resource cognitiveService 'Microsoft.CognitiveServices/accounts@2021-10-01' = {
  name: actualname
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
output endpoint string = 'https://${actualname}.cognitiveservices.azure.com/'
output name string = cognitiveService.name
//output key string = cognitiveService.listKeys().key1
