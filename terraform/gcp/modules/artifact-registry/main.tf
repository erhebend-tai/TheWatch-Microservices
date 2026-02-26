# Artifact Registry — container image registry for TheWatch
# GCP equivalent of Azure Container Registry / AWS ECR

resource "google_artifact_registry_repository" "main" {
  repository_id = "${var.resource_prefix}-registry"
  location      = var.region
  format        = "DOCKER"
  labels        = var.labels

  cleanup_policies {
    id     = "keep-recent"
    action = "KEEP"

    most_recent_versions {
      keep_count = var.keep_count
    }
  }

  cleanup_policies {
    id     = "delete-old-untagged"
    action = "DELETE"

    condition {
      tag_state  = "UNTAGGED"
      older_than = "${var.untagged_retention_days * 24 * 3600}s"
    }
  }
}

resource "google_artifact_registry_repository_iam_member" "reader" {
  for_each = toset(var.reader_members)

  repository = google_artifact_registry_repository.main.name
  location   = var.region
  role       = "roles/artifactregistry.reader"
  member     = each.value
}

resource "google_artifact_registry_repository_iam_member" "writer" {
  for_each = toset(var.writer_members)

  repository = google_artifact_registry_repository.main.name
  location   = var.region
  role       = "roles/artifactregistry.writer"
  member     = each.value
}
