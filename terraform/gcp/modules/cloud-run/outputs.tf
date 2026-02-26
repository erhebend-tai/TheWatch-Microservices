output "service_urls" {
  description = "Cloud Run service URLs"
  value       = { for k, v in google_cloud_run_v2_service.services : k => v.uri }
}

output "service_names" {
  description = "Cloud Run service names"
  value       = { for k, v in google_cloud_run_v2_service.services : k => v.name }
}
