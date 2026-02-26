output "default_domain" {
  value = azurerm_container_app_environment.main.default_domain
}

output "environment_id" {
  value = azurerm_container_app_environment.main.id
}

output "acr_login_server" {
  value = azurerm_container_registry.acr.login_server
}

output "app_fqdns" {
  value = {
    for name, app in azurerm_container_app.apps : name => try(app.ingress[0].fqdn, null)
  }
}
