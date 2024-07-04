@description('Location for all resources.')
param location string = resourceGroup().location
param tags object = {}

@allowed([
  'F0'
  'S0'
])

param sku string = 'S0'


param documentIntelligenceName string = ''
// Because name is optional in main.bicep, we make sure the name is set here.
var defaultName = 'doc-intelligence-${uniqueString(resourceGroup().id)}'
var actualname = !empty(documentIntelligenceName) ? documentIntelligenceName : defaultName

resource cognitiveService 'Microsoft.CognitiveServices/accounts@2021-10-01' = {
  name: actualname
  location: 'westeurope' //invoice model is only available in eastus, west us2 and west europe for now
  tags: tags
  sku: {
    name: sku
  }
  kind: 'FormRecognizer'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: actualname
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

output id string = cognitiveService.id
output endpoint string = 'https://${actualname}.cognitiveservices.azure.com/'
output name string = cognitiveService.name
//output key string = cognitiveService.listKeys().key1
