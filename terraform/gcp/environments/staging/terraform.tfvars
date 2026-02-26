# TheWatch GCP — Staging Environment
# Update project_id to your GCP project before running terraform init

project_id  = "thewatch-staging"
project     = "thewatch"
region      = "us-central1"
environment = "staging"

labels = {
  team = "thewatch"
  cost_center = "staging"
}
