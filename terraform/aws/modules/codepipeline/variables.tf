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

variable "github_repo" {
  description = "GitHub repository in owner/repo format"
  type        = string
}

variable "github_branch" {
  description = "GitHub branch to track for source changes"
  type        = string
  default     = "main"
}

variable "ecr_repository_urls" {
  description = "Map of service name to ECR repository URL"
  type        = map(string)
}

variable "ecs_cluster_name" {
  description = "Name of the ECS cluster for deployments"
  type        = string
}

variable "ecs_service_names" {
  description = "Map of environment (staging, production) to ECS service name"
  type        = map(string)
}
