# ── VPC Outputs ───────────────────────────────────────────────────────────────────
output "vpc_id" {
  description = "VPC ID"
  value       = module.vpc.vpc_id
}

output "private_subnet_ids" {
  description = "Private subnet IDs"
  value       = module.vpc.private_subnet_ids
}

output "public_subnet_ids" {
  description = "Public subnet IDs"
  value       = module.vpc.public_subnet_ids
}

# ── RDS Outputs ──────────────────────────────────────────────────────────────────
output "rds_endpoint" {
  description = "RDS PostgreSQL endpoint"
  value       = module.rds.rds_endpoint
}

output "rds_db_name" {
  value = module.rds.rds_db_name
}

# ── ElastiCache Outputs ──────────────────────────────────────────────────────────
output "redis_primary_endpoint" {
  description = "Redis primary endpoint for CONNECTIONSTRINGS__REDISCONNECTIONSTRING"
  value       = module.elasticache.redis_primary_endpoint
}

# ── EKS Outputs ─────────────────────────────────────────────────────────────────
output "eks_cluster_name" {
  value = module.eks.cluster_name
}

output "eks_cluster_endpoint" {
  value = module.eks.cluster_endpoint
}

output "api_irsa_role_arn" {
  description = "Annotate the api-dotnet Kubernetes ServiceAccount with this ARN"
  value       = module.eks.api_irsa_role_arn
}

# ── S3 Outputs ──────────────────────────────────────────────────────────────────
output "s3_bucket_name" {
  description = "S3 bucket name for AWS_BUCKET_NAME env var"
  value       = module.s3.bucket_name
}

output "s3_bucket_arn" {
  value = module.s3.bucket_arn
}

# ── Secrets Manager Outputs ───────────────────────────────────────────────────
output "secret_arns" {
  description = "ARNs of all Secrets Manager secrets — reference in ExternalSecrets"
  value       = module.secrets.secret_arns
}
