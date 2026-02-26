variable "environment" {
  description = "Deployment environment (dev, staging, production)"
  type        = string
}

variable "project_name" {
  description = "Project name used in resource naming"
  type        = string
  default     = "TheWatch"
}

variable "kms_key_arn" {
  description = "ARN of the KMS key for backup vault encryption"
  type        = string
}

variable "dr_region" {
  description = "Disaster recovery region for cross-region backup copies"
  type        = string
  default     = "us-west-2"
}
