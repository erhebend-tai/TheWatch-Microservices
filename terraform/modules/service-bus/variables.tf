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

variable "sku" {
  description = "Service Bus SKU: Basic, Standard, or Premium"
  type        = string
  default     = "Standard"
}

variable "topics" {
  description = "Map of topic name to config (subscriptions list)"
  type = map(object({
    subscriptions = list(string)
  }))
}

variable "key_vault_id" {
  type = string
}
