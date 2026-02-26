output "sampling_rule_arns" {
  description = "Map of sampling rule names to their ARNs"
  value = {
    health_check   = aws_xray_sampling_rule.health_check.arn
    error_traces   = aws_xray_sampling_rule.error_traces.arn
    normal_traffic = aws_xray_sampling_rule.normal_traffic.arn
  }
}

output "group_arns" {
  description = "Map of service name to X-Ray group ARN"
  value       = { for k, v in aws_xray_group.service_groups : k => v.arn }
}
