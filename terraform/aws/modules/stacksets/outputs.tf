output "stack_set_id" {
  description = "ID of the CloudFormation StackSet"
  value       = aws_cloudformation_stack_set.multi_account.id
}

output "scp_policy_id" {
  description = "ID of the region restriction Service Control Policy"
  value       = aws_organizations_policy.region_restriction.id
}
