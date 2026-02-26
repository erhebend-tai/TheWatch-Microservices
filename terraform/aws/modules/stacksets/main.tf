# CloudFormation StackSets — multi-account deployment for TheWatch
# StackSets for cross-account governance and Service Control Policies

# ---------------------------------------------------------------------------
# CloudFormation StackSet — multi-account baseline
# ---------------------------------------------------------------------------
resource "aws_cloudformation_stack_set" "multi_account" {
  name             = "${var.project_name}-baseline-${var.environment}"
  description      = "TheWatch multi-account baseline configuration for ${var.environment}"
  permission_model = "SERVICE_MANAGED"

  auto_deployment {
    enabled                          = true
    retain_stacks_on_account_removal = false
  }

  template_body = jsonencode({
    AWSTemplateFormatVersion = "2010-09-09"
    Description              = "TheWatch baseline stack for account-level configuration"

    Resources = {
      CloudTrailBucket = {
        Type = "AWS::S3::Bucket"
        Properties = {
          BucketName = "thewatch-cloudtrail-${var.environment}-$${AWS::AccountId}"
          BucketEncryption = {
            ServerSideEncryptionConfiguration = [{
              ServerSideEncryptionByDefault = {
                SSEAlgorithm = "aws:kms"
              }
            }]
          }
          Tags = [
            { Key = "Name", Value = "${var.project_name}-cloudtrail-${var.environment}" },
            { Key = "Environment", Value = var.environment }
          ]
        }
      }

      ConfigRecorder = {
        Type = "AWS::Config::ConfigurationRecorder"
        Properties = {
          Name = "thewatch-config-recorder"
          RecordingGroup = {
            AllSupported = true
          }
          RoleARN = { "Fn::GetAtt" = ["ConfigRole", "Arn"] }
        }
      }

      ConfigRole = {
        Type = "AWS::IAM::Role"
        Properties = {
          RoleName = "thewatch-config-role-${var.environment}"
          AssumeRolePolicyDocument = {
            Version = "2012-10-17"
            Statement = [{
              Effect    = "Allow"
              Principal = { Service = "config.amazonaws.com" }
              Action    = "sts:AssumeRole"
            }]
          }
          ManagedPolicyArns = [
            "arn:aws:iam::aws:policy/service-role/AWS_ConfigRole"
          ]
        }
      }
    }
  })

  tags = {
    Name        = "${var.project_name}-stackset-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# StackSet Instance — deploy to all target accounts
# ---------------------------------------------------------------------------
resource "aws_cloudformation_stack_set_instance" "accounts" {
  for_each = var.account_ids

  stack_set_name = aws_cloudformation_stack_set.multi_account.name
  account_id     = each.value
  region         = var.allowed_regions[0]
}

# ---------------------------------------------------------------------------
# Service Control Policy — restrict regions
# ---------------------------------------------------------------------------
resource "aws_organizations_policy" "region_restriction" {
  name        = "${var.project_name}-region-restriction-${var.environment}"
  description = "Restrict AWS resource creation to allowed regions for TheWatch"
  type        = "SERVICE_CONTROL_POLICY"

  content = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "DenyOutsideAllowedRegions"
        Effect    = "Deny"
        NotAction = [
          "a4b:*",
          "budgets:*",
          "ce:*",
          "chime:*",
          "cloudfront:*",
          "cur:*",
          "globalaccelerator:*",
          "health:*",
          "iam:*",
          "importexport:*",
          "organizations:*",
          "route53:*",
          "route53domains:*",
          "s3:GetBucketLocation",
          "s3:ListAllMyBuckets",
          "sts:*",
          "support:*",
          "trustedadvisor:*",
          "waf:*"
        ]
        Resource = "*"
        Condition = {
          StringNotEquals = {
            "aws:RequestedRegion" = var.allowed_regions
          }
        }
      }
    ]
  })

  tags = {
    Name        = "${var.project_name}-region-scp-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# SCP Attachment — attach to each account
# ---------------------------------------------------------------------------
resource "aws_organizations_policy_attachment" "region_restriction" {
  for_each = var.account_ids

  policy_id = aws_organizations_policy.region_restriction.id
  target_id = each.value
}
