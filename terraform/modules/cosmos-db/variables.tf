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

variable "max_throughput" {
  description = "Autoscale max RU/s for collections"
  type        = number
  default     = 4000
}

variable "enable_multi_region" {
  description = "Enable multi-region writes (production only)"
  type        = bool
  default     = false
}

variable "key_vault_id" {
  type = string
}
