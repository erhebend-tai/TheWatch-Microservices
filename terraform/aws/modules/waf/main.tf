# WAF v2 — web application firewall for TheWatch ALB/API Gateway
# Rate limiting, managed rule groups, geo-restriction, and XSS protection

resource "aws_wafv2_web_acl" "main" {
  name        = "${var.project_name}-waf-${var.environment}"
  description = "WAF WebACL for TheWatch ${var.environment} environment"
  scope       = "REGIONAL"

  default_action {
    allow {}
  }

  # ── Rule 1: Rate-based — 2,000 requests per 5 minutes per IP ──

  rule {
    name     = "${var.project_name}-rate-limit"
    priority = 1

    action {
      block {}
    }

    statement {
      rate_based_statement {
        limit              = 2000
        aggregate_key_type = "IP"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project_name}-rate-limit-${var.environment}"
      sampled_requests_enabled   = true
    }
  }

  # ── Rule 2: Geo-restriction — block OFAC-sanctioned countries ──

  rule {
    name     = "${var.project_name}-geo-block"
    priority = 2

    action {
      block {}
    }

    statement {
      geo_match_statement {
        country_codes = ["CU", "IR", "KP", "SY", "SD", "RU", "BY"]
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project_name}-geo-block-${var.environment}"
      sampled_requests_enabled   = true
    }
  }

  # ── Rule 3: AWS Managed — Common Rule Set ──

  rule {
    name     = "${var.project_name}-aws-common"
    priority = 3

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        vendor_name = "AWS"
        name        = "AWSManagedRulesCommonRuleSet"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project_name}-aws-common-${var.environment}"
      sampled_requests_enabled   = true
    }
  }

  # ── Rule 4: AWS Managed — SQL Injection Rule Set ──

  rule {
    name     = "${var.project_name}-aws-sqli"
    priority = 4

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        vendor_name = "AWS"
        name        = "AWSManagedRulesSQLiRuleSet"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project_name}-aws-sqli-${var.environment}"
      sampled_requests_enabled   = true
    }
  }

  # ── Rule 5: AWS Managed — Known Bad Inputs Rule Set ──

  rule {
    name     = "${var.project_name}-aws-bad-inputs"
    priority = 5

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        vendor_name = "AWS"
        name        = "AWSManagedRulesKnownBadInputsRuleSet"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project_name}-aws-bad-inputs-${var.environment}"
      sampled_requests_enabled   = true
    }
  }

  # ── Rule 6: AWS Managed — IP Reputation List ──

  rule {
    name     = "${var.project_name}-aws-ip-reputation"
    priority = 6

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        vendor_name = "AWS"
        name        = "AWSManagedRulesAmazonIpReputationList"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project_name}-aws-ip-reputation-${var.environment}"
      sampled_requests_enabled   = true
    }
  }

  # ── Rule 7: Custom XSS detection ──

  rule {
    name     = "${var.project_name}-custom-xss"
    priority = 7

    action {
      block {}
    }

    statement {
      or_statement {
        statement {
          xss_match_statement {
            field_to_match {
              body {
                oversize_handling = "CONTINUE"
              }
            }
            text_transformation {
              priority = 1
              type     = "URL_DECODE"
            }
            text_transformation {
              priority = 2
              type     = "HTML_ENTITY_DECODE"
            }
          }
        }
        statement {
          xss_match_statement {
            field_to_match {
              query_string {}
            }
            text_transformation {
              priority = 1
              type     = "URL_DECODE"
            }
            text_transformation {
              priority = 2
              type     = "HTML_ENTITY_DECODE"
            }
          }
        }
        statement {
          xss_match_statement {
            field_to_match {
              uri_path {}
            }
            text_transformation {
              priority = 1
              type     = "URL_DECODE"
            }
            text_transformation {
              priority = 2
              type     = "HTML_ENTITY_DECODE"
            }
          }
        }
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.project_name}-custom-xss-${var.environment}"
      sampled_requests_enabled   = true
    }
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "${var.project_name}-waf-${var.environment}"
    sampled_requests_enabled   = true
  }

  tags = {
    Name        = "${var.project_name}-waf-${var.environment}"
    Environment = var.environment
  }
}

# ─────────────────────────────────────────────────────────────
# Associate WAF with ALB
# ─────────────────────────────────────────────────────────────

resource "aws_wafv2_web_acl_association" "alb" {
  resource_arn = var.alb_arn
  web_acl_arn  = aws_wafv2_web_acl.main.arn
}

# ─────────────────────────────────────────────────────────────
# WAF Logging Configuration
# ─────────────────────────────────────────────────────────────

resource "aws_wafv2_web_acl_logging_configuration" "main" {
  log_destination_configs = [var.log_destination_arn]
  resource_arn            = aws_wafv2_web_acl.main.arn

  logging_filter {
    default_behavior = "KEEP"

    filter {
      behavior    = "KEEP"
      requirement = "MEETS_ANY"

      condition {
        action_condition {
          action = "BLOCK"
        }
      }

      condition {
        action_condition {
          action = "COUNT"
        }
      }
    }
  }
}
