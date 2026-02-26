resource "aws_db_subnet_group" "main" {
  name       = "${var.project_name}-aurora-subnets-${var.environment}"
  subnet_ids = var.subnet_ids

  tags = {
    Name        = "${var.project_name}-aurora-subnets-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_rds_cluster_parameter_group" "main" {
  name        = "${var.project_name}-aurora-pg-${var.environment}"
  family      = "aurora-postgresql15"
  description = "Parameter group enabling PostGIS readiness"

  parameter {
    name  = "rds.force_ssl"
    value = "1"
  }
}

resource "aws_rds_cluster" "main" {
  cluster_identifier      = "${var.project_name}-aurora-${var.environment}"
  engine                  = "aurora-postgresql"
  engine_version          = "15.3"
  database_name           = var.db_name
  master_username         = var.master_username
  master_password         = var.master_password
  db_subnet_group_name    = aws_db_subnet_group.main.name
  vpc_security_group_ids  = var.vpc_security_group_ids
  backup_retention_period = 7
  deletion_protection     = false
  storage_encrypted       = true
  apply_immediately       = true
  db_cluster_parameter_group_name = aws_rds_cluster_parameter_group.main.name

  tags = {
    Name        = "${var.project_name}-aurora-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_rds_cluster_instance" "writer" {
  identifier         = "${var.project_name}-aurora-writer-${var.environment}"
  cluster_identifier = aws_rds_cluster.main.id
  instance_class     = var.instance_class
  engine             = aws_rds_cluster.main.engine
  engine_version     = aws_rds_cluster.main.engine_version
}

resource "aws_rds_cluster_instance" "readers" {
  count              = var.replica_count
  identifier         = "${var.project_name}-aurora-reader-${count.index + 1}-${var.environment}"
  cluster_identifier = aws_rds_cluster.main.id
  instance_class     = var.instance_class
  engine             = aws_rds_cluster.main.engine
  engine_version     = aws_rds_cluster.main.engine_version
}
