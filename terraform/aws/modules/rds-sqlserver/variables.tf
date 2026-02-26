variable "environment" {
  description = "The deployment environment"
  type        = string
}

variable "project_name" {
  description = "The name of the project"
  type        = string
}

variable "subnet_ids" {
  description = "List of private subnet IDs for RDS"
  type        = list(string)
}

variable "vpc_security_group_ids" {
  description = "List of security group IDs for RDS"
  type        = list(string)
}

variable "instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.r6i.xlarge"
}

variable "db_name" {
  description = "Primary database name"
  type        = string
}

variable "username" {
  description = "Master username"
  type        = string
}

variable "password" {
  description = "Master password"
  type        = string
  sensitive   = true
}

variable "backup_retention" {
  description = "Backup retention in days"
  type        = number
  default     = 35
}
