output "log_group_arns" {
  description = "Map of service name to CloudWatch Log Group ARN"
  value       = { for k, v in aws_cloudwatch_log_group.service_logs : k => v.arn }
}

output "dashboard_arn" {
  description = "ARN of the CloudWatch dashboard"
  value       = aws_cloudwatch_dashboard.main.dashboard_arn
}

output "canary_arn" {
  description = "ARN of the Synthetics canary for health monitoring"
  value       = aws_synthetics_canary.health_check.arn
}
