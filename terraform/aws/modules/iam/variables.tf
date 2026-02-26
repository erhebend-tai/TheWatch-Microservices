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

variable "evidence_bucket_arn" {
  description = "ARN of the S3 evidence bucket for scoped PutObject access"
  type        = string
}

variable "log_group_arns" {
  description = "Map of service name to CloudWatch log group ARN"
  type        = map(string)
  default     = {}
}

variable "cognito_user_pool_arn" {
  description = "ARN of the Cognito User Pool for P5 AuthSecurity admin operations"
  type        = string
}
