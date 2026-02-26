variable "environment" {
  description = "Deployment environment (dev, staging, production)"
  type        = string
}

variable "project_name" {
  description = "Project name used in resource naming"
  type        = string
  default     = "TheWatch"
}

variable "lambda_function_arns" {
  description = "Map of function purpose to Lambda ARN (validate_caller, dispatch_responder, monitor_resolution, generate_report)"
  type        = map(string)
}

variable "ecs_cluster_arn" {
  description = "ARN of the ECS cluster for running evidence collection tasks"
  type        = string
}
