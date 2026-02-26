resource "aws_apigatewayv2_api" "main" {
  name          = "${var.project_name}-api-${var.environment}"
  protocol_type = "HTTP"

  tags = {
    Name        = "${var.project_name}-api-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_apigatewayv2_stage" "main" {
  api_id      = aws_apigatewayv2_api.main.id
  name        = "$default"
  auto_deploy = true

  default_route_settings {
    throttling_burst_limit   = 10000
    throttling_rate_limit    = 5000
  }

  tags = {
    Name        = "${var.project_name}-api-stage-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_apigatewayv2_authorizer" "cognito" {
  count            = var.cognito_user_pool_id != null ? 1 : 0
  api_id           = aws_apigatewayv2_api.main.id
  authorizer_type  = "JWT"
  identity_sources = ["$request.header.Authorization"]
  name             = "cognito-authorizer"

  jwt_configuration {
    audience = [var.cognito_client_id]
    issuer   = "https://cognito-idp.us-east-1.amazonaws.com/${var.cognito_user_pool_id}"
  }
}

resource "aws_apigatewayv2_integration" "alb" {
  api_id           = aws_apigatewayv2_api.main.id
  integration_type = "HTTP_PROXY"
  integration_uri  = "http://${var.alb_dns_name}/{proxy}"
  integration_method = "ANY"
  payload_format_version = "1.0"
}

resource "aws_apigatewayv2_route" "default" {
  api_id    = aws_apigatewayv2_api.main.id
  route_key = "ANY /{proxy+}"
  target    = "integrations/${aws_apigatewayv2_integration.alb.id}"

  authorization_type = var.cognito_user_pool_id != null ? "JWT" : "NONE"
  authorizer_id      = var.cognito_user_pool_id != null ? aws_apigatewayv2_authorizer.cognito[0].id : null
}
