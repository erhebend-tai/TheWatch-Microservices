output "lambda_function_arn" {
  description = "ARN of the evidence processor Lambda function"
  value       = aws_lambda_function.evidence_processor.arn
}

output "sqs_queue_arn" {
  description = "ARN of the evidence processing SQS queue"
  value       = aws_sqs_queue.evidence_processing.arn
}

output "dlq_arn" {
  description = "ARN of the dead letter queue for failed evidence processing"
  value       = aws_sqs_queue.evidence_dlq.arn
}
