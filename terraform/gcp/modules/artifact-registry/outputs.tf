output "repository_id" {
  value = google_artifact_registry_repository.main.repository_id
}

output "repository_url" {
  value = "${var.region}-docker.pkg.dev/${google_artifact_registry_repository.main.project}/${google_artifact_registry_repository.main.repository_id}"
}

output "repository_name" {
  value = google_artifact_registry_repository.main.name
}
