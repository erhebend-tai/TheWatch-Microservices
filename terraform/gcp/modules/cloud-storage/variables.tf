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

variable "storage_class" {
  type    = string
  default = "STANDARD"
}

variable "versioning_enabled" {
  type    = bool
  default = true
}

variable "lifecycle_age_days" {
  type    = number
  default = 90
}

variable "archive_age_days" {
  type    = number
  default = 365
}

variable "cors_origins" {
  type    = list(string)
  default = ["*"]
}

variable "admin_member" {
  type = string
}
