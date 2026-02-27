# ──────────────────────────────────────────────────────────────
# TheWatch AWS — Production Environment
# ──────────────────────────────────────────────────────────────

terraform {
  required_version = ">= 1.5"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

# ── Primary region provider ──────────────────────────────────
provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "terraform"
      CostCenter  = "production"
    }
  }
}

# ── DR region provider (us-west-2) ──────────────────────────
provider "aws" {
  alias  = "dr"
  region = var.dr_region

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "terraform"
      CostCenter  = "production-dr"
    }
  }
}

# ──────────────────────────────────────────────────────────────
# Locals
# ──────────────────────────────────────────────────────────────

locals {
  services = [
    "p1-coregateway",
    "p2-voiceemergency",
    "p3-meshnetwork",
    "p4-wearable",
    "p5-authsecurity",
    "p6-firstresponder",
    "p7-familyhealth",
    "p8-disasterrelief",
    "p9-doctorservices",
    "p10-gamification",
    "p11-surveillance",
    "p12-notifications",
    "geospatial",
    "dashboard",
  ]

  databases = {
    "WatchCoreGatewayDB"    = { service = "p1-coregateway", tier = "standard" }
    "WatchVoiceEmergencyDB" = { service = "p2-voiceemergency", tier = "critical" }
    "WatchMeshNetworkDB"    = { service = "p3-meshnetwork", tier = "standard" }
    "WatchWearableDB"       = { service = "p4-wearable", tier = "standard" }
    "WatchAuthSecurityDB"   = { service = "p5-authsecurity", tier = "critical" }
    "WatchFirstResponderDB" = { service = "p6-firstresponder", tier = "critical" }
    "WatchFamilyHealthDB"   = { service = "p7-familyhealth", tier = "standard" }
    "WatchDisasterReliefDB" = { service = "p8-disasterrelief", tier = "standard" }
    "WatchDoctorServicesDB" = { service = "p9-doctorservices", tier = "standard" }
    "WatchGamificationDB"   = { service = "p10-gamification", tier = "standard" }
    "WatchSurveillanceDB"   = { service = "p11-surveillance", tier = "standard" }
    "WatchNotificationsDB"  = { service = "p12-notifications", tier = "standard" }
  }

  ecs_services = {
    "p1-coregateway" = {
      cpu          = 1024
      memory       = 2048
      desired      = 3
      min_capacity = 2
      max_capacity = 10
      port         = 8081
      path_pattern = "/api/gateway/*"
      dns_name     = "coregateway.${var.project_name}.local"
    }
    "p2-voiceemergency" = {
      cpu          = 2048
      memory       = 4096
      desired      = 5
      min_capacity = 3
      max_capacity = 20
      port         = 8082
      path_pattern = "/api/voice/*"
      dns_name     = "voiceemergency.${var.project_name}.local"
    }
    "p3-meshnetwork" = {
      cpu          = 1024
      memory       = 2048
      desired      = 2
      min_capacity = 2
      max_capacity = 10
      port         = 8083
      path_pattern = "/api/mesh/*"
      dns_name     = "meshnetwork.${var.project_name}.local"
    }
    "p4-wearable" = {
      cpu          = 1024
      memory       = 2048
      desired      = 2
      min_capacity = 2
      max_capacity = 5
      port         = 8084
      path_pattern = "/api/wearable/*"
      dns_name     = "wearable.${var.project_name}.local"
    }
    "p5-authsecurity" = {
      cpu          = 2048
      memory       = 4096
      desired      = 3
      min_capacity = 2
      max_capacity = 10
      port         = 8085
      path_pattern = "/api/auth/*"
      dns_name     = "authsecurity.${var.project_name}.local"
    }
    "p6-firstresponder" = {
      cpu          = 2048
      memory       = 4096
      desired      = 5
      min_capacity = 3
      max_capacity = 15
      port         = 8086
      path_pattern = "/api/responder/*"
      dns_name     = "firstresponder.${var.project_name}.local"
    }
    "p7-familyhealth" = {
      cpu          = 1024
      memory       = 2048
      desired      = 2
      min_capacity = 2
      max_capacity = 10
      port         = 8087
      path_pattern = "/api/family/*"
      dns_name     = "familyhealth.${var.project_name}.local"
    }
    "p8-disasterrelief" = {
      cpu          = 1024
      memory       = 2048
      desired      = 2
      min_capacity = 2
      max_capacity = 10
      port         = 8088
      path_pattern = "/api/disaster/*"
      dns_name     = "disasterrelief.${var.project_name}.local"
    }
    "p9-doctorservices" = {
      cpu          = 1024
      memory       = 2048
      desired      = 2
      min_capacity = 2
      max_capacity = 5
      port         = 8089
      path_pattern = "/api/doctor/*"
      dns_name     = "doctorservices.${var.project_name}.local"
    }
    "p10-gamification" = {
      cpu          = 512
      memory       = 1024
      desired      = 1
      min_capacity = 1
      max_capacity = 3
      port         = 8090
      path_pattern = "/api/gamification/*"
      dns_name     = "gamification.${var.project_name}.local"
    }
    "p11-surveillance" = {
      cpu          = 2048
      memory       = 4096
      desired      = 2
      min_capacity = 2
      max_capacity = 10
      port         = 8092
      path_pattern = "/api/surveillance/*"
      dns_name     = "surveillance.${var.project_name}.local"
    }
    "geospatial" = {
      cpu          = 2048
      memory       = 4096
      desired      = 3
      min_capacity = 2
      max_capacity = 10
      port         = 8091
      path_pattern = "/api/geo/*"
      dns_name     = "geospatial.${var.project_name}.local"
    }
    "dashboard" = {
      cpu          = 1024
      memory       = 2048
      desired      = 2
      min_capacity = 2
      max_capacity = 5
      port         = 8080
      path_pattern = "/dashboard/*"
      dns_name     = "dashboard.${var.project_name}.local"
    }
  }

  kafka_topics = {
    "incident-created"   = { partitions = 12, replication = 3, consumers = ["p6-firstresponder", "p3-meshnetwork", "dashboard"] }
    "dispatch-requested" = { partitions = 12, replication = 3, consumers = ["p3-meshnetwork", "p6-firstresponder"] }
    "responder-located"  = { partitions = 6, replication = 3, consumers = ["p2-voiceemergency", "dashboard"] }
    "checkin-completed"  = { partitions = 6, replication = 3, consumers = ["p7-familyhealth", "dashboard"] }
    "vital-alert"        = { partitions = 6, replication = 3, consumers = ["p7-familyhealth", "p9-doctorservices"] }
    "evidence-uploaded"  = { partitions = 6, replication = 3, consumers = ["p2-voiceemergency"] }
    "disaster-declared"       = { partitions = 6, replication = 3, consumers = ["p8-disasterrelief", "p6-firstresponder", "dashboard"] }
    "footage-submitted"       = { partitions = 6, replication = 3, consumers = ["p11-surveillance", "p12-notifications", "p2-voiceemergency"] }
    "crime-location-reported" = { partitions = 6, replication = 3, consumers = ["p11-surveillance", "p12-notifications", "p6-firstresponder"] }
    "dead-letter"             = { partitions = 3, replication = 3, consumers = ["monitoring"] }
  }
}

# ──────────────────────────────────────────────────────────────
# Random passwords
# ──────────────────────────────────────────────────────────────

resource "random_password" "rds_admin" {
  length           = 32
  special          = true
  override_special = "!@#$%&*()-_=+[]{}"
}

resource "random_password" "aurora_admin" {
  length           = 32
  special          = true
  override_special = "!@#$%&*()-_=+[]{}"
}

resource "random_password" "jwt_signing_key" {
  length  = 64
  special = false
}

# ──────────────────────────────────────────────────────────────
# Networking
# ──────────────────────────────────────────────────────────────

module "vpc" {
  source = "../../modules/vpc"

  vpc_cidr             = var.vpc_cidr
  public_subnet_cidrs  = ["10.2.1.0/24", "10.2.2.0/24", "10.2.3.0/24"]
  private_subnet_cidrs = ["10.2.10.0/24", "10.2.11.0/24", "10.2.12.0/24"]
  environment          = var.environment
  project_name         = var.project_name
  flow_log_bucket_arn  = module.s3.backups_bucket_id
}

# ──────────────────────────────────────────────────────────────
# Encryption (KMS)
# ──────────────────────────────────────────────────────────────

module "kms" {
  source = "../../modules/kms"

  environment      = var.environment
  project_name     = var.project_name
  aws_region       = var.aws_region
  admin_role_arns  = ["arn:aws:iam::${data.aws_caller_identity.current.account_id}:root"]
  service_role_arns = values(module.iam.service_role_arns)

  depends_on = [module.iam]
}

data "aws_caller_identity" "current" {}

# ──────────────────────────────────────────────────────────────
# Container Registry (ECR) — with cross-region replication for DR
# ──────────────────────────────────────────────────────────────

module "ecr" {
  source = "../../modules/ecr"

  environment  = var.environment
  project_name = var.project_name
  services     = local.services
  kms_key_arn  = module.kms.data_key_arn
  dr_region    = var.dr_region

  depends_on = [module.kms]
}

# ──────────────────────────────────────────────────────────────
# Databases
# ──────────────────────────────────────────────────────────────

module "rds_sqlserver" {
  source = "../../modules/rds-sqlserver"

  environment            = var.environment
  project_name           = var.project_name
  subnet_ids             = module.vpc.private_subnet_ids
  vpc_security_group_ids = [module.msk.security_group_id]
  instance_class         = var.rds_instance_class
  db_name                = "WatchCoreDB"
  username               = "watchadmin"
  password               = random_password.rds_admin.result
  backup_retention       = 35

  depends_on = [module.vpc]
}

module "aurora_postgres" {
  source = "../../modules/aurora-postgres"

  environment            = var.environment
  project_name           = var.project_name
  subnet_ids             = module.vpc.private_subnet_ids
  vpc_security_group_ids = [module.msk.security_group_id]
  db_name                = "WatchGeospatialDB"
  master_username        = "watchpgadmin"
  master_password        = random_password.aurora_admin.result
  instance_class         = var.rds_instance_class
  replica_count          = 3

  depends_on = [module.vpc]
}

# ──────────────────────────────────────────────────────────────
# Caching (ElastiCache Redis)
# ──────────────────────────────────────────────────────────────

module "elasticache_redis" {
  source = "../../modules/elasticache-redis"

  environment        = var.environment
  project_name       = var.project_name
  aws_region         = var.aws_region
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids
  vpc_cidr           = var.vpc_cidr
  node_type          = var.redis_node_type
  kms_key_arn        = module.kms.data_key_arn

  depends_on = [module.vpc, module.kms]
}

# ──────────────────────────────────────────────────────────────
# Event Streaming (MSK / Kafka)
# ──────────────────────────────────────────────────────────────

module "msk" {
  source = "../../modules/msk"

  environment        = var.environment
  project_name       = var.project_name
  aws_region         = var.aws_region
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids
  vpc_cidr           = var.vpc_cidr
  instance_type      = var.msk_instance_type
  kms_key_arn        = module.kms.data_key_arn

  depends_on = [module.vpc, module.kms]
}

# ──────────────────────────────────────────────────────────────
# Object Storage (S3)
# ──────────────────────────────────────────────────────────────

module "s3" {
  source = "../../modules/s3"

  environment  = var.environment
  project_name = var.project_name
  aws_region   = var.aws_region
  kms_key_arn  = module.kms.evidence_key_arn

  depends_on = [module.kms]
}

# ──────────────────────────────────────────────────────────────
# NoSQL (DynamoDB)
# ──────────────────────────────────────────────────────────────

module "dynamodb" {
  source = "../../modules/dynamodb"

  environment  = var.environment
  project_name = var.project_name
  aws_region   = var.aws_region
  kms_key_arn  = module.kms.data_key_arn

  depends_on = [module.kms]
}

# ──────────────────────────────────────────────────────────────
# S3 Event Processing (evidence upload pipeline)
# ──────────────────────────────────────────────────────────────

module "s3_events" {
  source = "../../modules/s3-events"

  environment         = var.environment
  project_name        = var.project_name
  aws_region          = var.aws_region
  evidence_bucket_id  = module.s3.evidence_bucket_id
  evidence_bucket_arn = module.s3.evidence_bucket_arn
  dynamodb_table_name = module.dynamodb.table_name
  kms_key_arn         = module.kms.data_key_arn

  depends_on = [module.s3, module.dynamodb, module.kms]
}

# ──────────────────────────────────────────────────────────────
# Identity (Cognito)
# ──────────────────────────────────────────────────────────────

module "cognito" {
  source = "../../modules/cognito"

  environment    = var.environment
  project_name   = var.project_name
  aws_region     = var.aws_region
  callback_urls  = var.callback_urls
  logout_urls    = var.logout_urls
}

# ──────────────────────────────────────────────────────────────
# IAM Roles
# ──────────────────────────────────────────────────────────────

module "iam" {
  source = "../../modules/iam"

  environment           = var.environment
  project_name          = var.project_name
  aws_region            = var.aws_region
  evidence_bucket_arn   = module.s3.evidence_bucket_arn
  cognito_user_pool_arn = module.cognito.user_pool_arn

  depends_on = [module.s3, module.cognito]
}

# ──────────────────────────────────────────────────────────────
# Compute (ECS Fargate)
# ──────────────────────────────────────────────────────────────

module "ecs" {
  source = "../../modules/ecs"

  environment        = var.environment
  project_name       = var.project_name
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids

  services = {
    for name, svc in local.ecs_services : name => {
      cpu          = svc.cpu
      memory       = svc.memory
      port         = svc.port
      image        = "${module.ecr.repository_urls[name]}:latest"
      dns_name     = svc.dns_name
      path_pattern = svc.path_pattern
    }
  }

  depends_on = [module.vpc, module.ecr, module.iam]
}

# ──────────────────────────────────────────────────────────────
# Load Balancer (ALB)
# ──────────────────────────────────────────────────────────────

module "alb" {
  source = "../../modules/alb"

  environment       = var.environment
  project_name      = var.project_name
  vpc_id            = module.vpc.vpc_id
  public_subnet_ids = module.vpc.public_subnet_ids

  services = {
    for name, svc in local.ecs_services : name => {
      port         = svc.port
      path_pattern = svc.path_pattern
    }
  }

  depends_on = [module.vpc]
}

# ──────────────────────────────────────────────────────────────
# WAF
# ──────────────────────────────────────────────────────────────

module "waf" {
  source = "../../modules/waf"

  environment         = var.environment
  project_name        = var.project_name
  aws_region          = var.aws_region
  alb_arn             = module.alb.alb_arn
  log_destination_arn = module.cloudwatch.log_group_arns["p1-coregateway"]

  depends_on = [module.alb, module.cloudwatch]
}

# ──────────────────────────────────────────────────────────────
# Observability (CloudWatch)
# ──────────────────────────────────────────────────────────────

module "cloudwatch" {
  source = "../../modules/cloudwatch"

  environment        = var.environment
  project_name       = var.project_name
  aws_region         = var.aws_region
  services           = local.services
  alarm_sns_topic_arn = module.guardduty.sns_topic_arn

  depends_on = [module.guardduty]
}

# ──────────────────────────────────────────────────────────────
# Distributed Tracing (X-Ray)
# ──────────────────────────────────────────────────────────────

module "xray" {
  source = "../../modules/xray"

  environment  = var.environment
  project_name = var.project_name
  services     = local.services
}

# ──────────────────────────────────────────────────────────────
# Threat Detection (GuardDuty)
# ──────────────────────────────────────────────────────────────

module "guardduty" {
  source = "../../modules/guardduty"

  environment  = var.environment
  project_name = var.project_name
  aws_region   = var.aws_region
  alert_email  = var.alert_email
}

# ──────────────────────────────────────────────────────────────
# Security Posture (Security Hub)
# ──────────────────────────────────────────────────────────────

module "security_hub" {
  source = "../../modules/security-hub"

  environment  = var.environment
  project_name = var.project_name
  aws_region   = var.aws_region

  depends_on = [module.guardduty]
}

# ──────────────────────────────────────────────────────────────
# Backup — with cross-region copy to DR
# ──────────────────────────────────────────────────────────────

module "backup" {
  source = "../../modules/backup"

  environment  = var.environment
  project_name = var.project_name
  kms_key_arn  = module.kms.data_key_arn
  dr_region    = var.dr_region

  depends_on = [module.kms]
}

# ──────────────────────────────────────────────────────────────
# Service Mesh (App Mesh)
# ──────────────────────────────────────────────────────────────

module "appmesh" {
  source = "../../modules/appmesh"

  environment  = var.environment
  project_name = var.project_name

  services = {
    for name, svc in local.ecs_services : name => {
      dns_name = svc.dns_name
      port     = svc.port
    }
  }

  depends_on = [module.ecs]
}

# ──────────────────────────────────────────────────────────────
# API Gateway
# ──────────────────────────────────────────────────────────────

module "apigateway" {
  source = "../../modules/apigateway"

  environment          = var.environment
  project_name         = var.project_name
  alb_dns_name         = module.alb.alb_dns_name
  cognito_user_pool_id = module.cognito.user_pool_id
  cognito_client_id    = module.cognito.client_id

  depends_on = [module.alb, module.cognito]
}

# ──────────────────────────────────────────────────────────────
# Global Accelerator
# ──────────────────────────────────────────────────────────────

module "globalaccelerator" {
  source = "../../modules/globalaccelerator"

  environment  = var.environment
  project_name = var.project_name
  alb_arn      = module.alb.alb_arn

  depends_on = [module.alb]
}

# ──────────────────────────────────────────────────────────────
# Event Bus (EventBridge)
# ──────────────────────────────────────────────────────────────

module "eventbridge" {
  source = "../../modules/eventbridge"

  environment          = var.environment
  project_name         = var.project_name
  lambda_function_arns = { "evidence-processor" = module.s3_events.lambda_function_arn }
  sns_topic_arn        = module.guardduty.sns_topic_arn
  step_function_arn    = module.step_functions.state_machine_arn
  log_group_arn        = module.cloudwatch.log_group_arns["p1-coregateway"]

  depends_on = [module.s3_events, module.guardduty, module.step_functions, module.cloudwatch]
}

# ──────────────────────────────────────────────────────────────
# Orchestration (Step Functions)
# ──────────────────────────────────────────────────────────────

module "step_functions" {
  source = "../../modules/step-functions"

  environment          = var.environment
  project_name         = var.project_name
  lambda_function_arns = { "evidence-processor" = module.s3_events.lambda_function_arn }
  ecs_cluster_arn      = module.ecs.cluster_arn

  depends_on = [module.s3_events, module.ecs]
}

# ──────────────────────────────────────────────────────────────
# CI/CD (CodePipeline)
# ──────────────────────────────────────────────────────────────

module "codepipeline" {
  source = "../../modules/codepipeline"

  environment          = var.environment
  project_name         = var.project_name
  aws_region           = var.aws_region
  github_repo          = var.github_repo
  github_branch        = var.github_branch
  ecr_repository_urls  = module.ecr.repository_urls
  ecs_cluster_name     = module.ecs.cluster_name
  ecs_service_names    = { for name, _ in local.ecs_services : name => "${var.project_name}-${name}-${var.environment}" }

  depends_on = [module.ecr, module.ecs]
}

# ──────────────────────────────────────────────────────────────
# Multi-Account Governance (StackSets)
# ──────────────────────────────────────────────────────────────

module "stacksets" {
  source = "../../modules/stacksets"

  environment     = var.environment
  project_name    = var.project_name
  account_ids     = var.account_ids
  allowed_regions = [var.aws_region, var.dr_region]
}
