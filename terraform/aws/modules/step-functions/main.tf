# Step Functions — incident lifecycle orchestration for TheWatch
# State machine: ValidateCaller → DispatchResponder → StartEvidenceCollection → MonitorResolution → GenerateReport

# ---------------------------------------------------------------------------
# CloudWatch Log Group — execution logs
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_log_group" "sfn_logs" {
  name              = "/aws/states/${var.project_name}-incident-lifecycle-${var.environment}"
  retention_in_days = 90

  tags = {
    Name        = "${var.project_name}-sfn-logs-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Step Functions State Machine — incident lifecycle
# ---------------------------------------------------------------------------
resource "aws_sfn_state_machine" "incident_lifecycle" {
  name     = "${var.project_name}-incident-lifecycle-${var.environment}"
  role_arn = aws_iam_role.sfn_role.arn

  logging_configuration {
    log_destination        = "${aws_cloudwatch_log_group.sfn_logs.arn}:*"
    include_execution_data = true
    level                  = "ALL"
  }

  definition = jsonencode({
    Comment = "TheWatch Incident Lifecycle Orchestration — validates caller, dispatches responders, collects evidence, monitors resolution, and generates reports"
    StartAt = "ValidateCaller"

    States = {
      # State 1: Validate the incoming caller/incident
      ValidateCaller = {
        Type     = "Task"
        Resource = "arn:aws:states:::lambda:invoke"
        Parameters = {
          FunctionName = var.lambda_function_arns["validate_caller"]
          "Payload.$"  = "$"
        }
        ResultPath    = "$.validation"
        TimeoutSeconds = 30
        Retry = [{
          ErrorEquals     = ["Lambda.ServiceException", "Lambda.AWSLambdaException", "Lambda.SdkClientException"]
          IntervalSeconds = 2
          MaxAttempts     = 3
          BackoffRate     = 2.0
        }]
        Catch = [{
          ErrorEquals = ["States.ALL"]
          ResultPath  = "$.error"
          Next        = "HandleValidationError"
        }]
        Next = "DispatchResponder"
      }

      HandleValidationError = {
        Type  = "Pass"
        Result = {
          status  = "VALIDATION_FAILED"
          message = "Caller validation failed — incident logged for manual review"
        }
        ResultPath = "$.errorInfo"
        Next       = "GenerateReport"
      }

      # State 2: Dispatch the nearest available responder
      DispatchResponder = {
        Type     = "Task"
        Resource = "arn:aws:states:::lambda:invoke"
        Parameters = {
          FunctionName = var.lambda_function_arns["dispatch_responder"]
          "Payload.$"  = "$"
        }
        ResultPath    = "$.dispatch"
        TimeoutSeconds = 60
        Retry = [{
          ErrorEquals     = ["Lambda.ServiceException", "Lambda.AWSLambdaException", "Lambda.SdkClientException"]
          IntervalSeconds = 5
          MaxAttempts     = 3
          BackoffRate     = 2.0
        }]
        Catch = [{
          ErrorEquals = ["States.ALL"]
          ResultPath  = "$.error"
          Next        = "HandleDispatchError"
        }]
        Next = "StartEvidenceCollection"
      }

      HandleDispatchError = {
        Type  = "Pass"
        Result = {
          status  = "DISPATCH_FAILED"
          message = "Responder dispatch failed — escalating to backup dispatch"
        }
        ResultPath = "$.errorInfo"
        Next       = "GenerateReport"
      }

      # State 3: Start evidence collection (photos, audio, location data)
      StartEvidenceCollection = {
        Type     = "Task"
        Resource = "arn:aws:states:::ecs:runTask.sync"
        Parameters = {
          Cluster        = var.ecs_cluster_arn
          TaskDefinition = "${var.project_name}-evidence-collector-${var.environment}"
          LaunchType     = "FARGATE"
          NetworkConfiguration = {
            AwsvpcConfiguration = {
              AssignPublicIp = "DISABLED"
              Subnets        = []
            }
          }
          Overrides = {
            ContainerOverrides = [{
              Name = "evidence-collector"
              Environment = [
                { Name = "INCIDENT_ID", "Value.$" = "$.validation.Payload.incidentId" },
                { Name = "ENVIRONMENT", Value = var.environment }
              ]
            }]
          }
        }
        ResultPath    = "$.evidence"
        TimeoutSeconds = 300
        Retry = [{
          ErrorEquals     = ["States.TaskFailed"]
          IntervalSeconds = 10
          MaxAttempts     = 2
          BackoffRate     = 2.0
        }]
        Catch = [{
          ErrorEquals = ["States.ALL"]
          ResultPath  = "$.error"
          Next        = "HandleEvidenceError"
        }]
        Next = "MonitorResolution"
      }

      HandleEvidenceError = {
        Type  = "Pass"
        Result = {
          status  = "EVIDENCE_COLLECTION_FAILED"
          message = "Evidence collection failed — continuing with available data"
        }
        ResultPath = "$.errorInfo"
        Next       = "MonitorResolution"
      }

      # State 4: Monitor incident resolution (wait for responder update)
      MonitorResolution = {
        Type     = "Task"
        Resource = "arn:aws:states:::lambda:invoke.waitForTaskToken"
        Parameters = {
          FunctionName = var.lambda_function_arns["monitor_resolution"]
          Payload = {
            "incidentId.$" = "$.validation.Payload.incidentId"
            "taskToken.$"  = "$$.Task.Token"
            environment    = var.environment
          }
        }
        ResultPath    = "$.resolution"
        TimeoutSeconds = 3600
        Retry = [{
          ErrorEquals     = ["Lambda.ServiceException", "Lambda.AWSLambdaException"]
          IntervalSeconds = 10
          MaxAttempts     = 2
          BackoffRate     = 2.0
        }]
        Catch = [{
          ErrorEquals = ["States.ALL"]
          ResultPath  = "$.error"
          Next        = "HandleMonitorError"
        }]
        Next = "GenerateReport"
      }

      HandleMonitorError = {
        Type  = "Pass"
        Result = {
          status  = "MONITORING_TIMEOUT"
          message = "Resolution monitoring timed out — generating partial report"
        }
        ResultPath = "$.errorInfo"
        Next       = "GenerateReport"
      }

      # State 5: Generate the final incident report
      GenerateReport = {
        Type     = "Task"
        Resource = "arn:aws:states:::lambda:invoke"
        Parameters = {
          FunctionName = var.lambda_function_arns["generate_report"]
          "Payload.$"  = "$"
        }
        ResultPath    = "$.report"
        TimeoutSeconds = 60
        Retry = [{
          ErrorEquals     = ["Lambda.ServiceException", "Lambda.AWSLambdaException", "Lambda.SdkClientException"]
          IntervalSeconds = 5
          MaxAttempts     = 3
          BackoffRate     = 2.0
        }]
        Catch = [{
          ErrorEquals = ["States.ALL"]
          ResultPath  = "$.error"
          Next        = "IncidentFailed"
        }]
        Next = "IncidentComplete"
      }

      # Terminal States
      IncidentComplete = {
        Type = "Succeed"
      }

      IncidentFailed = {
        Type  = "Fail"
        Error = "IncidentProcessingFailed"
        Cause = "The incident lifecycle could not be completed — check execution history for details"
      }
    }
  })

  tags = {
    Name        = "${var.project_name}-incident-lifecycle-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# IAM Role — Step Functions execution role
# ---------------------------------------------------------------------------
resource "aws_iam_role" "sfn_role" {
  name = "${var.project_name}-sfn-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "states.amazonaws.com"
      }
    }]
  })

  tags = {
    Name        = "${var.project_name}-sfn-role-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy" "sfn_policy" {
  name = "${var.project_name}-sfn-policy-${var.environment}"
  role = aws_iam_role.sfn_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "lambda:InvokeFunction"
        ]
        Resource = values(var.lambda_function_arns)
      },
      {
        Effect = "Allow"
        Action = [
          "ecs:RunTask",
          "ecs:StopTask",
          "ecs:DescribeTasks"
        ]
        Resource = "*"
        Condition = {
          ArnEquals = {
            "ecs:cluster" = var.ecs_cluster_arn
          }
        }
      },
      {
        Effect = "Allow"
        Action = [
          "iam:PassRole"
        ]
        Resource = "*"
        Condition = {
          StringEquals = {
            "iam:PassedToService" = "ecs-tasks.amazonaws.com"
          }
        }
      },
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogDelivery",
          "logs:GetLogDelivery",
          "logs:UpdateLogDelivery",
          "logs:DeleteLogDelivery",
          "logs:ListLogDeliveries",
          "logs:PutResourcePolicy",
          "logs:DescribeResourcePolicies",
          "logs:DescribeLogGroups"
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "xray:PutTraceSegments",
          "xray:PutTelemetryRecords",
          "xray:GetSamplingRules",
          "xray:GetSamplingTargets"
        ]
        Resource = "*"
      }
    ]
  })
}
