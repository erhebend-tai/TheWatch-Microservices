# ============================================================
# Azure Service Bus Module
# Item 124: Event queues replacing Kafka in cloud
# ============================================================

resource "azurerm_servicebus_namespace" "main" {
  name                = "${var.resource_prefix}-sb"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.sku

  minimum_tls_version = "1.2"

  tags = var.tags
}

# Topics — one per event type
resource "azurerm_servicebus_topic" "topics" {
  for_each = var.topics

  name                  = each.key
  namespace_id          = azurerm_servicebus_namespace.main.id
  max_size_in_megabytes = 1024
  default_message_ttl   = "P14D"

  enable_partitioning = var.sku == "Premium" ? true : false
}

# Subscriptions — one per consumer per topic
resource "azurerm_servicebus_subscription" "subscriptions" {
  for_each = {
    for pair in flatten([
      for topic_name, topic_config in var.topics : [
        for sub in topic_config.subscriptions : {
          key        = "${topic_name}--${sub}"
          topic_name = topic_name
          sub_name   = sub
        }
      ]
    ]) : pair.key => pair
  }

  name                                 = each.value.sub_name
  topic_id                             = azurerm_servicebus_topic.topics[each.value.topic_name].id
  max_delivery_count                   = 10
  default_message_ttl                  = "P7D"
  dead_lettering_on_message_expiration = true
  lock_duration                        = "PT1M"
}

# Dead-letter topic for failed messages
resource "azurerm_servicebus_topic" "dead_letter_audit" {
  name                  = "dead-letter-audit"
  namespace_id          = azurerm_servicebus_namespace.main.id
  max_size_in_megabytes = 5120
  default_message_ttl   = "P30D"
}

# Authorization rules
resource "azurerm_servicebus_namespace_authorization_rule" "app_sender" {
  name         = "app-sender"
  namespace_id = azurerm_servicebus_namespace.main.id
  listen       = false
  send         = true
  manage       = false
}

resource "azurerm_servicebus_namespace_authorization_rule" "app_listener" {
  name         = "app-listener"
  namespace_id = azurerm_servicebus_namespace.main.id
  listen       = true
  send         = false
  manage       = false
}

# Store connection string in Key Vault
resource "azurerm_key_vault_secret" "servicebus_connection" {
  name         = "servicebus-connection-string"
  value        = azurerm_servicebus_namespace.main.default_primary_connection_string
  key_vault_id = var.key_vault_id

  tags = var.tags
}
