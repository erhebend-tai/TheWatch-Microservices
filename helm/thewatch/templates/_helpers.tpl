{{/*
TheWatch Helm Chart — Template Helpers
*/}}

{{/*
Chart name (truncated to 63 chars)
*/}}
{{- define "thewatch.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Fully qualified app name (release-chart, truncated to 63 chars)
*/}}
{{- define "thewatch.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "thewatch.labels" -}}
helm.sh/chart: {{ include "thewatch.name" . }}-{{ .Chart.Version | replace "+" "_" }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/part-of: thewatch
{{- end }}

{{/*
Service-specific labels
*/}}
{{- define "thewatch.serviceLabels" -}}
app.kubernetes.io/name: {{ .serviceName }}
app.kubernetes.io/instance: {{ .releaseName }}
app.kubernetes.io/component: {{ .serviceName }}
{{- end }}

{{/*
Service-specific selector labels
*/}}
{{- define "thewatch.selectorLabels" -}}
app.kubernetes.io/name: {{ .serviceName }}
app.kubernetes.io/instance: {{ .releaseName }}
{{- end }}

{{/*
Full image path for a service
*/}}
{{- define "thewatch.image" -}}
{{- $registry := .global.image.registry -}}
{{- $repository := .svcValues.image.repository -}}
{{- $tag := default .global.image.tag (.svcValues.image).tag -}}
{{- printf "%s/%s:%s" $registry $repository $tag -}}
{{- end }}

{{/*
SQL Server connection string
*/}}
{{- define "thewatch.sqlConnectionString" -}}
Server={{ .Values.sqlserver.host }},{{ .Values.sqlserver.port }};Database={{ .database }};User Id={{ .Values.sqlserver.user }};Password=$(DB_PASSWORD);TrustServerCertificate=true
{{- end }}

{{/*
PostgreSQL connection string
*/}}
{{- define "thewatch.pgConnectionString" -}}
Host={{ .Values.postgres.host }};Port={{ .Values.postgres.port }};Database={{ .database }};Username={{ .Values.postgres.user }};Password=$(DB_PASSWORD)
{{- end }}
