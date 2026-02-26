# AWS Backup — automated backup plans for TheWatch data stores
# Vault with lock, daily/weekly/monthly backup rules, tag-based resource selection

# ---------------------------------------------------------------------------
# Backup Vault — encrypted with vault lock
# ---------------------------------------------------------------------------
resource "aws_backup_vault" "main" {
  name        = "${var.project_name}-vault-${var.environment}"
  kms_key_arn = var.kms_key_arn

  tags = {
    Name        = "${var.project_name}-backup-vault-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_backup_vault_lock_configuration" "main" {
  backup_vault_name   = aws_backup_vault.main.name
  changeable_for_days = 3
  max_retention_days  = 1825
  min_retention_days  = 365
}

# ---------------------------------------------------------------------------
# Backup Plan — daily RDS, weekly DynamoDB, monthly S3 cross-region
# ---------------------------------------------------------------------------
resource "aws_backup_plan" "main" {
  name = "${var.project_name}-backup-plan-${var.environment}"

  # Daily RDS snapshots — 35-day retention, 120min completion window
  rule {
    rule_name         = "${var.project_name}-daily-rds-${var.environment}"
    target_vault_name = aws_backup_vault.main.name
    schedule          = "cron(0 3 * * ? *)"
    start_window      = 60
    completion_window = 120

    lifecycle {
      delete_after = 35
    }
  }

  # Weekly DynamoDB backups — 90-day retention
  rule {
    rule_name         = "${var.project_name}-weekly-dynamodb-${var.environment}"
    target_vault_name = aws_backup_vault.main.name
    schedule          = "cron(0 4 ? * SUN *)"
    start_window      = 60
    completion_window = 180

    lifecycle {
      delete_after = 90
    }
  }

  # Monthly S3 cross-region copy to DR region
  rule {
    rule_name         = "${var.project_name}-monthly-s3-xregion-${var.environment}"
    target_vault_name = aws_backup_vault.main.name
    schedule          = "cron(0 5 1 * ? *)"
    start_window      = 120
    completion_window = 360

    lifecycle {
      delete_after = 365
    }

    copy_action {
      destination_vault_arn = aws_backup_vault.dr.arn

      lifecycle {
        delete_after = 365
      }
    }
  }

  tags = {
    Name        = "${var.project_name}-backup-plan-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# DR Vault — cross-region destination for monthly copies
# ---------------------------------------------------------------------------
resource "aws_backup_vault" "dr" {
  provider    = aws.dr
  name        = "${var.project_name}-vault-dr-${var.environment}"
  kms_key_arn = var.kms_key_arn

  tags = {
    Name        = "${var.project_name}-backup-vault-dr-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Backup Selection — tag-based resource selection
# ---------------------------------------------------------------------------
resource "aws_backup_selection" "rds_dynamodb" {
  name         = "${var.project_name}-backup-selection-${var.environment}"
  iam_role_arn = aws_iam_role.backup_role.arn
  plan_id      = aws_backup_plan.main.id

  selection_tag {
    type  = "STRINGEQUALS"
    key   = "Backup"
    value = "true"
  }
}

# ---------------------------------------------------------------------------
# IAM Role — AWS Backup service role
# ---------------------------------------------------------------------------
resource "aws_iam_role" "backup_role" {
  name = "${var.project_name}-backup-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "backup.amazonaws.com"
      }
    }]
  })

  tags = {
    Name        = "${var.project_name}-backup-role-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy_attachment" "backup_policy" {
  role       = aws_iam_role.backup_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSBackupServiceRolePolicyForBackup"
}

resource "aws_iam_role_policy_attachment" "restore_policy" {
  role       = aws_iam_role.backup_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSBackupServiceRolePolicyForRestores"
}
