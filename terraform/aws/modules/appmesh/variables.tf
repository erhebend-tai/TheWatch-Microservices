variable "environment" {
  description = "The deployment environment"
  type        = string
}

variable "project_name" {
  description = "The name of the project"
  type        = string
}

variable "services" {
  description = "Map of services to configure in App Mesh"
  type = map(object({
    dns_name = string
    port     = number
  }))
}
