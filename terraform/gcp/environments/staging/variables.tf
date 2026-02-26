variable "project_id" {
  description = "GCP project ID"
  type        = string
}

variable "project" {
  description = "Project name prefix"
  type        = string
  default     = "thewatch"
}

variable "region" {
  description = "GCP region"
  type        = string
  default     = "us-central1"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "staging"
}

variable "labels" {
  description = "Additional labels to apply to resources"
  type        = map(string)
  default     = {}
}
