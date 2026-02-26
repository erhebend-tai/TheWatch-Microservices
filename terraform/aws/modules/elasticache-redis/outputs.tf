output "primary_endpoint" {
  description = "Primary endpoint address for the Redis replication group (cluster mode)"
  value       = aws_elasticache_replication_group.redis.configuration_endpoint_address
}

output "reader_endpoint" {
  description = "Reader endpoint address for read replicas"
  value       = aws_elasticache_replication_group.redis.configuration_endpoint_address
}

output "port" {
  description = "Redis port number"
  value       = aws_elasticache_replication_group.redis.port
}

output "security_group_id" {
  description = "Security group ID attached to the Redis cluster"
  value       = aws_security_group.redis.id
}
