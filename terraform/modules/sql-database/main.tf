# ============================================================
# Azure SQL Database Module
# Item 121: 10 databases, geo-replicated
# ============================================================

resource "azurerm_mssql_server" "primary" {
  name                         = "${var.resource_prefix}-sql"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.admin_username
  administrator_login_password = var.admin_password
  minimum_tls_version          = "1.2"

  azuread_administrator {
    login_username = "thewatch-sql-admin"
    object_id      = data.azuread_client_config.current.object_id
  }

  tags = var.tags
}

# Secondary server for geo-replication (production only)
resource "azurerm_mssql_server" "secondary" {
  count = var.enable_geo_replication ? 1 : 0

  name                         = "${var.resource_prefix}-sql-secondary"
  resource_group_name          = var.resource_group_name
  location                     = var.location_secondary
  version                      = "12.0"
  administrator_login          = var.admin_username
  administrator_login_password = var.admin_password
  minimum_tls_version          = "1.2"

  tags = var.tags
}

# Firewall: allow Azure services
resource "azurerm_mssql_firewall_rule" "allow_azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.primary.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# 10 databases — one per microservice
resource "azurerm_mssql_database" "databases" {
  for_each = var.databases

  name      = each.key
  server_id = azurerm_mssql_server.primary.id
  sku_name  = each.value.tier == "critical" ? var.sku_name_critical : var.sku_name
  collation = "SQL_Latin1_General_CP1_CI_AS"

  max_size_gb = var.max_size_gb

  short_term_retention_policy {
    retention_days           = 7
    backup_interval_in_hours = 12
  }

  long_term_retention_policy {
    weekly_retention  = "P4W"
    monthly_retention = "P12M"
    yearly_retention  = each.value.tier == "critical" ? "P5Y" : "P1Y"
    week_of_year      = 1
  }

  threat_detection_policy {
    state = "Enabled"
  }

  tags = merge(var.tags, {
    Service = each.value.service
    Tier    = each.value.tier
  })
}

# Geo-replication for critical databases (production only)
resource "azurerm_mssql_failover_group" "failover" {
  count = var.enable_geo_replication ? 1 : 0

  name      = "${var.resource_prefix}-sql-failover"
  server_id = azurerm_mssql_server.primary.id

  partner_server {
    id = azurerm_mssql_server.secondary[0].id
  }

  read_write_endpoint_failover_policy {
    mode          = "Automatic"
    grace_minutes = 60
  }

  databases = [
    for name, db in azurerm_mssql_database.databases : db.id
    if var.databases[name].tier == "critical"
  ]
}

# Auditing — write to Key Vault-secured storage
resource "azurerm_mssql_server_extended_auditing_policy" "audit" {
  server_id              = azurerm_mssql_server.primary.id
  retention_in_days      = 90
  log_monitoring_enabled = true
}

# Store connection string in Key Vault
resource "azurerm_key_vault_secret" "sql_connection" {
  name         = "sql-connection-string"
  value        = "Server=tcp:${azurerm_mssql_server.primary.fully_qualified_domain_name},1433;User ID=${var.admin_username};Password=${var.admin_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = var.key_vault_id

  tags = var.tags
}

resource "azurerm_key_vault_secret" "sql_password" {
  name         = "sql-admin-password"
  value        = var.admin_password
  key_vault_id = var.key_vault_id

  tags = var.tags
}

data "azuread_client_config" "current" {}
