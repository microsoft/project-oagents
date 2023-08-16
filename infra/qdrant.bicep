param environmentName string = ''
param storageSKU string = 'Premium_LRS'
param storageName string = '${environmentName}'
param shareName string = '${environmentName}qdrantazfiles'
param location string = ''
param addressPrefix string = '10.0.0.0/16'
param subnetPrefix string = '10.0.0.0/23'
var storageAccountKey = listKeys(resourceId('Microsoft.Storage/storageAccounts', storageName), '2021-09-01').keys[0].value

resource storage_account 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: storageName
  location: location
  sku: {
    name: storageSKU
  }
  kind: 'FileStorage'
  properties: {
    supportsHttpsTrafficOnly: true
  }
}

resource file_share 'Microsoft.Storage/storageAccounts/fileServices/shares@2021-09-01' = {
  name: '${storageName}/default/${shareName}'
  dependsOn: [
    storage_account
  ]
}

resource envVnet 'Microsoft.Network/virtualNetworks@2020-08-01' = {
  name: '${environmentName}-infra-vnet'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        addressPrefix
      ]
    }
    subnets: [
      {
        name: 'infrasubnet'
        properties: {
          addressPrefix: subnetPrefix
        }
      }
    ]
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-11-01-preview' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'azure-monitor'
    }
    vnetConfiguration: {
      infrastructureSubnetId: envVnet.properties.subnets[0].id
    }
  }
}

resource qdrantstorage 'Microsoft.App/managedEnvironments/storages@2022-11-01-preview' = {
  name: '${environmentName}/qdrantstoragemount'
  dependsOn: [
    containerAppsEnvironment
    storage_account
  ]
  properties: {
    azureFile: {
      accountName: storageName
      shareName: shareName
      accountKey: storageAccountKey
      accessMode: 'ReadWrite'
    }
  }
}

resource qdrantApiContainerApp1 'Microsoft.App/containerApps@2022-11-01-preview' = {
  name: '${environmentName}http'
  location: location
  dependsOn: [
    qdrantstorage
  ]
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
        ingress: {
          external: true
          targetPort: 6333
        }
    }
    template: {
      containers: [
        {
          name: 'qdrantapicontainerapp'
          image: 'qdrant/qdrant'
          resources: {
            cpu: 1
            memory: '2Gi'
          }
          volumeMounts: [
            {
              volumeName: 'qdrantstoragevol'
              mountPath: '/qdrant/storage'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: [
        {
          name: 'qdrantstoragevol'
          storageName: 'qdrantstoragemount'
          storageType: 'AzureFile'
        }
      ]
    }
  }
}

resource qdrantDbContainerApp2 'Microsoft.App/containerApps@2022-11-01-preview' = {
  name: '${environmentName}grpc'
  location: location
  dependsOn: [
    qdrantstorage
  ]
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      ingress:{
        external: true
        transport: 'TCP'
        targetPort: 6334
        exposedPort: 6334    
      }
    }
    template: {
      containers: [
        {
          name: 'qdrantdbcontainerapp'
          image: 'qdrant/qdrant'
          resources: {
            cpu: 2
            memory: '4Gi'
          }
          volumeMounts: [
            {
              volumeName: 'qdrantstoragevol'
              mountPath: '/qdrant/storage'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: [
        {
          name: 'qdrantstoragevol'
          storageName: 'qdrantstoragemount'
          storageType: 'AzureFile'
        }
      ]
    }
  }
}
