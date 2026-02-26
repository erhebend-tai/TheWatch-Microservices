output "account_name" {
  value = azurerm_storage_account.main.name
}

output "account_id" {
  value = azurerm_storage_account.main.id
}

output "connection_string" {
  value     = azurerm_storage_account.main.primary_connection_string
  sensitive = true
}

output "primary_blob_endpoint" {
  value = azurerm_storage_account.main.primary_blob_endpoint
}
