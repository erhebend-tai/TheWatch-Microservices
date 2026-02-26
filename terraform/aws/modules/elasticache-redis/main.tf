# ElastiCache Redis — session store, rate limiting, and real-time caching
# AWS equivalent of GCP Memorystore / Azure Redis Cache
# Cluster mode enabled: 3 shards x 2 replicas for high availability

# ------------------------------------------------------------------------------
# Parameter Group
# ------------------------------------------------------------------------------
resource "aws_elasticache_parameter_group" "redis" {
  name   = "${var.project_name}-redis-params-${var.environment}"
  family = "redis7"

  parameter {
    name  = "maxmemory-policy"
    value = "allkeys-lru"
  }

  parameter {
    name  = "cluster-enabled"
    value = "yes"
  }

  tags = {
    Name        = "${var.project_name}-redis-params-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# Subnet Group
# ------------------------------------------------------------------------------
resource "aws_elasticache_subnet_group" "redis" {
  name       = "${var.project_name}-redis-subnet-${var.environment}"
  subnet_ids = var.private_subnet_ids

  tags = {
    Name        = "${var.project_name}-redis-subnet-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# Security Group
# ------------------------------------------------------------------------------
resource "aws_security_group" "redis" {
  name        = "${var.project_name}-redis-sg-${var.environment}"
  description = "Security group for TheWatch ElastiCache Redis cluster"
  vpc_id      = var.vpc_id

  ingress {
    description = "Redis from VPC"
    from_port   = 6379
    to_port     = 6379
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
  }

  egress {
    description = "Allow all outbound"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-redis-sg-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# Replication Group (Cluster Mode Enabled)
# ------------------------------------------------------------------------------
resource "aws_elasticache_replication_group" "redis" {
  replication_group_id = "${var.project_name}-redis-${var.environment}"
  description          = "TheWatch Redis cluster for ${var.environment}"

  engine         = "redis"
  engine_version = "7.1"
  node_type      = var.node_type
  port           = 6379

  # Cluster mode: 3 shards x 2 replicas per shard
  num_node_groups         = 3
  replicas_per_node_group = 2

  parameter_group_name = aws_elasticache_parameter_group.redis.name
  subnet_group_name    = aws_elasticache_subnet_group.redis.name
  security_group_ids   = [aws_security_group.redis.id]

  # Encryption
  at_rest_encryption_enabled = true
  kms_key_id                 = var.kms_key_arn
  transit_encryption_enabled = true

  # Maintenance
  automatic_failover_enabled = true
  multi_az_enabled           = true
  maintenance_window         = "sun:03:00-sun:05:00"
  snapshot_retention_limit   = 7
  snapshot_window            = "01:00-02:00"

  tags = {
    Name        = "${var.project_name}-redis-${var.environment}"
    Environment = var.environment
  }
}
