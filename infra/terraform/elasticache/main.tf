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
  description = "VPC ID where the Redis cluster will be placed"
  type        = string
}

variable "private_subnet_ids" {
  description = "Private subnet IDs for the ElastiCache subnet group"
  type        = list(string)
}

variable "eks_node_sg_id" {
  description = "Security group ID of EKS nodes (allowed to reach Redis on 6379)"
  type        = string
}

variable "redis_node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.t3.micro"
}

variable "redis_engine_version" {
  description = "Redis engine version"
  type        = string
  default     = "7.1"
}

# ── Security Group ────────────────────────────────────────────────────────────
resource "aws_security_group" "redis" {
  name        = "planora-redis-sg-${var.environment}"
  description = "Allow Redis traffic from EKS nodes only"
  vpc_id      = var.vpc_id

  ingress {
    description     = "Redis from EKS nodes"
    from_port       = 6379
    to_port         = 6379
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
    Name        = "planora-redis-sg-${var.environment}"
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# ── Subnet Group ───────────────────────────────────────────────────────────────
resource "aws_elasticache_subnet_group" "planora" {
  name       = "planora-redis-subnet-group-${var.environment}"
  subnet_ids = var.private_subnet_ids

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
  }
}

# ── Parameter Group ───────────────────────────────────────────────────────────
resource "aws_elasticache_parameter_group" "planora" {
  name   = "planora-redis-params-${var.environment}"
  family = "redis7"

  # Note: AOF (appendonly) is disabled because it is unsupported on t3.micro Free Tier nodes.
  
  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
  }
}

# ── ElastiCache Replication Group (Redis with 1 replica for HA) ─────────────────
resource "aws_elasticache_replication_group" "planora" {
  replication_group_id = "planora-redis-${var.environment}"
  description          = "Planora Redis cache - HybridCache and DistributedCache"

  node_type            = var.redis_node_type
  engine_version       = var.redis_engine_version
  parameter_group_name = aws_elasticache_parameter_group.planora.name
  subnet_group_name    = aws_elasticache_subnet_group.planora.name
  security_group_ids   = [aws_security_group.redis.id]

  # 1 node only for Free Tier (750 hours/month)
  num_cache_clusters         = 1
  automatic_failover_enabled = false
  multi_az_enabled           = false

  # Encryption
  at_rest_encryption_enabled = true
  transit_encryption_enabled = true

  # Maintenance
  maintenance_window         = "sun:03:00-sun:04:00"
  snapshot_retention_limit   = 7
  snapshot_window            = "01:00-02:00"

  # Application startup must fail if Redis is unreachable — no lazy connect
  apply_immediately = false

  tags = {
    Name        = "planora-redis-${var.environment}"
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# ── Outputs ───────────────────────────────────────────────────────────────────
output "redis_primary_endpoint" {
  description = "Redis primary endpoint — use as CONNECTIONSTRINGS__REDISCONNECTIONSTRING"
  value       = aws_elasticache_replication_group.planora.primary_endpoint_address
}

output "redis_reader_endpoint" {
  description = "Redis reader endpoint for read replicas"
  value       = aws_elasticache_replication_group.planora.reader_endpoint_address
}

output "redis_port" {
  value = 6379
}

output "redis_sg_id" {
  description = "Security group ID of the Redis cluster"
  value       = aws_security_group.redis.id
}
