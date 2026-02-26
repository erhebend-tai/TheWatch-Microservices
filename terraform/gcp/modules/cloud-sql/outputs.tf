output "instance_name" {
  value = google_sql_database_instance.main.name
}

output "private_ip" {
  value = google_sql_database_instance.main.private_ip_address
}

output "connection_name" {
  value = google_sql_database_instance.main.connection_name
}

output "database_names" {
  value = [for db in google_sql_database.databases : db.name]
}
