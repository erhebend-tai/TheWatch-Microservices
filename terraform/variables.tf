variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

variable "environment" {
  description = "Environment name (staging, production)"
  type        = string
  validation {
    condition     = contains(["staging", "production"], var.environment)
    error_message = "Environment must be 'staging' or 'production'."
  }
}

variable "location" {
  description = "Primary Azure region"
  type        = string
  default     = "eastus2"
}

variable "location_secondary" {
  description = "Secondary Azure region for geo-replication"
  type        = string
  default     = "westus2"
}

variable "project" {
  description = "Project name prefix for all resources"
  type        = string
  default     = "thewatch"
}

variable "tags" {
  description = "Common tags applied to all resources"
  type        = map(string)
  default     = {}
}

# SQL Server
variable "sql_admin_username" {
  description = "SQL Server administrator username"
  type        = string
  default     = "watchadmin"
}

variable "sql_sku" {
  description = "SQL Server SKU name (e.g., GP_S_Gen5_1 for serverless General Purpose)"
  type        = string
  default     = "GP_S_Gen5_1"
}

variable "sql_max_size_gb" {
  description = "Maximum database size in GB"
  type        = number
  default     = 32
}

# Cosmos DB
variable "cosmos_max_throughput" {
  description = "Cosmos DB autoscale max throughput (RU/s)"
  type        = number
  default     = 4000
}

# Redis
variable "redis_sku" {
  description = "Redis Cache SKU (Basic, Standard, Premium)"
  type        = string
  default     = "Standard"
}

variable "redis_capacity" {
  description = "Redis Cache capacity (0-6 for Basic/Standard, 1-5 for Premium)"
  type        = number
  default     = 1
}

# Service Bus
variable "servicebus_sku" {
  description = "Service Bus namespace SKU (Basic, Standard, Premium)"
  type        = string
  default     = "Standard"
}

# Container Apps
variable "container_apps_sku" {
  description = "Container Apps Environment workload profile (Consumption, D4, D8, etc.)"
  type        = string
  default     = "Consumption"
}

# Storage
variable "storage_replication" {
  description = "Storage account replication type"
  type        = string
  default     = "GRS"
}

# ACR
variable "acr_name" {
  description = "Azure Container Registry name (globally unique)"
  type        = string
  default     = "thewatchacr"
}
