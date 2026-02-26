# ============================================================
# Azure Container Apps Module
# Item 126: ACA environment, ACR, 12 container apps
# ============================================================

# Azure Container Registry
resource "azurerm_container_registry" "acr" {
  name                = var.acr_name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "Standard"
  admin_enabled       = true

  tags = var.tags
}

# Log Analytics workspace for Container Apps
resource "azurerm_log_analytics_workspace" "aca" {
  name                = "${var.resource_prefix}-aca-logs"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = var.tags
}

# Container Apps Environment
resource "azurerm_container_app_environment" "main" {
  name                       = "${var.resource_prefix}-aca-env"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.aca.id

  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
    minimum_count         = 0
    maximum_count         = 0
  }

  tags = var.tags
}

# Container Apps — one per microservice
resource "azurerm_container_app" "apps" {
  for_each = var.container_apps

  name                         = "${var.resource_prefix}-${each.key}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  template {
    min_replicas = each.value.min_replicas
    max_replicas = each.value.max_replicas

    container {
      name   = each.key
      image  = "${azurerm_container_registry.acr.login_server}/${each.key}:latest"
      cpu    = each.value.cpu
      memory = each.value.memory

      env {
        name        = "ASPNETCORE_ENVIRONMENT"
        value       = "Production"
      }

      env {
        name        = "ConnectionStrings__SqlServer"
        secret_name = "sql-connection"
      }

      env {
        name        = "ConnectionStrings__CosmosDB"
        secret_name = "cosmos-connection"
      }

      env {
        name        = "ConnectionStrings__Redis"
        secret_name = "redis-connection"
      }

      env {
        name        = "ConnectionStrings__ServiceBus"
        secret_name = "servicebus-connection"
      }

      env {
        name        = "ConnectionStrings__Storage"
        secret_name = "storage-connection"
      }

      liveness_probe {
        transport = "HTTP"
        path      = "/health/live"
        port      = 8080
      }

      readiness_probe {
        transport = "HTTP"
        path      = "/health/ready"
        port      = 8080
      }

      startup_probe {
        transport = "HTTP"
        path      = "/health/startup"
        port      = 8080
      }
    }
  }

  secret {
    name  = "sql-connection"
    value = var.sql_connection_string
  }

  secret {
    name  = "cosmos-connection"
    value = var.cosmos_connection_string
  }

  secret {
    name  = "redis-connection"
    value = var.redis_connection_string
  }

  secret {
    name  = "servicebus-connection"
    value = var.servicebus_connection
  }

  secret {
    name  = "storage-connection"
    value = var.storage_connection
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.acr.admin_password
  }

  registry {
    server               = azurerm_container_registry.acr.login_server
    username             = azurerm_container_registry.acr.admin_username
    password_secret_name = "acr-password"
  }

  # Only the gateway and dashboard get external ingress
  ingress {
    external_enabled = contains(["p1-coregateway", "dashboard"], each.key)
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = var.tags
}

# Store ACR credentials in Key Vault
resource "azurerm_key_vault_secret" "acr_admin_password" {
  name         = "acr-admin-password"
  value        = azurerm_container_registry.acr.admin_password
  key_vault_id = var.key_vault_id

  tags = var.tags
}
