# Cloud Storage — evidence blobs and file storage for TheWatch
# GCP equivalent of Azure Blob Storage / AWS S3

resource "google_storage_bucket" "main" {
  name     = "${var.resource_prefix}-storage"
  location = var.region
  labels   = var.labels

  storage_class               = var.storage_class
  uniform_bucket_level_access = true

  versioning {
    enabled = var.versioning_enabled
  }

  lifecycle_rule {
    condition {
      age = var.lifecycle_age_days
    }
    action {
      type          = "SetStorageClass"
      storage_class = "NEARLINE"
    }
  }

  lifecycle_rule {
    condition {
      age = var.archive_age_days
    }
    action {
      type          = "SetStorageClass"
      storage_class = "ARCHIVE"
    }
  }

  cors {
    origin          = var.cors_origins
    method          = ["GET", "HEAD", "PUT", "POST"]
    response_header = ["Content-Type", "Content-Disposition"]
    max_age_seconds = 3600
  }
}

resource "google_storage_bucket_iam_member" "object_admin" {
  bucket = google_storage_bucket.main.name
  role   = "roles/storage.objectAdmin"
  member = var.admin_member
}
