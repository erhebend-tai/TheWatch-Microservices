# Amazon MSK — Managed Streaming for Apache Kafka
# Event-driven messaging backbone for TheWatch microservices
# AWS equivalent of GCP Pub/Sub / Azure Event Hubs

# ------------------------------------------------------------------------------
# MSK Configuration
# ------------------------------------------------------------------------------
resource "aws_msk_configuration" "main" {
  name              = "${var.project_name}-msk-config-${var.environment}"
  kafka_versions    = ["3.6.0"]
  description       = "TheWatch MSK broker configuration for ${var.environment}"

  server_properties = <<-PROPERTIES
    auto.create.topics.enable=false
    default.replication.factor=3
    min.insync.replicas=2
    num.partitions=6
    log.retention.hours=168
    log.retention.bytes=1073741824
    delete.topic.enable=true
    compression.type=producer
  PROPERTIES
}

# ------------------------------------------------------------------------------
# Security Group
# ------------------------------------------------------------------------------
resource "aws_security_group" "msk" {
  name        = "${var.project_name}-msk-sg-${var.environment}"
  description = "Security group for TheWatch MSK Kafka cluster"
  vpc_id      = var.vpc_id

  ingress {
    description = "Kafka IAM auth from VPC"
    from_port   = 9098
    to_port     = 9098
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
  }

  ingress {
    description = "Kafka TLS from VPC"
    from_port   = 9094
    to_port     = 9094
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
  }

  ingress {
    description = "Kafka SASL/SCRAM from VPC"
    from_port   = 9096
    to_port     = 9096
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
  }

  ingress {
    description = "Zookeeper from VPC"
    from_port   = 2181
    to_port     = 2181
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
    Name        = "${var.project_name}-msk-sg-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# CloudWatch Log Group for MSK Broker Logs
# ------------------------------------------------------------------------------
resource "aws_cloudwatch_log_group" "msk" {
  name              = "/aws/msk/${var.project_name}-${var.environment}"
  retention_in_days = 30

  tags = {
    Name        = "${var.project_name}-msk-logs-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# Secrets Manager Secret for SASL/SCRAM Credentials
# ------------------------------------------------------------------------------
resource "aws_secretsmanager_secret" "msk_scram" {
  name        = "AmazonMSK_${var.project_name}_${var.environment}_scram"
  description = "SASL/SCRAM credentials for TheWatch MSK cluster"
  kms_key_id  = var.kms_key_arn

  tags = {
    Name        = "${var.project_name}-msk-scram-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_secretsmanager_secret_version" "msk_scram" {
  secret_id = aws_secretsmanager_secret.msk_scram.id
  secret_string = jsonencode({
    username = "thewatch-msk-user"
    password = "CHANGE_ME_AFTER_DEPLOY"
  })

  lifecycle {
    ignore_changes = [secret_string]
  }
}

# ------------------------------------------------------------------------------
# MSK Cluster
# ------------------------------------------------------------------------------
resource "aws_msk_cluster" "main" {
  cluster_name           = "${var.project_name}-msk-${var.environment}"
  kafka_version          = "3.6.0"
  number_of_broker_nodes = 3

  configuration_info {
    arn      = aws_msk_configuration.main.arn
    revision = aws_msk_configuration.main.latest_revision
  }

  broker_node_group_info {
    instance_type  = var.instance_type
    client_subnets = var.private_subnet_ids
    security_groups = [aws_security_group.msk.id]

    storage_info {
      ebs_storage_info {
        volume_size = 100

        provisioned_throughput {
          enabled = false
        }
      }
    }
  }

  encryption_info {
    encryption_at_rest_kms_key_arn = var.kms_key_arn

    encryption_in_transit {
      client_broker = "TLS"
      in_cluster    = true
    }
  }

  client_authentication {
    sasl {
      iam   = true
      scram = true
    }
  }

  logging_info {
    broker_logs {
      cloudwatch_logs {
        enabled   = true
        log_group = aws_cloudwatch_log_group.msk.name
      }
    }
  }

  tags = {
    Name        = "${var.project_name}-msk-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# SCRAM Secret Association
# ------------------------------------------------------------------------------
resource "aws_msk_scram_secret_association" "main" {
  cluster_arn     = aws_msk_cluster.main.arn
  secret_arn_list = [aws_secretsmanager_secret.msk_scram.arn]
}
