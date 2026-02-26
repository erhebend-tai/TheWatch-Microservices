variable "environment" {
  description = "The deployment environment"
  type        = string
}

variable "project_name" {
  description = "The name of the project"
  type        = string
}

variable "alb_dns_name" {
  description = "The DNS name of the ALB to route traffic to"
  type        = string
}

variable "cognito_user_pool_id" {
  description = "The Cognito User Pool ID for JWT authorizer"
  type        = string
  default     = null
}

variable "cognito_client_id" {
  description = "The Cognito Client ID for JWT authorizer"
  type        = string
  default     = null
}
