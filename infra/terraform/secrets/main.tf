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

# These are passed in from root module outputs or CI/CD pipeline
# NEVER hardcode values here — pass via -var-file or environment variables
variable "db_host" {
  description = "RDS endpoint hostname"
  type        = string
}

variable "db_password" {
  description = "RDS master password"
  type        = string
  sensitive   = true
}

variable "hangfire_db_password" {
  description = "Hangfire DB password (can be same as db_password)"
  type        = string
  sensitive   = true
}

variable "redis_endpoint" {
  description = "ElastiCache Redis primary endpoint"
  type        = string
}

variable "jwt_secret_key" {
  description = "JWT signing key (min 256-bit random string)"
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

variable "hangfire_dashboard_username" {
  type      = string
  sensitive = true
  default   = "admin"
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

variable "aws_bucket_name" {
  type = string
}

variable "aws_region" {
  type    = string
  default = "us-east-1"
}

# ── Database Connection Secret ────────────────────────────────────────────────
resource "aws_secretsmanager_secret" "db" {
  name                    = "planora/${var.environment}/database"
  description             = "PlanoraDb and PlanoraHangfire PostgreSQL connection strings"
  recovery_window_in_days = 0
  force_overwrite_replica_secret = true

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

resource "aws_secretsmanager_secret_version" "db" {
  secret_id = aws_secretsmanager_secret.db.id
  secret_string = jsonencode({
    CONNECTION_MODE                      = "Prod"
    CONNECTIONSTRINGS__PRODCS           = "Host=${var.db_host};Port=5432;Database=PlanoraDb;Username=planora_admin;Password=${var.db_password};SSL Mode=Require;Trust Server Certificate=true"
    CONNECTIONSTRINGS__HANGFIRECONNECTIONSTRING = "Host=${var.db_host};Port=5432;Database=PlanoraHangfire;Username=planora_admin;Password=${var.hangfire_db_password};SSL Mode=Require;Trust Server Certificate=true"
  })
}

# ── Redis Connection Secret ───────────────────────────────────────────────────
resource "aws_secretsmanager_secret" "redis" {
  name                    = "planora/${var.environment}/redis"
  description             = "Redis connection string for HybridCache"
  recovery_window_in_days = 0
  force_overwrite_replica_secret = true

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

resource "aws_secretsmanager_secret_version" "redis" {
  secret_id = aws_secretsmanager_secret.redis.id
  secret_string = jsonencode({
    CONNECTIONSTRINGS__REDISCONNECTIONSTRING = "${var.redis_endpoint}:6379,ssl=true,abortConnect=false"
  })
}

# ── JWT Secret ──────────────────────────────────────────────────────────────────
resource "aws_secretsmanager_secret" "jwt" {
  name                    = "planora/${var.environment}/jwt"
  description             = "JWT signing key for ASP.NET Identity + JWT auth"
  recovery_window_in_days = 0
  force_overwrite_replica_secret = true

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

resource "aws_secretsmanager_secret_version" "jwt" {
  secret_id = aws_secretsmanager_secret.jwt.id
  secret_string = jsonencode({
    JWTSETTINGS__SECRETKEY = var.jwt_secret_key
  })
}

# ── SMTP Secret ─────────────────────────────────────────────────────────────────
resource "aws_secretsmanager_secret" "smtp" {
  name                    = "planora/${var.environment}/smtp"
  description             = "SMTP credentials for OTP and notification emails"
  recovery_window_in_days = 0
  force_overwrite_replica_secret = true

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

resource "aws_secretsmanager_secret_version" "smtp" {
  secret_id = aws_secretsmanager_secret.smtp.id
  secret_string = jsonencode({
    EMAILSETTINGS__SMTPHOST      = var.smtp_host
    EMAILSETTINGS__SMTPPORT      = tostring(var.smtp_port)
    EMAILSETTINGS__USERNAME      = var.smtp_username
    EMAILSETTINGS__PASSWORD      = var.smtp_password
    EMAILSETTINGS__SENDEREMAIL   = var.smtp_sender_email
    EMAILSETTINGS__SENDERNAME    = var.smtp_sender_name
    EMAILSETTINGS__USESSL        = "true"
  })
}

# ── Hangfire Dashboard Secret ─────────────────────────────────────────────────
resource "aws_secretsmanager_secret" "hangfire" {
  name                    = "planora/${var.environment}/hangfire"
  description             = "Hangfire dashboard credentials"
  recovery_window_in_days = 0
  force_overwrite_replica_secret = true

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

resource "aws_secretsmanager_secret_version" "hangfire" {
  secret_id = aws_secretsmanager_secret.hangfire.id
  secret_string = jsonencode({
    HANGFIRESETTINGS__USERNAME = var.hangfire_dashboard_username
    HANGFIRESETTINGS__PASSWORD = var.hangfire_dashboard_password
  })
}

# ── AI Service Secret ───────────────────────────────────────────────────────────
resource "aws_secretsmanager_secret" "ai_service" {
  name                    = "planora/${var.environment}/ai-service"
  description             = "Python AI service endpoint and API key"
  recovery_window_in_days = 0
  force_overwrite_replica_secret = true

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

resource "aws_secretsmanager_secret_version" "ai_service" {
  secret_id = aws_secretsmanager_secret.ai_service.id
  secret_string = jsonencode({
    AIOPTIONS__BASEURL = var.ai_service_base_url
    AIOPTIONS__APIKEY  = var.ai_service_api_key
  })
}

# ── AWS S3 Config Secret (bucket + region reference) ───────────────────────────
resource "aws_secretsmanager_secret" "s3_config" {
  name                    = "planora/${var.environment}/s3"
  description             = "AWS S3 bucket config for the .NET backend"
  recovery_window_in_days = 0
  force_overwrite_replica_secret = true

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

resource "aws_secretsmanager_secret_version" "s3_config" {
  secret_id = aws_secretsmanager_secret.s3_config.id
  secret_string = jsonencode({
    AWS_REGION      = var.aws_region
    AWS_BUCKET_NAME = var.aws_bucket_name
  })
}

# ── Outputs (ARNs only — actual values never output) ────────────────────────────
output "secret_arns" {
  description = "Map of secret names to ARNs — reference in K8s ExternalSecrets"
  value = {
    database  = aws_secretsmanager_secret.db.arn
    redis     = aws_secretsmanager_secret.redis.arn
    jwt       = aws_secretsmanager_secret.jwt.arn
    smtp      = aws_secretsmanager_secret.smtp.arn
    hangfire  = aws_secretsmanager_secret.hangfire.arn
    ai_service = aws_secretsmanager_secret.ai_service.arn
    s3_config = aws_secretsmanager_secret.s3_config.arn
  }
}
