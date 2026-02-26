output "data_key_arn" {
  description = "ARN of the data encryption key (RDS, ElastiCache, S3)"
  value       = aws_kms_key.keys["data-key"].arn
}

output "data_key_id" {
  description = "ID of the data encryption key"
  value       = aws_kms_key.keys["data-key"].key_id
}

output "jwt_key_arn" {
  description = "ARN of the JWT signing key"
  value       = aws_kms_key.keys["jwt-key"].arn
}

output "jwt_key_id" {
  description = "ID of the JWT signing key"
  value       = aws_kms_key.keys["jwt-key"].key_id
}

output "evidence_key_arn" {
  description = "ARN of the evidence bucket encryption key"
  value       = aws_kms_key.keys["evidence-key"].arn
}

output "evidence_key_id" {
  description = "ID of the evidence bucket encryption key"
  value       = aws_kms_key.keys["evidence-key"].key_id
}
