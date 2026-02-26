output "user_pool_id" {
  description = "Cognito User Pool ID"
  value       = aws_cognito_user_pool.main.id
}

output "user_pool_arn" {
  description = "Cognito User Pool ARN"
  value       = aws_cognito_user_pool.main.arn
}

output "client_id" {
  description = "MAUI app client ID"
  value       = aws_cognito_user_pool_client.maui_app.id
}

output "client_secret" {
  description = "Admin dashboard client secret"
  value       = aws_cognito_user_pool_client.admin_dashboard.client_secret
  sensitive   = true
}

output "admin_client_id" {
  description = "Admin dashboard client ID"
  value       = aws_cognito_user_pool_client.admin_dashboard.id
}

output "domain" {
  description = "Cognito hosted UI domain"
  value       = aws_cognito_user_pool_domain.main.domain
}
