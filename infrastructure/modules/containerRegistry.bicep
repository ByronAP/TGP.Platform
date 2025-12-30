param location string
param acrName string
param tags object

resource acr 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

output loginServer string = acr.properties.loginServer
output registryName string = acr.name
output registryUsername string = acr.listCredentials().username
output registryPassword string = acr.listCredentials().passwords[0].value
