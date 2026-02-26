variable "aws_region" {
  description = "The AWS region to deploy to"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "The deployment environment (e.g. dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "project_name" {
  description = "The name of the project"
  type        = string
  default     = "TheWatch"
}
