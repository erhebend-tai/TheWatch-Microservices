variable "resource_prefix" {
  type = string
}

variable "labels" {
  type    = map(string)
  default = {}
}

variable "topics" {
  type = map(object({
    subscriptions = list(string)
  }))
}

variable "message_retention" {
  type    = string
  default = "604800s"
}
