# ============================================================================
# TheWatch AWS — Staging Environment Variables
# ============================================================================

variable "aws_region" {
  description = "AWS region for all resources"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Deployment environment identifier"
  type        = string
  default     = "staging"
}

variable "project_name" {
  description = "Project name used in resource naming and tagging"
  type        = string
  default     = "TheWatch"
}

variable "vpc_cidr" {
  description = "CIDR block for the staging VPC"
  type        = string
  default     = "10.1.0.0/16"
}

variable "rds_instance_class" {
  description = "Instance class for RDS SQL Server and Aurora PostgreSQL"
  type        = string
  default     = "db.r6i.large"
}

variable "redis_node_type" {
  description = "ElastiCache Redis node instance type"
  type        = string
  default     = "cache.r6g.large"
}

variable "msk_instance_type" {
  description = "MSK (Kafka) broker instance type"
  type        = string
  default     = "kafka.m5.large"
}

variable "alert_email" {
  description = "Email address for GuardDuty and CloudWatch alarm notifications"
  type        = string
}

variable "github_repo" {
  description = "GitHub repository for CI/CD integration and default tagging"
  type        = string
  default     = "TheWatch-Project/Microservices"
}

variable "github_branch" {
  description = "Git branch used by CI/CD pipelines for this environment"
  type        = string
  default     = "develop"
}

variable "callback_urls" {
  description = "Allowed OAuth callback URLs for Cognito app clients"
  type        = list(string)
  default     = ["https://staging.thewatch.app/callback"]
}

variable "logout_urls" {
  description = "Allowed OAuth logout URLs for Cognito app clients"
  type        = list(string)
  default     = ["https://staging.thewatch.app/logout"]
}
