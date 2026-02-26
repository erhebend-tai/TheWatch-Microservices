# EventBridge — event-driven architecture for TheWatch microservices
# Custom event bus, domain event rules, targets, and event archive

# ---------------------------------------------------------------------------
# Custom Event Bus
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_event_bus" "main" {
  name = "thewatch-events-${var.environment}"

  tags = {
    Name        = "${var.project_name}-event-bus-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Domain Event Rules and Targets
# ---------------------------------------------------------------------------

# Local map of event rules to their targets
locals {
  event_rules = {
    IncidentCreated = {
      description = "Incident created in TheWatch system"
      pattern = {
        source      = ["thewatch.coregateway"]
        detail-type = ["IncidentCreated"]
      }
      targets = ["logs", "lambda", "sns"]
    }
    DispatchRequested = {
      description = "First responder dispatch requested"
      pattern = {
        source      = ["thewatch.firstresponder"]
        detail-type = ["DispatchRequested"]
      }
      targets = ["logs", "lambda"]
    }
    SOSActivated = {
      description = "SOS emergency signal activated"
      pattern = {
        source      = ["thewatch.voiceemergency"]
        detail-type = ["SOSActivated"]
      }
      targets = ["logs", "sns", "sfn"]
    }
    ResponderLocated = {
      description = "Nearest responder located via geospatial service"
      pattern = {
        source      = ["thewatch.geospatial"]
        detail-type = ["ResponderLocated"]
      }
      targets = ["logs"]
    }
    FamilyCheckIn = {
      description = "Family member check-in received"
      pattern = {
        source      = ["thewatch.familyhealth"]
        detail-type = ["FamilyCheckIn"]
      }
      targets = ["logs"]
    }
    VitalAlert = {
      description = "Vital signs alert from wearable device"
      pattern = {
        source      = ["thewatch.wearable"]
        detail-type = ["VitalAlert"]
      }
      targets = ["logs", "sns"]
    }
    DisasterDeclared = {
      description = "Disaster event declared"
      pattern = {
        source      = ["thewatch.disasterrelief"]
        detail-type = ["DisasterDeclared"]
      }
      targets = ["logs", "sns", "sfn"]
    }
    MeshBroadcast = {
      description = "Mesh network broadcast message"
      pattern = {
        source      = ["thewatch.meshnetwork"]
        detail-type = ["MeshBroadcast"]
      }
      targets = ["logs"]
    }
  }
}

resource "aws_cloudwatch_event_rule" "domain_events" {
  for_each = local.event_rules

  name           = "${var.project_name}-${each.key}-${var.environment}"
  description    = each.value.description
  event_bus_name = aws_cloudwatch_event_bus.main.name

  event_pattern = jsonencode(each.value.pattern)

  tags = {
    Name        = "${var.project_name}-rule-${each.key}-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Targets — CloudWatch Logs (all events)
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_event_target" "logs" {
  for_each = local.event_rules

  rule           = aws_cloudwatch_event_rule.domain_events[each.key].name
  event_bus_name = aws_cloudwatch_event_bus.main.name
  target_id      = "${each.key}-logs"
  arn            = var.log_group_arn
}

# ---------------------------------------------------------------------------
# Targets — Lambda (IncidentCreated, DispatchRequested, SOSActivated)
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_event_target" "lambda" {
  for_each = {
    for k, v in local.event_rules : k => v
    if contains(v.targets, "lambda")
  }

  rule           = aws_cloudwatch_event_rule.domain_events[each.key].name
  event_bus_name = aws_cloudwatch_event_bus.main.name
  target_id      = "${each.key}-lambda"
  arn            = var.lambda_function_arns[each.key]
}

# ---------------------------------------------------------------------------
# Targets — SNS (IncidentCreated, SOSActivated, VitalAlert, DisasterDeclared)
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_event_target" "sns" {
  for_each = {
    for k, v in local.event_rules : k => v
    if contains(v.targets, "sns")
  }

  rule           = aws_cloudwatch_event_rule.domain_events[each.key].name
  event_bus_name = aws_cloudwatch_event_bus.main.name
  target_id      = "${each.key}-sns"
  arn            = var.sns_topic_arn
}

# ---------------------------------------------------------------------------
# Targets — Step Functions (SOSActivated, DisasterDeclared)
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_event_target" "sfn" {
  for_each = {
    for k, v in local.event_rules : k => v
    if contains(v.targets, "sfn")
  }

  rule           = aws_cloudwatch_event_rule.domain_events[each.key].name
  event_bus_name = aws_cloudwatch_event_bus.main.name
  target_id      = "${each.key}-sfn"
  arn            = var.step_function_arn
  role_arn       = aws_iam_role.eventbridge_sfn_role.arn
}

# ---------------------------------------------------------------------------
# Event Archive — 90-day replay capability
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_event_archive" "main" {
  name             = "${var.project_name}-event-archive-${var.environment}"
  event_source_arn = aws_cloudwatch_event_bus.main.arn
  retention_days   = 90

  description = "Event archive for TheWatch domain events — 90-day retention for replay"
}

# ---------------------------------------------------------------------------
# IAM Role — EventBridge to Step Functions
# ---------------------------------------------------------------------------
resource "aws_iam_role" "eventbridge_sfn_role" {
  name = "${var.project_name}-eventbridge-sfn-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "events.amazonaws.com"
      }
    }]
  })

  tags = {
    Name        = "${var.project_name}-eventbridge-sfn-role-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy" "eventbridge_sfn_policy" {
  name = "${var.project_name}-eventbridge-sfn-policy-${var.environment}"
  role = aws_iam_role.eventbridge_sfn_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = "states:StartExecution"
      Resource = var.step_function_arn
    }]
  })
}
