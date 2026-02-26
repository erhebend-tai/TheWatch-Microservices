# AlloyDB for PostgreSQL — TheWatch Geospatial (PostGIS)
# GCP-native PostgreSQL, equivalent to Azure Database for PostgreSQL

resource "google_alloydb_cluster" "main" {
  cluster_id = "${var.resource_prefix}-alloydb"
  location   = var.region
  labels     = var.labels

  network_config {
    network = var.vpc_network_id
  }

  initial_user {
    user     = var.admin_username
    password = var.admin_password
  }

  automated_backup_policy {
    enabled = true

    weekly_schedule {
      days_of_week = ["SUNDAY"]
      start_times {
        hours = 3
      }
    }

    backup_window    = "3600s"
    quantity_based_retention {
      count = var.backup_retention_count
    }
  }
}

resource "google_alloydb_instance" "primary" {
  cluster       = google_alloydb_cluster.main.name
  instance_id   = "${var.resource_prefix}-alloydb-primary"
  instance_type = "PRIMARY"
  labels        = var.labels

  machine_config {
    cpu_count = var.cpu_count
  }

  database_flags = {
    "shared_preload_libraries" = "postgis"
  }
}

resource "google_alloydb_instance" "read_replica" {
  count = var.read_replicas

  cluster       = google_alloydb_cluster.main.name
  instance_id   = "${var.resource_prefix}-alloydb-replica-${count.index}"
  instance_type = "READ_POOL"
  labels        = var.labels

  machine_config {
    cpu_count = var.cpu_count
  }

  read_pool_config {
    node_count = 1
  }
}
