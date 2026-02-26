# ──────────────────────────────────────────────────────────────────────────────
# TheWatch AWS — Development Environment Variables
# ──────────────────────────────────────────────────────────────────────────────

variable "aws_region" {
  description = "AWS region for all resources"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Deployment environment (dev, staging, production)"
  type        = string
  default     = "dev"
}

variable "project_name" {
  description = "Project name used in resource naming and tagging"
  type        = string
  default     = "TheWatch"
}

variable "vpc_cidr" {
  description = "CIDR block for the VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "rds_instance_class" {
  description = "RDS instance class for SQL Server"
  type        = string
  default     = "db.t3.medium"
}

variable "redis_node_type" {
  description = "ElastiCache node type for Redis"
  type        = string
  default     = "cache.t3.medium"
}

variable "msk_instance_type" {
  description = "MSK broker instance type for Kafka"
  type        = string
  default     = "kafka.t3.small"
}

variable "alert_email" {
  description = "Email address to receive GuardDuty and CloudWatch alarm notifications"
  type        = string
}

variable "github_repo" {
  description = "GitHub repository for CI/CD pipelines (org/repo format)"
  type        = string
  default     = ""
}

variable "github_branch" {
  description = "GitHub branch to trigger CI/CD deployments"
  type        = string
  default     = "develop"
}

variable "callback_urls" {
  description = "Allowed OAuth callback URLs for Cognito"
  type        = list(string)
  default     = ["http://localhost:5100/callback"]
}

variable "logout_urls" {
  description = "Allowed logout redirect URLs for Cognito"
  type        = list(string)
  default     = ["http://localhost:5100"]
}
