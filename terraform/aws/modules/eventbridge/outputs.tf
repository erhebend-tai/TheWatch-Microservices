output "event_bus_arn" {
  description = "ARN of the custom EventBridge event bus"
  value       = aws_cloudwatch_event_bus.main.arn
}

output "event_bus_name" {
  description = "Name of the custom EventBridge event bus"
  value       = aws_cloudwatch_event_bus.main.name
}

output "rule_arns" {
  description = "Map of event rule name to ARN"
  value       = { for k, v in aws_cloudwatch_event_rule.domain_events : k => v.arn }
}
