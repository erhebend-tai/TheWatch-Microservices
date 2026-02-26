# Cognito User Pool — authentication and user management for TheWatch
# AWS equivalent of GCP Identity Platform / Azure AD B2C

resource "aws_cognito_user_pool" "main" {
  name = "${var.project_name}-user-pool-${var.environment}"

  # MFA enforcement
  mfa_configuration = "ON"

  software_token_mfa_configuration {
    enabled = true
  }

  sms_configuration {
    external_id    = "${var.project_name}-cognito-sms-${var.environment}"
    sns_caller_arn = aws_iam_role.cognito_sms.arn
    sns_region     = var.aws_region
  }

  # Advanced security — compromised credentials detection, adaptive auth
  user_pool_add_ons {
    advanced_security_mode = "ENFORCED"
  }

  # Password policy
  password_policy {
    minimum_length                   = 12
    require_uppercase                = true
    require_lowercase                = true
    require_numbers                  = true
    require_symbols                  = true
    temporary_password_validity_days = 7
  }

  # Account recovery priority
  account_recovery_setting {
    recovery_mechanism {
      name     = "verified_email"
      priority = 1
    }
    recovery_mechanism {
      name     = "verified_phone_number"
      priority = 2
    }
  }

  # Standard schema attributes
  schema {
    name                = "email"
    attribute_data_type = "String"
    required            = true
    mutable             = true

    string_attribute_constraints {
      min_length = 5
      max_length = 256
    }
  }

  schema {
    name                = "phone_number"
    attribute_data_type = "String"
    required            = false
    mutable             = true

    string_attribute_constraints {
      min_length = 0
      max_length = 20
    }
  }

  schema {
    name                = "name"
    attribute_data_type = "String"
    required            = true
    mutable             = true

    string_attribute_constraints {
      min_length = 1
      max_length = 256
    }
  }

  schema {
    name                = "preferred_username"
    attribute_data_type = "String"
    required            = false
    mutable             = true

    string_attribute_constraints {
      min_length = 1
      max_length = 256
    }
  }

  # Custom attributes for TheWatch domain
  schema {
    name                = "watch_role"
    attribute_data_type = "String"
    mutable             = true

    string_attribute_constraints {
      min_length = 1
      max_length = 50
    }
  }

  schema {
    name                = "service_area"
    attribute_data_type = "String"
    mutable             = true

    string_attribute_constraints {
      min_length = 0
      max_length = 256
    }
  }

  schema {
    name                = "badge_level"
    attribute_data_type = "String"
    mutable             = true

    string_attribute_constraints {
      min_length = 0
      max_length = 50
    }
  }

  # Email configuration via SES
  email_configuration {
    email_sending_account  = var.ses_email_arn != "" ? "DEVELOPER" : "COGNITO_DEFAULT"
    source_arn             = var.ses_email_arn != "" ? var.ses_email_arn : null
    reply_to_email_address = var.ses_email_arn != "" ? "noreply@thewatch.app" : null
  }

  # Auto-verify email
  auto_verified_attributes = ["email"]

  tags = {
    Name        = "${var.project_name}-user-pool-${var.environment}"
    Environment = var.environment
  }
}

# IAM role for Cognito to send SMS via SNS
resource "aws_iam_role" "cognito_sms" {
  name = "${var.project_name}-cognito-sms-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "cognito-idp.amazonaws.com"
        }
        Action = "sts:AssumeRole"
        Condition = {
          StringEquals = {
            "sts:ExternalId" = "${var.project_name}-cognito-sms-${var.environment}"
          }
        }
      }
    ]
  })

  tags = {
    Name        = "${var.project_name}-cognito-sms-role-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy" "cognito_sms" {
  name = "${var.project_name}-cognito-sms-policy-${var.environment}"
  role = aws_iam_role.cognito_sms.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = "sns:Publish"
        Resource = "*"
      }
    ]
  })
}

# User Pool Client — MAUI mobile app
resource "aws_cognito_user_pool_client" "maui_app" {
  name         = "${var.project_name}-maui-client-${var.environment}"
  user_pool_id = aws_cognito_user_pool.main.id

  generate_secret                      = false
  explicit_auth_flows                  = [
    "ALLOW_USER_SRP_AUTH",
    "ALLOW_REFRESH_TOKEN_AUTH",
    "ALLOW_USER_PASSWORD_AUTH",
  ]
  supported_identity_providers         = ["COGNITO"]
  callback_urls                        = var.callback_urls
  logout_urls                          = var.logout_urls
  allowed_oauth_flows                  = ["code"]
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_scopes                 = ["email", "openid", "profile"]
  prevent_user_existence_errors        = "ENABLED"

  access_token_validity  = 1
  id_token_validity      = 1
  refresh_token_validity = 30

  token_validity_units {
    access_token  = "hours"
    id_token      = "hours"
    refresh_token = "days"
  }
}

# User Pool Client — Admin Dashboard (Blazor)
resource "aws_cognito_user_pool_client" "admin_dashboard" {
  name         = "${var.project_name}-admin-client-${var.environment}"
  user_pool_id = aws_cognito_user_pool.main.id

  generate_secret                      = true
  explicit_auth_flows                  = [
    "ALLOW_USER_SRP_AUTH",
    "ALLOW_REFRESH_TOKEN_AUTH",
  ]
  supported_identity_providers         = ["COGNITO"]
  callback_urls                        = var.callback_urls
  logout_urls                          = var.logout_urls
  allowed_oauth_flows                  = ["code"]
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_scopes                 = ["email", "openid", "profile"]
  prevent_user_existence_errors        = "ENABLED"

  access_token_validity  = 1
  id_token_validity      = 1
  refresh_token_validity = 7

  token_validity_units {
    access_token  = "hours"
    id_token      = "hours"
    refresh_token = "days"
  }
}

# Hosted UI domain
resource "aws_cognito_user_pool_domain" "main" {
  domain       = "${lower(var.project_name)}-${var.environment}"
  user_pool_id = aws_cognito_user_pool.main.id
}
