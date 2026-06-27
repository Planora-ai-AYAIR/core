$secrets = @(
    "planora/production/database",
    "planora/production/redis",
    "planora/production/jwt",
    "planora/production/smtp",
    "planora/production/hangfire",
    "planora/production/ai-service",
    "planora/production/s3"
)

$k8s_names = @{
    "planora/production/database" = "planora-db-secret"
    "planora/production/redis" = "planora-redis-secret"
    "planora/production/jwt" = "planora-jwt-secret"
    "planora/production/smtp" = "planora-smtp-secret"
    "planora/production/hangfire" = "planora-hangfire-secret"
    "planora/production/ai-service" = "planora-ai-secret"
    "planora/production/s3" = "planora-s3-secret"
}

$yaml = ""

foreach ($secret in $secrets) {
    $value = aws secretsmanager get-secret-value --secret-id $secret --region us-east-1 --query SecretString --output text
    $json = $value | ConvertFrom-Json
    $k8s_name = $k8s_names[$secret]
    
    $yaml += "apiVersion: v1`n"
    $yaml += "kind: Secret`n"
    $yaml += "metadata:`n"
    $yaml += "  name: $k8s_name`n"
    $yaml += "  namespace: planora`n"
    $yaml += "type: Opaque`n"
    $yaml += "stringData:`n"
    
    foreach ($property in $json.psobject.properties) {
        $key = $property.Name
        $val = $property.Value
        
        if ($null -ne $val) {
            # ToString helps if it's boolean/number
            $strVal = $val.ToString()
            # Escape double quotes
            $strVal = $strVal -replace '"', '\"'
            $yaml += "  ${key}: `"$strVal`"`n"
        }
    }
    $yaml += "---`n"
}

Set-Content -Path "infra/k8s/generated-secrets.yaml" -Value $yaml
