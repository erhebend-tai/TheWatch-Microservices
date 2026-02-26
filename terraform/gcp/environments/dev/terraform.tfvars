# TheWatch GCP — Development Environment
# Update project_id to your GCP project before running terraform init

project_id  = "thewatch-dev"
project     = "thewatch"
region      = "us-central1"
environment = "dev"

labels = {
  team = "thewatch"
  cost_center = "development"
}
