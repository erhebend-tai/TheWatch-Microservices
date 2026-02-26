# GuardDuty — threat detection and security monitoring for TheWatch
# S3 protection, ECS runtime monitoring, malware protection, and SNS alerting

# ─────────────────────────────────────────────────────────────
# GuardDuty Detector
# ─────────────────────────────────────────────────────────────

resource "aws_guardduty_detector" "main" {
  enable = true

  datasources {
    s3_logs {
      enable = true
    }

    malware_protection {
      scan_ec2_instance_with_findings {
        ebs_volumes {
          enable = true
        }
      }
    }
  }

  tags = {
    Name        = "${var.project_name}-guardduty-${var.environment}"
    Environment = var.environment
  }
}

# ECS Runtime Monitoring feature
resource "aws_guardduty_detector_feature" "ecs_runtime" {
  detector_id = aws_guardduty_detector.main.id
  name        = "RUNTIME_MONITORING"
  status      = "ENABLED"

  additional_configuration {
    name   = "ECS_FARGATE_AGENT_MANAGEMENT"
    status = "ENABLED"
  }
}

# ─────────────────────────────────────────────────────────────
# SNS Topic for Security Alerts
# ─────────────────────────────────────────────────────────────

resource "aws_sns_topic" "security_alerts" {
  name = "${var.project_name}-guardduty-alerts-${var.environment}"

  tags = {
    Name        = "${var.project_name}-guardduty-alerts-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_sns_topic_policy" "security_alerts" {
  arn = aws_sns_topic.security_alerts.arn

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowEventBridgePublish"
        Effect = "Allow"
        Principal = {
          Service = "events.amazonaws.com"
        }
        Action   = "SNS:Publish"
        Resource = aws_sns_topic.security_alerts.arn
      }
    ]
  })
}

resource "aws_sns_topic_subscription" "email" {
  topic_arn = aws_sns_topic.security_alerts.arn
  protocol  = "email"
  endpoint  = var.alert_email
}

# ─────────────────────────────────────────────────────────────
# EventBridge Rule for HIGH and CRITICAL findings
# ─────────────────────────────────────────────────────────────

resource "aws_cloudwatch_event_rule" "guardduty_findings" {
  name        = "${var.project_name}-guardduty-high-critical-${var.environment}"
  description = "Capture GuardDuty HIGH and CRITICAL severity findings"

  event_pattern = jsonencode({
    source      = ["aws.guardduty"]
    detail-type = ["GuardDuty Finding"]
    detail = {
      severity = [
        { numeric = [">=", 7] }
      ]
    }
  })

  tags = {
    Name        = "${var.project_name}-guardduty-high-critical-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_cloudwatch_event_target" "sns" {
  rule      = aws_cloudwatch_event_rule.guardduty_findings.name
  target_id = "${var.project_name}-guardduty-sns-${var.environment}"
  arn       = aws_sns_topic.security_alerts.arn

  input_transformer {
    input_paths = {
      severity    = "$.detail.severity"
      type        = "$.detail.type"
      description = "$.detail.description"
      region      = "$.detail.region"
      account     = "$.detail.accountId"
    }
    input_template = <<-EOF
      "TheWatch GuardDuty Alert [<severity>]: <type> in <region> (Account: <account>). Description: <description>"
    EOF
  }
}
