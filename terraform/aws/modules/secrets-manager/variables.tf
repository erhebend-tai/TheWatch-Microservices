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

variable "secrets" {
  description = "Map of secret name to object with description and value. Keys matching ^db_ get rotation."
  type = map(object({
    description = string
    value       = string
  }))
  default = {}
}

variable "rotation_lambda_arn" {
  description = "ARN of the Lambda function for database credential rotation. Leave empty to skip rotation."
  type        = string
  default     = ""
}

variable "kms_key_arn" {
  description = "ARN of the KMS key used to encrypt secrets"
  type        = string
}
