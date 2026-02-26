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
  description = "AWS region for SMS configuration"
  type        = string
  default     = "us-east-1"
}

variable "ses_email_arn" {
  description = "SES verified email identity ARN for Cognito email sending. Leave empty for Cognito default."
  type        = string
  default     = ""
}

variable "callback_urls" {
  description = "Allowed callback URLs for OAuth flows"
  type        = list(string)
  default     = ["https://localhost/callback"]
}

variable "logout_urls" {
  description = "Allowed logout URLs for OAuth flows"
  type        = list(string)
  default     = ["https://localhost/logout"]
}
