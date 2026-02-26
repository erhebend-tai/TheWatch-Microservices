output "bootstrap_brokers_iam" {
  description = "IAM-authenticated bootstrap broker connection string"
  value       = aws_msk_cluster.main.bootstrap_brokers_sasl_iam
}

output "bootstrap_brokers_tls" {
  description = "TLS bootstrap broker connection string"
  value       = aws_msk_cluster.main.bootstrap_brokers_tls
}

output "zookeeper_connect" {
  description = "Zookeeper connection string"
  value       = aws_msk_cluster.main.zookeeper_connect_string
}

output "cluster_arn" {
  description = "ARN of the MSK cluster"
  value       = aws_msk_cluster.main.arn
}

output "security_group_id" {
  description = "Security group ID attached to the MSK cluster"
  value       = aws_security_group.msk.id
}
