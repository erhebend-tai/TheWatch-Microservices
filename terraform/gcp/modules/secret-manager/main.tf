# Secret Manager — secrets and configuration for TheWatch
# GCP equivalent of Azure Key Vault / AWS Secrets Manager

resource "google_secret_manager_secret" "secrets" {
  for_each = var.secrets

  secret_id = "${var.resource_prefix}-${each.key}"
  labels    = var.labels

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "versions" {
  for_each = { for k, v in var.secrets : k => v if v.initial_value != null }

  secret      = google_secret_manager_secret.secrets[each.key].id
  secret_data = each.value.initial_value
}

resource "google_secret_manager_secret_iam_member" "accessors" {
  for_each = { for item in local.accessor_list : "${item.secret}-${item.member}" => item }

  secret_id = google_secret_manager_secret.secrets[each.value.secret].secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = each.value.member
}

locals {
  accessor_list = flatten([
    for secret_key, secret_val in var.secrets : [
      for member in secret_val.accessors : {
        secret = secret_key
        member = member
      }
    ]
  ])
}
