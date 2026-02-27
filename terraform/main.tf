locals {
  resource_prefix = "${var.project}-${var.environment}"
  common_tags = merge(var.tags, {
    Project     = "TheWatch"
    Environment = var.environment
    ManagedBy   = "Terraform"
  })

  # All 10 SQL Server databases for microservices
  databases = {
    "WatchCoreGatewayDB"    = { service = "p1-coregateway", tier = "standard" }
    "WatchVoiceEmergencyDB" = { service = "p2-voiceemergency", tier = "critical" }
    "WatchMeshNetworkDB"    = { service = "p3-meshnetwork", tier = "standard" }
    "WatchWearableDB"       = { service = "p4-wearable", tier = "standard" }
    "WatchAuthSecurityDB"   = { service = "p5-authsecurity", tier = "critical" }
    "WatchFirstResponderDB" = { service = "p6-firstresponder", tier = "critical" }
    "WatchFamilyHealthDB"   = { service = "p7-familyhealth", tier = "standard" }
    "WatchDisasterReliefDB" = { service = "p8-disasterrelief", tier = "standard" }
    "WatchDoctorServicesDB" = { service = "p9-doctorservices", tier = "standard" }
    "WatchGamificationDB"   = { service = "p10-gamification", tier = "standard" }
    "WatchSurveillanceDB"   = { service = "p11-surveillance", tier = "standard" }
    "WatchNotificationsDB"  = { service = "p12-notifications", tier = "standard" }
  }

  # Service Bus topics (replacing Kafka in cloud)
  servicebus_topics = {
    "incident-created"    = { subscriptions = ["p6-firstresponder", "p3-meshnetwork", "dashboard", "p12-notifications"] }
    "dispatch-requested"  = { subscriptions = ["p3-meshnetwork", "p6-firstresponder"] }
    "responder-located"   = { subscriptions = ["p2-voiceemergency", "dashboard"] }
    "checkin-completed"   = { subscriptions = ["p7-familyhealth", "dashboard", "p12-notifications"] }
    "vital-alert"         = { subscriptions = ["p7-familyhealth", "p9-doctorservices", "p12-notifications"] }
    "evidence-uploaded"   = { subscriptions = ["p2-voiceemergency"] }
    "disaster-declared"   = { subscriptions = ["p8-disasterrelief", "p6-firstresponder", "dashboard"] }
    "footage-submitted"   = { subscriptions = ["p11-surveillance", "p2-voiceemergency"] }
    "crime-location-reported" = { subscriptions = ["p11-surveillance", "p6-firstresponder"] }
    "dead-letter"         = { subscriptions = ["monitoring"] }
  }

  # Container Apps services
  container_apps = {
    "p1-coregateway"    = { cpu = 0.25, memory = "0.5Gi", min_replicas = 1, max_replicas = 3 }
    "p2-voiceemergency" = { cpu = 0.5, memory = "1Gi", min_replicas = 2, max_replicas = 20 }
    "p3-meshnetwork"    = { cpu = 0.25, memory = "0.5Gi", min_replicas = 1, max_replicas = 5 }
    "p4-wearable"       = { cpu = 0.25, memory = "0.5Gi", min_replicas = 1, max_replicas = 3 }
    "p5-authsecurity"   = { cpu = 0.5, memory = "1Gi", min_replicas = 1, max_replicas = 5 }
    "p6-firstresponder" = { cpu = 0.5, memory = "1Gi", min_replicas = 2, max_replicas = 15 }
    "p7-familyhealth"   = { cpu = 0.25, memory = "0.5Gi", min_replicas = 1, max_replicas = 5 }
    "p8-disasterrelief" = { cpu = 0.25, memory = "0.5Gi", min_replicas = 1, max_replicas = 5 }
    "p9-doctorservices" = { cpu = 0.25, memory = "0.5Gi", min_replicas = 1, max_replicas = 3 }
    "p10-gamification"  = { cpu = 0.25, memory = "0.5Gi", min_replicas = 1, max_replicas = 2 }
    "p11-surveillance"  = { cpu = 0.5, memory = "1Gi", min_replicas = 1, max_replicas = 10 }
    "p12-notifications" = { cpu = 0.25, memory = "0.5Gi", min_replicas = 1, max_replicas = 5 }
    "geospatial"        = { cpu = 0.5, memory = "1Gi", min_replicas = 1, max_replicas = 5 }
    "dashboard"         = { cpu = 0.25, memory = "0.5Gi", min_replicas = 1, max_replicas = 3 }
  }
}

# ============================================================
# Resource Group
# ============================================================

resource "azurerm_resource_group" "main" {
  name     = "${local.resource_prefix}-rg"
  location = var.location
  tags     = local.common_tags
}

# ============================================================
# Item 121: Azure SQL Database (10 databases, geo-replicated)
# ============================================================

module "sql_database" {
  source = "./modules/sql-database"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  location_secondary  = var.location_secondary
  resource_prefix     = local.resource_prefix
  tags                = local.common_tags

  admin_username = var.sql_admin_username
  admin_password = random_password.sql_admin.result
  databases      = local.databases
  sku_name       = var.sql_sku
  max_size_gb    = var.sql_max_size_gb

  enable_geo_replication = var.environment == "production"
  key_vault_id           = module.key_vault.key_vault_id
}

# ============================================================
# Item 122: Azure Cosmos DB (MongoDB API, multi-region writes)
# ============================================================

module "cosmos_db" {
  source = "./modules/cosmos-db"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  location_secondary  = var.location_secondary
  resource_prefix     = local.resource_prefix
  tags                = local.common_tags

  max_throughput          = var.cosmos_max_throughput
  enable_multi_region     = var.environment == "production"
  key_vault_id            = module.key_vault.key_vault_id
}

# ============================================================
# Item 123: Azure Redis Cache (session store, rate limiting)
# ============================================================

module "redis_cache" {
  source = "./modules/redis-cache"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  resource_prefix     = local.resource_prefix
  tags                = local.common_tags

  sku_name     = var.redis_sku
  capacity     = var.redis_capacity
  key_vault_id = module.key_vault.key_vault_id
}

# ============================================================
# Item 124: Azure Service Bus (event queues)
# ============================================================

module "service_bus" {
  source = "./modules/service-bus"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  resource_prefix     = local.resource_prefix
  tags                = local.common_tags

  sku          = var.servicebus_sku
  topics       = local.servicebus_topics
  key_vault_id = module.key_vault.key_vault_id
}

# ============================================================
# Item 125: Azure Key Vault (secrets, certificates, JWT keys)
# ============================================================

module "key_vault" {
  source = "./modules/key-vault"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  resource_prefix     = local.resource_prefix
  tags                = local.common_tags

  environment = var.environment
}

# ============================================================
# Item 126: Azure Container Apps (ACA environment + apps)
# ============================================================

module "container_apps" {
  source = "./modules/container-apps"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  resource_prefix     = local.resource_prefix
  tags                = local.common_tags

  acr_name       = var.acr_name
  container_apps = local.container_apps
  key_vault_id   = module.key_vault.key_vault_id

  sql_connection_string    = module.sql_database.primary_connection_string
  cosmos_connection_string = module.cosmos_db.connection_string
  redis_connection_string  = module.redis_cache.connection_string
  servicebus_connection    = module.service_bus.connection_string
  storage_connection       = module.storage_account.connection_string
}

# ============================================================
# Item 127: Azure Storage (evidence blobs)
# ============================================================

module "storage_account" {
  source = "./modules/storage-account"

  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  resource_prefix     = local.resource_prefix
  tags                = local.common_tags

  replication_type = var.storage_replication
  key_vault_id     = module.key_vault.key_vault_id
}

# ============================================================
# Shared secrets
# ============================================================

resource "random_password" "sql_admin" {
  length           = 32
  special          = true
  override_special = "!@#$%&*()-_=+[]{}"
}

resource "random_password" "jwt_signing_key" {
  length  = 64
  special = false
}
