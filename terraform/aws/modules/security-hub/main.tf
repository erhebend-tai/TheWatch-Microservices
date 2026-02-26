# Security Hub — centralized security posture and compliance for TheWatch
# CIS Benchmarks, AWS Foundational Best Practices, and NIST 800-53 standards

data "aws_region" "current" {}

# ─────────────────────────────────────────────────────────────
# Enable Security Hub
# ─────────────────────────────────────────────────────────────

resource "aws_securityhub_account" "main" {
  enable_default_standards = false
  auto_enable_controls     = true
}

# ─────────────────────────────────────────────────────────────
# Standards Subscriptions
# ─────────────────────────────────────────────────────────────

# CIS AWS Foundations Benchmark v1.4.0
resource "aws_securityhub_standards_subscription" "cis" {
  depends_on    = [aws_securityhub_account.main]
  standards_arn = "arn:aws:securityhub:${data.aws_region.current.name}::standards/cis-aws-foundations-benchmark/v/1.4.0"
}

# AWS Foundational Security Best Practices v1.0.0
resource "aws_securityhub_standards_subscription" "aws_foundational" {
  depends_on    = [aws_securityhub_account.main]
  standards_arn = "arn:aws:securityhub:${data.aws_region.current.name}::standards/aws-foundational-security-best-practices/v/1.0.0"
}

# NIST Special Publication 800-53 Revision 5
resource "aws_securityhub_standards_subscription" "nist_800_53" {
  depends_on    = [aws_securityhub_account.main]
  standards_arn = "arn:aws:securityhub:${data.aws_region.current.name}::standards/nist-800-53/v/5.0.0"
}

# ─────────────────────────────────────────────────────────────
# Action Targets for Automated Remediation
# ─────────────────────────────────────────────────────────────

resource "aws_securityhub_action_target" "quarantine" {
  depends_on  = [aws_securityhub_account.main]
  name        = "${var.project_name}-quarantine-${var.environment}"
  identifier  = "QuarantineResource"
  description = "Quarantine a resource that has a critical or high severity finding"
}

resource "aws_securityhub_action_target" "notify_soc" {
  depends_on  = [aws_securityhub_account.main]
  name        = "${var.project_name}-notify-soc-${var.environment}"
  identifier  = "NotifySOC"
  description = "Send finding details to the TheWatch Security Operations Center"
}

resource "aws_securityhub_action_target" "auto_remediate" {
  depends_on  = [aws_securityhub_account.main]
  name        = "${var.project_name}-auto-remediate-${var.environment}"
  identifier  = "AutoRemediate"
  description = "Trigger automated remediation workflow for supported finding types"
}
