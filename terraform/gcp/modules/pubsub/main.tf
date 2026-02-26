# Pub/Sub — event-driven messaging for TheWatch services
# GCP equivalent of Azure Service Bus / AWS SNS+SQS

resource "google_pubsub_topic" "topics" {
  for_each = var.topics

  name   = "${var.resource_prefix}-${each.key}"
  labels = var.labels

  message_retention_duration = var.message_retention
}

resource "google_pubsub_subscription" "subscriptions" {
  for_each = { for item in local.subscription_list : "${item.topic}-${item.subscriber}" => item }

  name  = "${var.resource_prefix}-${each.value.topic}-${each.value.subscriber}"
  topic = google_pubsub_topic.topics[each.value.topic].id

  labels = var.labels

  ack_deadline_seconds = 30

  retry_policy {
    minimum_backoff = "10s"
    maximum_backoff = "600s"
  }

  dead_letter_policy {
    dead_letter_topic     = google_pubsub_topic.dead_letter.id
    max_delivery_attempts = 5
  }
}

resource "google_pubsub_topic" "dead_letter" {
  name   = "${var.resource_prefix}-dead-letter"
  labels = var.labels
}

locals {
  subscription_list = flatten([
    for topic_key, topic_val in var.topics : [
      for sub in topic_val.subscriptions : {
        topic      = topic_key
        subscriber = sub
      }
    ]
  ])
}
