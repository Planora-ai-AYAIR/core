resource "aws_api_gateway_rest_api" "planora_api" {
  name        = "planora-api-${var.environment}"
  description = "API Gateway for Planora .NET Backend"
}

resource "aws_api_gateway_resource" "proxy" {
  rest_api_id = aws_api_gateway_rest_api.planora_api.id
  parent_id   = aws_api_gateway_rest_api.planora_api.root_resource_id
  path_part   = "{proxy+}"
}

resource "aws_api_gateway_method" "proxy_any" {
  rest_api_id   = aws_api_gateway_rest_api.planora_api.id
  resource_id   = aws_api_gateway_resource.proxy.id
  http_method   = "ANY"
  authorization = "NONE"
  
  # Ensure the proxy path parameter is required
  request_parameters = {
    "method.request.path.proxy" = true
  }
}

resource "aws_api_gateway_integration" "proxy_integration" {
  rest_api_id             = aws_api_gateway_rest_api.planora_api.id
  resource_id             = aws_api_gateway_resource.proxy.id
  http_method             = aws_api_gateway_method.proxy_any.http_method
  integration_http_method = "ANY"
  type                    = "HTTP_PROXY"
  uri                     = "${var.api_backend_url}/{proxy}"
  
  request_parameters = {
    "integration.request.path.proxy" = "method.request.path.proxy"
  }
}

resource "aws_api_gateway_deployment" "planora_api" {
  depends_on = [
    aws_api_gateway_integration.proxy_integration
  ]
  rest_api_id = aws_api_gateway_rest_api.planora_api.id

  triggers = {
    redeployment = sha1(jsonencode(aws_api_gateway_integration.proxy_integration))
  }

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_api_gateway_stage" "prod" {
  deployment_id = aws_api_gateway_deployment.planora_api.id
  rest_api_id   = aws_api_gateway_rest_api.planora_api.id
  stage_name    = "v1"
}

output "api_gateway_invoke_url" {
  value = aws_api_gateway_stage.prod.invoke_url
}
