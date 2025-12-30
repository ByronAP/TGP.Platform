param location string
param namespaceName string
param tags object

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: 'Standard' // Standard required for Topics if used later, and better features
    tier: 'Standard'
  }
  properties: {}
}

output endpoint string = serviceBus.properties.serviceBusEndpoint
output namespaceName string = serviceBus.name
#disable-next-line use-resource-id-functions // Suppress warning about manual ID construction if needed, or just use listKeys on the resource
output primaryKey string = listKeys('${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey', serviceBus.apiVersion).primaryKey
