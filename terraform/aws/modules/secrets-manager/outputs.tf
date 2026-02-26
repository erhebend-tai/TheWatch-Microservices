output "secret_arns" {
  description = "Map of secret name to ARN"
  value       = { for k, v in aws_secretsmanager_secret.secrets : k => v.arn }
}

output "secret_ids" {
  description = "Map of secret name to ID"
  value       = { for k, v in aws_secretsmanager_secret.secrets : k => v.id }
}

output "rotation_lambda_arn" {
  description = "ARN of the rotation Lambda (pass-through)"
  value       = var.rotation_lambda_arn
}
