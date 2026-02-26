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

variable "alb_arn" {
  description = "ARN of the Application Load Balancer to associate the WAF with"
  type        = string
}

variable "log_destination_arn" {
  description = "ARN of the log destination (CloudWatch Log Group, S3 bucket, or Kinesis Firehose)"
  type        = string
}
