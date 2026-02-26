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

variable "artifact_registry_url" {
  type = string
}

variable "service_account_email" {
  type = string
}

variable "services" {
  type = map(object({
    cpu           = string
    memory        = string
    min_instances = number
    max_instances = number
    image_tag     = string
    env_vars      = map(string)
    public        = bool
  }))
}
