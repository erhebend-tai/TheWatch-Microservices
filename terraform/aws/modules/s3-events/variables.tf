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
  description = "AWS region for resource deployment"
  type        = string
  default     = "us-east-1"
}

variable "evidence_bucket_id" {
  description = "Name/ID of the evidence S3 bucket to attach notifications to"
  type        = string
}

variable "evidence_bucket_arn" {
  description = "ARN of the evidence S3 bucket for IAM policies"
  type        = string
}

variable "dynamodb_table_name" {
  description = "Name of the DynamoDB audit log table for writing evidence metadata"
  type        = string
}

variable "kms_key_arn" {
  description = "ARN of the KMS key for encrypting SQS messages and Lambda environment"
  type        = string
}
