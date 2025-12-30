param location string
param redisName string
param tags object

resource redis 'Microsoft.Cache/Redis@2023-04-01' = {
  name: redisName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0 // C0 (Low cost)
    }
    enableNonSslPort: false
    publicNetworkAccess: 'Enabled'
  }
}

output hostName string = redis.properties.hostName
output sslPort int = redis.properties.sslPort
output primaryKey string = redis.listKeys().primaryKey
