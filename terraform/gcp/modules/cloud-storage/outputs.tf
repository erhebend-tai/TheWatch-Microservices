output "bucket_name" {
  value = google_storage_bucket.main.name
}

output "bucket_url" {
  value = google_storage_bucket.main.url
}

output "bucket_self_link" {
  value = google_storage_bucket.main.self_link
}
