resource "aws_service_discovery_private_dns_namespace" "main" {
  name        = "${var.project_name}.local"
  description = "Private DNS namespace for TheWatch services"
  vpc         = var.vpc_id
}

resource "aws_ecs_cluster" "main" {
  name = "${var.project_name}-cluster-${var.environment}"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }

  tags = {
    Name        = "${var.project_name}-cluster-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role" "ecs_task_execution_role" {
  name = "${var.project_name}-ecs-task-execution-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_policy" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_iam_role" "ecs_task_role" {
  name = "${var.project_name}-ecs-task-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })
}

# ECS task definition per service
resource "aws_ecs_task_definition" "service" {
  for_each = var.services

  family                   = "${var.project_name}-${each.key}-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = each.value.cpu
  memory                   = each.value.memory
  execution_role_arn       = aws_iam_role.ecs_task_execution_role.arn
  task_role_arn            = aws_iam_role.ecs_task_role.arn

  container_definitions = jsonencode([
    {
      name      = each.key
      image     = each.value.image
      essential = true
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = "/ecs/${var.project_name}/${each.key}-${var.environment}"
          "awslogs-region"        = "us-east-1"
          "awslogs-stream-prefix" = "ecs"
        }
      }
      portMappings = [
        {
          containerPort = each.value.port
          hostPort      = each.value.port
          protocol      = "tcp"
          name          = each.key
        }
      ]
    }
  ])

  tags = {
    Name        = "${var.project_name}-${each.key}-task-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_cloudwatch_log_group" "service" {
  for_each = var.services

  name              = "/ecs/${var.project_name}/${each.key}-${var.environment}"
  retention_in_days = 90

  tags = {
    Name        = "${var.project_name}-${each.key}-logs-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_ecs_service" "main" {
  for_each = var.services

  name                   = "${var.project_name}-${each.key}-${var.environment}"
  cluster                = aws_ecs_cluster.main.id
  task_definition        = aws_ecs_task_definition.service[each.key].arn
  desired_count          = 1
  launch_type            = "FARGATE"
  enable_execute_command = true

  service_connect_configuration {
    enabled   = true
    namespace = aws_service_discovery_private_dns_namespace.main.arn
    service {
      client_alias {
        dns_name = each.value.dns_name
        port     = each.value.port
      }
      port_name      = each.key
      discovery_name = each.key
    }
  }

  network_configuration {
    subnets          = var.private_subnet_ids
    security_groups  = [aws_security_group.ecs_tasks[each.key].id]
    assign_public_ip = false
  }

  tags = {
    Name        = "${var.project_name}-${each.key}-service-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_appautoscaling_target" "service" {
  for_each = var.services

  max_capacity       = (each.key == "p2-voiceemergency" || each.key == "p6-firstresponder") ? 20 : 10
  min_capacity       = (each.key == "p2-voiceemergency" || each.key == "p6-firstresponder") ? 2 : 1
  resource_id        = "service/${aws_ecs_cluster.main.name}/${aws_ecs_service.main[each.key].name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

resource "aws_appautoscaling_policy" "cpu" {
  for_each = { for k, v in var.services : k => v if k == "p2-voiceemergency" || k == "p6-firstresponder" }

  name               = "${each.key}-cpu-autoscaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.service[each.key].resource_id
  scalable_dimension = aws_appautoscaling_target.service[each.key].scalable_dimension
  service_namespace  = aws_appautoscaling_target.service[each.key].service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value = 60.0
  }
resource "aws_security_group" "ecs_tasks" {
  for_each = var.services

  name        = "${var.project_name}-${each.key}-sg-${var.environment}"
  description = "Security group for ECS task ${each.key}"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = each.value.port
    to_port     = each.value.port
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"] # This should be restricted to ALB in later steps
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-${each.key}-sg-${var.environment}"
    Environment = var.environment
  }
}
