# Stream 5: CLOUD-AZURE — Stage 11A + 11B (TODO Items 121-131)

You are working in a git worktree of TheWatch microservices solution. Your task is to create Azure infrastructure-as-code using Bicep and integrate Azure services.

## YOUR ASSIGNED TODO ITEMS

### 11A. Azure Infrastructure (121-127)
121. Create Bicep module for Azure SQL Database (10 databases, geo-replicated)
122. Create Bicep module for Azure Cosmos DB (MongoDB API, multi-region writes)
123. Create Bicep module for Azure Redis Cache (session store, rate limiting)
124. Create Bicep module for Azure Service Bus (event queues)
125. Create Bicep module for Azure Key Vault (secrets, certificates, JWT signing keys)
126. Create Bicep module for Azure Container Apps or AKS cluster
127. Create Bicep module for Azure Storage (evidence blobs)

### 11B. Azure Service Integration (128-131)
128. Integrate Azure SignalR Service (managed, replaces self-hosted SignalR)
129. Integrate Azure Maps for geospatial (cloud alternative to self-hosted PostGIS)
130. Integrate Azure Communication Services for SMS/email notifications
131. Integrate Application Insights for distributed tracing and APM

## FILES YOU MAY CREATE (your exclusive scope — all new files)

- `infra/bicep/main.bicep` — orchestrator
- `infra/bicep/parameters/dev.bicepparam`
- `infra/bicep/parameters/staging.bicepparam`
- `infra/bicep/parameters/prod.bicepparam`
- `infra/bicep/modules/sql-server.bicep`
- `infra/bicep/modules/cosmos-db.bicep`
- `infra/bicep/modules/redis-cache.bicep`
- `infra/bicep/modules/service-bus.bicep`
- `infra/bicep/modules/key-vault.bicep`
- `infra/bicep/modules/container-apps.bicep` (or `aks-cluster.bicep`)
- `infra/bicep/modules/storage-account.bicep`
- `infra/bicep/modules/signalr-service.bicep`
- `infra/bicep/modules/azure-maps.bicep`
- `infra/bicep/modules/communication-services.bicep`
- `infra/bicep/modules/application-insights.bicep`
- `infra/bicep/modules/log-analytics.bicep`
- `infra/bicep/README.md`

## FILES YOU MUST NOT TOUCH

- Any `.cs` file
- Any `.csproj` file
- `TheWatch.sln`
- `TheWatch.Shared/`
- `TheWatch.Mobile/`
- `TheWatch.Dashboard/`
- `docker/`, `helm/`, `.github/`
- `infra/cloudflare/` (owned by Stream 6)

## BICEP PATTERNS

### Main orchestrator:
```bicep
targetScope = 'subscription'

@description('Environment name')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Primary Azure region')
param location string = 'eastus'

@description('Project name prefix')
param projectName string = 'thewatch'

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: '${projectName}-${environment}-rg'
  location: location
}

// Modules
module sql 'modules/sql-server.bicep' = {
  scope: rg
  name: 'sql-deployment'
  params: {
    location: location
    environment: environment
    projectName: projectName
  }
}
// ... other modules
```

### SQL Server module (10 databases):
```bicep
param location string
param environment string
param projectName string

var databases = [
  'WatchCoreGatewayDB'
  'WatchVoiceEmergencyDB'
  'WatchMeshNetworkDB'
  'WatchWearableDB'
  'WatchAuthSecurityDB'
  'WatchFirstResponderDB'
  'WatchFamilyHealthDB'
  'WatchDisasterReliefDB'
  'WatchDoctorServicesDB'
  'WatchGamificationDB'
]

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${projectName}-${environment}-sql'
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: // reference Key Vault
    minimalTlsVersion: '1.2'
  }
}

resource dbs 'Microsoft.Sql/servers/databases@2023-08-01-preview' = [for db in databases: {
  parent: sqlServer
  name: db
  location: location
  sku: {
    name: environment == 'prod' ? 'S2' : 'Basic'
  }
  properties: {
    // geo-replication for prod
  }
}]
```

### Container Apps module:
```bicep
resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${projectName}-${environment}-env'
  location: location
  properties: {
    daprAIInstrumentationKey: appInsights.outputs.instrumentationKey
    // ...
  }
}

var services = [
  { name: 'p1-coregateway', image: 'thewatch/p1-coregateway' }
  { name: 'p2-voiceemergency', image: 'thewatch/p2-voiceemergency' }
  // ... all 12 services
]

resource containerApps 'Microsoft.App/containerApps@2024-03-01' = [for svc in services: {
  name: '${projectName}-${svc.name}'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: { external: true, targetPort: 8080 }
      secrets: [/* from Key Vault */]
    }
    template: {
      containers: [{
        name: svc.name
        image: '${acrLoginServer}/${svc.image}:latest'
        resources: { cpu: json('0.5'), memory: '1Gi' }
      }]
      scale: {
        minReplicas: environment == 'prod' ? 2 : 1
        maxReplicas: svc.name == 'p2-voiceemergency' || svc.name == 'p6-firstresponder' ? 20 : 5
      }
    }
  }
}]
```

### Key Vault with secrets:
```bicep
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${projectName}-${environment}-kv'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}
```

### Azure SignalR Service:
```bicep
resource signalr 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: '${projectName}-${environment}-signalr'
  location: location
  sku: {
    name: environment == 'prod' ? 'Standard_S1' : 'Free_F1'
    capacity: environment == 'prod' ? 2 : 1
  }
  properties: {
    features: [{ flag: 'ServiceMode', value: 'Default' }]
    cors: { allowedOrigins: ['*'] }
  }
}
```

## WHEN DONE

Commit all changes with message:
```
feat(infra): add Azure Bicep modules for SQL, Cosmos, Redis, Service Bus, Key Vault, Container Apps

Items 121-131: Azure SQL (10 DBs, geo-replicated), Cosmos DB (MongoDB API),
Redis Cache, Service Bus, Key Vault, Container Apps with auto-scaling,
Storage Account, SignalR Service, Azure Maps, Communication Services, App Insights
```
