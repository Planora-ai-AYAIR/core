terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

module "vpc" {
  source = "./vpc"
}

module "eks" {
  source = "./eks"
}

module "rds" {
  source = "./rds"
}

module "s3" {
  source      = "./s3"
  environment = var.environment
  aws_region  = var.aws_region
}
