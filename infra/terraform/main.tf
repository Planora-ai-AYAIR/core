terraform {
  required_version = ">= 1.10.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    tls = {
      source  = "hashicorp/tls"
      version = "~> 4.0"
    }
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "Planora-AI"
      Environment = var.environment
      ManagedBy   = "terraform"
    }
  }
}

# ── Auto-Generated Credentials ─────────────────────────────────────────────
resource "random_password" "db_master_password" {
  length           = 16
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "random_password" "jwt_secret_key" {
  length  = 64
  special = true
}

resource "random_password" "hangfire_dashboard_password" {
  length  = 16
  special = true
}

resource "random_password" "ai_service_api_key" {
  length  = 32
  special = true
}

# ─────────────────────────────────────────────────────────────────────────────
# 1. VPC — networking foundation everything else depends on
# ─────────────────────────────────────────────────────────────────────────────
module "vpc" {
  source = "./vpc"

  aws_region         = var.aws_region
  environment        = var.environment
  vpc_cidr           = var.vpc_cidr
  availability_zones = var.availability_zones
}

# ─────────────────────────────────────────────────────────────────────────────
# 2. S3 — already deployed; wired here so outputs feed other modules
# ─────────────────────────────────────────────────────────────────────────────
module "s3" {
  source      = "./s3"
  environment = var.environment
  aws_region  = var.aws_region
}

# ─────────────────────────────────────────────────────────────────────────────
# 3. EKS — provides node_sg_id consumed by RDS and ElastiCache SGs
# ─────────────────────────────────────────────────────────────────────────────
module "eks" {
  source = "./eks"

  environment        = var.environment
  vpc_id             = module.vpc.vpc_id
  public_subnet_ids  = module.vpc.public_subnet_ids
  private_subnet_ids = module.vpc.private_subnet_ids
  s3_bucket_arn      = module.s3.bucket_arn

  kubernetes_version  = var.kubernetes_version
  node_instance_type  = var.node_instance_type
  node_desired_size   = var.node_desired_size
  node_min_size       = var.node_min_size
  node_max_size       = var.node_max_size
}

# ─────────────────────────────────────────────────────────────────────────────
# 4. RDS — PostgreSQL 16 with PostGIS, private subnets, SG locked to EKS nodes
# ─────────────────────────────────────────────────────────────────────────────
module "rds" {
  source = "./rds"

  environment          = var.environment
  vpc_id               = module.vpc.vpc_id
  private_subnet_ids   = module.vpc.private_subnet_ids
  eks_node_sg_id       = module.eks.node_sg_id

  db_instance_class      = var.db_instance_class
  db_master_username     = var.db_master_username
  db_master_password     = random_password.db_master_password.result
}

# ─────────────────────────────────────────────────────────────────────────────
# 5. ElastiCache — Redis 7, private subnets, SG locked to EKS nodes
# ─────────────────────────────────────────────────────────────────────────────
module "elasticache" {
  source = "./elasticache"

  environment        = var.environment
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids
  eks_node_sg_id     = module.eks.node_sg_id
  redis_node_type    = var.redis_node_type
}

# ─────────────────────────────────────────────────────────────────────────────
# 6. Secrets Manager — all app secrets stored under planora/{env}/* path
# ─────────────────────────────────────────────────────────────────────────────
module "secrets" {
  source = "./secrets"

  environment = var.environment
  aws_region  = var.aws_region

  db_host              = module.rds.rds_endpoint
  db_password          = random_password.db_master_password.result
  hangfire_db_password = random_password.db_master_password.result

  redis_endpoint = module.elasticache.redis_primary_endpoint

  jwt_secret_key = random_password.jwt_secret_key.result

  smtp_host         = var.smtp_host
  smtp_port         = var.smtp_port
  smtp_username     = var.smtp_username
  smtp_password     = var.smtp_password
  smtp_sender_email = var.smtp_sender_email
  smtp_sender_name  = var.smtp_sender_name

  hangfire_dashboard_password = random_password.hangfire_dashboard_password.result

  ai_service_base_url = var.ai_service_base_url
  ai_service_api_key  = random_password.ai_service_api_key.result

  aws_bucket_name = module.s3.bucket_name
}

# ─────────────────────────────────────────────────────────────────────────────
# 7. API Gateway — REST API proxy to the EKS NLB
# ─────────────────────────────────────────────────────────────────────────────
module "apigw" {
  source = "./apigw"

  environment     = var.environment
  api_backend_url = "http://ac780c7e579dc4ba5ac4c59fefff278a-c16daf94c84d9209.elb.us-east-1.amazonaws.com"
}
