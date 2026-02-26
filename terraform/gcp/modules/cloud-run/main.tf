# Cloud Run services for TheWatch microservices
# GCP equivalent of Azure Container Apps / AWS ECS Fargate

resource "google_cloud_run_v2_service" "services" {
  for_each = var.services

  name     = "${var.resource_prefix}-${each.key}"
  location = var.region
  labels   = var.labels

  template {
    containers {
      image = "${var.artifact_registry_url}/${each.key}:${each.value.image_tag}"

      resources {
        limits = {
          cpu    = each.value.cpu
          memory = each.value.memory
        }
      }

      dynamic "env" {
        for_each = each.value.env_vars
        content {
          name  = env.key
          value = env.value
        }
      }

      ports {
        container_port = 8080
      }
    }

    scaling {
      min_instance_count = each.value.min_instances
      max_instance_count = each.value.max_instances
    }

    service_account = var.service_account_email
  }

  traffic {
    type    = "TRAFFIC_TARGET_ALLOCATION_TYPE_LATEST"
    percent = 100
  }
}

resource "google_cloud_run_v2_service_iam_member" "public" {
  for_each = { for k, v in var.services : k => v if v.public }

  location = var.region
  name     = google_cloud_run_v2_service.services[each.key].name
  role     = "roles/run.invoker"
  member   = "allUsers"
}
