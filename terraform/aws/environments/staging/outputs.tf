# ============================================================================
# TheWatch AWS — Staging Environment Outputs
# ============================================================================

output "vpc_id" {
  description = "VPC identifier for the staging environment"
  value       = module.vpc.vpc_id
}

output "rds_endpoint" {
  description = "RDS SQL Server primary endpoint"
  value       = module.rds_sqlserver.endpoint
}

output "aurora_endpoint" {
  description = "Aurora PostgreSQL cluster endpoint"
  value       = module.aurora_postgres.endpoint
}

output "redis_endpoint" {
  description = "ElastiCache Redis primary endpoint"
  value       = module.elasticache_redis.primary_endpoint
}

output "msk_brokers" {
  description = "MSK bootstrap broker connection string (IAM auth)"
  value       = module.msk.bootstrap_brokers_iam
}

output "ecr_urls" {
  description = "Map of service name to ECR repository URL"
  value       = module.ecr.repository_urls
}

output "alb_dns" {
  description = "Application Load Balancer DNS name"
  value       = module.alb.dns_name
}

output "cognito_user_pool_id" {
  description = "Cognito User Pool ID for authentication"
  value       = module.cognito.user_pool_id
}

output "s3_evidence_bucket" {
  description = "S3 evidence bucket name"
  value       = module.s3.evidence_bucket_id
}

output "dynamodb_table" {
  description = "DynamoDB audit log table name"
  value       = module.dynamodb.table_name
}

output "kms_data_key_arn" {
  description = "ARN of the KMS data encryption key"
  value       = module.kms.data_key_arn
}

output "waf_acl_arn" {
  description = "ARN of the WAF Web ACL"
  value       = module.waf.web_acl_arn
}

output "step_function_arn" {
  description = "ARN of the Step Functions incident lifecycle state machine"
  value       = module.step_functions.state_machine_arn
}

output "eventbridge_bus_arn" {
  description = "ARN of the custom EventBridge event bus"
  value       = module.eventbridge.event_bus_arn
}
