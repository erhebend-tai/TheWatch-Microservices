output "server_fqdn" {
  value = azurerm_mssql_server.primary.fully_qualified_domain_name
}

output "server_id" {
  value = azurerm_mssql_server.primary.id
}

output "primary_connection_string" {
  value     = "Server=tcp:${azurerm_mssql_server.primary.fully_qualified_domain_name},1433;User ID=${var.admin_username};Password=${var.admin_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  sensitive = true
}

output "database_ids" {
  value = { for name, db in azurerm_mssql_database.databases : name => db.id }
}

output "failover_group_fqdn" {
  value = var.enable_geo_replication ? azurerm_mssql_failover_group.failover[0].partner_server[0].id : null
}
