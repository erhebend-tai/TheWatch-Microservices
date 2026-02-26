resource "aws_globalaccelerator_accelerator" "main" {
  name            = "${var.project_name}-ga-${var.environment}"
  ip_address_type = "IPV4"
  enabled         = true

  tags = {
    Name        = "${var.project_name}-ga-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_globalaccelerator_listener" "main" {
  accelerator_arn = aws_globalaccelerator_accelerator.main.id
  client_affinity = "NONE"
  protocol        = "TCP"

  port_range {
    from_port = 80
    to_port   = 80
  }

  port_range {
    from_port = 443
    to_port   = 443
  }
}

resource "aws_globalaccelerator_endpoint_group" "main" {
  listener_arn = aws_globalaccelerator_listener.main.id

  endpoint_configuration {
    endpoint_id = var.alb_arn
    weight      = 100
  }

  health_check_port             = 80
  health_check_protocol         = "HTTP"
  health_check_path             = "/health"
  health_check_interval_seconds = 30
  threshold_count               = 3
}
