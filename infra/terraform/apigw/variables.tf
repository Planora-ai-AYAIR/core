variable "environment" {
  type    = string
  default = "production"
}

variable "api_backend_url" {
  description = "The HTTP endpoint the API Gateway will proxy to (e.g., the EKS NLB DNS name)"
  type        = string
  default     = "http://placeholder.nlb.amazonaws.com"
}
