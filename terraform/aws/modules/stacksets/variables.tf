variable "environment" {
  description = "Deployment environment (dev, staging, production)"
  type        = string
}

variable "project_name" {
  description = "Project name used in resource naming"
  type        = string
  default     = "TheWatch"
}

variable "account_ids" {
  description = "Map of environment name to AWS account ID (dev, staging, prod)"
  type        = map(string)
}

variable "allowed_regions" {
  description = "List of AWS regions allowed by the Service Control Policy"
  type        = list(string)
  default     = ["us-east-1", "us-west-2"]
}
