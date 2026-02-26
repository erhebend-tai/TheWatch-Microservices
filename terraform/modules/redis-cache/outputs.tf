output "hostname" {
  value = azurerm_redis_cache.main.hostname
}

output "ssl_port" {
  value = azurerm_redis_cache.main.ssl_port
}

output "connection_string" {
  value     = azurerm_redis_cache.main.primary_connection_string
  sensitive = true
}

output "redis_id" {
  value = azurerm_redis_cache.main.id
}
