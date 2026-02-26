# Memorystore for Redis — session store and rate limiting
# GCP equivalent of Azure Redis Cache / AWS ElastiCache

resource "google_redis_instance" "main" {
  name           = "${var.resource_prefix}-redis"
  region         = var.region
  labels         = var.labels
  tier           = var.tier
  memory_size_gb = var.memory_size_gb
  redis_version  = var.redis_version

  authorized_network = var.vpc_network_id

  redis_configs = {
    maxmemory-policy = "allkeys-lru"
    notify-keyspace-events = "Ex"
  }

  transit_encryption_mode = "SERVER_AUTHENTICATION"

  maintenance_policy {
    weekly_maintenance_window {
      day = "SUNDAY"
      start_time {
        hours = 3
      }
    }
  }
}
