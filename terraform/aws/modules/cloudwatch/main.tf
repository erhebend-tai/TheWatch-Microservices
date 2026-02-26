# CloudWatch Monitoring — observability for TheWatch microservices
# Amazon CloudWatch log groups, metric filters, alarms, dashboard, and synthetics

# ---------------------------------------------------------------------------
# Log Groups — one per service
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_log_group" "service_logs" {
  for_each = toset(var.services)

  name              = "/ecs/thewatch/${each.key}-${var.environment}"
  retention_in_days = 90

  tags = {
    Name        = "${var.project_name}-logs-${each.key}-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Metric Filters — ErrorRate and HighLatency per service
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_log_metric_filter" "error_rate" {
  for_each = toset(var.services)

  name           = "${var.project_name}-${each.key}-error-rate-${var.environment}"
  log_group_name = aws_cloudwatch_log_group.service_logs[each.key].name
  pattern        = "ERROR"

  metric_transformation {
    name          = "${each.key}-ErrorCount"
    namespace     = "${var.project_name}/${var.environment}"
    value         = "1"
    default_value = "0"
  }
}

resource "aws_cloudwatch_log_metric_filter" "high_latency" {
  for_each = toset(var.services)

  name           = "${var.project_name}-${each.key}-high-latency-${var.environment}"
  log_group_name = aws_cloudwatch_log_group.service_logs[each.key].name
  pattern        = "ElapsedMilliseconds > 5000"

  metric_transformation {
    name          = "${each.key}-HighLatencyCount"
    namespace     = "${var.project_name}/${var.environment}"
    value         = "1"
    default_value = "0"
  }
}

# ---------------------------------------------------------------------------
# Alarms — ErrorRate > 10 in 5 min per service
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_metric_alarm" "error_rate_alarm" {
  for_each = toset(var.services)

  alarm_name          = "${var.project_name}-${each.key}-error-rate-${var.environment}"
  alarm_description   = "Error rate exceeded threshold for ${each.key} in ${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "${each.key}-ErrorCount"
  namespace           = "${var.project_name}/${var.environment}"
  period              = 300
  statistic           = "Sum"
  threshold           = 10
  treat_missing_data  = "notBreaching"

  alarm_actions = [var.alarm_sns_topic_arn]
  ok_actions    = [var.alarm_sns_topic_arn]

  tags = {
    Name        = "${var.project_name}-${each.key}-error-alarm-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Composite Alarm — CPUUtilization > 80% across services
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_metric_alarm" "cpu_high" {
  for_each = toset(var.services)

  alarm_name          = "${var.project_name}-${each.key}-cpu-high-${var.environment}"
  alarm_description   = "CPU utilization exceeded 80% for ${each.key} in ${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/ECS"
  period              = 300
  statistic           = "Average"
  threshold           = 80
  treat_missing_data  = "notBreaching"

  dimensions = {
    ServiceName = each.key
  }

  alarm_actions = [var.alarm_sns_topic_arn]

  tags = {
    Name        = "${var.project_name}-${each.key}-cpu-alarm-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_cloudwatch_composite_alarm" "cpu_composite" {
  alarm_name        = "${var.project_name}-cpu-composite-${var.environment}"
  alarm_description = "Composite alarm: CPU utilization high across TheWatch services"

  alarm_rule = join(" OR ", [
    for svc in var.services : "ALARM(${aws_cloudwatch_metric_alarm.cpu_high[svc].alarm_name})"
  ])

  alarm_actions = [var.alarm_sns_topic_arn]

  tags = {
    Name        = "${var.project_name}-cpu-composite-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Dashboard — widgets for all 12 services
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_dashboard" "main" {
  dashboard_name = "${var.project_name}-services-${var.environment}"

  dashboard_body = jsonencode({
    widgets = concat(
      # Error rate widgets
      [for i, svc in var.services : {
        type   = "metric"
        x      = (i % 4) * 6
        y      = floor(i / 4) * 6
        width  = 6
        height = 6
        properties = {
          title   = "${svc} Error Rate"
          metrics = [["${var.project_name}/${var.environment}", "${svc}-ErrorCount", { stat = "Sum", period = 300 }]]
          view    = "timeSeries"
          region  = var.aws_region
        }
      }],
      # Latency widgets
      [for i, svc in var.services : {
        type   = "metric"
        x      = (i % 4) * 6
        y      = (floor(i / 4) * 6) + (ceil(length(var.services) / 4) * 6)
        width  = 6
        height = 6
        properties = {
          title   = "${svc} High Latency"
          metrics = [["${var.project_name}/${var.environment}", "${svc}-HighLatencyCount", { stat = "Sum", period = 300 }]]
          view    = "timeSeries"
          region  = var.aws_region
        }
      }],
      # Request count widgets (ECS RequestCount)
      [for i, svc in var.services : {
        type   = "metric"
        x      = (i % 4) * 6
        y      = (floor(i / 4) * 6) + (2 * ceil(length(var.services) / 4) * 6)
        width  = 6
        height = 6
        properties = {
          title   = "${svc} Request Count"
          metrics = [["AWS/ApplicationELB", "RequestCount", "TargetGroup", svc, { stat = "Sum", period = 300 }]]
          view    = "timeSeries"
          region  = var.aws_region
        }
      }]
    )
  })
}

# ---------------------------------------------------------------------------
# Synthetics Canary — health endpoint monitoring every 5 minutes
# ---------------------------------------------------------------------------
resource "aws_synthetics_canary" "health_check" {
  name                 = "${lower(var.project_name)}-health-${var.environment}"
  artifact_s3_location = "s3://${aws_s3_bucket.canary_artifacts.id}/canary/"
  execution_role_arn   = aws_iam_role.canary_role.arn
  handler              = "apiCanaryBlueprint.handler"
  runtime_version      = "syn-nodejs-puppeteer-9.0"
  start_canary         = true

  schedule {
    expression = "rate(5 minutes)"
  }

  run_config {
    timeout_in_seconds = 60
  }

  tags = {
    Name        = "${var.project_name}-health-canary-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_s3_bucket" "canary_artifacts" {
  bucket        = "${lower(var.project_name)}-canary-artifacts-${var.environment}"
  force_destroy = true

  tags = {
    Name        = "${var.project_name}-canary-artifacts-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "canary_artifacts_lifecycle" {
  bucket = aws_s3_bucket.canary_artifacts.id

  rule {
    id     = "expire-old-artifacts"
    status = "Enabled"

    expiration {
      days = 30
    }
  }
}

resource "aws_iam_role" "canary_role" {
  name = "${var.project_name}-canary-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "lambda.amazonaws.com"
      }
    }]
  })

  tags = {
    Name        = "${var.project_name}-canary-role-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy_attachment" "canary_cloudwatch" {
  role       = aws_iam_role.canary_role.name
  policy_arn = "arn:aws:iam::aws:policy/CloudWatchSyntheticsFullAccess"
}

resource "aws_iam_role_policy" "canary_s3" {
  name = "${var.project_name}-canary-s3-${var.environment}"
  role = aws_iam_role.canary_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "s3:PutObject",
        "s3:GetObject",
        "s3:GetBucketLocation"
      ]
      Resource = [
        aws_s3_bucket.canary_artifacts.arn,
        "${aws_s3_bucket.canary_artifacts.arn}/*"
      ]
    }]
  })
}
