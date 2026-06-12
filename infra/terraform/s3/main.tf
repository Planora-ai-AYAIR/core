terraform {
  required_version = ">= 1.5.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
  backend "s3" {
    bucket = "planora-tfstate"
    key    = "s3/terraform.tfstate"
    region = "eu-south-1"
  }
}

provider "aws" {
  region = var.aws_region
}

# ── Variables ────────────────────────────────────────────────────────────────
variable "aws_region" {
  description = "AWS region to deploy the bucket"
  type        = string
  default     = "eu-south-1"
}

variable "environment" {
  description = "Deployment environment"
  type        = string
  default     = "production"
}

# ── S3 Bucket ────────────────────────────────────────────────────────────────
resource "aws_s3_bucket" "geosense" {
  bucket = "geosense-ai-${var.environment}"

  tags = {
    Project     = "GeoSense-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# Block all public access — frontend never accesses S3 directly;
# .NET backend generates pre-signed URLs instead.
resource "aws_s3_bucket_public_access_block" "geosense" {
  bucket = aws_s3_bucket.geosense.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# Server-side encryption at rest (AES-256)
resource "aws_s3_bucket_server_side_encryption_configuration" "geosense" {
  bucket = aws_s3_bucket.geosense.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# Versioning — allows recovery of overwritten GeoJSON/GeoTIFF outputs
resource "aws_s3_bucket_versioning" "geosense" {
  bucket = aws_s3_bucket.geosense.id
  versioning_configuration {
    status = "Enabled"
  }
}

# Lifecycle: move objects to Infrequent Access after 90 days to cut storage cost
resource "aws_s3_bucket_lifecycle_configuration" "geosense" {
  bucket = aws_s3_bucket.geosense.id

  rule {
    id     = "transition-to-ia"
    status = "Enabled"

    transition {
      days          = 90
      storage_class = "STANDARD_IA"
    }
  }
}

# CORS — allows the Angular frontend to load pre-signed URLs directly
resource "aws_s3_bucket_cors_configuration" "geosense" {
  bucket = aws_s3_bucket.geosense.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET"]
    allowed_origins = ["*"]   # tighten to your domain before go-live
    max_age_seconds = 3600
  }
}

# ── IAM: dedicated service account for AI engine uploads only ────────────────
resource "aws_iam_user" "geosense_ai" {
  name = "geosense-ai-service"
  tags = {
    Project = "GeoSense-AI"
  }
}

resource "aws_iam_access_key" "geosense_ai" {
  user = aws_iam_user.geosense_ai.name
}

data "aws_iam_policy_document" "geosense_s3" {
  # AI engine: put objects (upload generated files)
  statement {
    sid    = "AllowAIPutObject"
    effect = "Allow"
    actions = [
      "s3:PutObject",
      "s3:GetObject",
      "s3:DeleteObject",
    ]
    resources = ["${aws_s3_bucket.geosense.arn}/*"]
  }

  # .NET backend: generate pre-signed URLs (needs s3:GetObject on bucket)
  statement {
    sid    = "AllowBackendPresign"
    effect = "Allow"
    actions = [
      "s3:GetObject",
      "s3:ListBucket",
    ]
    resources = [
      aws_s3_bucket.geosense.arn,
      "${aws_s3_bucket.geosense.arn}/*",
    ]
  }
}

resource "aws_iam_user_policy" "geosense_ai" {
  name   = "geosense-s3-policy"
  user   = aws_iam_user.geosense_ai.name
  policy = data.aws_iam_policy_document.geosense_s3.json
}

# ── Outputs — consumed by K8s secrets and other modules ─────────────────────
output "bucket_name" {
  description = "S3 bucket name — set as AWS_BUCKET_NAME env var"
  value       = aws_s3_bucket.geosense.bucket
}

output "bucket_arn" {
  description = "S3 bucket ARN"
  value       = aws_s3_bucket.geosense.arn
}

output "iam_access_key_id" {
  description = "IAM access key ID — set as AWS_ACCESS_KEY_ID in K8s secret"
  value       = aws_iam_access_key.geosense_ai.id
  sensitive   = true
}

output "iam_secret_access_key" {
  description = "IAM secret access key — set as AWS_SECRET_ACCESS_KEY in K8s secret"
  value       = aws_iam_access_key.geosense_ai.secret
  sensitive   = true
}
