output "table_name" {
  description = "Name of the DynamoDB audit log table"
  value       = aws_dynamodb_table.audit_log.name
}

output "table_arn" {
  description = "ARN of the DynamoDB audit log table"
  value       = aws_dynamodb_table.audit_log.arn
}

output "stream_arn" {
  description = "ARN of the DynamoDB Stream for change data capture"
  value       = aws_dynamodb_table.audit_log.stream_arn
}
