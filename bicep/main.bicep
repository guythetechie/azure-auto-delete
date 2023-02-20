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

module functionApp 'functionapp.bicep' = {
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

output logAnalyticsWorkspaceName string = logAnalyticsWorkspaceName
output applicationInsightsName string = applicationInsightsName
output storageAccountName string = storageAccountName
output appServicePlanName string = appServicePlanName
output functionAppName string = functionAppName
