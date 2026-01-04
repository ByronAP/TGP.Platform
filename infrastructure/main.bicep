targetScope = 'resourceGroup'

@description('The environment name (e.g. dev, prod)')
@allowed([
  'dev'
  'prod'
])
param environmentName string = 'prod'

@description('The location for all resources')
param location string = resourceGroup().location

@description('Unique suffix for global resources')
param resourceSuffix string = uniqueString(resourceGroup().id)

var tags = {
  Environment: environmentName
  Project: 'TGP'
  ManagedBy: 'Bicep'
}

@secure()
@description('The administrator password for the SQL server')
param dbPassword string

@secure()
@description('The JWT signing secret key (min 32 characters)')
param jwtSecretKey string

// Module: Container Registry
module acr 'modules/containerRegistry.bicep' = {
  name: 'acrDeployment'
  params: {
    location: location
    acrName: 'tgpacr${resourceSuffix}'
    tags: tags
  }
}

// Module: Log Analytics & Container Apps Environment
module acaEnv 'modules/containerAppsEnv.bicep' = {
  name: 'acaEnvDeployment'
  params: {
    location: location
    environmentName: 'tgp-env-${environmentName}'
    logAnalyticsName: 'tgp-logs-${environmentName}'
    tags: tags
  }
}

// Module: Application Insights
module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsightsDeployment'
  params: {
    location: location
    appInsightsName: 'tgp-ai-${environmentName}-${resourceSuffix}'
    logAnalyticsWorkspaceId: acaEnv.outputs.logAnalyticsId
    tags: tags
  }
}

// Module: Azure SQL Database
module sql 'modules/sql.bicep' = {
  name: 'sqlDeployment'
  params: {
    location: location
    serverName: 'tgp-sql-${environmentName}-${resourceSuffix}'
    databaseName: 'tgp'
    adminUsername: 'tgpadmin'
    adminPassword: dbPassword
    tags: tags
  }
}

// Module: Redis
module redis 'modules/redis.bicep' = {
  name: 'redisDeployment'
  params: {
    location: location
    redisName: 'tgp-redis-${environmentName}-${resourceSuffix}'
    tags: tags
  }
}

// Module: Service Bus
module serviceBus 'modules/serviceBus.bicep' = {
  name: 'serviceBusDeployment'
  params: {
    location: location
    namespaceName: 'tgp-sb-${environmentName}-${resourceSuffix}'
    tags: tags
  }
}

// Module: Storage (Blob)
module storage 'modules/storage.bicep' = {
  name: 'storageDeployment'
  params: {
    location: location
    storageAccountName: 'tgpstore${resourceSuffix}'
    tags: tags
  }
}

// Module: Key Vault with all secrets
module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVaultDeployment'
  params: {
    location: location
    keyVaultName: 'tgpkv${environmentName}${take(resourceSuffix, 10)}'
    tags: tags
    jwtSecretKey: jwtSecretKey
    dbConnectionString: 'Server=${sql.outputs.fqdn};Database=${sql.outputs.databaseName};User Id=tgpadmin;Password=${dbPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
    redisConnectionString: '${redis.outputs.hostName}:6380,password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
    serviceBusConnectionString: 'Endpoint=${serviceBus.outputs.endpoint};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=${serviceBus.outputs.primaryKey}'
    storageConnectionString: 'DefaultEndpointsProtocol=https;AccountName=${storage.outputs.storageAccountName};AccountKey=${storage.outputs.storageKey};EndpointSuffix=${environment().suffixes.storage}'
    appInsightsConnectionString: appInsights.outputs.connectionString
  }
}

// ============================================================================
// Microservices Deployment
// ============================================================================

// Common environment variables - non-sensitive only
var commonEnvVars = [
  {
    name: 'KeyVault__Uri'
    value: keyVault.outputs.keyVaultUri
  }
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Production' 
  }
  {
    name: 'ServiceUrls__Sso'
    value: 'https://tgp-sso-${environmentName}.${acaEnv.outputs.defaultDomain}'
  }
  {
    name: 'ServiceUrls__Gateway'
    value: 'https://tgp-gateway-${environmentName}.${acaEnv.outputs.defaultDomain}'
  }
  {
    name: 'ServiceUrls__Reporting'
    value: 'https://tgp-reporting-${environmentName}.internal.${acaEnv.outputs.defaultDomain}'
  }
  {
    name: 'ServiceUrls__Api'
    value: 'https://tgp-api-${environmentName}.${acaEnv.outputs.defaultDomain}'
  }
]

// Secrets pulled from Key Vault at runtime (not exposed in plain text)
var secretEnvVars = [
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    secretName: 'appinsights-connectionstring'
  }
  {
    name: 'Redis__ConnectionString'
    secretName: 'redis-connectionstring'
  }
]



// 1. SSO Service (Auth)
module sso 'modules/container-app.bicep' = {
  name: 'ssoDeployment'
  params: {
    location: location
    containerAppName: 'tgp-sso-${environmentName}'
    environmentId: acaEnv.outputs.environmentId
    containerImage: '${acr.outputs.loginServer}/tgp.microservices.sso:latest'
    registryServer: acr.outputs.loginServer
    registryUsername: acr.outputs.registryUsername
    registryPassword: acr.outputs.registryPassword
    envVars: commonEnvVars
    secretEnvVars: secretEnvVars
    keyVaultName: keyVault.outputs.keyVaultName
    containerPort: 8080
    isExternalIngress: true
    tags: tags
  }
  dependsOn: [keyVault]
}

// 2. Device Gateway (Ingestion)
module deviceGateway 'modules/container-app.bicep' = {
  name: 'deviceGatewayDeployment'
  params: {
    location: location
    containerAppName: 'tgp-gateway-${environmentName}'
    environmentId: acaEnv.outputs.environmentId
    containerImage: '${acr.outputs.loginServer}/tgp.microservices.devicegateway:latest'
    registryServer: acr.outputs.loginServer
    registryUsername: acr.outputs.registryUsername
    registryPassword: acr.outputs.registryPassword
    envVars: commonEnvVars
    secretEnvVars: secretEnvVars
    keyVaultName: keyVault.outputs.keyVaultName
    containerPort: 8080
    isExternalIngress: true
    tags: tags
  }
  dependsOn: [keyVault]
}

// 3. User Dashboard (UI)
module userDashboard 'modules/container-app.bicep' = {
  name: 'userDashboardDeployment'
  params: {
    location: location
    containerAppName: 'tgp-dashboard-${environmentName}'
    environmentId: acaEnv.outputs.environmentId
    containerImage: '${acr.outputs.loginServer}/tgp.userdashboard:latest'
    registryServer: acr.outputs.loginServer
    registryUsername: acr.outputs.registryUsername
    registryPassword: acr.outputs.registryPassword
    envVars: commonEnvVars
    secretEnvVars: secretEnvVars
    keyVaultName: keyVault.outputs.keyVaultName
    containerPort: 8080
    isExternalIngress: true
    tags: tags
  }
  dependsOn: [keyVault]
}

// 4. Analysis Service (Worker)
module analysis 'modules/container-app.bicep' = {
  name: 'analysisDeployment'
  params: {
    location: location
    containerAppName: 'tgp-analysis-${environmentName}'
    environmentId: acaEnv.outputs.environmentId
    containerImage: '${acr.outputs.loginServer}/tgp.microservices.analysis:latest'
    registryServer: acr.outputs.loginServer
    registryUsername: acr.outputs.registryUsername
    registryPassword: acr.outputs.registryPassword
    envVars: commonEnvVars
    secretEnvVars: secretEnvVars
    keyVaultName: keyVault.outputs.keyVaultName
    containerPort: 8080
    isExternalIngress: false
    tags: tags
  }
  dependsOn: [keyVault]
}

// 5. Reporting Service (Worker/API)
module reporting 'modules/container-app.bicep' = {
  name: 'reportingDeployment'
  params: {
    location: location
    containerAppName: 'tgp-reporting-${environmentName}'
    environmentId: acaEnv.outputs.environmentId
    containerImage: '${acr.outputs.loginServer}/tgp.microservices.reporting:latest'
    registryServer: acr.outputs.loginServer
    registryUsername: acr.outputs.registryUsername
    registryPassword: acr.outputs.registryPassword
    envVars: commonEnvVars
    secretEnvVars: secretEnvVars
    keyVaultName: keyVault.outputs.keyVaultName
    containerPort: 8080
    isExternalIngress: false
    tags: tags
  }
  dependsOn: [keyVault]
}

// 6. Api Service (General API/Billing)
module api 'modules/container-app.bicep' = {
  name: 'apiDeployment'
  params: {
    location: location
    containerAppName: 'tgp-api-${environmentName}'
    environmentId: acaEnv.outputs.environmentId
    containerImage: '${acr.outputs.loginServer}/tgp.microservices.api:latest'
    registryServer: acr.outputs.loginServer
    registryUsername: acr.outputs.registryUsername
    registryPassword: acr.outputs.registryPassword
    envVars: commonEnvVars
    secretEnvVars: secretEnvVars
    keyVaultName: keyVault.outputs.keyVaultName
    containerPort: 8080
    isExternalIngress: true
    tags: tags
  }
  dependsOn: [keyVault]
}

// 7. Admin Portal (Web App)
module adminPortal 'modules/container-app.bicep' = {
  name: 'adminPortalDeployment'
  params: {
    location: location
    containerAppName: 'tgp-admin-${environmentName}'
    environmentId: acaEnv.outputs.environmentId
    containerImage: '${acr.outputs.loginServer}/tgp.adminportal:latest'
    registryServer: acr.outputs.loginServer
    registryUsername: acr.outputs.registryUsername
    registryPassword: acr.outputs.registryPassword
    envVars: commonEnvVars
    secretEnvVars: secretEnvVars
    keyVaultName: keyVault.outputs.keyVaultName
    containerPort: 8080
    isExternalIngress: true
    tags: tags
  }
  dependsOn: [keyVault]
}

// Outputs
output acrLoginServer string = acr.outputs.loginServer
output acaEnvironmentId string = acaEnv.outputs.environmentId
output sqlServerFqdn string = sql.outputs.fqdn
output redisHost string = redis.outputs.hostName
output serviceBusEndpoint string = serviceBus.outputs.endpoint
output storageAccountName string = storage.outputs.storageAccountName
output keyVaultUri string = keyVault.outputs.keyVaultUri
output keyVaultName string = keyVault.outputs.keyVaultName
