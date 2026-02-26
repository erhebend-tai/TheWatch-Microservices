variable "resource_prefix" {
  type = string
}

variable "labels" {
  type    = map(string)
  default = {}
}

variable "secrets" {
  type = map(object({
    initial_value = optional(string)
    accessors     = list(string)
  }))
  default = {}
}
