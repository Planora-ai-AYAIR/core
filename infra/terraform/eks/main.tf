terraform {
  required_version = ">= 1.5.0"
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

# ── Variables ────────────────────────────────────────────────────────────────
variable "environment" {
  type    = string
  default = "production"
}

variable "vpc_id" {
  type = string
}

variable "public_subnet_ids" {
  type = list(string)
}

variable "private_subnet_ids" {
  type = list(string)
}

variable "s3_bucket_arn" {
  description = "S3 bucket ARN that the backend pods need access to"
  type        = string
}

variable "kubernetes_version" {
  description = "EKS Kubernetes version"
  type        = string
  default     = "1.30"
}

variable "node_instance_type" {
  description = "EC2 instance type for EKS managed nodes"
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

# ── IAM: EKS Cluster Role ───────────────────────────────────────────────────────
resource "aws_iam_role" "eks_cluster" {
  name = "planora-eks-cluster-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action    = "sts:AssumeRole"
      Effect    = "Allow"
      Principal = { Service = "eks.amazonaws.com" }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "eks_cluster_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSClusterPolicy"
  role       = aws_iam_role.eks_cluster.name
}

# ── IAM: EKS Node Group Role ────────────────────────────────────────────────────
resource "aws_iam_role" "eks_nodes" {
  name = "planora-eks-node-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action    = "sts:AssumeRole"
      Effect    = "Allow"
      Principal = { Service = "ec2.amazonaws.com" }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "eks_worker_node_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSWorkerNodePolicy"
  role       = aws_iam_role.eks_nodes.name
}

resource "aws_iam_role_policy_attachment" "eks_cni_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKS_CNI_Policy"
  role       = aws_iam_role.eks_nodes.name
}

resource "aws_iam_role_policy_attachment" "eks_ecr_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
  role       = aws_iam_role.eks_nodes.name
}

# ── Security Group for EKS Nodes ───────────────────────────────────────────────
resource "aws_security_group" "eks_nodes" {
  name        = "planora-eks-nodes-sg-${var.environment}"
  description = "EKS node group security group"
  vpc_id      = var.vpc_id

  # Allow all inter-node traffic (required for Kubernetes networking)
  ingress {
    from_port = 0
    to_port   = 0
    protocol  = "-1"
    self      = true
  }

  # Allow HTTPS from anywhere (ALB ingress / kubectl API)
  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "planora-eks-nodes-sg-${var.environment}"
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# ── EKS Cluster ──────────────────────────────────────────────────────────────────
resource "aws_eks_cluster" "planora" {
  name     = "planora-eks-${var.environment}"
  role_arn = aws_iam_role.eks_cluster.arn
  version  = var.kubernetes_version

  vpc_config {
    subnet_ids              = concat(var.public_subnet_ids, var.private_subnet_ids)
    security_group_ids      = [aws_security_group.eks_nodes.id]
    endpoint_public_access  = true
    endpoint_private_access = true
  }

  enabled_cluster_log_types = ["api", "audit", "authenticator", "controllerManager", "scheduler"]

  tags = {
    Name        = "planora-eks-${var.environment}"
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }

  depends_on = [aws_iam_role_policy_attachment.eks_cluster_policy]
}

# ── EKS Managed Node Group ───────────────────────────────────────────────────────
resource "aws_eks_node_group" "planora" {
  cluster_name    = aws_eks_cluster.planora.name
  node_group_name = "planora-nodes-${var.environment}"
  node_role_arn   = aws_iam_role.eks_nodes.arn

  # Deploy nodes in private subnets for security
  subnet_ids = var.private_subnet_ids

  instance_types = [var.node_instance_type]

  scaling_config {
    desired_size = var.node_desired_size
    min_size     = var.node_min_size
    max_size     = var.node_max_size
  }

  update_config {
    max_unavailable = 1
  }

  tags = {
    Name        = "planora-nodegroup-${var.environment}"
    Project     = "Planora-AI"
    Environment = var.environment
    ManagedBy   = "terraform"
  }

  depends_on = [
    aws_iam_role_policy_attachment.eks_worker_node_policy,
    aws_iam_role_policy_attachment.eks_cni_policy,
    aws_iam_role_policy_attachment.eks_ecr_policy,
  ]
}

# ── OIDC Provider for IRSA (IAM Roles for Service Accounts) ─────────────────────
# Allows pods to assume IAM roles without static AWS credentials
data "tls_certificate" "eks_oidc" {
  url = aws_eks_cluster.planora.identity[0].oidc[0].issuer
}

resource "aws_iam_openid_connect_provider" "eks" {
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = [data.tls_certificate.eks_oidc.certificates[0].sha1_fingerprint]
  url             = aws_eks_cluster.planora.identity[0].oidc[0].issuer

  tags = {
    Project     = "Planora-AI"
    Environment = var.environment
  }
}

# ── IRSA Role for .NET API pods (S3 + Secrets Manager access) ──────────────────
data "aws_caller_identity" "current" {}

resource "aws_iam_role" "planora_api_irsa" {
  name = "planora-api-irsa-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Principal = {
        Federated = aws_iam_openid_connect_provider.eks.arn
      }
      Action = "sts:AssumeRoleWithWebIdentity"
      Condition = {
        StringEquals = {
          "${replace(aws_iam_openid_connect_provider.eks.url, "https://", "")}:sub" = "system:serviceaccount:planora:api-dotnet"
          "${replace(aws_iam_openid_connect_provider.eks.url, "https://", "")}:aud" = "sts.amazonaws.com"
        }
      }
    }]
  })
}

resource "aws_iam_role_policy" "planora_api_s3" {
  name = "planora-api-s3-policy"
  role = aws_iam_role.planora_api_irsa.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "S3Access"
        Effect = "Allow"
        Action = ["s3:GetObject", "s3:PutObject", "s3:DeleteObject", "s3:ListBucket"]
        Resource = [
          var.s3_bucket_arn,
          "${var.s3_bucket_arn}/*"
        ]
      },
      {
        Sid    = "SecretsAccess"
        Effect = "Allow"
        Action = ["secretsmanager:GetSecretValue", "secretsmanager:DescribeSecret"]
        Resource = "arn:aws:secretsmanager:*:${data.aws_caller_identity.current.account_id}:secret:planora/*"
      }
    ]
  })
}

# ── Outputs ───────────────────────────────────────────────────────────────────
output "cluster_name" {
  value = aws_eks_cluster.planora.name
}

output "cluster_endpoint" {
  value = aws_eks_cluster.planora.endpoint
}

output "cluster_ca_certificate" {
  value     = aws_eks_cluster.planora.certificate_authority[0].data
  sensitive = true
}

output "oidc_provider_arn" {
  value = aws_iam_openid_connect_provider.eks.arn
}

output "node_sg_id" {
  description = "Security group ID of EKS nodes — referenced by RDS and ElastiCache SGs"
  value       = aws_eks_cluster.planora.vpc_config[0].cluster_security_group_id
}

output "api_irsa_role_arn" {
  description = "IRSA role ARN to annotate the api-dotnet Kubernetes ServiceAccount"
  value       = aws_iam_role.planora_api_irsa.arn
}
