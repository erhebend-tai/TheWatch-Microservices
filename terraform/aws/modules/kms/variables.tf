variable "environment" {
  description = "Deployment environment (dev, staging, production)"
  type        = string
}

variable "project_name" {
  description = "Project name used in resource naming"
  type        = string
  default     = "TheWatch"
}

variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "admin_role_arns" {
  description = "List of IAM role ARNs that can administer KMS keys"
  type        = list(string)
}

variable "service_role_arns" {
  description = "List of IAM role ARNs that can use keys for encrypt/decrypt"
  type        = list(string)
}
