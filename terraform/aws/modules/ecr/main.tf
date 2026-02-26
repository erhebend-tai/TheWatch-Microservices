# ECR Repositories — container image registry for TheWatch microservices
# Amazon ECR with lifecycle policies, image scanning, KMS encryption, and cross-region replication

# ---------------------------------------------------------------------------
# ECR Repositories — one per service
# ---------------------------------------------------------------------------
resource "aws_ecr_repository" "repos" {
  for_each = toset(var.services)

  name                 = "${lower(var.project_name)}-${each.key}-${var.environment}"
  image_tag_mutability = "MUTABLE"
  force_delete         = false

  image_scanning_configuration {
    scan_on_push = true
  }

  encryption_configuration {
    encryption_type = "KMS"
    kms_key         = var.kms_key_arn
  }

  tags = {
    Name        = "${var.project_name}-ecr-${each.key}-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Lifecycle Policies — keep last 20 tagged, expire untagged after 7 days
# ---------------------------------------------------------------------------
resource "aws_ecr_lifecycle_policy" "repos" {
  for_each = toset(var.services)

  repository = aws_ecr_repository.repos[each.key].name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Expire untagged images after 7 days"
        selection = {
          tagStatus   = "untagged"
          countType   = "sinceImagePushed"
          countUnit   = "days"
          countNumber = 7
        }
        action = {
          type = "expire"
        }
      },
      {
        rulePriority = 2
        description  = "Keep only the last 20 tagged images"
        selection = {
          tagStatus     = "tagged"
          tagPrefixList = ["v", "latest", "build"]
          countType     = "imageCountMoreThan"
          countNumber   = 20
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}

# ---------------------------------------------------------------------------
# Cross-Region Replication — replicate to DR region
# ---------------------------------------------------------------------------
resource "aws_ecr_replication_configuration" "cross_region" {
  replication_configuration {
    rule {
      destination {
        region      = var.dr_region
        registry_id = data.aws_caller_identity.current.account_id
      }

      repository_filter {
        filter      = "${lower(var.project_name)}-"
        filter_type = "PREFIX_MATCH"
      }
    }
  }
}

# ---------------------------------------------------------------------------
# Data Sources
# ---------------------------------------------------------------------------
data "aws_caller_identity" "current" {}
