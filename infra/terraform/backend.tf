terraform {
  required_version = ">= 1.10.0"

  backend "s3" {
    bucket       = "s3tflock-ayair"
    key          = "infrastructure/prod.tfstate"
    region       = "us-east-1"
    encrypt      = true
    use_lockfile = true
  }
}
