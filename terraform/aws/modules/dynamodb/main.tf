# Amazon DynamoDB — Audit logging table for TheWatch microservices
# Stores all service events with time-based partitioning and TTL
# AWS equivalent of GCP Bigtable / Azure Cosmos DB (Table API)

# ------------------------------------------------------------------------------
# DynamoDB Table — Audit Log
# ------------------------------------------------------------------------------
resource "aws_dynamodb_table" "audit_log" {
  name         = "${lower(var.project_name)}-audit-log-${var.environment}"
  billing_mode = "PAY_PER_REQUEST"

  # Partition key: "ServiceName#2026-02-26" — groups events by service and day
  hash_key  = "ServiceNameDate"
  # Sort key: "2026-02-26T12:00:00Z#uuid" — ordered by timestamp with unique ID
  range_key = "TimestampEventId"

  attribute {
    name = "ServiceNameDate"
    type = "S"
  }

  attribute {
    name = "TimestampEventId"
    type = "S"
  }

  attribute {
    name = "EventType"
    type = "S"
  }

  # TTL — automatically expire audit records after 365 days
  ttl {
    attribute_name = "ExpiresAt"
    enabled        = true
  }

  # DynamoDB Streams — capture all changes for downstream processing
  stream_enabled   = true
  stream_view_type = "NEW_AND_OLD_IMAGES"

  # GSI: Query audit events by type across all services
  global_secondary_index {
    name            = "EventTypeIndex"
    hash_key        = "EventType"
    range_key       = "TimestampEventId"
    projection_type = "ALL"
  }

  # Point-in-time recovery for disaster protection
  point_in_time_recovery {
    enabled = true
  }

  # Server-side encryption with customer-managed KMS key
  server_side_encryption {
    enabled     = true
    kms_key_arn = var.kms_key_arn
  }

  tags = {
    Name        = "${var.project_name}-audit-log-${var.environment}"
    Environment = var.environment
  }
}
