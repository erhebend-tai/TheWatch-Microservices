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
  description = "Map of event name to Lambda function ARN for event processing"
  type        = map(string)
}

variable "sns_topic_arn" {
  description = "ARN of the SNS topic for event notifications"
  type        = string
}

variable "step_function_arn" {
  description = "ARN of the Step Functions state machine for orchestration workflows"
  type        = string
}

variable "log_group_arn" {
  description = "ARN of the CloudWatch Log Group for event logging"
  type        = string
}
