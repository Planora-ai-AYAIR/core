#!/bin/bash
set -e

# Prevent Git Bash (MinGW) on Windows from converting /aws/... to a local C:/ file path
export MSYS_NO_PATHCONV=1

EXPORT_DIR="planora-logs-export"
mkdir -p "$EXPORT_DIR"

# 1. Kubernetes Pod Logs
echo "Extracting Kubernetes Pod Logs..."
KUBE_DIR="$EXPORT_DIR/kubernetes"
mkdir -p "$KUBE_DIR"
PODS=$(kubectl get pods -n planora -o jsonpath="{.items[*].metadata.name}")
for pod in $PODS; do
    if [ -n "$pod" ]; then
        echo "Fetching logs for $pod"
        kubectl logs "$pod" -n planora --all-containers=true > "$KUBE_DIR/$pod.log" 2>/dev/null || true
    fi
done

# 2. RDS Database Logs
echo "Extracting RDS PostgreSQL Logs..."
RDS_DIR="$EXPORT_DIR/rds"
mkdir -p "$RDS_DIR"
LOG_FILES=$(aws rds describe-db-log-files --db-instance-identifier planora-postgres-production --query "DescribeDBLogFiles[*].LogFileName" --output text)
# Download only the last 10 logs to save time
RECENT_LOGS=$(echo "$LOG_FILES" | tr '\t' '\n' | tr ' ' '\n' | tail -n 10)
for log in $RECENT_LOGS; do
    if [ -n "$log" ]; then
        SAFE_NAME=$(echo "$log" | tr '/' '_')
        echo "Downloading RDS log $log"
        aws rds download-db-log-file-portion --db-instance-identifier planora-postgres-production --log-file-name "$log" --starting-token 0 --output text > "$RDS_DIR/$SAFE_NAME"
    fi
done

# 3. EKS Control Plane Logs (CloudWatch)
echo "Extracting EKS Control Plane Logs..."
EKS_DIR="$EXPORT_DIR/eks-control-plane"
mkdir -p "$EKS_DIR"
# Get the streams
STREAMS=$(aws logs describe-log-streams --log-group-name "/aws/eks/planora-eks-production/cluster" --order-by LastEventTime --descending --limit 5 --query "logStreams[*].logStreamName" --output text)
for stream in $STREAMS; do
    if [ -n "$stream" ]; then
        SAFE_NAME=$(echo "$stream" | tr '/' '_')
        echo "Downloading CloudWatch stream $stream"
        aws logs get-log-events --log-group-name "/aws/eks/planora-eks-production/cluster" --log-stream-name "$stream" --query "events[*].message" --output text > "$EKS_DIR/$SAFE_NAME.log"
    fi
done

# 4. Zip the archive
echo "Zipping the archive..."
# Using zip (standard in most bash environments). If zip is unavailable, use tar -czf planora-logs-archive.tar.gz
zip -r planora-logs-archive.zip "$EXPORT_DIR"

# 5. Upload to S3
echo "Uploading to S3..."
BUCKET_NAME="planora-exported-logs-production-639182473909916708"
aws s3 cp "planora-logs-archive.zip" "s3://$BUCKET_NAME/planora-logs-archive.zip"

# 6. Generate presigned URL
echo "Generating Presigned URL..."
URL=$(aws s3 presign "s3://$BUCKET_NAME/planora-logs-archive.zip" --expires-in 604800)
echo ""
echo "========================================="
echo "PRESIGNED_URL=$URL"
echo "========================================="
