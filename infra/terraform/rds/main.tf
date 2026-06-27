terraform {
  required_version = ">= 1.5.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# ── Variables ────────────────────────────────────────────────────────────────
variable "environment" {
  type    = string
  default = "production"
}

variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "private_subnet_ids" {
  description = "Private subnet IDs for the RDS subnet group"
  type        = list(string)
}

variable "eks_node_sg_id" {
  description = "EKS node security group allowed to connect on port 5432"
  type        = string
}

variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.medium"
}

variable "db_allocated_storage" {
  description = "Initial storage in GiB"
  type        = number
  default     = 20
}

variable "db_max_allocated_storage" {
  description = "Max storage for autoscaling in GiB"
  type        = number
  default     = 100
}

variable "db_master_username" {
  description = "Master DB username"
  type        = string
  default     = "planora_admin"
  sensitive   = true
}

variable "db_master_password" {
  description = "Master DB password (injected from Secrets Manager / CI)"
  type        = string
  sensitive   = true
}

# ── Security Group ────────────────────────────────────────────────────────────
resource "aws_security_group" "rds" {
  name        = "planora-rds-sg-${var.environment}"
  description = "Allow PostgreSQL access from EKS nodes only"
  vpc_id      = var.vpc_id

  ingress {
    description     = "PostgreSQL from EKS nodes"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [var.eks_node_sg_id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "planora-rds-sg-${var.environment}"
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# ── DB Subnet Group ───────────────────────────────────────────────────────────
resource "aws_db_subnet_group" "planora" {
  name       = "planora-db-subnet-group-${var.environment}"
  subnet_ids = var.private_subnet_ids

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# ── DB Parameter Group ─────────────────────────────────────────────────────────
# Enables PostGIS and the required shared libraries for spatial queries
resource "aws_db_parameter_group" "planora" {
  name   = "planora-pg16-postgis-${var.environment}"
  family = "postgres16"

  parameter {
    name         = "shared_preload_libraries"
    value        = "pg_stat_statements"
    apply_method = "pending-reboot"
  }

  parameter {
    name  = "log_min_duration_statement"
    value = "1000"
  }

  parameter {
    name  = "log_connections"
    value = "1"
  }

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# ── RDS Instance (PostgreSQL 16) ──────────────────────────────────────────────────
# NOTE: PostGIS extension must be created manually after first launch:
#   psql> CREATE EXTENSION IF NOT EXISTS postgis;
# This is done via EF Core migration or a one-time DBA step.
resource "aws_db_instance" "planora" {
  identifier = "planora-postgres-${var.environment}"

  engine               = "postgres"
  engine_version       = "16"
  instance_class       = var.db_instance_class
  allocated_storage    = var.db_allocated_storage
  max_allocated_storage = var.db_max_allocated_storage
  storage_type         = "gp3"
  storage_encrypted    = true

  db_name  = "PlanoraDb"
  username = var.db_master_username
  password = var.db_master_password

  db_subnet_group_name   = aws_db_subnet_group.planora.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  parameter_group_name   = aws_db_parameter_group.planora.name

  # High availability - Disabled for Free Tier
  multi_az               = false
  publicly_accessible    = false

  # Backups — Disabled for Free Tier
  backup_retention_period = 0
  backup_window           = "02:00-03:00"
  maintenance_window      = "sun:04:00-sun:05:00"
  copy_tags_to_snapshot   = true

  # Performance Insights
  performance_insights_enabled = true

  # Prevent accidental deletion in production
  deletion_protection = true
  skip_final_snapshot = false
  final_snapshot_identifier = "planora-final-${var.environment}"

  tags = {
    Name        = "planora-postgres-${var.environment}"
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# ── Hangfire Database ───────────────────────────────────────────────────────────
# Hangfire requires a separate database (PlanoraHangfire) on the same instance.
# This null_resource documents the requirement — the DB is created by the app
# or a one-time SQL init script run during CI/CD:
#   psql -h <endpoint> -U planora_admin -c "CREATE DATABASE \"PlanoraHangfire\";"
resource "null_resource" "hangfire_db_note" {
  triggers = {
    note = "Run: psql -h ${aws_db_instance.planora.address} -U ${var.db_master_username} -c 'CREATE DATABASE PlanoraHangfire;'"
  }
}

# ── Outputs ───────────────────────────────────────────────────────────────────
output "rds_endpoint" {
  description = "RDS instance endpoint hostname — use in connection string"
  value       = aws_db_instance.planora.address
}

output "rds_port" {
  value = aws_db_instance.planora.port
}

output "rds_db_name" {
  value = aws_db_instance.planora.db_name
}

output "rds_sg_id" {
  description = "RDS security group ID"
  value       = aws_security_group.rds.id
}
