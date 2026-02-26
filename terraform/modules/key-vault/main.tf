# ============================================================
# Azure Key Vault Module
# Item 125: Secrets, certificates, JWT signing keys
# ============================================================

data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "main" {
  name                = "${var.resource_prefix}-kv"
  location            = var.location
  resource_group_name = var.resource_group_name
  tenant_id           = data.azurerm_client_config.current.tenant_id

  sku_name                  = "standard"
  soft_delete_retention_days = 90
  purge_protection_enabled  = var.environment == "production"

  enable_rbac_authorization = true

  network_acls {
    default_action = "Allow"
    bypass         = "AzureServices"
  }

  tags = var.tags
}

# Grant current deployer full Key Vault access
resource "azurerm_role_assignment" "deployer_secrets" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_role_assignment" "deployer_certificates" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Certificates Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_role_assignment" "deployer_keys" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Crypto Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

# Diagnostic settings for audit logging
# Enable when Log Analytics workspace is wired in:
#
# resource "azurerm_monitor_diagnostic_setting" "keyvault" {
#   name                       = "${var.resource_prefix}-kv-diag"
#   target_resource_id         = azurerm_key_vault.main.id
#   log_analytics_workspace_id = var.log_analytics_workspace_id
#
#   enabled_log {
#     category = "AuditEvent"
#   }
#
#   metric {
#     category = "AllMetrics"
#     enabled  = true
#   }
# }
