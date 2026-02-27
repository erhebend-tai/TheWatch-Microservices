using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates Apache ECharts dashboards and Apache Superset analytics infrastructure.
/// Per-service: metrics export configuration.
/// Per-domain: Superset Helm chart, ECharts dashboard web app, docker-compose files.
/// </summary>
public sealed class ApacheAnalyticsGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public ApacheAnalyticsGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
    {
        _engine = engine;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Generates per-service metrics export configuration.
    /// </summary>
    public async Task GenerateAsync(
        ServiceDescriptor service,
        string serviceRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        _logger.LogDebug("  Generating analytics config for {Service}...", service.PascalName);
        var model = new { Service = service, Config = _config };

        await emitter.EmitAsync(
            Path.Combine(serviceRoot, "deploy", "analytics", "metrics-export.yaml"),
            _engine.Render(MetricsTemplates.MetricsExport, model), ct);
    }

    /// <summary>
    /// Generates domain-level Superset and ECharts analytics infrastructure.
    /// </summary>
    public async Task GenerateDomainAnalyticsAsync(
        DomainDescriptor domain,
        string domainRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        if (domain.Services.Count == 0) return;

        _logger.LogDebug("  Generating Apache analytics for {Domain}...", domain.DomainName);
        var model = new { Domain = domain, Config = _config };

        // ── Superset ───────────────────────────────────────────────────
        if (_config.Features.Superset)
        {
            var ssHelm = Path.Combine(domainRoot, "infrastructure", "superset", "helm");
            var ssK8s = Path.Combine(domainRoot, "infrastructure", "superset", "k8s");

            await emitter.EmitAsync(
                Path.Combine(ssHelm, "Chart.yaml"),
                _engine.Render(SupersetTemplates.HelmChart, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ssHelm, "values.yaml"),
                _engine.Render(SupersetTemplates.HelmValues, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ssHelm, "templates", "superset-deployment.yaml"),
                _engine.Render(SupersetTemplates.Deployment, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ssHelm, "templates", "superset-configmap.yaml"),
                _engine.Render(SupersetTemplates.ConfigMap, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ssHelm, "templates", "superset-secret.yaml"),
                _engine.Render(SupersetTemplates.Secret, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ssHelm, "templates", "superset-ingress.yaml"),
                _engine.Render(SupersetTemplates.Ingress, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ssK8s, "namespace.yaml"),
                _engine.Render(SupersetTemplates.Namespace, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ssK8s, "database-connections.yaml"),
                _engine.Render(SupersetTemplates.DatabaseConnections, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ssK8s, "dashboards.yaml"),
                _engine.Render(SupersetTemplates.Dashboards, model), ct);

            await emitter.EmitAsync(
                Path.Combine(domainRoot, "infrastructure", "superset", "docker-compose.yaml"),
                _engine.Render(SupersetTemplates.DockerCompose, model), ct);
        }

        // ── ECharts ────────────────────────────────────────────────────
        if (_config.Features.ECharts)
        {
            var ecHelm = Path.Combine(domainRoot, "infrastructure", "echarts", "helm");
            var ecSrc = Path.Combine(domainRoot, "infrastructure", "echarts", "src");
            var ecK8s = Path.Combine(domainRoot, "infrastructure", "echarts", "k8s");

            await emitter.EmitAsync(
                Path.Combine(ecHelm, "Chart.yaml"),
                _engine.Render(EChartsTemplates.HelmChart, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecHelm, "values.yaml"),
                _engine.Render(EChartsTemplates.HelmValues, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecHelm, "templates", "echarts-deployment.yaml"),
                _engine.Render(EChartsTemplates.Deployment, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecHelm, "templates", "echarts-service.yaml"),
                _engine.Render(EChartsTemplates.Service, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecHelm, "templates", "echarts-configmap.yaml"),
                _engine.Render(EChartsTemplates.HelmConfigMap, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecHelm, "templates", "echarts-ingress.yaml"),
                _engine.Render(EChartsTemplates.HelmIngress, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecSrc, "index.html"),
                _engine.Render(EChartsTemplates.IndexHtml, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecSrc, "js", "dashboard.js"),
                _engine.Render(EChartsTemplates.DashboardJs, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecSrc, "js", "config.js"),
                _engine.Render(EChartsTemplates.ConfigJs, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecSrc, "css", "dashboard.css"),
                _engine.Render(EChartsTemplates.DashboardCss, model), ct);

            await emitter.EmitAsync(
                Path.Combine(domainRoot, "infrastructure", "echarts", "Dockerfile"),
                _engine.Render(EChartsTemplates.Dockerfile, model), ct);

            await emitter.EmitAsync(
                Path.Combine(domainRoot, "infrastructure", "echarts", "docker-compose.yaml"),
                _engine.Render(EChartsTemplates.DockerCompose, model), ct);

            await emitter.EmitAsync(
                Path.Combine(ecK8s, "namespace.yaml"),
                _engine.Render(EChartsTemplates.Namespace, model), ct);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Per-Service Metrics Templates
    // ═══════════════════════════════════════════════════════════════════
    private static class MetricsTemplates
    {
        public const string MetricsExport = """
            # Metrics Export Configuration for {{ Service.PascalName }}
            # Defines Prometheus scrape targets and metric definitions for Superset/ECharts
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Service.KebabName }}-metrics-export
              namespace: {{ Service.KebabName }}
              labels:
                app.kubernetes.io/name: {{ Service.KebabName }}
                app.kubernetes.io/component: metrics-export
                thewatch.io/domain: {{ Service.DomainName | string.downcase }}
              annotations:
                prometheus.io/scrape: "true"
                prometheus.io/port: "8080"
                prometheus.io/path: "/metrics"
            data:
              metrics-config.yaml: |
                service:
                  name: {{ Service.KebabName }}
                  domain: {{ Service.DomainName | string.downcase }}
                  metricsPort: 8080
                  metricsPath: /metrics

                prometheus:
                  scrapeInterval: 15s
                  scrapeTimeout: 10s

                metrics:
                  # HTTP request metrics
                  - name: http_requests_total
                    type: counter
                    help: "Total HTTP requests"
                    labels: [method, path, status]
                  - name: http_request_duration_seconds
                    type: histogram
                    help: "HTTP request duration in seconds"
                    labels: [method, path]
                    buckets: [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10]
                  - name: http_request_size_bytes
                    type: histogram
                    help: "HTTP request body size in bytes"
                  - name: http_response_size_bytes
                    type: histogram
                    help: "HTTP response body size in bytes"

                  # Application metrics
                  - name: active_connections
                    type: gauge
                    help: "Number of active connections"
                  - name: db_query_duration_seconds
                    type: histogram
                    help: "Database query duration"
                    labels: [operation, table]
                  - name: cache_hits_total
                    type: counter
                    help: "Cache hit count"
                  - name: cache_misses_total
                    type: counter
                    help: "Cache miss count"
            {{~ if Config.Features.Kafka ~}}

                  # Kafka metrics
                  - name: kafka_consumer_lag
                    type: gauge
                    help: "Kafka consumer lag per topic/partition"
                    labels: [topic, partition, consumer_group]
                  - name: kafka_messages_produced_total
                    type: counter
                    help: "Total messages produced"
                    labels: [topic]
                  - name: kafka_messages_consumed_total
                    type: counter
                    help: "Total messages consumed"
                    labels: [topic, consumer_group]
            {{~ end ~}}

                dashboards:
                  superset:
                    datasets:
                      - name: "{{ Service.PascalName }} Request Metrics"
                        sql: "SELECT * FROM http_requests_total WHERE service='{{ Service.KebabName }}'"
                      - name: "{{ Service.PascalName }} Latency"
                        sql: "SELECT * FROM http_request_duration_seconds WHERE service='{{ Service.KebabName }}'"
                  echarts:
                    panels:
                      - title: "Request Rate"
                        query: "rate(http_requests_total{service=\"{{ Service.KebabName }}\"}[5m])"
                      - title: "P95 Latency"
                        query: "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{service=\"{{ Service.KebabName }}\"}[5m]))"
                      - title: "Error Rate"
                        query: "rate(http_requests_total{service=\"{{ Service.KebabName }}\",status=~\"5..\"}[5m])"
            """;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Superset Templates
    // ═══════════════════════════════════════════════════════════════════
    private static class SupersetTemplates
    {
        public const string HelmChart = """
            apiVersion: v2
            name: {{ Domain.KebabName }}-superset
            description: Apache Superset analytics platform for the {{ Domain.PascalName }} domain
            type: application
            version: 1.0.0
            appVersion: "{{ Config.Apache.SupersetVersion }}"
            keywords:
              - superset
              - analytics
              - bi
              - visualization
              - {{ Domain.KebabName }}
            maintainers:
              - name: TheWatch Platform Team
            """;

        public const string HelmValues = """
            # Apache Superset configuration for {{ Domain.PascalName }}
            superset:
              version: "{{ Config.Apache.SupersetVersion }}"

              web:
                replicas: 1
                resources:
                  requests:
                    cpu: "250m"
                    memory: "512Mi"
                  limits:
                    cpu: "1"
                    memory: "2Gi"

              worker:
                replicas: {{ Config.Apache.SupersetWorkers }}
                resources:
                  requests:
                    cpu: "250m"
                    memory: "512Mi"
                  limits:
                    cpu: "1"
                    memory: "2Gi"

              beat:
                replicas: 1
                resources:
                  requests:
                    cpu: "100m"
                    memory: "256Mi"
                  limits:
                    cpu: "500m"
                    memory: "512Mi"

              initDb:
                enabled: true
                adminUser: admin
                adminEmail: admin@thewatch.local

              config:
                rowLimit: 50000
                mapboxApiKey: ""
                featureFlags:
                  DASHBOARD_NATIVE_FILTERS: true
                  DASHBOARD_CROSS_FILTERS: true
                  ENABLE_TEMPLATE_PROCESSING: true
                  ALERT_REPORTS: true
                  EMBEDDED_SUPERSET: true

            postgresql:
              enabled: true
              auth:
                database: superset
                username: superset
              persistence:
                size: 5Gi

            redis:
              enabled: true
              architecture: standalone
              auth:
                enabled: false

            ingress:
              enabled: true
              hostname: "{{ Domain.KebabName }}-superset.thewatch.local"

            namespace: {{ Domain.KebabName }}-superset
            """;

        public const string Deployment = """
            # Apache Superset Deployment for {{ Domain.PascalName }}
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.KebabName }}-superset-web
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: superset-web
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-superset
            spec:
              replicas: {{ "{{" }} .Values.superset.web.replicas {{ "}}" }}
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-superset-web
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-superset-web
                spec:
                  containers:
                    - name: superset
                      image: apache/superset:{{ "{{" }} .Values.superset.version {{ "}}" }}
                      command: ["gunicorn", "-w", "4", "-k", "gevent", "--timeout", "120", "-b", "0.0.0.0:8088", "superset.app:create_app()"]
                      ports:
                        - containerPort: 8088
                      envFrom:
                        - configMapRef:
                            name: {{ Domain.KebabName }}-superset-config
                        - secretRef:
                            name: {{ Domain.KebabName }}-superset-secret
                      resources:
                        requests:
                          cpu: {{ "{{" }} .Values.superset.web.resources.requests.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.superset.web.resources.requests.memory {{ "}}" }}
                        limits:
                          cpu: {{ "{{" }} .Values.superset.web.resources.limits.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.superset.web.resources.limits.memory {{ "}}" }}
                      livenessProbe:
                        httpGet:
                          path: /health
                          port: 8088
                        initialDelaySeconds: 30
                        periodSeconds: 10
                      readinessProbe:
                        httpGet:
                          path: /health
                          port: 8088
                        initialDelaySeconds: 15
                        periodSeconds: 5
            ---
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.KebabName }}-superset-worker
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: superset-worker
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-superset
            spec:
              replicas: {{ "{{" }} .Values.superset.worker.replicas {{ "}}" }}
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-superset-worker
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-superset-worker
                spec:
                  containers:
                    - name: worker
                      image: apache/superset:{{ "{{" }} .Values.superset.version {{ "}}" }}
                      command: ["celery", "--app=superset.tasks.celery_app:app", "worker", "-O", "fair", "-c", "4"]
                      envFrom:
                        - configMapRef:
                            name: {{ Domain.KebabName }}-superset-config
                        - secretRef:
                            name: {{ Domain.KebabName }}-superset-secret
                      resources:
                        requests:
                          cpu: {{ "{{" }} .Values.superset.worker.resources.requests.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.superset.worker.resources.requests.memory {{ "}}" }}
                        limits:
                          cpu: {{ "{{" }} .Values.superset.worker.resources.limits.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.superset.worker.resources.limits.memory {{ "}}" }}
            ---
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.KebabName }}-superset-beat
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: superset-beat
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-superset
            spec:
              replicas: 1
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-superset-beat
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-superset-beat
                spec:
                  containers:
                    - name: beat
                      image: apache/superset:{{ "{{" }} .Values.superset.version {{ "}}" }}
                      command: ["celery", "--app=superset.tasks.celery_app:app", "beat", "--pidfile=/tmp/celerybeat.pid", "-s", "/tmp/celerybeat-schedule"]
                      envFrom:
                        - configMapRef:
                            name: {{ Domain.KebabName }}-superset-config
                        - secretRef:
                            name: {{ Domain.KebabName }}-superset-secret
                      resources:
                        requests:
                          cpu: {{ "{{" }} .Values.superset.beat.resources.requests.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.superset.beat.resources.requests.memory {{ "}}" }}
                        limits:
                          cpu: {{ "{{" }} .Values.superset.beat.resources.limits.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.superset.beat.resources.limits.memory {{ "}}" }}
            ---
            apiVersion: batch/v1
            kind: Job
            metadata:
              name: {{ Domain.KebabName }}-superset-init-db
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: superset-init
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-superset
            spec:
              template:
                spec:
                  restartPolicy: OnFailure
                  containers:
                    - name: init
                      image: apache/superset:{{ "{{" }} .Values.superset.version {{ "}}" }}
                      command: ["/bin/sh", "-c"]
                      args:
                        - |
                          superset db upgrade &&
                          superset fab create-admin \
                            --username {{ "{{" }} .Values.superset.initDb.adminUser {{ "}}" }} \
                            --firstname Admin \
                            --lastname User \
                            --email {{ "{{" }} .Values.superset.initDb.adminEmail {{ "}}" }} \
                            --password admin &&
                          superset init
                      envFrom:
                        - configMapRef:
                            name: {{ Domain.KebabName }}-superset-config
                        - secretRef:
                            name: {{ Domain.KebabName }}-superset-secret
            ---
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.KebabName }}-superset
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            spec:
              selector:
                app: {{ Domain.KebabName }}-superset-web
              ports:
                - port: 8088
                  targetPort: 8088
            """;

        public const string ConfigMap = """
            # Superset Configuration for {{ Domain.PascalName }}
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Domain.KebabName }}-superset-config
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            data:
              SUPERSET_ENV: "production"
              SUPERSET_PORT: "8088"
              SUPERSET_ROW_LIMIT: "{{ "{{" }} .Values.superset.config.rowLimit {{ "}}" }}"
              MAPBOX_API_KEY: "{{ "{{" }} .Values.superset.config.mapboxApiKey {{ "}}" }}"
              FEATURE_FLAGS: '{"DASHBOARD_NATIVE_FILTERS": true, "DASHBOARD_CROSS_FILTERS": true, "ENABLE_TEMPLATE_PROCESSING": true, "ALERT_REPORTS": true, "EMBEDDED_SUPERSET": true}'
              superset_config.py: |
                import os

                SECRET_KEY = os.environ.get("SUPERSET_SECRET_KEY", "thewatch-superset-{{ Domain.KebabName }}")
                SQLALCHEMY_DATABASE_URI = os.environ.get("DATABASE_URL", "postgresql://superset:superset@{{ Domain.KebabName }}-superset-db:5432/superset")

                class CeleryConfig:
                    broker_url = os.environ.get("REDIS_URL", "redis://{{ Domain.KebabName }}-superset-redis:6379/0")
                    result_backend = os.environ.get("REDIS_URL", "redis://{{ Domain.KebabName }}-superset-redis:6379/1")
                    task_annotations = {"sql_lab.get_sql_results": {"rate_limit": "100/s"}}

                CELERY_CONFIG = CeleryConfig

                ENABLE_PROXY_FIX = True
                PREFERRED_DATABASES = ["PostgreSQL", "Microsoft SQL Server"]

                DATA_CACHE_CONFIG = {
                    "CACHE_TYPE": "RedisCache",
                    "CACHE_DEFAULT_TIMEOUT": 300,
                    "CACHE_KEY_PREFIX": "superset_data_",
                    "CACHE_REDIS_URL": os.environ.get("REDIS_URL", "redis://{{ Domain.KebabName }}-superset-redis:6379/2"),
                }

                FILTER_STATE_CACHE_CONFIG = {
                    "CACHE_TYPE": "RedisCache",
                    "CACHE_DEFAULT_TIMEOUT": 600,
                    "CACHE_KEY_PREFIX": "superset_filter_",
                    "CACHE_REDIS_URL": os.environ.get("REDIS_URL", "redis://{{ Domain.KebabName }}-superset-redis:6379/3"),
                }
            """;

        public const string Secret = """
            # Superset Secrets for {{ Domain.PascalName }}
            apiVersion: v1
            kind: Secret
            metadata:
              name: {{ Domain.KebabName }}-superset-secret
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            type: Opaque
            stringData:
              SUPERSET_SECRET_KEY: "CHANGE-ME-{{ Domain.KebabName }}-superset-secret"
              DATABASE_URL: "postgresql://superset:superset@{{ Domain.KebabName }}-superset-db:5432/superset"
              REDIS_URL: "redis://{{ Domain.KebabName }}-superset-redis:6379/0"
            """;

        public const string Ingress = """
            # Superset Ingress for {{ Domain.PascalName }}
            apiVersion: networking.k8s.io/v1
            kind: Ingress
            metadata:
              name: {{ Domain.KebabName }}-superset-ingress
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              annotations:
                kubernetes.io/ingress.class: nginx
                cert-manager.io/cluster-issuer: letsencrypt-prod
                nginx.ingress.kubernetes.io/proxy-body-size: "50m"
                nginx.ingress.kubernetes.io/proxy-read-timeout: "120"
            spec:
              tls:
                - hosts:
                    - {{ "{{" }} .Values.ingress.hostname {{ "}}" }}
                  secretName: {{ Domain.KebabName }}-superset-tls
              rules:
                - host: {{ "{{" }} .Values.ingress.hostname {{ "}}" }}
                  http:
                    paths:
                      - path: /
                        pathType: Prefix
                        backend:
                          service:
                            name: {{ Domain.KebabName }}-superset
                            port:
                              number: 8088
            """;

        public const string Namespace = """
            apiVersion: v1
            kind: Namespace
            metadata:
              name: {{ Domain.KebabName }}-superset
              labels:
                app.kubernetes.io/name: superset
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            """;

        public const string DatabaseConnections = """
            # Superset Database Connections for {{ Domain.PascalName }}
            # Pre-configured connections to all service databases in this domain
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Domain.KebabName }}-superset-db-connections
              namespace: {{ Domain.KebabName }}-superset
              labels:
                app.kubernetes.io/name: superset
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            data:
              database-connections.json: |
                {
                  "databases": [
            {{~ for svc in Domain.Services ~}}
                    {
                      "database_name": "{{ svc.PascalName }}",
                      "sqlalchemy_uri": "postgresql://app:secret@{{ Domain.KebabName | string.downcase }}-postgres:5432/{{ svc.KebabName }}",
                      "expose_in_sqllab": true,
                      "allow_ctas": false,
                      "allow_cvas": false,
                      "allow_dml": false,
                      "extra": "{\"metadata_params\": {}, \"engine_params\": {\"pool_size\": 5, \"max_overflow\": 10}}",
                      "tables": [
            {{~ for schema in svc.Schemas ~}}
            {{~ if schema.IsEntity ~}}
                        {"table_name": "{{ schema.PluralName ?? schema.Name + 's' }}", "schema": "public"}{{ if !for.last }},{{ end }}
            {{~ end ~}}
            {{~ end ~}}
                      ]
                    }{{ if !for.last }},{{ end }}
            {{~ end ~}}
                  ]
                }
            """;

        public const string Dashboards = """
            # Superset Pre-built Dashboards for {{ Domain.PascalName }}
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Domain.KebabName }}-superset-dashboards
              namespace: {{ Domain.KebabName }}-superset
              labels:
                app.kubernetes.io/name: superset
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            data:
              dashboards.json: |
                {
                  "dashboards": [
                    {
                      "dashboard_title": "{{ Domain.PascalName }} - Service Health",
                      "slug": "{{ Domain.KebabName }}-health",
                      "position_json": "{}",
                      "charts": [
                        {
                          "slice_name": "Request Rate by Service",
                          "viz_type": "echarts_timeseries_bar",
                          "datasource": "prometheus",
                          "params": {
                            "metric": "rate(http_requests_total[5m])",
                            "groupby": ["service"],
                            "time_range": "Last 24 hours"
                          }
                        },
                        {
                          "slice_name": "P95 Latency by Service",
                          "viz_type": "echarts_timeseries_line",
                          "datasource": "prometheus",
                          "params": {
                            "metric": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))",
                            "groupby": ["service"],
                            "time_range": "Last 24 hours"
                          }
                        },
                        {
                          "slice_name": "Error Rate",
                          "viz_type": "echarts_area",
                          "datasource": "prometheus",
                          "params": {
                            "metric": "rate(http_requests_total{status=~\"5..\"}[5m]) / rate(http_requests_total[5m])",
                            "groupby": ["service"],
                            "time_range": "Last 24 hours"
                          }
                        }
            {{~ if Config.Features.Kafka ~}}
                        ,{
                          "slice_name": "Kafka Consumer Lag",
                          "viz_type": "echarts_timeseries_line",
                          "datasource": "prometheus",
                          "params": {
                            "metric": "kafka_consumer_lag",
                            "groupby": ["topic", "consumer_group"],
                            "time_range": "Last 6 hours"
                          }
                        }
            {{~ end ~}}
                      ]
                    },
                    {
                      "dashboard_title": "{{ Domain.PascalName }} - Database Analytics",
                      "slug": "{{ Domain.KebabName }}-db",
                      "charts": [
            {{~ for svc in Domain.Services ~}}
                        {
                          "slice_name": "{{ svc.PascalName }} - Record Counts",
                          "viz_type": "echarts_timeseries_bar",
                          "datasource": "{{ svc.PascalName }}",
                          "params": {
                            "viz_type": "table",
                            "groupby": ["table_name"],
                            "metrics": ["count"]
                          }
                        }{{ if !for.last }},{{ end }}
            {{~ end ~}}
                      ]
                    }
                  ]
                }
            """;

        public const string DockerCompose = """
            # Superset local development stack for {{ Domain.PascalName }}
            version: "3.9"

            services:
              superset:
                image: apache/superset:{{ Config.Apache.SupersetVersion }}
                ports:
                  - "8088:8088"
                environment:
                  SUPERSET_SECRET_KEY: "dev-secret-{{ Domain.KebabName }}"
                  DATABASE_URL: "postgresql://superset:superset@superset-db:5432/superset"
                  REDIS_URL: "redis://superset-redis:6379/0"
                  SUPERSET_ENV: "development"
                depends_on:
                  superset-db:
                    condition: service_healthy
                  superset-redis:
                    condition: service_healthy
                command: >
                  bash -c "
                    superset db upgrade &&
                    superset fab create-admin --username admin --firstname Admin --lastname User --email admin@thewatch.local --password admin || true &&
                    superset init &&
                    gunicorn -w 2 -k gevent --timeout 120 -b 0.0.0.0:8088 'superset.app:create_app()'
                  "
                healthcheck:
                  test: ["CMD", "curl", "-sf", "http://localhost:8088/health"]
                  interval: 15s
                  timeout: 5s
                  retries: 5
                  start_period: 60s

              superset-worker:
                image: apache/superset:{{ Config.Apache.SupersetVersion }}
                environment:
                  SUPERSET_SECRET_KEY: "dev-secret-{{ Domain.KebabName }}"
                  DATABASE_URL: "postgresql://superset:superset@superset-db:5432/superset"
                  REDIS_URL: "redis://superset-redis:6379/0"
                command: ["celery", "--app=superset.tasks.celery_app:app", "worker", "-O", "fair", "-c", "2"]
                depends_on:
                  superset:
                    condition: service_healthy

              superset-db:
                image: postgres:18-alpine
                environment:
                  POSTGRES_DB: superset
                  POSTGRES_USER: superset
                  POSTGRES_PASSWORD: superset
                volumes:
                  - superset-pgdata:/var/lib/postgresql/data
                healthcheck:
                  test: ["CMD-SHELL", "pg_isready -U superset"]
                  interval: 5s
                  timeout: 5s
                  retries: 5

              superset-redis:
                image: redis:7-alpine
                volumes:
                  - superset-redisdata:/data
                healthcheck:
                  test: ["CMD", "redis-cli", "ping"]
                  interval: 5s
                  timeout: 5s
                  retries: 5

            volumes:
              superset-pgdata:
              superset-redisdata:
            """;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ECharts Templates
    // ═══════════════════════════════════════════════════════════════════
    private static class EChartsTemplates
    {
        public const string HelmChart = """
            apiVersion: v2
            name: {{ Domain.KebabName }}-echarts-dashboard
            description: Apache ECharts metrics dashboard for the {{ Domain.PascalName }} domain
            type: application
            version: 1.0.0
            appVersion: "{{ Config.Apache.EChartsVersion }}"
            keywords:
              - echarts
              - dashboard
              - visualization
              - metrics
              - {{ Domain.KebabName }}
            maintainers:
              - name: TheWatch Platform Team
            """;

        public const string HelmValues = """
            # ECharts Dashboard configuration for {{ Domain.PascalName }}
            dashboard:
              replicas: 1
              image:
                repository: thewatch/{{ Domain.KebabName }}-echarts-dashboard
                tag: latest
                pullPolicy: IfNotPresent
              resources:
                requests:
                  cpu: "50m"
                  memory: "64Mi"
                limits:
                  cpu: "200m"
                  memory: "128Mi"

            prometheus:
              url: "http://prometheus.monitoring.svc.cluster.local:9090"

            ingress:
              enabled: true
              hostname: "{{ Domain.KebabName }}-dashboard.thewatch.local"

            namespace: {{ Domain.KebabName }}-echarts
            """;

        public const string Deployment = """
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.KebabName }}-echarts-dashboard
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: echarts-dashboard
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            spec:
              replicas: {{ "{{" }} .Values.dashboard.replicas {{ "}}" }}
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-echarts-dashboard
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-echarts-dashboard
                spec:
                  containers:
                    - name: dashboard
                      image: {{ "{{" }} .Values.dashboard.image.repository {{ "}}" }}:{{ "{{" }} .Values.dashboard.image.tag {{ "}}" }}
                      imagePullPolicy: {{ "{{" }} .Values.dashboard.image.pullPolicy {{ "}}" }}
                      ports:
                        - containerPort: 80
                      resources:
                        requests:
                          cpu: {{ "{{" }} .Values.dashboard.resources.requests.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.dashboard.resources.requests.memory {{ "}}" }}
                        limits:
                          cpu: {{ "{{" }} .Values.dashboard.resources.limits.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.dashboard.resources.limits.memory {{ "}}" }}
                      livenessProbe:
                        httpGet:
                          path: /
                          port: 80
                        periodSeconds: 30
                      readinessProbe:
                        httpGet:
                          path: /
                          port: 80
                        periodSeconds: 10
                      volumeMounts:
                        - name: config
                          mountPath: /usr/share/nginx/html/js/config.js
                          subPath: config.js
                  volumes:
                    - name: config
                      configMap:
                        name: {{ Domain.KebabName }}-echarts-config
            """;

        public const string Service = """
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.KebabName }}-echarts-dashboard
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            spec:
              selector:
                app: {{ Domain.KebabName }}-echarts-dashboard
              ports:
                - port: 80
                  targetPort: 80
            """;

        public const string HelmConfigMap = """
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Domain.KebabName }}-echarts-config
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            data:
              config.js: |
                window.DASHBOARD_CONFIG = {
                  prometheusUrl: "{{ "{{" }} .Values.prometheus.url {{ "}}" }}",
                  domain: "{{ Domain.KebabName }}",
                  refreshInterval: 30000,
                  services: [
            {{~ for svc in Domain.Services ~}}
                    { name: "{{ svc.PascalName }}", kebab: "{{ svc.KebabName }}", namespace: "{{ svc.KebabName }}" }{{ if !for.last }},{{ end }}
            {{~ end ~}}
                  ]
                };
            """;

        public const string HelmIngress = """
            apiVersion: networking.k8s.io/v1
            kind: Ingress
            metadata:
              name: {{ Domain.KebabName }}-echarts-ingress
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              annotations:
                kubernetes.io/ingress.class: nginx
                cert-manager.io/cluster-issuer: letsencrypt-prod
            spec:
              tls:
                - hosts:
                    - {{ "{{" }} .Values.ingress.hostname {{ "}}" }}
                  secretName: {{ Domain.KebabName }}-echarts-tls
              rules:
                - host: {{ "{{" }} .Values.ingress.hostname {{ "}}" }}
                  http:
                    paths:
                      - path: /
                        pathType: Prefix
                        backend:
                          service:
                            name: {{ Domain.KebabName }}-echarts-dashboard
                            port:
                              number: 80
            """;

        public const string IndexHtml = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>{{ Domain.PascalName }} — Service Dashboard</title>
              <script src="https://cdn.jsdelivr.net/npm/echarts@{{ Config.Apache.EChartsVersion }}/dist/echarts.min.js"></script>
              <link rel="stylesheet" href="css/dashboard.css">
            </head>
            <body>
              <header>
                <h1>{{ Domain.PascalName }} Domain Dashboard</h1>
                <div class="controls">
                  <span id="last-updated"></span>
                  <button id="theme-toggle" onclick="toggleTheme()">Toggle Theme</button>
                  <button onclick="refreshAll()">Refresh</button>
                </div>
              </header>

              <div class="dashboard-grid">
                <!-- Summary row -->
                <div class="card full-width">
                  <h2>Domain Overview</h2>
                  <div id="overview-chart" class="chart-container"></div>
                </div>

                <!-- Per-service panels -->
            {{~ for svc in Domain.Services ~}}
                <div class="card">
                  <h3>{{ svc.PascalName }}</h3>
                  <div class="chart-row">
                    <div id="chart-{{ svc.KebabName }}-rps" class="chart-sm"></div>
                    <div id="chart-{{ svc.KebabName }}-latency" class="chart-sm"></div>
                    <div id="chart-{{ svc.KebabName }}-errors" class="chart-sm"></div>
                  </div>
                </div>
            {{~ end ~}}

                <!-- Resource utilization -->
                <div class="card full-width">
                  <h2>Resource Utilization</h2>
                  <div class="chart-row">
                    <div id="chart-cpu" class="chart-md"></div>
                    <div id="chart-memory" class="chart-md"></div>
                  </div>
                </div>
            {{~ if Config.Features.Kafka ~}}

                <!-- Kafka metrics -->
                <div class="card full-width">
                  <h2>Kafka Consumer Lag</h2>
                  <div id="chart-kafka-lag" class="chart-container"></div>
                </div>
            {{~ end ~}}
              </div>

              <script src="js/config.js"></script>
              <script src="js/dashboard.js"></script>
            </body>
            </html>
            """;

        public const string DashboardJs = """
            // ECharts Dashboard for {{ Domain.PascalName }}
            // Auto-generated by MicroGen
            (function() {
              'use strict';

              const config = window.DASHBOARD_CONFIG || {
                prometheusUrl: 'http://localhost:9090',
                domain: '{{ Domain.KebabName }}',
                refreshInterval: 30000,
                services: [
            {{~ for svc in Domain.Services ~}}
                  { name: '{{ svc.PascalName }}', kebab: '{{ svc.KebabName }}', namespace: '{{ svc.KebabName }}' }{{ if !for.last }},{{ end }}
            {{~ end ~}}
                ]
              };

              let isDarkTheme = true;
              const charts = {};

              function getTheme() { return isDarkTheme ? 'dark' : null; }

              window.toggleTheme = function() {
                isDarkTheme = !isDarkTheme;
                document.body.classList.toggle('light-theme');
                Object.values(charts).forEach(c => { c.dispose(); });
                initCharts();
                refreshAll();
              };

              async function queryPrometheus(query, start, end, step) {
                const url = config.prometheusUrl + '/api/v1/query_range?' +
                  'query=' + encodeURIComponent(query) +
                  '&start=' + start + '&end=' + end + '&step=' + step;
                try {
                  const resp = await fetch(url);
                  const data = await resp.json();
                  return data.data?.result || [];
                } catch (e) {
                  console.warn('Prometheus query failed:', query, e);
                  return [];
                }
              }

              function createTimeseriesChart(domId, title, yAxisLabel) {
                const el = document.getElementById(domId);
                if (!el) return null;
                const chart = echarts.init(el, getTheme());
                chart.setOption({
                  title: { text: title, textStyle: { fontSize: 12 } },
                  tooltip: { trigger: 'axis' },
                  xAxis: { type: 'time' },
                  yAxis: { type: 'value', name: yAxisLabel },
                  series: [],
                  grid: { left: '10%', right: '5%', top: '20%', bottom: '15%' },
                  animation: false
                });
                charts[domId] = chart;
                return chart;
              }

              function updateChart(chart, results, formatter) {
                if (!chart || !results.length) return;
                const series = results.map((r, i) => ({
                  name: r.metric?.service || r.metric?.topic || 'series-' + i,
                  type: 'line',
                  smooth: true,
                  showSymbol: false,
                  data: r.values.map(v => [v[0] * 1000, formatter ? formatter(v[1]) : parseFloat(v[1])])
                }));
                chart.setOption({ series });
              }

              function initCharts() {
                // Overview
                const overviewEl = document.getElementById('overview-chart');
                if (overviewEl) {
                  const overview = echarts.init(overviewEl, getTheme());
                  overview.setOption({
                    title: { text: 'Request Rate — All Services' },
                    tooltip: { trigger: 'axis' },
                    legend: { type: 'scroll', bottom: 0 },
                    xAxis: { type: 'time' },
                    yAxis: { type: 'value', name: 'req/s' },
                    series: [],
                    grid: { left: '8%', right: '3%', top: '15%', bottom: '15%' }
                  });
                  charts['overview-chart'] = overview;
                }

                // Per-service
                config.services.forEach(svc => {
                  createTimeseriesChart('chart-' + svc.kebab + '-rps', 'RPS', 'req/s');
                  createTimeseriesChart('chart-' + svc.kebab + '-latency', 'P95 Latency', 'seconds');
                  createTimeseriesChart('chart-' + svc.kebab + '-errors', 'Error Rate', '%');
                });

                // Resources
                createTimeseriesChart('chart-cpu', 'CPU Usage', 'cores');
                createTimeseriesChart('chart-memory', 'Memory Usage', 'MB');
            {{~ if Config.Features.Kafka ~}}
                createTimeseriesChart('chart-kafka-lag', 'Consumer Lag', 'messages');
            {{~ end ~}}
              }

              window.refreshAll = async function() {
                const end = Math.floor(Date.now() / 1000);
                const start = end - 3600;
                const step = '30';

                // Overview
                const overviewData = await queryPrometheus(
                  'sum(rate(http_requests_total{domain="' + config.domain + '"}[5m])) by (service)',
                  start, end, step);
                updateChart(charts['overview-chart'], overviewData);

                // Per-service
                for (const svc of config.services) {
                  const rpsData = await queryPrometheus(
                    'rate(http_requests_total{service="' + svc.kebab + '"}[5m])', start, end, step);
                  updateChart(charts['chart-' + svc.kebab + '-rps'], rpsData);

                  const latencyData = await queryPrometheus(
                    'histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{service="' + svc.kebab + '"}[5m]))',
                    start, end, step);
                  updateChart(charts['chart-' + svc.kebab + '-latency'], latencyData);

                  const errorData = await queryPrometheus(
                    'rate(http_requests_total{service="' + svc.kebab + '",status=~"5.."}[5m]) / rate(http_requests_total{service="' + svc.kebab + '"}[5m]) * 100',
                    start, end, step);
                  updateChart(charts['chart-' + svc.kebab + '-errors'], errorData);
                }

                // Resources
                const cpuData = await queryPrometheus(
                  'sum(rate(container_cpu_usage_seconds_total{namespace=~".*' + config.domain + '.*"}[5m])) by (pod)',
                  start, end, step);
                updateChart(charts['chart-cpu'], cpuData);

                const memData = await queryPrometheus(
                  'sum(container_memory_working_set_bytes{namespace=~".*' + config.domain + '.*"}) by (pod) / 1024 / 1024',
                  start, end, step);
                updateChart(charts['chart-memory'], memData);
            {{~ if Config.Features.Kafka ~}}

                const kafkaData = await queryPrometheus(
                  'kafka_consumer_lag{domain="' + config.domain + '"}',
                  start, end, step);
                updateChart(charts['chart-kafka-lag'], kafkaData);
            {{~ end ~}}

                document.getElementById('last-updated').textContent = 'Updated: ' + new Date().toLocaleTimeString();
              };

              // Init
              initCharts();
              refreshAll();
              setInterval(refreshAll, config.refreshInterval);

              // Responsive
              window.addEventListener('resize', () => {
                Object.values(charts).forEach(c => c.resize());
              });
            })();
            """;

        public const string ConfigJs = """
            // Dashboard Configuration for {{ Domain.PascalName }}
            // Override this file via K8s ConfigMap mount in production
            window.DASHBOARD_CONFIG = {
              prometheusUrl: 'http://localhost:9090',
              domain: '{{ Domain.KebabName }}',
              refreshInterval: 30000,
              services: [
            {{~ for svc in Domain.Services ~}}
                { name: '{{ svc.PascalName }}', kebab: '{{ svc.KebabName }}', namespace: '{{ svc.KebabName }}' }{{ if !for.last }},{{ end }}
            {{~ end ~}}
              ]
            };
            """;

        public const string DashboardCss = """
            /* ECharts Dashboard Styles for {{ Domain.PascalName }} */
            :root {
              --bg-primary: #1a1a2e;
              --bg-card: #16213e;
              --text-primary: #e0e0e0;
              --text-secondary: #a0a0a0;
              --accent: #0f3460;
              --border: #2a2a4a;
            }

            .light-theme {
              --bg-primary: #f5f5f5;
              --bg-card: #ffffff;
              --text-primary: #333333;
              --text-secondary: #666666;
              --accent: #e3f2fd;
              --border: #e0e0e0;
            }

            * { margin: 0; padding: 0; box-sizing: border-box; }

            body {
              font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
              background-color: var(--bg-primary);
              color: var(--text-primary);
              min-height: 100vh;
            }

            header {
              display: flex;
              justify-content: space-between;
              align-items: center;
              padding: 1rem 2rem;
              background-color: var(--bg-card);
              border-bottom: 1px solid var(--border);
            }

            header h1 { font-size: 1.4rem; }

            .controls {
              display: flex;
              gap: 1rem;
              align-items: center;
            }

            .controls button {
              padding: 0.4rem 1rem;
              background: var(--accent);
              color: var(--text-primary);
              border: 1px solid var(--border);
              border-radius: 4px;
              cursor: pointer;
              font-size: 0.85rem;
            }

            #last-updated {
              color: var(--text-secondary);
              font-size: 0.85rem;
            }

            .dashboard-grid {
              display: grid;
              grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
              gap: 1rem;
              padding: 1rem 2rem;
            }

            .card {
              background: var(--bg-card);
              border: 1px solid var(--border);
              border-radius: 8px;
              padding: 1rem;
            }

            .card.full-width {
              grid-column: 1 / -1;
            }

            .card h2 { font-size: 1.1rem; margin-bottom: 0.5rem; }
            .card h3 { font-size: 0.95rem; margin-bottom: 0.5rem; color: var(--text-secondary); }

            .chart-container { width: 100%; height: 300px; }
            .chart-row { display: flex; gap: 0.5rem; }
            .chart-sm { flex: 1; height: 180px; }
            .chart-md { flex: 1; height: 250px; }

            @media (max-width: 768px) {
              .dashboard-grid { grid-template-columns: 1fr; padding: 0.5rem; }
              .chart-row { flex-direction: column; }
              .chart-sm, .chart-md { height: 200px; }
              header { flex-direction: column; gap: 0.5rem; }
            }
            """;

        public const string Dockerfile = """
            FROM nginx:1.27-alpine
            COPY src/ /usr/share/nginx/html/
            EXPOSE 80
            HEALTHCHECK --interval=30s --timeout=3s \
              CMD wget -q --spider http://localhost/ || exit 1
            """;

        public const string DockerCompose = """
            # ECharts Dashboard local development stack for {{ Domain.PascalName }}
            version: "3.9"

            services:
              echarts-dashboard:
                build:
                  context: .
                  dockerfile: Dockerfile
                ports:
                  - "3000:80"
                volumes:
                  - ./src:/usr/share/nginx/html:ro
                depends_on:
                  prometheus:
                    condition: service_healthy

              prometheus:
                image: prom/prometheus:v2.54.0
                ports:
                  - "9090:9090"
                volumes:
                  - prometheus-data:/prometheus
                command:
                  - '--config.file=/etc/prometheus/prometheus.yml'
                  - '--storage.tsdb.retention.time=7d'
                  - '--web.enable-lifecycle'
                healthcheck:
                  test: ["CMD", "wget", "-q", "--spider", "http://localhost:9090/-/healthy"]
                  interval: 10s
                  timeout: 5s
                  retries: 3

            volumes:
              prometheus-data:
            """;

        public const string Namespace = """
            apiVersion: v1
            kind: Namespace
            metadata:
              name: {{ Domain.KebabName }}-echarts
              labels:
                app.kubernetes.io/name: echarts-dashboard
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            """;
    }
}
