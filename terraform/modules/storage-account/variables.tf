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

variable "replication_type" {
  description = "Storage replication type: LRS, GRS, ZRS, GZRS"
  type        = string
  default     = "GRS"
}

variable "key_vault_id" {
  type = string
}
