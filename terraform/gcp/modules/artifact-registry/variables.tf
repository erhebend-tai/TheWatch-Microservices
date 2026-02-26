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

variable "keep_count" {
  type    = number
  default = 10
}

variable "untagged_retention_days" {
  type    = number
  default = 30
}

variable "reader_members" {
  type    = list(string)
  default = []
}

variable "writer_members" {
  type    = list(string)
  default = []
}
