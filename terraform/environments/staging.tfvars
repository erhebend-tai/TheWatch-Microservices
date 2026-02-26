environment        = "staging"
location           = "eastus2"
location_secondary = "westus2"

# SQL
sql_admin_username = "thewatch_admin"
sql_sku            = "GP_S_Gen5_1"
sql_max_size_gb    = 32

# Cosmos DB
cosmos_max_throughput = 4000

# Redis
redis_sku      = "Standard"
redis_capacity = 1

# Service Bus
servicebus_sku = "Standard"

# Storage
storage_replication = "LRS"

# ACR
acr_name = "thewatchacrstaging"
