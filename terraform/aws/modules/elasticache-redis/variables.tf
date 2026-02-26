variable "environment" {
  description = "The deployment environment"
  type        = string
}

variable "project_name" {
  description = "The name of the project"
  type        = string
}

variable "subnet_ids" {
  description = "List of private subnet IDs for ElastiCache"
  type        = list(string)
}

variable "vpc_id" {
  description = "VPC ID for security group"
  type        = string
}

variable "node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.r6g.large"
}

variable "num_node_groups" {
  description = "Number of shards"
  type        = number
  default     = 3
}

variable "replicas_per_node_group" {
  description = "Replicas per shard"
  type        = number
  default     = 2
}

variable "kms_key_id" {
  description = "KMS key ID for encryption at rest"
  type        = string
  default     = null
}
