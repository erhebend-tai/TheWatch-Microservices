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

variable "vpc_network_id" {
  type = string
}

variable "admin_username" {
  type    = string
  default = "postgres"
}

variable "admin_password" {
  type      = string
  sensitive = true
}

variable "cpu_count" {
  type    = number
  default = 2
}

variable "read_replicas" {
  type    = number
  default = 0
}

variable "backup_retention_count" {
  type    = number
  default = 7
}
