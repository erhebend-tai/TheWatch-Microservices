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

variable "kms_key_arn" {
  description = "ARN of the KMS key for server-side encryption (evidence and backups buckets)"
  type        = string
}

variable "cloudfront_oai_arn" {
  description = "ARN of the CloudFront Origin Access Identity for the static bucket policy (empty string to allow public read)"
  type        = string
  default     = ""
}
