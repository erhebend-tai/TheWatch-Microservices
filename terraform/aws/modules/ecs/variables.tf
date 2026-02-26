variable "environment" {
  description = "The deployment environment"
  type        = string
}

variable "project_name" {
  description = "The name of the project"
  type        = string
}

variable "vpc_id" {
  description = "The VPC ID where the ECS cluster will be deployed"
  type        = string
}

variable "private_subnet_ids" {
  description = "List of private subnet IDs for ECS tasks"
  type        = list(string)
}

variable "services" {
  description = "Map of services to their configuration"
  type = map(object({
    cpu          = number
    memory       = number
    port         = number
    image        = string
    dns_name     = string
    path_pattern = string
  }))
}
