# IAM Roles — least-privilege policies for TheWatch ECS tasks
# Service-specific roles with scoped permissions per microservice

locals {
  services = {
    "P1-CoreGateway"    = {}
    "P2-VoiceEmergency" = {}
    "P3-MeshNetwork"    = {}
    "P4-Wearable"       = {}
    "P5-AuthSecurity"   = {}
    "P6-FirstResponder" = {}
    "P7-FamilyHealth"   = {}
    "P8-DisasterRelief" = {}
    "P9-DoctorServices" = {}
    "P10-Gamification"  = {}
  }
}

# ─────────────────────────────────────────────────────────────
# Shared ECS Task Execution Role
# ─────────────────────────────────────────────────────────────

resource "aws_iam_role" "ecs_task_execution" {
  name = "${var.project_name}-ecs-execution-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })

  tags = {
    Name        = "${var.project_name}-ecs-execution-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution" {
  role       = aws_iam_role.ecs_task_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# ─────────────────────────────────────────────────────────────
# Per-Service Task Roles
# ─────────────────────────────────────────────────────────────

resource "aws_iam_role" "service_task" {
  for_each = local.services

  name = "${var.project_name}-${each.key}-task-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })

  tags = {
    Name        = "${var.project_name}-${each.key}-task-${var.environment}"
    Environment = var.environment
    Service     = each.key
  }
}

# ─────────────────────────────────────────────────────────────
# Base policy — shared across all services (S3, CloudWatch Logs)
# ─────────────────────────────────────────────────────────────

resource "aws_iam_role_policy" "service_base" {
  for_each = local.services

  name = "${var.project_name}-${each.key}-base-${var.environment}"
  role = aws_iam_role.service_task[each.key].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "S3ServicePrefix"
        Effect = "Allow"
        Action = [
          "s3:PutObject",
          "s3:GetObject",
        ]
        Resource = "${var.evidence_bucket_arn}/${each.key}/*"
      },
      {
        Sid    = "CloudWatchLogs"
        Effect = "Allow"
        Action = [
          "logs:CreateLogStream",
          "logs:PutLogEvents",
        ]
        Resource = lookup(var.log_group_arns, each.key, "arn:aws:logs:*:*:log-group:${var.project_name}-${each.key}-*")
      }
    ]
  })
}

# ─────────────────────────────────────────────────────────────
# P2 VoiceEmergency — Rekognition + Transcribe
# ─────────────────────────────────────────────────────────────

resource "aws_iam_role_policy" "p2_voice_emergency" {
  name = "${var.project_name}-P2-VoiceEmergency-ai-${var.environment}"
  role = aws_iam_role.service_task["P2-VoiceEmergency"].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "Rekognition"
        Effect = "Allow"
        Action = [
          "rekognition:DetectModerationLabels",
        ]
        Resource = "*"
      },
      {
        Sid    = "Transcribe"
        Effect = "Allow"
        Action = [
          "transcribe:StartStreamTranscription",
        ]
        Resource = "*"
      }
    ]
  })
}

# ─────────────────────────────────────────────────────────────
# P5 AuthSecurity — Cognito admin operations
# ─────────────────────────────────────────────────────────────

resource "aws_iam_role_policy" "p5_auth_security" {
  name = "${var.project_name}-P5-AuthSecurity-cognito-${var.environment}"
  role = aws_iam_role.service_task["P5-AuthSecurity"].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "CognitoAdmin"
        Effect = "Allow"
        Action = [
          "cognito-idp:AdminCreateUser",
          "cognito-idp:AdminDeleteUser",
          "cognito-idp:AdminDisableUser",
          "cognito-idp:AdminEnableUser",
          "cognito-idp:AdminGetUser",
          "cognito-idp:AdminInitiateAuth",
          "cognito-idp:AdminListGroupsForUser",
          "cognito-idp:AdminResetUserPassword",
          "cognito-idp:AdminRespondToAuthChallenge",
          "cognito-idp:AdminSetUserMFAPreference",
          "cognito-idp:AdminSetUserPassword",
          "cognito-idp:AdminUpdateUserAttributes",
          "cognito-idp:AdminUserGlobalSignOut",
        ]
        Resource = var.cognito_user_pool_arn
      }
    ]
  })
}

# ─────────────────────────────────────────────────────────────
# P7 FamilyHealth — HealthLake access
# ─────────────────────────────────────────────────────────────

resource "aws_iam_role_policy" "p7_family_health" {
  name = "${var.project_name}-P7-FamilyHealth-healthlake-${var.environment}"
  role = aws_iam_role.service_task["P7-FamilyHealth"].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "HealthLake"
        Effect = "Allow"
        Action = [
          "healthlake:CreateResource",
          "healthlake:DeleteResource",
          "healthlake:GetCapabilities",
          "healthlake:ReadResource",
          "healthlake:SearchWithGet",
          "healthlake:SearchWithPost",
          "healthlake:UpdateResource",
        ]
        Resource = "*"
      }
    ]
  })
}

# ─────────────────────────────────────────────────────────────
# P9 DoctorServices — HealthLake access
# ─────────────────────────────────────────────────────────────

resource "aws_iam_role_policy" "p9_doctor_services" {
  name = "${var.project_name}-P9-DoctorServices-healthlake-${var.environment}"
  role = aws_iam_role.service_task["P9-DoctorServices"].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "HealthLake"
        Effect = "Allow"
        Action = [
          "healthlake:CreateResource",
          "healthlake:DeleteResource",
          "healthlake:GetCapabilities",
          "healthlake:ReadResource",
          "healthlake:SearchWithGet",
          "healthlake:SearchWithPost",
          "healthlake:UpdateResource",
        ]
        Resource = "*"
      }
    ]
  })
}
