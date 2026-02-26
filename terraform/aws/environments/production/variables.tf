# ──────────────────────────────────────────────────────────────
# TheWatch AWS — Production Variables
# ──────────────────────────────────────────────────────────────

variable "aws_region" {
  description = "Primary AWS region for all production resources"
  type        = string
  default     = "us-east-1"
}

variable "dr_region" {
  description = "Disaster recovery region for cross-region replication and backup"
  type        = string
  default     = "us-west-2"
}

variable "environment" {
  description = "Deployment environment identifier"
  type        = string
  default     = "production"
}

variable "project_name" {
  description = "Project name used as prefix for all resource naming"
  type        = string
  default     = "TheWatch"
}

variable "vpc_cidr" {
  description = "CIDR block for the production VPC"
  type        = string
  default     = "10.2.0.0/16"
}

variable "rds_instance_class" {
  description = "Instance class for RDS SQL Server and Aurora PostgreSQL"
  type        = string
  default     = "db.r6i.xlarge"
}

variable "redis_node_type" {
  description = "Node type for ElastiCache Redis cluster"
  type        = string
  default     = "cache.r6g.xlarge"
}

variable "msk_instance_type" {
  description = "Broker instance type for Amazon MSK (Kafka)"
  type        = string
  default     = "kafka.m5.2xlarge"
}

variable "alert_email" {
  description = "Email address for production security and operational alerts"
  type        = string
}

variable "github_repo" {
  description = "GitHub repository in owner/repo format for CI/CD pipeline"
  type        = string
}

variable "github_branch" {
  description = "GitHub branch to track for production deployments"
  type        = string
  default     = "main"
}

variable "callback_urls" {
  description = "Allowed OAuth callback URLs for Cognito authentication flows"
  type        = list(string)
  default     = ["https://app.thewatch.app/callback"]
}

variable "logout_urls" {
  description = "Allowed OAuth logout URLs for Cognito authentication flows"
  type        = list(string)
  default     = ["https://app.thewatch.app/logout"]
}

variable "account_ids" {
  description = "Map of environment name to AWS account ID for multi-account governance"
  type        = map(string)

  validation {
    condition     = alltrue([for k in ["dev", "staging", "prod"] : contains(keys(var.account_ids), k)])
    error_message = "account_ids must contain keys: dev, staging, prod."
  }
}
