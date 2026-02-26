# ============================================================
# Azure Cosmos DB Module
# Item 122: MongoDB API, multi-region writes
# ============================================================

resource "azurerm_cosmosdb_account" "main" {
  name                = "${var.resource_prefix}-cosmos"
  location            = var.location
  resource_group_name = var.resource_group_name
  offer_type          = "Standard"
  kind                = "MongoDB"

  mongo_server_version = "4.2"

  automatic_failover_enabled = true

  capabilities {
    name = "EnableMongo"
  }

  capabilities {
    name = "EnableServerless"
  }

  consistency_policy {
    consistency_level       = "Session"
    max_interval_in_seconds = 5
    max_staleness_prefix    = 100
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }

  dynamic "geo_location" {
    for_each = var.enable_multi_region ? [1] : []
    content {
      location          = var.location_secondary
      failover_priority = 1
    }
  }

  tags = var.tags
}

# MongoDB databases for microservices
resource "azurerm_cosmosdb_mongo_database" "geospatial" {
  name                = "thewatch-geospatial"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
}

resource "azurerm_cosmosdb_mongo_database" "mesh_network" {
  name                = "thewatch-mesh"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
}

resource "azurerm_cosmosdb_mongo_database" "disaster_relief" {
  name                = "thewatch-disaster"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
}

# Collections with autoscale throughput
resource "azurerm_cosmosdb_mongo_collection" "incident_locations" {
  name                = "incident-locations"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.geospatial.name

  default_ttl_seconds = -1
  shard_key           = "regionId"

  index {
    keys   = ["_id"]
    unique = true
  }

  index {
    keys = ["location"]
  }

  index {
    keys = ["regionId", "timestamp"]
  }

  autoscale_settings {
    max_throughput = var.max_throughput
  }
}

resource "azurerm_cosmosdb_mongo_collection" "responder_positions" {
  name                = "responder-positions"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.geospatial.name

  default_ttl_seconds = 86400
  shard_key           = "responderId"

  index {
    keys   = ["_id"]
    unique = true
  }

  index {
    keys = ["location"]
  }

  index {
    keys = ["responderId"]
  }

  autoscale_settings {
    max_throughput = var.max_throughput
  }
}

resource "azurerm_cosmosdb_mongo_collection" "mesh_nodes" {
  name                = "mesh-nodes"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.mesh_network.name

  default_ttl_seconds = 3600
  shard_key           = "meshId"

  index {
    keys   = ["_id"]
    unique = true
  }

  index {
    keys = ["meshId", "lastSeen"]
  }

  autoscale_settings {
    max_throughput = var.max_throughput
  }
}

resource "azurerm_cosmosdb_mongo_collection" "shelter_locations" {
  name                = "shelter-locations"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.disaster_relief.name

  default_ttl_seconds = -1
  shard_key           = "regionId"

  index {
    keys   = ["_id"]
    unique = true
  }

  index {
    keys = ["location"]
  }

  autoscale_settings {
    max_throughput = var.max_throughput
  }
}

# Store connection string in Key Vault
resource "azurerm_key_vault_secret" "cosmos_connection" {
  name         = "cosmos-connection-string"
  value        = azurerm_cosmosdb_account.main.primary_mongodb_connection_string
  key_vault_id = var.key_vault_id

  tags = var.tags
}
