output "host" {
  value = google_redis_instance.main.host
}

output "port" {
  value = google_redis_instance.main.port
}

output "connection_string" {
  value     = "redis://${google_redis_instance.main.host}:${google_redis_instance.main.port}"
  sensitive = true
}
