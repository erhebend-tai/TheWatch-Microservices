output "evidence_bucket_id" {
  description = "Name/ID of the evidence S3 bucket"
  value       = aws_s3_bucket.evidence.id
}

output "evidence_bucket_arn" {
  description = "ARN of the evidence S3 bucket"
  value       = aws_s3_bucket.evidence.arn
}

output "backups_bucket_id" {
  description = "Name/ID of the backups S3 bucket"
  value       = aws_s3_bucket.backups.id
}

output "static_bucket_id" {
  description = "Name/ID of the static assets S3 bucket"
  value       = aws_s3_bucket.static.id
}

output "static_bucket_domain_name" {
  description = "Regional domain name of the static bucket (for CloudFront origin)"
  value       = aws_s3_bucket.static.bucket_regional_domain_name
}
