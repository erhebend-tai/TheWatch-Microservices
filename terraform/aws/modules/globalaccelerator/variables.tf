variable "environment" {
  description = "The deployment environment"
  type        = string
}

variable "project_name" {
  description = "The name of the project"
  type        = string
}

variable "alb_arn" {
  description = "The ARN of the ALB to register with Global Accelerator"
  type        = string
}
