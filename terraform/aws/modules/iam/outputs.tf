output "task_execution_role_arn" {
  description = "Shared ECS task execution role ARN"
  value       = aws_iam_role.ecs_task_execution.arn
}

output "task_execution_role_name" {
  description = "Shared ECS task execution role name"
  value       = aws_iam_role.ecs_task_execution.name
}

output "service_role_arns" {
  description = "Map of service name to task role ARN"
  value       = { for k, v in aws_iam_role.service_task : k => v.arn }
}

output "service_role_names" {
  description = "Map of service name to task role name"
  value       = { for k, v in aws_iam_role.service_task : k => v.name }
}
