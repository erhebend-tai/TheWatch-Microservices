# TheWatch GCP — Production Environment
# Update project_id to your GCP project before running terraform init

project_id  = "thewatch-production"
project     = "thewatch"
region      = "us-central1"
environment = "production"

labels = {
  team = "thewatch"
  cost_center = "production"
}
