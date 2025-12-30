@description('Location for the Key Vault')
param location string

@description('Name of the Key Vault')
param keyVaultName string

@description('Tags to apply to the resource')
param tags object = {}

@description('Enable soft delete for the Key Vault')
param enableSoftDelete bool = true

@description('Number of days to retain deleted vaults')
param softDeleteRetentionInDays int = 90

// Secrets to store
@secure()
@description('JWT signing secret key')
param jwtSecretKey string

@description('JWT token issuer')
param jwtIssuer string = 'TGP.SSO'

@description('JWT token audience')
param jwtAudience string = 'TGP.Platform'

@secure()
@description('SQL Server connection string')
param dbConnectionString string

@secure()
@description('Redis connection string')
param redisConnectionString string

@secure()
@description('Service Bus connection string')
param serviceBusConnectionString string

@secure()
@description('Storage connection string')
param storageConnectionString string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enabledForDeployment: true
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: false
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enableRbacAuthorization: true
    publicNetworkAccess: 'Enabled'
  }
}

// JWT Secrets
resource jwtSecretKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Jwt--SecretKey'
  properties: {
    value: jwtSecretKey
  }
}

resource jwtIssuerSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Jwt--Issuer'
  properties: {
    value: jwtIssuer
  }
}

resource jwtAudienceSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Jwt--Audience'
  properties: {
    value: jwtAudience
  }
}

// Connection Strings
resource dbConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--DefaultConnection'
  properties: {
    value: dbConnectionString
  }
}

resource redisConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Redis--ConnectionString'
  properties: {
    value: redisConnectionString
  }
}

resource serviceBusConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ServiceBus--ConnectionString'
  properties: {
    value: serviceBusConnectionString
  }
}

resource storageConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Storage--ConnectionString'
  properties: {
    value: storageConnectionString
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultId string = keyVault.id
