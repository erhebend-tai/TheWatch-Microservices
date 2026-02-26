output "state_machine_arn" {
  description = "ARN of the incident lifecycle state machine"
  value       = aws_sfn_state_machine.incident_lifecycle.arn
}

output "state_machine_name" {
  description = "Name of the incident lifecycle state machine"
  value       = aws_sfn_state_machine.incident_lifecycle.name
}

output "execution_role_arn" {
  description = "ARN of the IAM role used by the Step Functions state machine"
  value       = aws_iam_role.sfn_role.arn
}
