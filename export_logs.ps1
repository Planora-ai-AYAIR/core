$ErrorActionPreference = "Stop"

$ExportDir = "planora-logs-export"
New-Item -ItemType Directory -Force -Path $ExportDir | Out-Null

# 1. Kubernetes Pod Logs
Write-Host "Extracting Kubernetes Pod Logs..."
$KubeDir = "$ExportDir/kubernetes"
New-Item -ItemType Directory -Force -Path $KubeDir | Out-Null
$pods = kubectl get pods -n planora -o jsonpath="{.items[*].metadata.name}"
$podArray = $pods -split ' '
foreach ($pod in $podArray) {
    if ([string]::IsNullOrWhiteSpace($pod)) { continue }
    Write-Host "Fetching logs for $pod"
    kubectl logs $pod -n planora --all-containers=true > "$KubeDir/$pod.log" 2>$null
}

# 2. RDS Database Logs
Write-Host "Extracting RDS PostgreSQL Logs..."
$RdsDir = "$ExportDir/rds"
New-Item -ItemType Directory -Force -Path $RdsDir | Out-Null
$logFiles = aws rds describe-db-log-files --db-instance-identifier planora-postgres-production --query "DescribeDBLogFiles[*].LogFileName" --output text
$logFilesArray = $logFiles -split '\s+'
# Download only the last 10 logs to save time
$recentLogs = $logFilesArray | Select-Object -Last 10
foreach ($log in $recentLogs) {
    if ([string]::IsNullOrWhiteSpace($log)) { continue }
    $safeName = $log -replace '/', '_'
    Write-Host "Downloading RDS log $log"
    aws rds download-db-log-file-portion --db-instance-identifier planora-postgres-production --log-file-name $log --starting-token 0 --output text > "$RdsDir/$safeName"
}

# 3. EKS Control Plane Logs (CloudWatch)
Write-Host "Extracting EKS Control Plane Logs..."
$EksDir = "$ExportDir/eks-control-plane"
New-Item -ItemType Directory -Force -Path $EksDir | Out-Null
# Get the streams
$streams = aws logs describe-log-streams --log-group-name "/aws/eks/planora-eks-production/cluster" --order-by LastEventTime --descending --limit 5 --query "logStreams[*].logStreamName" --output text
$streamsArray = $streams -split '\s+'
foreach ($stream in $streamsArray) {
    if ([string]::IsNullOrWhiteSpace($stream)) { continue }
    $safeName = $stream -replace '/', '_'
    Write-Host "Downloading CloudWatch stream $stream"
    aws logs get-log-events --log-group-name "/aws/eks/planora-eks-production/cluster" --log-stream-name $stream --query "events[*].message" --output text > "$EksDir/$safeName.log"
}

# 4. Zip the archive
Write-Host "Zipping the archive..."
Compress-Archive -Path "$ExportDir/*" -DestinationPath "planora-logs-archive.zip" -Force

# 5. Upload to S3
Write-Host "Uploading to S3..."
$BucketName = "planora-exported-logs-production-639182473909916708"
aws s3 cp "planora-logs-archive.zip" "s3://$BucketName/planora-logs-archive.zip"

# 6. Generate presigned URL
Write-Host "Generating Presigned URL..."
$Url = aws s3 presign "s3://$BucketName/planora-logs-archive.zip" --expires-in 604800
Write-Host ""
Write-Host "========================================="
Write-Host "PRESIGNED_URL=$Url"
Write-Host "========================================="
