output "endpoint" {
  value = azurerm_cosmosdb_account.main.endpoint
}

output "account_id" {
  value = azurerm_cosmosdb_account.main.id
}

output "connection_string" {
  value     = azurerm_cosmosdb_account.main.primary_mongodb_connection_string
  sensitive = true
}

output "account_name" {
  value = azurerm_cosmosdb_account.main.name
}
