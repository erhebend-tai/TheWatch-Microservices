output "hub_arn" {
  description = "ARN of the Security Hub account"
  value       = aws_securityhub_account.main.arn
}

output "cis_subscription_arn" {
  description = "ARN of the CIS Foundations Benchmark subscription"
  value       = aws_securityhub_standards_subscription.cis.id
}

output "foundational_subscription_arn" {
  description = "ARN of the AWS Foundational Security Best Practices subscription"
  value       = aws_securityhub_standards_subscription.aws_foundational.id
}

output "nist_subscription_arn" {
  description = "ARN of the NIST 800-53 subscription"
  value       = aws_securityhub_standards_subscription.nist_800_53.id
}
