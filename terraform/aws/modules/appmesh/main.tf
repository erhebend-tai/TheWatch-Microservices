resource "aws_appmesh_mesh" "main" {
  name = "${var.project_name}-mesh-${var.environment}"

  spec {
    egress_filter {
      type = "ALLOW_ALL"
    }
  }

  tags = {
    Name        = "${var.project_name}-mesh-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_appmesh_virtual_node" "service" {
  for_each = var.services

  name      = "${each.key}-node-${var.environment}"
  mesh_name = aws_appmesh_mesh.main.name

  spec {
    listener {
      port_mapping {
        port     = each.value.port
        protocol = "http"
      }

      # Circuit Breaker Policy (Item 155: 5xx threshold: 5 in 30s)
      outlier_detection {
        max_server_errors      = 5
        interval {
          value = 30
          unit  = "s"
        }
        base_ejection_duration {
          value = 10
          unit  = "s"
        }
        max_ejection_percent = 50
      }
    }

    service_discovery {
      dns {
        hostname = each.value.dns_name
      }
    }

    # mTLS can be configured here if certificates are provided (via ACM or secret)
    # backend {
    #   virtual_service {
    #     virtual_service_name = ...
    #   }
    # }
  }

  tags = {
    Name        = "${each.key}-node-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_appmesh_virtual_service" "service" {
  for_each = var.services

  name      = each.value.dns_name
  mesh_name = aws_appmesh_mesh.main.name

  spec {
    provider {
      virtual_node {
        virtual_node_name = aws_appmesh_virtual_node.service[each.key].name
      }
    }
  }
}

resource "aws_appmesh_virtual_router" "service" {
  for_each = var.services

  name      = "${each.key}-router-${var.environment}"
  mesh_name = aws_appmesh_mesh.main.name

  spec {
    listener {
      port_mapping {
        port     = each.value.port
        protocol = "http"
      }
    }
  }
}

resource "aws_appmesh_route" "service" {
  for_each = var.services

  name                = "${each.key}-route-${var.environment}"
  mesh_name           = aws_appmesh_mesh.main.name
  virtual_router_name = aws_appmesh_virtual_router.service[each.key].name

  spec {
    http_route {
      match {
        prefix = "/"
      }

      action {
        weighted_target {
          virtual_node = aws_appmesh_virtual_node.service[each.key].name
          weight       = 100
        }
      }

      # Retry policy (Item 155: 3 retries, 2s timeout)
      retry_policy {
        http_retry_events = ["server-error", "gateway-error"]
        max_retries      = 3
        per_retry_timeout {
          unit  = "s"
          value = 2
        }
      }
    }
  }
}
