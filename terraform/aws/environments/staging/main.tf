# ============================================================================
# TheWatch AWS — Staging Environment
# ============================================================================
# Staging-grade infrastructure for pre-production validation.
# Sized between dev and production for realistic load testing.
# ============================================================================

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

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "terraform"
      Repository  = var.github_repo
    }
  }
}

# ============================================================================
# Locals — service catalogue, database map, ECS sizing, Kafka topics
# ============================================================================
locals {
  # -- Microservice names (used by ECR, CloudWatch, X-Ray, IAM) ---------------
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

  # -- Database catalogue (RDS SQL Server holds all eleven) ----------------------
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

  # -- ECS Fargate sizing — STAGING tier --------------------------------------
  ecs_services = {
    "p1-coregateway" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 2
      min_capacity   = 1
      max_capacity   = 5
      port           = 8080
      image          = "${module.ecr.repository_urls["p1-coregateway"]}:latest"
      dns_name       = "p1-coregateway"
      path_pattern   = "/api/v1/gateway/*"
    }
    "p2-voiceemergency" = {
      cpu            = 1024
      memory         = 2048
      desired_count  = 2
      min_capacity   = 2
      max_capacity   = 10
      port           = 8080
      image          = "${module.ecr.repository_urls["p2-voiceemergency"]}:latest"
      dns_name       = "p2-voiceemergency"
      path_pattern   = "/api/v1/voice/*"
    }
    "p3-meshnetwork" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 5
      port           = 8080
      image          = "${module.ecr.repository_urls["p3-meshnetwork"]}:latest"
      dns_name       = "p3-meshnetwork"
      path_pattern   = "/api/v1/mesh/*"
    }
    "p4-wearable" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 3
      port           = 8080
      image          = "${module.ecr.repository_urls["p4-wearable"]}:latest"
      dns_name       = "p4-wearable"
      path_pattern   = "/api/v1/wearable/*"
    }
    "p5-authsecurity" = {
      cpu            = 1024
      memory         = 2048
      desired_count  = 2
      min_capacity   = 1
      max_capacity   = 5
      port           = 8080
      image          = "${module.ecr.repository_urls["p5-authsecurity"]}:latest"
      dns_name       = "p5-authsecurity"
      path_pattern   = "/api/v1/auth/*"
    }
    "p6-firstresponder" = {
      cpu            = 1024
      memory         = 2048
      desired_count  = 2
      min_capacity   = 2
      max_capacity   = 10
      port           = 8080
      image          = "${module.ecr.repository_urls["p6-firstresponder"]}:latest"
      dns_name       = "p6-firstresponder"
      path_pattern   = "/api/v1/responder/*"
    }
    "p7-familyhealth" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 5
      port           = 8080
      image          = "${module.ecr.repository_urls["p7-familyhealth"]}:latest"
      dns_name       = "p7-familyhealth"
      path_pattern   = "/api/v1/family/*"
    }
    "p8-disasterrelief" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 5
      port           = 8080
      image          = "${module.ecr.repository_urls["p8-disasterrelief"]}:latest"
      dns_name       = "p8-disasterrelief"
      path_pattern   = "/api/v1/disaster/*"
    }
    "p9-doctorservices" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 3
      port           = 8080
      image          = "${module.ecr.repository_urls["p9-doctorservices"]}:latest"
      dns_name       = "p9-doctorservices"
      path_pattern   = "/api/v1/doctor/*"
    }
    "p10-gamification" = {
      cpu            = 256
      memory         = 512
      desired_count  = 1
      min_capacity   = 0
      max_capacity   = 2
      port           = 8080
      image          = "${module.ecr.repository_urls["p10-gamification"]}:latest"
      dns_name       = "p10-gamification"
      path_pattern   = "/api/v1/gamification/*"
    }
    "p11-surveillance" = {
      cpu            = 1024
      memory         = 2048
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 5
      port           = 8080
      image          = "${module.ecr.repository_urls["p11-surveillance"]}:latest"
      dns_name       = "p11-surveillance"
      path_pattern   = "/api/v1/surveillance/*"
    }
    "geospatial" = {
      cpu            = 1024
      memory         = 2048
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 5
      port           = 8080
      image          = "${module.ecr.repository_urls["geospatial"]}:latest"
      dns_name       = "geospatial"
      path_pattern   = "/api/v1/geo/*"
    }
    "dashboard" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 3
      port           = 8080
      image          = "${module.ecr.repository_urls["dashboard"]}:latest"
      dns_name       = "dashboard"
      path_pattern   = "/dashboard/*"
    }
  }

  # -- Kafka / MSK topics ----------------------------------------------------
  kafka_topics = {
    "incident-created"   = { partitions = 6,  replication_factor = 3, retention_ms = 604800000 }
    "dispatch-requested" = { partitions = 6,  replication_factor = 3, retention_ms = 604800000 }
    "responder-located"  = { partitions = 3,  replication_factor = 3, retention_ms = 604800000 }
    "checkin-completed"  = { partitions = 3,  replication_factor = 3, retention_ms = 604800000 }
    "vital-alert"        = { partitions = 6,  replication_factor = 3, retention_ms = 604800000 }
    "evidence-uploaded"  = { partitions = 3,  replication_factor = 3, retention_ms = 604800000 }
    "disaster-declared"       = { partitions = 3,  replication_factor = 3, retention_ms = 604800000 }
    "footage-submitted"       = { partitions = 3,  replication_factor = 3, retention_ms = 604800000 }
    "crime-location-reported" = { partitions = 3,  replication_factor = 3, retention_ms = 604800000 }
    "dead-letter"             = { partitions = 1,  replication_factor = 3, retention_ms = 2592000000 }
  }
}

# ============================================================================
# 1. Networking — VPC, subnets, NAT gateways
# ============================================================================
module "vpc" {
  source = "../../modules/vpc"

  project_name         = var.project_name
  environment          = var.environment
  vpc_cidr             = var.vpc_cidr
  public_subnet_cidrs  = ["10.1.1.0/24", "10.1.2.0/24", "10.1.3.0/24"]
  private_subnet_cidrs = ["10.1.10.0/24", "10.1.11.0/24", "10.1.12.0/24"]
}

# ============================================================================
# 2. Encryption — KMS customer-managed keys
# ============================================================================
module "kms" {
  source = "../../modules/kms"

  project_name     = var.project_name
  environment      = var.environment
  aws_region       = var.aws_region
  admin_role_arns  = [data.aws_caller_identity.current.arn]
  service_role_arns = [module.iam.task_execution_role_arn]

  depends_on = [module.iam]
}

# ============================================================================
# 3. Container Registry — ECR repos per service
# ============================================================================
module "ecr" {
  source = "../../modules/ecr"

  project_name = var.project_name
  environment  = var.environment
  services     = local.services
  kms_key_arn  = module.kms.data_key_arn

  depends_on = [module.kms]
}

# ============================================================================
# 4. Relational — RDS SQL Server (primary OLTP store)
# ============================================================================
module "rds_sqlserver" {
  source = "../../modules/rds-sqlserver"

  project_name           = var.project_name
  environment            = var.environment
  subnet_ids             = module.vpc.private_subnet_ids
  vpc_security_group_ids = []
  instance_class         = var.rds_instance_class
  db_name                = "WatchCoreDB"
  username               = "watchadmin"
  password               = random_password.sql_admin.result
  backup_retention       = 14

  depends_on = [module.vpc]
}

# ============================================================================
# 5. Relational — Aurora PostgreSQL (PostGIS / geospatial)
# ============================================================================
module "aurora_postgres" {
  source = "../../modules/aurora-postgres"

  project_name           = var.project_name
  environment            = var.environment
  subnet_ids             = module.vpc.private_subnet_ids
  vpc_security_group_ids = []
  db_name                = "WatchGeospatialDB"
  master_username        = "watchpgadmin"
  master_password        = random_password.pg_admin.result
  instance_class         = var.rds_instance_class
  replica_count          = 1

  depends_on = [module.vpc]
}

# ============================================================================
# 6. Cache — ElastiCache Redis cluster-mode
# ============================================================================
module "elasticache_redis" {
  source = "../../modules/elasticache-redis"

  project_name       = var.project_name
  environment        = var.environment
  aws_region         = var.aws_region
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids
  vpc_cidr           = var.vpc_cidr
  node_type          = var.redis_node_type
  kms_key_arn        = module.kms.data_key_arn

  depends_on = [module.vpc, module.kms]
}

# ============================================================================
# 7. Streaming — Amazon MSK (Kafka)
# ============================================================================
module "msk" {
  source = "../../modules/msk"

  project_name       = var.project_name
  environment        = var.environment
  aws_region         = var.aws_region
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids
  vpc_cidr           = var.vpc_cidr
  instance_type      = var.msk_instance_type
  kms_key_arn        = module.kms.data_key_arn

  depends_on = [module.vpc, module.kms]
}

# ============================================================================
# 8. Object Storage — S3 buckets (evidence, backups, static)
# ============================================================================
module "s3" {
  source = "../../modules/s3"

  project_name = var.project_name
  environment  = var.environment
  aws_region   = var.aws_region
  kms_key_arn  = module.kms.evidence_key_arn

  depends_on = [module.kms]
}

# ============================================================================
# 9. NoSQL — DynamoDB audit log table
# ============================================================================
module "dynamodb" {
  source = "../../modules/dynamodb"

  project_name = var.project_name
  environment  = var.environment
  aws_region   = var.aws_region
  kms_key_arn  = module.kms.data_key_arn

  depends_on = [module.kms]
}

# ============================================================================
# 10. Event Processing — S3 event notifications (evidence pipeline)
# ============================================================================
module "s3_events" {
  source = "../../modules/s3-events"

  project_name        = var.project_name
  environment         = var.environment
  aws_region          = var.aws_region
  evidence_bucket_id  = module.s3.evidence_bucket_id
  evidence_bucket_arn = module.s3.evidence_bucket_arn
  dynamodb_table_name = module.dynamodb.table_name
  kms_key_arn         = module.kms.data_key_arn

  depends_on = [module.s3, module.dynamodb, module.kms]
}

# ============================================================================
# 11. Identity — Cognito User Pool + App Clients
# ============================================================================
module "cognito" {
  source = "../../modules/cognito"

  project_name  = var.project_name
  environment   = var.environment
  aws_region    = var.aws_region
  callback_urls = var.callback_urls
  logout_urls   = var.logout_urls
}

# ============================================================================
# 12. IAM — ECS task roles, service-specific policies
# ============================================================================
module "iam" {
  source = "../../modules/iam"

  project_name          = var.project_name
  environment           = var.environment
  aws_region            = var.aws_region
  evidence_bucket_arn   = module.s3.evidence_bucket_arn
  cognito_user_pool_arn = module.cognito.user_pool_arn
  log_group_arns        = module.cloudwatch.log_group_arns

  depends_on = [module.s3, module.cognito, module.cloudwatch]
}

# ============================================================================
# 13. Compute — ECS Fargate cluster + services
# ============================================================================
module "ecs" {
  source = "../../modules/ecs"

  project_name       = var.project_name
  environment        = var.environment
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids
  services = { for k, v in local.ecs_services : k => {
    cpu          = v.cpu
    memory       = v.memory
    port         = v.port
    image        = v.image
    dns_name     = v.dns_name
    path_pattern = v.path_pattern
  }}

  depends_on = [module.vpc, module.ecr, module.iam]
}

# ============================================================================
# 14. Load Balancing — Application Load Balancer
# ============================================================================
module "alb" {
  source = "../../modules/alb"

  project_name      = var.project_name
  environment       = var.environment
  vpc_id            = module.vpc.vpc_id
  public_subnet_ids = module.vpc.public_subnet_ids
  services = { for k, v in local.ecs_services : k => {
    port         = v.port
    path_pattern = v.path_pattern
  }}

  depends_on = [module.vpc]
}

# ============================================================================
# 15. Web Application Firewall
# ============================================================================
module "waf" {
  source = "../../modules/waf"

  project_name        = var.project_name
  environment         = var.environment
  aws_region          = var.aws_region
  alb_arn             = module.alb.alb_arn
  log_destination_arn = module.cloudwatch.log_group_arns["p1-coregateway"]

  depends_on = [module.alb, module.cloudwatch]
}

# ============================================================================
# 16. Observability — CloudWatch log groups, dashboards, alarms
# ============================================================================
module "cloudwatch" {
  source = "../../modules/cloudwatch"

  project_name        = var.project_name
  environment         = var.environment
  aws_region          = var.aws_region
  services            = local.services
  alarm_sns_topic_arn = module.guardduty.sns_topic_arn

  depends_on = [module.guardduty]
}

# ============================================================================
# 17. Distributed Tracing — X-Ray groups + sampling rules
# ============================================================================
module "xray" {
  source = "../../modules/xray"

  project_name = var.project_name
  environment  = var.environment
  services     = local.services
}

# ============================================================================
# 18. Threat Detection — GuardDuty
# ============================================================================
module "guardduty" {
  source = "../../modules/guardduty"

  project_name = var.project_name
  environment  = var.environment
  aws_region   = var.aws_region
  alert_email  = var.alert_email
}

# ============================================================================
# 19. Compliance — Security Hub
# ============================================================================
module "security_hub" {
  source = "../../modules/security-hub"

  project_name = var.project_name
  environment  = var.environment
  aws_region   = var.aws_region

  depends_on = [module.guardduty]
}

# ============================================================================
# 20. Disaster Recovery — AWS Backup vault + plans
# ============================================================================
module "backup" {
  source = "../../modules/backup"

  project_name = var.project_name
  environment  = var.environment
  kms_key_arn  = module.kms.data_key_arn

  depends_on = [module.kms]
}

# ============================================================================
# 21. Service Mesh — AWS App Mesh (staging-only)
# ============================================================================
module "appmesh" {
  source = "../../modules/appmesh"

  project_name = var.project_name
  environment  = var.environment
  services = { for k, v in local.ecs_services : k => {
    dns_name = v.dns_name
    port     = v.port
  }}

  depends_on = [module.ecs]
}

# ============================================================================
# 22. Event Bus — Amazon EventBridge (staging-only)
# ============================================================================
module "eventbridge" {
  source = "../../modules/eventbridge"

  project_name         = var.project_name
  environment          = var.environment
  lambda_function_arns = {}
  sns_topic_arn        = module.guardduty.sns_topic_arn
  step_function_arn    = module.step_functions.state_machine_arn
  log_group_arn        = module.cloudwatch.log_group_arns["p1-coregateway"]

  depends_on = [module.guardduty, module.step_functions, module.cloudwatch]
}

# ============================================================================
# 23. Workflow Orchestration — AWS Step Functions (staging-only)
# ============================================================================
module "step_functions" {
  source = "../../modules/step-functions"

  project_name         = var.project_name
  environment          = var.environment
  lambda_function_arns = {}
  ecs_cluster_arn      = module.ecs.cluster_arn

  depends_on = [module.ecs]
}

# ============================================================================
# Random passwords — SQL Server admin, PostgreSQL admin, JWT signing key
# ============================================================================
resource "random_password" "sql_admin" {
  length           = 32
  special          = true
  override_special = "!@#$%&*()-_=+[]{}"
}

resource "random_password" "pg_admin" {
  length           = 32
  special          = true
  override_special = "!@#$%&*()-_=+[]{}"
}

resource "random_password" "jwt_signing_key" {
  length  = 64
  special = false
}

# ============================================================================
# Data Sources
# ============================================================================
data "aws_caller_identity" "current" {}
data "aws_region" "current" {}
