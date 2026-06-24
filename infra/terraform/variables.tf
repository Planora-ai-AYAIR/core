# ── Global Variables ────────────────────────────────────────────────────────────
variable "aws_region" {
  description = "AWS region for all resources"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Deployment environment (production / staging)"
  type        = string
  default     = "production"
}

# ── VPC ──────────────────────────────────────────────────────────────────────
variable "vpc_cidr" {
  description = "CIDR block for the VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "availability_zones" {
  description = "AZs to deploy into (min 2 for Multi-AZ RDS and EKS)"
  type        = list(string)
  default     = ["us-east-1a", "us-east-1b"]
}

# ── RDS ──────────────────────────────────────────────────────────────────────
variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.medium"
}

variable "db_master_username" {
  description = "RDS master username"
  type        = string
  default     = "planora_admin"
}

variable "db_master_password" {
  description = "RDS master password — supply via TF_VAR_db_master_password env var or tfvars file (never hardcode)"
  type        = string
  sensitive   = true
}

# ── ElastiCache ────────────────────────────────────────────────────────────
variable "redis_node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.t3.micro"
}

# ── EKS ──────────────────────────────────────────────────────────────────────
variable "kubernetes_version" {
  description = "EKS Kubernetes version"
  type        = string
  default     = "1.30"
}

variable "node_instance_type" {
  description = "EC2 instance type for EKS worker nodes"
  type        = string
  default     = "t3.medium"
}

variable "node_desired_size" {
  type    = number
  default = 2
}

variable "node_min_size" {
  type    = number
  default = 1
}

variable "node_max_size" {
  type    = number
  default = 4
}

# ── Secrets — all sensitive, injected via CI/CD ──────────────────────────────
variable "jwt_secret_key" {
  description = "JWT signing secret key (256-bit minimum)"
  type        = string
  sensitive   = true
}

variable "smtp_host" {
  type = string
}

variable "smtp_port" {
  type    = number
  default = 587
}

variable "smtp_username" {
  type      = string
  sensitive = true
}

variable "smtp_password" {
  type      = string
  sensitive = true
}

variable "smtp_sender_email" {
  type = string
}

variable "smtp_sender_name" {
  type    = string
  default = "Planora AI"
}

variable "hangfire_dashboard_password" {
  type      = string
  sensitive = true
}

variable "ai_service_base_url" {
  type = string
}

variable "ai_service_api_key" {
  type      = string
  sensitive = true
}
