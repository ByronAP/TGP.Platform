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

resource clientUpdatesTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBus
  name: 'client-updates'
  properties: {}
}

resource deviceGatewaySubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: clientUpdatesTopic
  name: 'device-gateway'
  properties: {
    maxDeliveryCount: 10
    deadLetteringOnFilterEvaluationExceptions: true
    deadLetteringOnMessageExpiration: true
  }
}


resource eventsTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBus
  name: 'tgp.events'
  properties: {}
}

resource dashboardSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: eventsTopic
  name: 'user-dashboard'
  properties: {
    maxDeliveryCount: 10
    deadLetteringOnFilterEvaluationExceptions: true
    deadLetteringOnMessageExpiration: true
  }
}


resource commandsQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBus
  name: 'tgp.device.commands'
  properties: {
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
  }
}

output endpoint string = serviceBus.properties.serviceBusEndpoint
output namespaceName string = serviceBus.name
#disable-next-line use-resource-id-functions // Suppress warning about manual ID construction if needed, or just use listKeys on the resource
output primaryKey string = listKeys('${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey', serviceBus.apiVersion).primaryKey
