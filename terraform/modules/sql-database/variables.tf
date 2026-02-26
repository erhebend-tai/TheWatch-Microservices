variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "location_secondary" {
  type = string
}

variable "resource_prefix" {
  type = string
}

variable "tags" {
  type = map(string)
}

variable "admin_username" {
  type = string
}

variable "admin_password" {
  type      = string
  sensitive = true
}

variable "databases" {
  description = "Map of database name to config (service, tier)"
  type = map(object({
    service = string
    tier    = string
  }))
}

variable "sku_name" {
  description = "SKU for standard-tier databases"
  type        = string
  default     = "GP_S_Gen5_1"
}

variable "sku_name_critical" {
  description = "SKU for critical-tier databases (P2, P5, P6)"
  type        = string
  default     = "GP_S_Gen5_2"
}

variable "max_size_gb" {
  type    = number
  default = 32
}

variable "enable_geo_replication" {
  type    = bool
  default = false
}

variable "key_vault_id" {
  type = string
}
