# ──────────────────────────────────────────────────────────────────────────────
# TheWatch AWS — Development Environment
# ──────────────────────────────────────────────────────────────────────────────

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
    }
  }
}

# ──────────────────────────────────────────────────────────────────────────────
# Locals
# ──────────────────────────────────────────────────────────────────────────────

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
      cpu            = 256
      memory         = 512
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 2
      port           = 8080
      image          = "${module.ecr.repository_urls["p1-coregateway"]}:latest"
      dns_name       = "p1-coregateway"
      path_pattern   = "/api/v1/gateway/*"
    }
    "p2-voiceemergency" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 3
      port           = 8080
      image          = "${module.ecr.repository_urls["p2-voiceemergency"]}:latest"
      dns_name       = "p2-voiceemergency"
      path_pattern   = "/api/v1/voice/*"
    }
    "p3-meshnetwork" = {
      cpu            = 256
      memory         = 512
      desired_count  = 1
      min_capacity   = 0
      max_capacity   = 2
      port           = 8080
      image          = "${module.ecr.repository_urls["p3-meshnetwork"]}:latest"
      dns_name       = "p3-meshnetwork"
      path_pattern   = "/api/v1/mesh/*"
    }
    "p4-wearable" = {
      cpu            = 256
      memory         = 512
      desired_count  = 1
      min_capacity   = 0
      max_capacity   = 2
      port           = 8080
      image          = "${module.ecr.repository_urls["p4-wearable"]}:latest"
      dns_name       = "p4-wearable"
      path_pattern   = "/api/v1/wearable/*"
    }
    "p5-authsecurity" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 2
      port           = 8080
      image          = "${module.ecr.repository_urls["p5-authsecurity"]}:latest"
      dns_name       = "p5-authsecurity"
      path_pattern   = "/api/v1/auth/*"
    }
    "p6-firstresponder" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 1
      max_capacity   = 3
      port           = 8080
      image          = "${module.ecr.repository_urls["p6-firstresponder"]}:latest"
      dns_name       = "p6-firstresponder"
      path_pattern   = "/api/v1/responder/*"
    }
    "p7-familyhealth" = {
      cpu            = 256
      memory         = 512
      desired_count  = 1
      min_capacity   = 0
      max_capacity   = 2
      port           = 8080
      image          = "${module.ecr.repository_urls["p7-familyhealth"]}:latest"
      dns_name       = "p7-familyhealth"
      path_pattern   = "/api/v1/family/*"
    }
    "p8-disasterrelief" = {
      cpu            = 256
      memory         = 512
      desired_count  = 1
      min_capacity   = 0
      max_capacity   = 2
      port           = 8080
      image          = "${module.ecr.repository_urls["p8-disasterrelief"]}:latest"
      dns_name       = "p8-disasterrelief"
      path_pattern   = "/api/v1/disaster/*"
    }
    "p9-doctorservices" = {
      cpu            = 256
      memory         = 512
      desired_count  = 1
      min_capacity   = 0
      max_capacity   = 2
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
      max_capacity   = 1
      port           = 8080
      image          = "${module.ecr.repository_urls["p10-gamification"]}:latest"
      dns_name       = "p10-gamification"
      path_pattern   = "/api/v1/gamification/*"
    }
    "p11-surveillance" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 0
      max_capacity   = 3
      port           = 8080
      image          = "${module.ecr.repository_urls["p11-surveillance"]}:latest"
      dns_name       = "p11-surveillance"
      path_pattern   = "/api/v1/surveillance/*"
    }
    "geospatial" = {
      cpu            = 512
      memory         = 1024
      desired_count  = 1
      min_capacity   = 0
      max_capacity   = 2
      port           = 8080
      image          = "${module.ecr.repository_urls["geospatial"]}:latest"
      dns_name       = "geospatial"
      path_pattern   = "/api/v1/geo/*"
    }
    "dashboard" = {
      cpu            = 256
      memory         = 512
      desired_count  = 1
      min_capacity   = 0
      max_capacity   = 2
      port           = 8080
      image          = "${module.ecr.repository_urls["dashboard"]}:latest"
      dns_name       = "dashboard"
      path_pattern   = "/dashboard/*"
    }
  }

  kafka_topics = {
    "incident-created"   = { partitions = 6, replication_factor = 3 }
    "dispatch-requested" = { partitions = 6, replication_factor = 3 }
    "responder-located"  = { partitions = 6, replication_factor = 3 }
    "checkin-completed"  = { partitions = 3, replication_factor = 3 }
    "vital-alert"        = { partitions = 6, replication_factor = 3 }
    "evidence-uploaded"  = { partitions = 3, replication_factor = 3 }
    "disaster-declared"      = { partitions = 3, replication_factor = 3 }
    "footage-submitted"      = { partitions = 3, replication_factor = 3 }
    "crime-location-reported" = { partitions = 3, replication_factor = 3 }
    "mesh-broadcast"         = { partitions = 6, replication_factor = 3 }
    "dead-letter"            = { partitions = 3, replication_factor = 3 }
  }

  # ALB routing map — port + path per service
  alb_services = {
    for k, v in local.ecs_services : k => {
      port         = v.port
      path_pattern = v.path_pattern
    }
  }
}

# ──────────────────────────────────────────────────────────────────────────────
# Random Passwords
# ──────────────────────────────────────────────────────────────────────────────

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

# ──────────────────────────────────────────────────────────────────────────────
# 1. VPC
# ──────────────────────────────────────────────────────────────────────────────

module "vpc" {
  source = "../../modules/vpc"

  project_name         = var.project_name
  environment          = var.environment
  vpc_cidr             = "10.0.0.0/16"
  public_subnet_cidrs  = ["10.0.1.0/24", "10.0.2.0/24", "10.0.3.0/24"]
  private_subnet_cidrs = ["10.0.10.0/24", "10.0.20.0/24", "10.0.30.0/24"]
}

# ──────────────────────────────────────────────────────────────────────────────
# 2. KMS
# ──────────────────────────────────────────────────────────────────────────────

data "aws_caller_identity" "current" {}

module "kms" {
  source = "../../modules/kms"

  project_name     = var.project_name
  environment      = var.environment
  aws_region       = var.aws_region
  admin_role_arns  = ["arn:aws:iam::${data.aws_caller_identity.current.account_id}:root"]
  service_role_arns = ["arn:aws:iam::${data.aws_caller_identity.current.account_id}:root"]
}

# ──────────────────────────────────────────────────────────────────────────────
# 3. ECR
# ──────────────────────────────────────────────────────────────────────────────

module "ecr" {
  source = "../../modules/ecr"

  project_name = var.project_name
  environment  = var.environment
  services     = local.services
  kms_key_arn  = module.kms.data_key_arn

  depends_on = [module.kms]
}

# ──────────────────────────────────────────────────────────────────────────────
# 4. RDS SQL Server
# ──────────────────────────────────────────────────────────────────────────────

module "rds_sqlserver" {
  source = "../../modules/rds-sqlserver"

  project_name           = var.project_name
  environment            = var.environment
  instance_class         = var.rds_instance_class
  db_name                = "WatchCoreDB"
  username               = "watchadmin"
  password               = random_password.sql_admin.result
  subnet_ids             = module.vpc.private_subnet_ids
  vpc_security_group_ids = [aws_security_group.rds.id]

  depends_on = [module.vpc, module.kms]
}

# ──────────────────────────────────────────────────────────────────────────────
# 5. Aurora PostgreSQL (PostGIS)
# ──────────────────────────────────────────────────────────────────────────────

module "aurora_postgres" {
  source = "../../modules/aurora-postgres"

  project_name           = var.project_name
  environment            = var.environment
  db_name                = "watchgeodb"
  master_username        = "pgadmin"
  master_password        = random_password.pg_admin.result
  instance_class         = "db.r6g.large"
  replica_count          = 1
  subnet_ids             = module.vpc.private_subnet_ids
  vpc_security_group_ids = [aws_security_group.aurora.id]

  depends_on = [module.vpc, module.kms]
}

# ──────────────────────────────────────────────────────────────────────────────
# 6. ElastiCache Redis
# ──────────────────────────────────────────────────────────────────────────────

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

# ──────────────────────────────────────────────────────────────────────────────
# 7. MSK (Kafka)
# ──────────────────────────────────────────────────────────────────────────────

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

# ──────────────────────────────────────────────────────────────────────────────
# 8. S3
# ──────────────────────────────────────────────────────────────────────────────

module "s3" {
  source = "../../modules/s3"

  project_name = var.project_name
  environment  = var.environment
  aws_region   = var.aws_region
  kms_key_arn  = module.kms.evidence_key_arn

  depends_on = [module.kms]
}

# ──────────────────────────────────────────────────────────────────────────────
# 9. DynamoDB
# ──────────────────────────────────────────────────────────────────────────────

module "dynamodb" {
  source = "../../modules/dynamodb"

  project_name = var.project_name
  environment  = var.environment
  aws_region   = var.aws_region
  kms_key_arn  = module.kms.data_key_arn

  depends_on = [module.kms]
}

# ──────────────────────────────────────────────────────────────────────────────
# 10. S3 Events (evidence processing pipeline)
# ──────────────────────────────────────────────────────────────────────────────

module "s3_events" {
  source = "../../modules/s3-events"

  project_name        = var.project_name
  environment         = var.environment
  aws_region          = var.aws_region
  evidence_bucket_id  = module.s3.evidence_bucket_id
  evidence_bucket_arn = module.s3.evidence_bucket_arn
  dynamodb_table_name = module.dynamodb.table_name
  kms_key_arn         = module.kms.evidence_key_arn

  depends_on = [module.s3, module.dynamodb]
}

# ──────────────────────────────────────────────────────────────────────────────
# 11. Cognito
# ──────────────────────────────────────────────────────────────────────────────

module "cognito" {
  source = "../../modules/cognito"

  project_name  = var.project_name
  environment   = var.environment
  aws_region    = var.aws_region
  callback_urls = var.callback_urls
  logout_urls   = var.logout_urls
}

# ──────────────────────────────────────────────────────────────────────────────
# 12. IAM
# ──────────────────────────────────────────────────────────────────────────────

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

# ──────────────────────────────────────────────────────────────────────────────
# 13. ECS (Fargate)
# ──────────────────────────────────────────────────────────────────────────────

module "ecs" {
  source = "../../modules/ecs"

  project_name       = var.project_name
  environment        = var.environment
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids
  services           = local.ecs_services

  depends_on = [module.vpc, module.ecr, module.kms]
}

# ──────────────────────────────────────────────────────────────────────────────
# 14. ALB
# ──────────────────────────────────────────────────────────────────────────────

module "alb" {
  source = "../../modules/alb"

  project_name      = var.project_name
  environment       = var.environment
  vpc_id            = module.vpc.vpc_id
  public_subnet_ids = module.vpc.public_subnet_ids
  services          = local.alb_services

  depends_on = [module.ecs]
}

# ──────────────────────────────────────────────────────────────────────────────
# 15. WAF
# ──────────────────────────────────────────────────────────────────────────────

resource "aws_cloudwatch_log_group" "waf_logs" {
  name              = "aws-waf-logs-${var.project_name}-${var.environment}"
  retention_in_days = 90

  tags = {
    Name        = "${var.project_name}-waf-logs-${var.environment}"
    Environment = var.environment
  }
}

module "waf" {
  source = "../../modules/waf"

  project_name        = var.project_name
  environment         = var.environment
  aws_region          = var.aws_region
  alb_arn             = module.alb.alb_arn
  log_destination_arn = aws_cloudwatch_log_group.waf_logs.arn

  depends_on = [module.alb]
}

# ──────────────────────────────────────────────────────────────────────────────
# 16. CloudWatch
# ──────────────────────────────────────────────────────────────────────────────

resource "aws_sns_topic" "alarm_notifications" {
  name = "${var.project_name}-alarm-notifications-${var.environment}"

  tags = {
    Name        = "${var.project_name}-alarm-notifications-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_sns_topic_subscription" "alarm_email" {
  topic_arn = aws_sns_topic.alarm_notifications.arn
  protocol  = "email"
  endpoint  = var.alert_email
}

module "cloudwatch" {
  source = "../../modules/cloudwatch"

  project_name        = var.project_name
  environment         = var.environment
  aws_region          = var.aws_region
  services            = local.services
  alarm_sns_topic_arn = aws_sns_topic.alarm_notifications.arn

  depends_on = [module.ecs]
}

# ──────────────────────────────────────────────────────────────────────────────
# 17. X-Ray
# ──────────────────────────────────────────────────────────────────────────────

module "xray" {
  source = "../../modules/xray"

  project_name = var.project_name
  environment  = var.environment
  services     = local.services
}

# ──────────────────────────────────────────────────────────────────────────────
# 18. GuardDuty
# ──────────────────────────────────────────────────────────────────────────────

module "guardduty" {
  source = "../../modules/guardduty"

  project_name = var.project_name
  environment  = var.environment
  aws_region   = var.aws_region
  alert_email  = var.alert_email
}

# ──────────────────────────────────────────────────────────────────────────────
# 19. Security Hub
# ──────────────────────────────────────────────────────────────────────────────

module "security_hub" {
  source = "../../modules/security-hub"

  project_name = var.project_name
  environment  = var.environment
  aws_region   = var.aws_region

  depends_on = [module.guardduty]
}

# ──────────────────────────────────────────────────────────────────────────────
# 20. AWS Backup
# ──────────────────────────────────────────────────────────────────────────────

module "backup" {
  source = "../../modules/backup"

  project_name = var.project_name
  environment  = var.environment
  kms_key_arn  = module.kms.data_key_arn

  depends_on = [module.kms, module.rds_sqlserver, module.aurora_postgres, module.dynamodb]
}

# ──────────────────────────────────────────────────────────────────────────────
# Shared Security Groups (for database-layer resources)
# ──────────────────────────────────────────────────────────────────────────────

resource "aws_security_group" "rds" {
  name        = "${var.project_name}-rds-sg-${var.environment}"
  description = "Security group for RDS SQL Server"
  vpc_id      = module.vpc.vpc_id

  ingress {
    description = "SQL Server from private subnets"
    from_port   = 1433
    to_port     = 1433
    protocol    = "tcp"
    cidr_blocks = ["10.0.10.0/24", "10.0.20.0/24", "10.0.30.0/24"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-rds-sg-${var.environment}"
    Environment = var.environment
  }

  depends_on = [module.vpc]
}

resource "aws_security_group" "aurora" {
  name        = "${var.project_name}-aurora-sg-${var.environment}"
  description = "Security group for Aurora PostgreSQL"
  vpc_id      = module.vpc.vpc_id

  ingress {
    description = "PostgreSQL from private subnets"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["10.0.10.0/24", "10.0.20.0/24", "10.0.30.0/24"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-aurora-sg-${var.environment}"
    Environment = var.environment
  }

  depends_on = [module.vpc]
}
