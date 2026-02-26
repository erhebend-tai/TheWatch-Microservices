variable "environment" {
  description = "The deployment environment"
  type        = string
}

variable "project_name" {
  description = "The name of the project"
  type        = string
}

variable "subnet_ids" {
  description = "List of private subnet IDs for Aurora"
  type        = list(string)
}

variable "vpc_security_group_ids" {
  description = "List of security group IDs for Aurora"
  type        = list(string)
}

variable "db_name" {
  description = "Database name"
  type        = string
}

variable "master_username" {
  description = "Master username"
  type        = string
}

variable "master_password" {
  description = "Master password"
  type        = string
  sensitive   = true
}

variable "instance_class" {
  description = "DB instance class for Aurora instances"
  type        = string
  default     = "db.r6g.large"
}

variable "replica_count" {
  description = "Number of reader replicas"
  type        = number
  default     = 2
}
