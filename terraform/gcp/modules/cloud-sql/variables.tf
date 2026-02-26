variable "resource_prefix" {
  type = string
}

variable "region" {
  type = string
}

variable "labels" {
  type    = map(string)
  default = {}
}

variable "environment" {
  type = string
}

variable "tier" {
  type    = string
  default = "db-custom-2-8192"
}

variable "disk_size_gb" {
  type    = number
  default = 100
}

variable "high_availability" {
  type    = bool
  default = false
}

variable "vpc_network_id" {
  type = string
}

variable "admin_username" {
  type    = string
  default = "sqladmin"
}

variable "admin_password" {
  type      = string
  sensitive = true
}

variable "databases" {
  type = map(object({
    service = string
    tier    = string
  }))
}

variable "connection_string_secret_id" {
  type = string
}
