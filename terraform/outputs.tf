output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "sql_server_fqdn" {
  value = module.sql_database.server_fqdn
}

output "cosmos_endpoint" {
  value = module.cosmos_db.endpoint
}

output "redis_hostname" {
  value = module.redis_cache.hostname
}

output "servicebus_namespace" {
  value = module.service_bus.namespace_name
}

output "key_vault_uri" {
  value = module.key_vault.vault_uri
}

output "container_apps_fqdn" {
  value = module.container_apps.default_domain
}

output "storage_account_name" {
  value = module.storage_account.account_name
}

output "acr_login_server" {
  value     = module.container_apps.acr_login_server
  sensitive = false
}
