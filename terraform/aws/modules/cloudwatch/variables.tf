variable "environment" {
  description = "Deployment environment (dev, staging, production)"
  type        = string
}

variable "project_name" {
  description = "Project name used in resource naming"
  type        = string
  default     = "TheWatch"
}

variable "aws_region" {
  description = "AWS region for resource deployment"
  type        = string
  default     = "us-east-1"
}

variable "services" {
  description = "List of microservice names to create log groups and alarms for"
  type        = list(string)
  default = [
    "p1-coregateway",
    "p2-voiceemergency",
    "p3-meshnetwork",
    "p4-wearable",
    "p5-authsecurity",
    "p6-firstresponder",
    "p7-familyhealth",
    "p8-disasterrelief",
    "p9-doctorservices",
    "p10-gamification",
    "geospatial",
    "dashboard"
  ]
}

variable "alarm_sns_topic_arn" {
  description = "ARN of the SNS topic for alarm notifications"
  type        = string
}
