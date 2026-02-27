terraform {
  required_version = ">= 1.5"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

locals {
  resource_prefix = "${var.project}-${var.environment}"
  common_labels = merge(var.labels, {
    project     = "thewatch"
    environment = var.environment
    managed_by  = "terraform"
  })

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

  pubsub_topics = {
    "incident-created"        = { subscriptions = ["p6-firstresponder", "p3-meshnetwork", "dashboard"] }
    "dispatch-requested"      = { subscriptions = ["p3-meshnetwork", "p6-firstresponder"] }
    "responder-located"       = { subscriptions = ["p2-voiceemergency", "dashboard"] }
    "checkin-completed"       = { subscriptions = ["p7-familyhealth", "dashboard"] }
    "vital-alert"             = { subscriptions = ["p7-familyhealth", "p9-doctorservices"] }
    "evidence-uploaded"       = { subscriptions = ["p2-voiceemergency"] }
    "disaster-declared"       = { subscriptions = ["p8-disasterrelief", "p6-firstresponder", "dashboard"] }
    "footage-submitted"       = { subscriptions = ["p11-surveillance", "p2-voiceemergency"] }
    "crime-location-reported" = { subscriptions = ["p11-surveillance", "p6-firstresponder"] }
    "dead-letter"             = { subscriptions = ["monitoring"] }
  }

  container_services = {
    "p1-coregateway"    = { cpu = "1", memory = "512Mi", min_instances = 1, max_instances = 3,  image_tag = "latest", env_vars = {}, public = false }
    "p2-voiceemergency" = { cpu = "2", memory = "1Gi",   min_instances = 2, max_instances = 10, image_tag = "latest", env_vars = {}, public = false }
    "p3-meshnetwork"    = { cpu = "1", memory = "512Mi", min_instances = 1, max_instances = 5,  image_tag = "latest", env_vars = {}, public = false }
    "p4-wearable"       = { cpu = "1", memory = "512Mi", min_instances = 1, max_instances = 3,  image_tag = "latest", env_vars = {}, public = false }
    "p5-authsecurity"   = { cpu = "2", memory = "1Gi",   min_instances = 1, max_instances = 5,  image_tag = "latest", env_vars = {}, public = false }
    "p6-firstresponder" = { cpu = "2", memory = "1Gi",   min_instances = 2, max_instances = 10, image_tag = "latest", env_vars = {}, public = false }
    "p7-familyhealth"   = { cpu = "1", memory = "512Mi", min_instances = 1, max_instances = 5,  image_tag = "latest", env_vars = {}, public = false }
    "p8-disasterrelief" = { cpu = "1", memory = "512Mi", min_instances = 1, max_instances = 5,  image_tag = "latest", env_vars = {}, public = false }
    "p9-doctorservices" = { cpu = "1", memory = "512Mi", min_instances = 1, max_instances = 3,  image_tag = "latest", env_vars = {}, public = false }
    "p10-gamification"  = { cpu = "1", memory = "512Mi", min_instances = 0, max_instances = 2,  image_tag = "latest", env_vars = {}, public = false }
    "p11-surveillance"  = { cpu = "2", memory = "1Gi",   min_instances = 1, max_instances = 5,  image_tag = "latest", env_vars = {}, public = false }
    "p12-notifications" = { cpu = "0.5", memory = "512Mi", min_instances = 0, max_instances = 3,  image_tag = "latest", env_vars = {}, public = false }
    "geospatial"        = { cpu = "2", memory = "1Gi",   min_instances = 1, max_instances = 5,  image_tag = "latest", env_vars = {}, public = false }
    "dashboard"         = { cpu = "1", memory = "512Mi", min_instances = 1, max_instances = 3,  image_tag = "latest", env_vars = {}, public = true }
  }
}

module "vpc_network" {
  source = "../../modules/vpc-network"

  resource_prefix = local.resource_prefix
  region          = var.region
}

module "secret_manager" {
  source = "../../modules/secret-manager"

  resource_prefix = local.resource_prefix
  labels          = local.common_labels
  secrets         = {}
}

module "artifact_registry" {
  source = "../../modules/artifact-registry"

  resource_prefix = local.resource_prefix
  region          = var.region
  labels          = local.common_labels
}

module "cloud_sql" {
  source = "../../modules/cloud-sql"

  resource_prefix             = local.resource_prefix
  region                      = var.region
  labels                      = local.common_labels
  environment                 = var.environment
  vpc_network_id              = module.vpc_network.network_id
  admin_password              = random_password.sql_admin.result
  databases                   = local.databases
  tier                        = "db-custom-4-16384"
  connection_string_secret_id = ""

  depends_on = [module.vpc_network]
}

module "alloydb" {
  source = "../../modules/alloydb"

  resource_prefix = local.resource_prefix
  region          = var.region
  labels          = local.common_labels
  vpc_network_id  = module.vpc_network.network_id
  admin_password  = random_password.pg_admin.result
  cpu_count       = 4

  depends_on = [module.vpc_network]
}

module "memorystore" {
  source = "../../modules/memorystore"

  resource_prefix = local.resource_prefix
  region          = var.region
  labels          = local.common_labels
  vpc_network_id  = module.vpc_network.network_id
  memory_size_gb  = 2
  tier            = "STANDARD_HA"

  depends_on = [module.vpc_network]
}

module "pubsub" {
  source = "../../modules/pubsub"

  resource_prefix = local.resource_prefix
  labels          = local.common_labels
  topics          = local.pubsub_topics
}

module "cloud_storage" {
  source = "../../modules/cloud-storage"

  resource_prefix = local.resource_prefix
  region          = var.region
  labels          = local.common_labels
  admin_member    = "serviceAccount:${var.project_id}@appspot.gserviceaccount.com"
}

module "cloud_run" {
  source = "../../modules/cloud-run"

  resource_prefix       = local.resource_prefix
  region                = var.region
  labels                = local.common_labels
  artifact_registry_url = module.artifact_registry.repository_url
  service_account_email = "${var.project_id}@appspot.gserviceaccount.com"
  services              = local.container_services

  depends_on = [module.artifact_registry]
}

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
