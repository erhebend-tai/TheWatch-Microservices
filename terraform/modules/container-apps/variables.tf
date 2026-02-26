variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "resource_prefix" {
  type = string
}

variable "tags" {
  type = map(string)
}

variable "acr_name" {
  description = "Azure Container Registry name"
  type        = string
}

variable "container_apps" {
  description = "Map of app name to resource config"
  type = map(object({
    cpu          = number
    memory       = string
    min_replicas = number
    max_replicas = number
  }))
}

variable "key_vault_id" {
  type = string
}

variable "sql_connection_string" {
  type      = string
  sensitive = true
}

variable "cosmos_connection_string" {
  type      = string
  sensitive = true
}

variable "redis_connection_string" {
  type      = string
  sensitive = true
}

variable "servicebus_connection" {
  type      = string
  sensitive = true
}

variable "storage_connection" {
  type      = string
  sensitive = true
}
