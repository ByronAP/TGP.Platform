param location string
param containerAppName string
param environmentId string
param containerImage string
param envVars array = []
param containerPort int = 80
param isExternalIngress bool = false
param registryServer string
@secure()
param registryUsername string
@secure()
param registryPassword string
param tags object

// Key Vault secret references
param keyVaultName string = ''
param secretEnvVars array = []  // [{name: 'ENV_VAR_NAME', secretName: 'kv-secret-name'}]

// Build secrets array: registry password + Key Vault references
var registrySecret = [
  {
    name: 'registry-password'
    value: registryPassword
  }
]

var keyVaultSecrets = [for secret in secretEnvVars: {
  name: secret.secretName
  keyVaultUrl: 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/secrets/${secret.secretName}'
  identity: 'system'
}]

// Build env vars: regular + secret references
var secretRefEnvVars = [for secret in secretEnvVars: {
  name: secret.name
  secretRef: secret.secretName
}]

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: containerAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      ingress: {
        external: isExternalIngress
        targetPort: containerPort
        transport: 'auto'
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: registryServer
          username: registryUsername
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: concat(registrySecret, keyVaultSecrets)
    }
    template: {
      containers: [
        {
          name: containerAppName
          image: containerImage
          env: concat(envVars, secretRefEnvVars)
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn
output principalId string = containerApp.identity.principalId
