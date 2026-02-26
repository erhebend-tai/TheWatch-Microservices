# Cloud SQL for SQL Server — TheWatch microservice databases
# GCP equivalent of Azure SQL Database / AWS RDS

resource "google_sql_database_instance" "main" {
  name             = "${var.resource_prefix}-sql"
  database_version = "SQLSERVER_2022_STANDARD"
  region           = var.region

  settings {
    tier              = var.tier
    availability_type = var.high_availability ? "REGIONAL" : "ZONAL"
    disk_size         = var.disk_size_gb
    disk_autoresize   = true

    ip_configuration {
      ipv4_enabled    = false
      private_network = var.vpc_network_id
    }

    backup_configuration {
      enabled                        = true
      point_in_time_recovery_enabled = true
      start_time                     = "03:00"
    }

    database_flags {
      name  = "contained database authentication"
      value = "on"
    }

    user_labels = var.labels
  }

  deletion_protection = var.environment == "production"
}

resource "google_sql_database" "databases" {
  for_each = var.databases

  name     = each.key
  instance = google_sql_database_instance.main.name
}

resource "google_sql_user" "admin" {
  name     = var.admin_username
  instance = google_sql_database_instance.main.name
  password = var.admin_password
}

resource "google_secret_manager_secret_version" "sql_connection" {
  secret      = var.connection_string_secret_id
  secret_data = "Server=${google_sql_database_instance.main.private_ip_address};User Id=${var.admin_username};Password=${var.admin_password};TrustServerCertificate=true"
}
