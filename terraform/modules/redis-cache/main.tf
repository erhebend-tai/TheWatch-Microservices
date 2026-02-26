# ============================================================
# Azure Redis Cache Module
# Item 123: Session store, rate limiting, real-time cache
# ============================================================

resource "azurerm_redis_cache" "main" {
  name                = "${var.resource_prefix}-redis"
  location            = var.location
  resource_group_name = var.resource_group_name

  capacity            = var.capacity
  family              = var.sku_name == "Premium" ? "P" : "C"
  sku_name            = var.sku_name
  non_ssl_port_enabled = false
  minimum_tls_version = "1.2"

  redis_configuration {
    maxmemory_policy = "allkeys-lru"
  }

  patch_schedule {
    day_of_week    = "Sunday"
    start_hour_utc = 4
  }

  tags = var.tags
}

# Firewall rule — allow Azure services
resource "azurerm_redis_firewall_rule" "allow_azure" {
  name                = "AllowAzureServices"
  redis_cache_name    = azurerm_redis_cache.main.name
  resource_group_name = var.resource_group_name
  start_ip            = "0.0.0.0"
  end_ip              = "0.0.0.0"
}

# Store connection string in Key Vault
resource "azurerm_key_vault_secret" "redis_connection" {
  name         = "redis-connection-string"
  value        = azurerm_redis_cache.main.primary_connection_string
  key_vault_id = var.key_vault_id

  tags = var.tags
}
