output "cloud_run_urls" {
  description = "Cloud Run service URLs"
  value       = module.cloud_run.service_urls
}

output "cloud_sql_ip" {
  description = "Cloud SQL private IP"
  value       = module.cloud_sql.private_ip
}

output "alloydb_ip" {
  description = "AlloyDB primary IP"
  value       = module.alloydb.primary_ip
}

output "redis_host" {
  description = "Memorystore Redis host"
  value       = module.memorystore.host
}

output "storage_bucket" {
  description = "Cloud Storage bucket name"
  value       = module.cloud_storage.bucket_name
}

output "artifact_registry_url" {
  description = "Artifact Registry URL"
  value       = module.artifact_registry.repository_url
}

output "vpc_network_id" {
  description = "VPC network ID"
  value       = module.vpc_network.network_id
}
