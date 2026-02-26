# CodePipeline CI/CD — automated build, test, and deploy for TheWatch microservices
# Full pipeline: Source → Build → Test → Deploy-Staging → Approval → Deploy-Prod

# ---------------------------------------------------------------------------
# S3 Artifact Bucket
# ---------------------------------------------------------------------------
resource "aws_s3_bucket" "artifacts" {
  bucket        = "${lower(var.project_name)}-pipeline-artifacts-${var.environment}"
  force_destroy = true

  tags = {
    Name        = "${var.project_name}-pipeline-artifacts-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_s3_bucket_versioning" "artifacts" {
  bucket = aws_s3_bucket.artifacts.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "artifacts" {
  bucket = aws_s3_bucket.artifacts.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "aws:kms"
    }
  }
}

# ---------------------------------------------------------------------------
# CodeStar Connection — GitHub source
# ---------------------------------------------------------------------------
resource "aws_codestarconnections_connection" "github" {
  name          = "${var.project_name}-github-${var.environment}"
  provider_type = "GitHub"

  tags = {
    Name        = "${var.project_name}-github-connection-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# CodeBuild — Build project (dotnet publish + Docker build)
# ---------------------------------------------------------------------------
resource "aws_codebuild_project" "build" {
  name         = "${var.project_name}-build-${var.environment}"
  description  = "Build project for TheWatch microservices — dotnet publish and Docker image build"
  service_role = aws_iam_role.codebuild_role.arn

  artifacts {
    type = "CODEPIPELINE"
  }

  environment {
    compute_type                = "BUILD_GENERAL1_MEDIUM"
    image                       = "aws/codebuild/amazonlinux2-x86_64-standard:5.0"
    type                        = "LINUX_CONTAINER"
    privileged_mode             = true
    image_pull_credentials_type = "CODEBUILD"

    environment_variable {
      name  = "ENVIRONMENT"
      value = var.environment
    }

    environment_variable {
      name  = "AWS_ACCOUNT_ID"
      value = data.aws_caller_identity.current.account_id
    }
  }

  source {
    type      = "CODEPIPELINE"
    buildspec = <<-BUILDSPEC
      version: 0.2
      phases:
        pre_build:
          commands:
            - echo Logging in to Amazon ECR...
            - aws ecr get-login-password --region $AWS_DEFAULT_REGION | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_DEFAULT_REGION.amazonaws.com
        build:
          commands:
            - echo Building .NET solution...
            - dotnet publish -c Release -o ./publish
            - echo Building Docker images...
            - |
              for svc in p1-coregateway p2-voiceemergency p3-meshnetwork p4-wearable p5-authsecurity p6-firstresponder p7-familyhealth p8-disasterrelief p9-doctorservices p10-gamification geospatial dashboard; do
                docker build -t $AWS_ACCOUNT_ID.dkr.ecr.$AWS_DEFAULT_REGION.amazonaws.com/thewatch-$svc-$ENVIRONMENT:$CODEBUILD_RESOLVED_SOURCE_VERSION -f Dockerfile.$svc .
                docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_DEFAULT_REGION.amazonaws.com/thewatch-$svc-$ENVIRONMENT:$CODEBUILD_RESOLVED_SOURCE_VERSION
              done
        post_build:
          commands:
            - echo Build completed on $(date)
      artifacts:
        files:
          - '**/*'
        base-directory: publish
    BUILDSPEC
  }

  tags = {
    Name        = "${var.project_name}-build-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# CodeBuild — Test project (dotnet test + Trivy scan)
# ---------------------------------------------------------------------------
resource "aws_codebuild_project" "test" {
  name         = "${var.project_name}-test-${var.environment}"
  description  = "Test project for TheWatch microservices — dotnet test and Trivy container scan"
  service_role = aws_iam_role.codebuild_role.arn

  artifacts {
    type = "CODEPIPELINE"
  }

  environment {
    compute_type                = "BUILD_GENERAL1_MEDIUM"
    image                       = "aws/codebuild/amazonlinux2-x86_64-standard:5.0"
    type                        = "LINUX_CONTAINER"
    privileged_mode             = true
    image_pull_credentials_type = "CODEBUILD"

    environment_variable {
      name  = "ENVIRONMENT"
      value = var.environment
    }
  }

  source {
    type      = "CODEPIPELINE"
    buildspec = <<-BUILDSPEC
      version: 0.2
      phases:
        install:
          commands:
            - echo Installing Trivy...
            - curl -sfL https://raw.githubusercontent.com/aquasecurity/trivy/main/contrib/install.sh | sh -s -- -b /usr/local/bin
        build:
          commands:
            - echo Running .NET tests...
            - dotnet test --configuration Release --logger trx --results-directory ./test-results
            - echo Running Trivy container scan...
            - |
              for svc in p1-coregateway p2-voiceemergency p3-meshnetwork p4-wearable p5-authsecurity p6-firstresponder p7-familyhealth p8-disasterrelief p9-doctorservices p10-gamification geospatial dashboard; do
                trivy image --severity HIGH,CRITICAL --exit-code 1 $AWS_ACCOUNT_ID.dkr.ecr.$AWS_DEFAULT_REGION.amazonaws.com/thewatch-$svc-$ENVIRONMENT:latest
              done
        post_build:
          commands:
            - echo Tests completed on $(date)
      reports:
        test-results:
          files:
            - '**/*.trx'
          base-directory: test-results
          file-format: VisualStudioTrx
    BUILDSPEC
  }

  tags = {
    Name        = "${var.project_name}-test-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# CodePipeline — Full CI/CD pipeline
# ---------------------------------------------------------------------------
resource "aws_codepipeline" "main" {
  name     = "${var.project_name}-pipeline-${var.environment}"
  role_arn = aws_iam_role.codepipeline_role.arn

  artifact_store {
    location = aws_s3_bucket.artifacts.bucket
    type     = "S3"
  }

  # Stage 1: Source from GitHub
  stage {
    name = "Source"

    action {
      name             = "GitHub-Source"
      category         = "Source"
      owner            = "AWS"
      provider         = "CodeStarSourceConnection"
      version          = "1"
      output_artifacts = ["source_output"]

      configuration = {
        ConnectionArn    = aws_codestarconnections_connection.github.arn
        FullRepositoryId = var.github_repo
        BranchName       = var.github_branch
      }
    }
  }

  # Stage 2: Build (dotnet publish + Docker build)
  stage {
    name = "Build"

    action {
      name             = "Build"
      category         = "Build"
      owner            = "AWS"
      provider         = "CodeBuild"
      version          = "1"
      input_artifacts  = ["source_output"]
      output_artifacts = ["build_output"]

      configuration = {
        ProjectName = aws_codebuild_project.build.name
      }
    }
  }

  # Stage 3: Test (dotnet test + Trivy scan)
  stage {
    name = "Test"

    action {
      name             = "Test"
      category         = "Test"
      owner            = "AWS"
      provider         = "CodeBuild"
      version          = "1"
      input_artifacts  = ["build_output"]
      output_artifacts = ["test_output"]

      configuration = {
        ProjectName = aws_codebuild_project.test.name
      }
    }
  }

  # Stage 4: Deploy to Staging (ECS rolling update)
  stage {
    name = "Deploy-Staging"

    action {
      name            = "Deploy-Staging"
      category        = "Deploy"
      owner           = "AWS"
      provider        = "ECS"
      version         = "1"
      input_artifacts = ["build_output"]

      configuration = {
        ClusterName = var.ecs_cluster_name
        ServiceName = var.ecs_service_names["staging"]
      }
    }
  }

  # Stage 5: Manual Approval
  stage {
    name = "Manual-Approval"

    action {
      name     = "Approval"
      category = "Approval"
      owner    = "AWS"
      provider = "Manual"
      version  = "1"

      configuration = {
        CustomData = "Review staging deployment for ${var.project_name}. Approve to proceed to production."
      }
    }
  }

  # Stage 6: Deploy to Production (ECS blue/green via CodeDeploy)
  stage {
    name = "Deploy-Prod"

    action {
      name            = "Deploy-Production"
      category        = "Deploy"
      owner           = "AWS"
      provider        = "CodeDeployToECS"
      version         = "1"
      input_artifacts = ["build_output"]

      configuration = {
        ApplicationName                = aws_codedeploy_app.main.name
        DeploymentGroupName            = aws_codedeploy_deployment_group.production.deployment_group_name
        TaskDefinitionTemplateArtifact = "build_output"
        AppSpecTemplateArtifact        = "build_output"
      }
    }
  }

  tags = {
    Name        = "${var.project_name}-pipeline-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# CodeDeploy — Blue/Green deployment for production
# ---------------------------------------------------------------------------
resource "aws_codedeploy_app" "main" {
  compute_platform = "ECS"
  name             = "${var.project_name}-deploy-${var.environment}"

  tags = {
    Name        = "${var.project_name}-codedeploy-app-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_codedeploy_deployment_group" "production" {
  app_name               = aws_codedeploy_app.main.name
  deployment_group_name  = "${var.project_name}-prod-deploy-group-${var.environment}"
  deployment_config_name = "CodeDeployDefault.ECSAllAtOnce"
  service_role_arn       = aws_iam_role.codedeploy_role.arn

  auto_rollback_configuration {
    enabled = true
    events  = ["DEPLOYMENT_FAILURE"]
  }

  blue_green_deployment_config {
    deployment_ready_option {
      action_on_timeout = "CONTINUE_DEPLOYMENT"
    }

    terminate_blue_instances_on_deployment_success {
      action                           = "TERMINATE"
      termination_wait_time_in_minutes = 5
    }
  }

  deployment_style {
    deployment_option = "WITH_TRAFFIC_CONTROL"
    deployment_type   = "BLUE_GREEN"
  }

  ecs_service {
    cluster_name = var.ecs_cluster_name
    service_name = var.ecs_service_names["production"]
  }

  tags = {
    Name        = "${var.project_name}-prod-deploy-group-${var.environment}"
    Environment = var.environment
  }
}

# ---------------------------------------------------------------------------
# Data Sources
# ---------------------------------------------------------------------------
data "aws_caller_identity" "current" {}

# ---------------------------------------------------------------------------
# IAM — CodePipeline Role
# ---------------------------------------------------------------------------
resource "aws_iam_role" "codepipeline_role" {
  name = "${var.project_name}-codepipeline-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "codepipeline.amazonaws.com"
      }
    }]
  })

  tags = {
    Name        = "${var.project_name}-codepipeline-role-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy" "codepipeline_policy" {
  name = "${var.project_name}-codepipeline-policy-${var.environment}"
  role = aws_iam_role.codepipeline_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:GetObjectVersion",
          "s3:GetBucketVersioning",
          "s3:PutObject",
          "s3:PutObjectAcl"
        ]
        Resource = [
          aws_s3_bucket.artifacts.arn,
          "${aws_s3_bucket.artifacts.arn}/*"
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "codestar-connections:UseConnection"
        ]
        Resource = [aws_codestarconnections_connection.github.arn]
      },
      {
        Effect = "Allow"
        Action = [
          "codebuild:BatchGetBuilds",
          "codebuild:StartBuild"
        ]
        Resource = [
          aws_codebuild_project.build.arn,
          aws_codebuild_project.test.arn
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "ecs:DescribeServices",
          "ecs:DescribeTaskDefinition",
          "ecs:DescribeTasks",
          "ecs:ListTasks",
          "ecs:RegisterTaskDefinition",
          "ecs:UpdateService"
        ]
        Resource = "*"
      },
      {
        Effect = "Allow"
        Action = [
          "codedeploy:CreateDeployment",
          "codedeploy:GetApplication",
          "codedeploy:GetApplicationRevision",
          "codedeploy:GetDeployment",
          "codedeploy:GetDeploymentConfig",
          "codedeploy:RegisterApplicationRevision"
        ]
        Resource = "*"
      },
      {
        Effect   = "Allow"
        Action   = "iam:PassRole"
        Resource = "*"
        Condition = {
          StringEqualsIfExists = {
            "iam:PassedToService" = [
              "ecs-tasks.amazonaws.com"
            ]
          }
        }
      }
    ]
  })
}

# ---------------------------------------------------------------------------
# IAM — CodeBuild Role
# ---------------------------------------------------------------------------
resource "aws_iam_role" "codebuild_role" {
  name = "${var.project_name}-codebuild-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "codebuild.amazonaws.com"
      }
    }]
  })

  tags = {
    Name        = "${var.project_name}-codebuild-role-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy" "codebuild_policy" {
  name = "${var.project_name}-codebuild-policy-${var.environment}"
  role = aws_iam_role.codebuild_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:*:*:*"
      },
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:GetObjectVersion",
          "s3:PutObject"
        ]
        Resource = [
          aws_s3_bucket.artifacts.arn,
          "${aws_s3_bucket.artifacts.arn}/*"
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "ecr:GetAuthorizationToken",
          "ecr:BatchCheckLayerAvailability",
          "ecr:GetDownloadUrlForLayer",
          "ecr:BatchGetImage",
          "ecr:PutImage",
          "ecr:InitiateLayerUpload",
          "ecr:UploadLayerPart",
          "ecr:CompleteLayerUpload"
        ]
        Resource = "*"
      }
    ]
  })
}

# ---------------------------------------------------------------------------
# IAM — CodeDeploy Role
# ---------------------------------------------------------------------------
resource "aws_iam_role" "codedeploy_role" {
  name = "${var.project_name}-codedeploy-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "codedeploy.amazonaws.com"
      }
    }]
  })

  tags = {
    Name        = "${var.project_name}-codedeploy-role-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy_attachment" "codedeploy_ecs" {
  role       = aws_iam_role.codedeploy_role.name
  policy_arn = "arn:aws:iam::aws:policy/AWSCodeDeployRoleForECS"
}
