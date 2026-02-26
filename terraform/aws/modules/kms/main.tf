# KMS — customer-managed encryption keys for TheWatch
# Data-at-rest encryption for RDS, ElastiCache, S3, JWT signing, and evidence storage

locals {
  keys = {
    "data-key" = {
      description     = "Encryption key for RDS, ElastiCache, and S3 data at rest"
      enable_rotation = true
      rotation_days   = 365
    }
    "jwt-key" = {
      description     = "Key for JWT token signing — application-managed rotation"
      enable_rotation = false
      rotation_days   = null
    }
    "evidence-key" = {
      description     = "SSE key for evidence bucket encryption"
      enable_rotation = true
      rotation_days   = 365
    }
  }
}

data "aws_caller_identity" "current" {}

resource "aws_kms_key" "keys" {
  for_each = local.keys

  description             = each.value.description
  deletion_window_in_days = 30
  enable_key_rotation     = each.value.enable_rotation
  rotation_period_in_days = each.value.rotation_days

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "RootFullAccess"
        Effect = "Allow"
        Principal = {
          AWS = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:root"
        }
        Action   = "kms:*"
        Resource = "*"
      },
      {
        Sid    = "AdminRoleAccess"
        Effect = "Allow"
        Principal = {
          AWS = var.admin_role_arns
        }
        Action = [
          "kms:Create*",
          "kms:Describe*",
          "kms:Enable*",
          "kms:List*",
          "kms:Put*",
          "kms:Update*",
          "kms:Revoke*",
          "kms:Disable*",
          "kms:Get*",
          "kms:Delete*",
          "kms:TagResource",
          "kms:UntagResource",
          "kms:ScheduleKeyDeletion",
          "kms:CancelKeyDeletion",
        ]
        Resource = "*"
      },
      {
        Sid    = "ServiceRoleEncryptDecrypt"
        Effect = "Allow"
        Principal = {
          AWS = var.service_role_arns
        }
        Action = [
          "kms:Encrypt",
          "kms:Decrypt",
          "kms:ReEncrypt*",
          "kms:GenerateDataKey*",
          "kms:DescribeKey",
        ]
        Resource = "*"
      }
    ]
  })

  tags = {
    Name        = "${var.project_name}-${each.key}-${var.environment}"
    Environment = var.environment
    KeyPurpose  = each.key
  }
}

resource "aws_kms_alias" "aliases" {
  for_each = local.keys

  name          = "alias/${var.project_name}-${each.key}-${var.environment}"
  target_key_id = aws_kms_key.keys[each.key].key_id
}
