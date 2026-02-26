output "cluster_id" {
  value = google_alloydb_cluster.main.cluster_id
}

output "primary_ip" {
  value = google_alloydb_instance.primary.ip_address
}

output "cluster_name" {
  value = google_alloydb_cluster.main.name
}
