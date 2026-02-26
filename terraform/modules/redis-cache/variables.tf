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

variable "sku_name" {
  description = "Redis SKU: Basic, Standard, or Premium"
  type        = string
  default     = "Standard"
}

variable "capacity" {
  description = "Redis cache capacity (0-6 for Basic/Standard, 1-5 for Premium)"
  type        = number
  default     = 1
}

variable "key_vault_id" {
  type = string
}
