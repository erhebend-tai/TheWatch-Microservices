output "namespace_name" {
  value = azurerm_servicebus_namespace.main.name
}

output "namespace_id" {
  value = azurerm_servicebus_namespace.main.id
}

output "connection_string" {
  value     = azurerm_servicebus_namespace.main.default_primary_connection_string
  sensitive = true
}

output "topic_ids" {
  value = { for name, topic in azurerm_servicebus_topic.topics : name => topic.id }
}
