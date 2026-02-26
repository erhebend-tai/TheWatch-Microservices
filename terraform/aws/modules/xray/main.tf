# X-Ray Distributed Tracing — request tracing for TheWatch microservices
# AWS X-Ray groups and sampling rules for observability

# ---------------------------------------------------------------------------
# X-Ray Groups — one per service for filtered trace views
# ---------------------------------------------------------------------------
resource "aws_xray_group" "service_groups" {
  for_each = toset(var.services)

  group_name        = "${var.project_name}-${each.key}-${var.environment}"
  filter_expression = "service(\"${each.key}\") AND annotation.environment = \"${var.environment}\""

  tags = {
    Name        = "${var.project_name}-xray-group-${each.key}-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Sampling Rules
# ---------------------------------------------------------------------------

# Health check sampling — 1% to reduce noise from frequent health probes
resource "aws_xray_sampling_rule" "health_check" {
  rule_name      = "${var.project_name}-health-check-${var.environment}"
  priority       = 1
  version        = 1
  reservoir_size = 1
  fixed_rate     = 0.01
  url_path       = "/health*"
  host           = "*"
  http_method    = "*"
  service_type   = "*"
  service_name   = "*"
  resource_arn   = "*"

  tags = {
    Name        = "${var.project_name}-xray-health-${var.environment}"
    Environment = var.environment
  }
}

# Error traces — 100% sampling for 5xx responses to capture all failures
resource "aws_xray_sampling_rule" "error_traces" {
  rule_name      = "${var.project_name}-error-traces-${var.environment}"
  priority       = 10
  version        = 1
  reservoir_size = 10
  fixed_rate     = 1.0
  url_path       = "*"
  host           = "*"
  http_method    = "*"
  service_type   = "*"
  service_name   = "*"
  resource_arn   = "*"

  attributes = {
    "http.status_code" = "5*"
  }

  tags = {
    Name        = "${var.project_name}-xray-errors-${var.environment}"
    Environment = var.environment
  }
}

# Normal traffic — 5% sampling for baseline visibility
resource "aws_xray_sampling_rule" "normal_traffic" {
  rule_name      = "${var.project_name}-normal-traffic-${var.environment}"
  priority       = 100
  version        = 1
  reservoir_size = 5
  fixed_rate     = 0.05
  url_path       = "*"
  host           = "*"
  http_method    = "*"
  service_type   = "*"
  service_name   = "*"
  resource_arn   = "*"

  tags = {
    Name        = "${var.project_name}-xray-normal-${var.environment}"
    Environment = var.environment
  }
}
