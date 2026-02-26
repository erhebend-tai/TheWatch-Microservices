# Secrets Manager — centralized secret storage for TheWatch services
# AWS equivalent of GCP Secret Manager / Azure Key Vault

# ─────────────────────────────────────────────────────────────
# Secrets (via for_each)
# ─────────────────────────────────────────────────────────────

resource "aws_secretsmanager_secret" "secrets" {
  for_each = var.secrets

  name        = "${var.project_name}-${each.key}-${var.environment}"
  description = each.value.description
  kms_key_id  = var.kms_key_arn

  recovery_window_in_days = var.environment == "production" ? 30 : 7

  tags = {
    Name        = "${var.project_name}-${each.key}-${var.environment}"
    Environment = var.environment
    SecretType  = each.key
  }
}

resource "aws_secretsmanager_secret_version" "versions" {
  for_each = { for k, v in var.secrets : k => v if v.value != "" }

  secret_id     = aws_secretsmanager_secret.secrets[each.key].id
  secret_string = each.value.value
}

# ─────────────────────────────────────────────────────────────
# Rotation configuration for database credentials (30-day cycle)
# ─────────────────────────────────────────────────────────────

locals {
  # Identify database connection string secrets for rotation
  db_secrets = { for k, v in var.secrets : k => v if can(regex("^db_", k)) }
}

resource "aws_secretsmanager_secret_rotation" "db_rotation" {
  for_each = var.rotation_lambda_arn != "" ? local.db_secrets : {}

  secret_id           = aws_secretsmanager_secret.secrets[each.key].id
  rotation_lambda_arn = var.rotation_lambda_arn

  rotation_rules {
    automatically_after_days = 30
  }
}
