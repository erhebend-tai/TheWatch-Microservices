environment        = "production"
location           = "eastus2"
location_secondary = "westus2"

# SQL
sql_admin_username = "thewatch_admin"
sql_sku            = "GP_S_Gen5_2"
sql_max_size_gb    = 64

# Cosmos DB
cosmos_max_throughput = 10000

# Redis
redis_sku      = "Premium"
redis_capacity = 2

# Service Bus
servicebus_sku = "Premium"

# Storage
storage_replication = "GRS"

# ACR
acr_name = "thewatchacr"
