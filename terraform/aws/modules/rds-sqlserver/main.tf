resource "aws_db_subnet_group" "main" {
  name       = "${var.project_name}-rds-subnets-${var.environment}"
  subnet_ids = var.subnet_ids

  tags = {
    Name        = "${var.project_name}-rds-subnets-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_db_instance" "sqlserver" {
  identifier              = "${var.project_name}-sqlserver-${var.environment}"
  allocated_storage       = 100
  engine                  = "sqlserver-se"
  engine_version          = "15.00"
  instance_class          = var.instance_class
  db_subnet_group_name    = aws_db_subnet_group.main.name
  vpc_security_group_ids  = var.vpc_security_group_ids
  multi_az                = true
  username                = var.username
  password                = var.password
  db_name                 = var.db_name
  backup_retention_period = var.backup_retention
  performance_insights_enabled = true
  deletion_protection     = false
  skip_final_snapshot     = true

  tags = {
    Name        = "${var.project_name}-sqlserver-${var.environment}"
    Environment = var.environment
  }
}
