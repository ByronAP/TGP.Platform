param location string
param storageAccountName string
param tags object

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS' // Low cost redundancy
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot' // TGP is active use
    allowBlobPublicAccess: false
  }
}

output storageAccountName string = storage.name
output primaryEndpoint string = storage.properties.primaryEndpoints.blob
output storageKey string = storage.listKeys().keys[0].value
