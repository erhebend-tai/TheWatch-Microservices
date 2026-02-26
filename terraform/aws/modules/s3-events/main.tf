# S3 Event Notification Pipeline — Evidence Processing
# S3 -> SQS -> Lambda for chain-of-custody validation and content moderation
# Processes uploaded evidence: SHA-256 verification, Rekognition analysis, DynamoDB metadata

# ------------------------------------------------------------------------------
# SQS Dead Letter Queue
# ------------------------------------------------------------------------------
resource "aws_sqs_queue" "evidence_dlq" {
  name                      = "${lower(var.project_name)}-evidence-dlq-${var.environment}"
  message_retention_seconds = 1209600 # 14 days
  kms_master_key_id         = var.kms_key_arn

  tags = {
    Name        = "${var.project_name}-evidence-dlq-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# SQS Processing Queue
# ------------------------------------------------------------------------------
resource "aws_sqs_queue" "evidence_processing" {
  name                       = "${lower(var.project_name)}-evidence-processing-${var.environment}"
  visibility_timeout_seconds = 300
  message_retention_seconds  = 86400 # 1 day
  kms_master_key_id          = var.kms_key_arn

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.evidence_dlq.arn
    maxReceiveCount     = 3
  })

  tags = {
    Name        = "${var.project_name}-evidence-processing-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# SQS Policy — Allow S3 to send messages
# ------------------------------------------------------------------------------
resource "aws_sqs_queue_policy" "evidence_processing" {
  queue_url = aws_sqs_queue.evidence_processing.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "AllowS3Notification"
        Effect    = "Allow"
        Principal = { Service = "s3.amazonaws.com" }
        Action    = "sqs:SendMessage"
        Resource  = aws_sqs_queue.evidence_processing.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = var.evidence_bucket_arn
          }
        }
      }
    ]
  })
}

# ------------------------------------------------------------------------------
# S3 Bucket Notification -> SQS
# ------------------------------------------------------------------------------
resource "aws_s3_bucket_notification" "evidence" {
  bucket = var.evidence_bucket_id

  queue {
    queue_arn     = aws_sqs_queue.evidence_processing.arn
    events        = ["s3:ObjectCreated:*"]
    filter_prefix = ""
    filter_suffix = ""
  }

  depends_on = [aws_sqs_queue_policy.evidence_processing]
}

# ------------------------------------------------------------------------------
# IAM Role for Lambda
# ------------------------------------------------------------------------------
data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

resource "aws_iam_role" "evidence_processor" {
  name = "${var.project_name}-evidence-processor-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name        = "${var.project_name}-evidence-processor-role-${var.environment}"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy" "evidence_processor" {
  name = "${var.project_name}-evidence-processor-policy-${var.environment}"
  role = aws_iam_role.evidence_processor.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "S3ReadEvidence"
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:GetObjectVersion"
        ]
        Resource = "${var.evidence_bucket_arn}/*"
      },
      {
        Sid    = "RekognitionModeration"
        Effect = "Allow"
        Action = [
          "rekognition:DetectModerationLabels"
        ]
        Resource = "*"
      },
      {
        Sid    = "DynamoDBWriteMetadata"
        Effect = "Allow"
        Action = [
          "dynamodb:PutItem"
        ]
        Resource = "arn:aws:dynamodb:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:table/${var.dynamodb_table_name}"
      },
      {
        Sid    = "SQSProcessMessages"
        Effect = "Allow"
        Action = [
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:GetQueueAttributes"
        ]
        Resource = aws_sqs_queue.evidence_processing.arn
      },
      {
        Sid    = "CloudWatchLogs"
        Effect = "Allow"
        Action = [
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "${aws_cloudwatch_log_group.evidence_processor.arn}:*"
      },
      {
        Sid    = "KMSDecrypt"
        Effect = "Allow"
        Action = [
          "kms:Decrypt",
          "kms:GenerateDataKey"
        ]
        Resource = var.kms_key_arn
      }
    ]
  })
}

# ------------------------------------------------------------------------------
# CloudWatch Log Group
# ------------------------------------------------------------------------------
resource "aws_cloudwatch_log_group" "evidence_processor" {
  name              = "/aws/lambda/${var.project_name}-evidence-processor-${var.environment}"
  retention_in_days = 30

  tags = {
    Name        = "${var.project_name}-evidence-processor-logs-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# Lambda Function — Evidence Processor
# ------------------------------------------------------------------------------
data "archive_file" "evidence_processor" {
  type        = "zip"
  output_path = "${path.module}/lambda/evidence_processor.zip"

  source {
    content  = <<-PYTHON
import json
import hashlib
import boto3
import os
import uuid
from datetime import datetime, timezone

s3_client = boto3.client('s3')
rekognition_client = boto3.client('rekognition')
dynamodb_client = boto3.resource('dynamodb')

TABLE_NAME = os.environ.get('DYNAMODB_TABLE_NAME', '')


def lambda_handler(event, context):
    """Process S3 evidence uploads: verify SHA-256 chain-of-custody,
    run Rekognition moderation, and write metadata to DynamoDB."""

    table = dynamodb_client.Table(TABLE_NAME)

    for record in event.get('Records', []):
        body = json.loads(record.get('body', '{}'))

        for s3_record in body.get('Records', []):
            bucket = s3_record['s3']['bucket']['name']
            key = s3_record['s3']['object']['key']
            size = s3_record['s3']['object'].get('size', 0)
            event_time = s3_record.get('eventTime', datetime.now(timezone.utc).isoformat())

            # Download object and compute SHA-256
            response = s3_client.get_object(Bucket=bucket, Key=key)
            obj_body = response['Body'].read()
            sha256_hash = hashlib.sha256(obj_body).hexdigest()

            # Run Rekognition moderation analysis (images only)
            moderation_labels = []
            content_type = response.get('ContentType', '')
            if content_type.startswith('image/'):
                try:
                    moderation_response = rekognition_client.detect_moderation_labels(
                        Image={'S3Object': {'Bucket': bucket, 'Name': key}},
                        MinConfidence=50.0
                    )
                    moderation_labels = [
                        {
                            'Name': label['Name'],
                            'Confidence': str(label['Confidence']),
                            'ParentName': label.get('ParentName', '')
                        }
                        for label in moderation_response.get('ModerationLabels', [])
                    ]
                except Exception as e:
                    print(f"Rekognition error for {key}: {str(e)}")

            # Write metadata to DynamoDB
            now = datetime.now(timezone.utc)
            event_id = str(uuid.uuid4())

            table.put_item(Item={
                'ServiceNameDate': f"EvidenceProcessor#{now.strftime('%Y-%m-%d')}",
                'TimestampEventId': f"{now.isoformat()}#{event_id}",
                'EventType': 'EvidenceUploaded',
                'Bucket': bucket,
                'Key': key,
                'Size': size,
                'SHA256': sha256_hash,
                'ContentType': content_type,
                'ModerationLabels': moderation_labels,
                'UploadTime': event_time,
                'ProcessedAt': now.isoformat()
            })

            print(f"Processed evidence: s3://{bucket}/{key} SHA256={sha256_hash} labels={len(moderation_labels)}")

    return {'statusCode': 200, 'body': json.dumps('Evidence processed successfully')}
    PYTHON
    filename = "lambda_function.py"
  }
}

resource "aws_lambda_function" "evidence_processor" {
  function_name    = "${var.project_name}-evidence-processor-${var.environment}"
  description      = "Processes evidence uploads: SHA-256 verification, Rekognition moderation, DynamoDB metadata"
  role             = aws_iam_role.evidence_processor.arn
  handler          = "lambda_function.lambda_handler"
  runtime          = "python3.12"
  timeout          = 120
  memory_size      = 512
  filename         = data.archive_file.evidence_processor.output_path
  source_code_hash = data.archive_file.evidence_processor.output_base64sha256

  environment {
    variables = {
      DYNAMODB_TABLE_NAME = var.dynamodb_table_name
    }
  }

  depends_on = [aws_cloudwatch_log_group.evidence_processor]

  tags = {
    Name        = "${var.project_name}-evidence-processor-${var.environment}"
    Environment = var.environment
  }
}

# ------------------------------------------------------------------------------
# Lambda Event Source Mapping (SQS -> Lambda)
# ------------------------------------------------------------------------------
resource "aws_lambda_event_source_mapping" "evidence_sqs" {
  event_source_arn = aws_sqs_queue.evidence_processing.arn
  function_name    = aws_lambda_function.evidence_processor.arn
  batch_size       = 5
  enabled          = true
}
