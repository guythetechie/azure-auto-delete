targetScope = 'subscription'

param tags object = {}
param location string
param resourceGroupName string = 'autodelete-rg'

var logAnalyticsWorkspaceName = 'autodelete-${uniqueString(resourceGroup.id)}-law'
var applicationInsightsName = 'autodelete-${uniqueString(resourceGroup.id)}-appinsights'
var storageAccountName = 'autodelete${uniqueString(resourceGroup.id)}'
var appServicePlanName = 'autodelete-${uniqueString(resourceGroup.id)}-appserviceplan'
var functionAppName = 'autodelete-${uniqueString(resourceGroup.id)}-functionapp'

resource resourceGroup 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module functionAppModule 'functionapp.bicep' = {
  scope: resourceGroup
  name: 'functionAppModule'
  params: {
    tags: tags
    location: location
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    applicationInsightsName: applicationInsightsName
    storageAccountName: storageAccountName
    appServicePlanName: appServicePlanName
    functionAppName: functionAppName
  }
}

resource contributorRoleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: 'b24988ac-6180-42a0-ab88-20f7382dd24c'
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' existing = {
  name: functionAppName
  scope: resourceGroup
}

resource functionAppContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  name: guid(subscription().id, functionApp.id, contributorRoleDefinition.id)
  scope: subscription()
  properties: {
    principalId: functionApp.identity.principalId
    roleDefinitionId: contributorRoleDefinition.id
    principalType: 'ServicePrincipal'
  }
}

output resourceGroupName string = resourceGroup.name
output functionAppName string = functionAppName
