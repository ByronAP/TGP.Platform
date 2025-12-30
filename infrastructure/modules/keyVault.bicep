@description('Location for the Key Vault')
param location string

@description('Name of the Key Vault')
param keyVaultName string

@description('Tags to apply to the resource')
param tags object = {}

@description('Principal IDs that should have access to secrets')
param accessPolicyPrincipalIds array = []

@description('Enable soft delete for the Key Vault')
param enableSoftDelete bool = true

@description('Number of days to retain deleted vaults')
param softDeleteRetentionInDays int = 90

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
    enableRbacAuthorization: true // Use Azure RBAC for access control
    publicNetworkAccess: 'Enabled'
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultId string = keyVault.id
