output "pipeline_arn" {
  description = "ARN of the CodePipeline"
  value       = aws_codepipeline.main.arn
}

output "build_project_arn" {
  description = "ARN of the CodeBuild build project"
  value       = aws_codebuild_project.build.arn
}

output "artifact_bucket" {
  description = "Name of the S3 bucket used for pipeline artifacts"
  value       = aws_s3_bucket.artifacts.bucket
}
