# ============================================================
# Azure Storage Account Module
# Item 127: Evidence blobs, disaster media, document storage
# ============================================================

resource "azurerm_storage_account" "main" {
  name                     = replace("${var.resource_prefix}stor", "-", "")
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = var.replication_type
  account_kind             = "StorageV2"

  min_tls_version                = "TLS1_2"
  allow_nested_items_to_be_public = false
  https_traffic_only_enabled      = true

  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = 30
    }

    container_delete_retention_policy {
      days = 30
    }
  }

  tags = var.tags
}

# Evidence container — SOS audio, video, photos
resource "azurerm_storage_container" "evidence" {
  name                  = "evidence"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# Disaster media — relief coordination assets
resource "azurerm_storage_container" "disaster_media" {
  name                  = "disaster-media"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# User documents — health records, ID verification
resource "azurerm_storage_container" "user_documents" {
  name                  = "user-documents"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# Audit logs — exported logs and reports
resource "azurerm_storage_container" "audit_logs" {
  name                  = "audit-logs"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# Lifecycle management — tier down old evidence
resource "azurerm_storage_management_policy" "lifecycle" {
  storage_account_id = azurerm_storage_account.main.id

  rule {
    name    = "evidence-lifecycle"
    enabled = true

    filters {
      prefix_match = ["evidence/"]
      blob_types   = ["blockBlob"]
    }

    actions {
      base_blob {
        tier_to_cool_after_days_since_modification_greater_than    = 30
        tier_to_archive_after_days_since_modification_greater_than = 90
        delete_after_days_since_modification_greater_than          = 730
      }
    }
  }

  rule {
    name    = "audit-lifecycle"
    enabled = true

    filters {
      prefix_match = ["audit-logs/"]
      blob_types   = ["blockBlob"]
    }

    actions {
      base_blob {
        tier_to_cool_after_days_since_modification_greater_than    = 90
        tier_to_archive_after_days_since_modification_greater_than = 365
      }
    }
  }

  rule {
    name    = "disaster-media-lifecycle"
    enabled = true

    filters {
      prefix_match = ["disaster-media/"]
      blob_types   = ["blockBlob"]
    }

    actions {
      base_blob {
        tier_to_cool_after_days_since_modification_greater_than = 60
        tier_to_archive_after_days_since_modification_greater_than = 180
      }
    }
  }
}

# Store connection string in Key Vault
resource "azurerm_key_vault_secret" "storage_connection" {
  name         = "storage-connection-string"
  value        = azurerm_storage_account.main.primary_connection_string
  key_vault_id = var.key_vault_id

  tags = var.tags
}
