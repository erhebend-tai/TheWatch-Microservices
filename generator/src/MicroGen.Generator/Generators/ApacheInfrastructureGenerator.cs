using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates Apache Kafka, OpenWhisk, and Dubbo infrastructure artifacts.
/// Per-service: Kafka topics, producer/consumer configs, OpenWhisk actions/triggers/rules, Dubbo provider/consumer configs.
/// Per-domain: Kafka cluster Helm chart, OpenWhisk Helm chart, Dubbo registry Helm chart, docker-compose files.
/// </summary>
public sealed class ApacheInfrastructureGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public ApacheInfrastructureGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
    {
        _engine = engine;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Generates per-service Kafka, OpenWhisk, and Dubbo configuration files.
    /// </summary>
    public async Task GenerateAsync(
        ServiceDescriptor service,
        string serviceRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        _logger.LogDebug("  Generating Apache infrastructure for {Service}...", service.PascalName);
        var model = new { Service = service, Config = _config };

        // ── Kafka per-service ──────────────────────────────────────────
        if (_config.Features.Kafka)
        {
            var kafkaDir = Path.Combine(serviceRoot, "deploy", "kafka");

            await emitter.EmitAsync(
                Path.Combine(kafkaDir, "topics.yaml"),
                _engine.Render(KafkaTemplates.Topics, model), ct);

            await emitter.EmitAsync(
                Path.Combine(kafkaDir, "producer-config.yaml"),
                _engine.Render(KafkaTemplates.ProducerConfig, model), ct);

            await emitter.EmitAsync(
                Path.Combine(kafkaDir, "consumer-config.yaml"),
                _engine.Render(KafkaTemplates.ConsumerConfig, model), ct);
        }

        // ── OpenWhisk per-service ──────────────────────────────────────
        if (_config.Features.OpenWhisk)
        {
            var owDir = Path.Combine(serviceRoot, "deploy", "openwhisk");

            await emitter.EmitAsync(
                Path.Combine(owDir, "actions.yaml"),
                _engine.Render(OpenWhiskTemplates.Actions, model), ct);

            await emitter.EmitAsync(
                Path.Combine(owDir, "triggers.yaml"),
                _engine.Render(OpenWhiskTemplates.Triggers, model), ct);

            await emitter.EmitAsync(
                Path.Combine(owDir, "rules.yaml"),
                _engine.Render(OpenWhiskTemplates.Rules, model), ct);

            await emitter.EmitAsync(
                Path.Combine(owDir, "packages.yaml"),
                _engine.Render(OpenWhiskTemplates.Packages, model), ct);
        }

        // ── Dubbo per-service ──────────────────────────────────────────
        if (_config.Features.Dubbo)
        {
            var dubboDir = Path.Combine(serviceRoot, "deploy", "dubbo");

            await emitter.EmitAsync(
                Path.Combine(dubboDir, "provider-config.yaml"),
                _engine.Render(DubboTemplates.ProviderConfig, model), ct);

            await emitter.EmitAsync(
                Path.Combine(dubboDir, "consumer-config.yaml"),
                _engine.Render(DubboTemplates.ConsumerConfig, model), ct);
        }
    }

    /// <summary>
    /// Generates domain-level Kafka cluster, OpenWhisk platform, and Dubbo registry Helm charts.
    /// </summary>
    public async Task GenerateDomainInfrastructureAsync(
        DomainDescriptor domain,
        string domainRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        if (domain.Services.Count == 0) return;

        _logger.LogDebug("  Generating Apache domain infrastructure for {Domain}...", domain.DomainName);
        var model = new { Domain = domain, Config = _config };

        // ── Kafka cluster ──────────────────────────────────────────────
        if (_config.Features.Kafka)
        {
            var kafkaHelm = Path.Combine(domainRoot, "infrastructure", "kafka", "helm");
            var kafkaK8s = Path.Combine(domainRoot, "infrastructure", "kafka", "k8s");

            await emitter.EmitAsync(
                Path.Combine(kafkaHelm, "Chart.yaml"),
                _engine.Render(KafkaTemplates.HelmChart, model), ct);

            await emitter.EmitAsync(
                Path.Combine(kafkaHelm, "values.yaml"),
                _engine.Render(KafkaTemplates.HelmValues, model), ct);

            await emitter.EmitAsync(
                Path.Combine(kafkaHelm, "templates", "kafka-cluster.yaml"),
                _engine.Render(KafkaTemplates.ClusterTemplate, model), ct);

            await emitter.EmitAsync(
                Path.Combine(kafkaHelm, "templates", "kafka-topics.yaml"),
                _engine.Render(KafkaTemplates.SharedTopics, model), ct);

            await emitter.EmitAsync(
                Path.Combine(kafkaHelm, "templates", "schema-registry.yaml"),
                _engine.Render(KafkaTemplates.SchemaRegistry, model), ct);

            if (_config.Apache.KafkaUI)
            {
                await emitter.EmitAsync(
                    Path.Combine(kafkaHelm, "templates", "kafka-ui.yaml"),
                    _engine.Render(KafkaTemplates.KafkaUI, model), ct);
            }

            await emitter.EmitAsync(
                Path.Combine(kafkaK8s, "namespace.yaml"),
                _engine.Render(KafkaTemplates.Namespace, model), ct);

            await emitter.EmitAsync(
                Path.Combine(kafkaK8s, "strimzi-operator.yaml"),
                _engine.Render(KafkaTemplates.StrimziOperator, model), ct);

            await emitter.EmitAsync(
                Path.Combine(domainRoot, "infrastructure", "kafka", "docker-compose.yaml"),
                _engine.Render(KafkaTemplates.DockerCompose, model), ct);
        }

        // ── OpenWhisk platform ─────────────────────────────────────────
        if (_config.Features.OpenWhisk)
        {
            var owHelm = Path.Combine(domainRoot, "infrastructure", "openwhisk", "helm");
            var owK8s = Path.Combine(domainRoot, "infrastructure", "openwhisk", "k8s");

            await emitter.EmitAsync(
                Path.Combine(owHelm, "Chart.yaml"),
                _engine.Render(OpenWhiskTemplates.HelmChart, model), ct);

            await emitter.EmitAsync(
                Path.Combine(owHelm, "values.yaml"),
                _engine.Render(OpenWhiskTemplates.HelmValues, model), ct);

            await emitter.EmitAsync(
                Path.Combine(owHelm, "templates", "openwhisk-deployment.yaml"),
                _engine.Render(OpenWhiskTemplates.Deployment, model), ct);

            await emitter.EmitAsync(
                Path.Combine(owK8s, "namespace.yaml"),
                _engine.Render(OpenWhiskTemplates.Namespace, model), ct);

            await emitter.EmitAsync(
                Path.Combine(domainRoot, "infrastructure", "openwhisk", "docker-compose.yaml"),
                _engine.Render(OpenWhiskTemplates.DockerCompose, model), ct);
        }

        // ── Dubbo registry ─────────────────────────────────────────────
        if (_config.Features.Dubbo)
        {
            var dubboHelm = Path.Combine(domainRoot, "infrastructure", "dubbo", "helm");
            var dubboK8s = Path.Combine(domainRoot, "infrastructure", "dubbo", "k8s");

            await emitter.EmitAsync(
                Path.Combine(dubboHelm, "Chart.yaml"),
                _engine.Render(DubboTemplates.HelmChart, model), ct);

            await emitter.EmitAsync(
                Path.Combine(dubboHelm, "values.yaml"),
                _engine.Render(DubboTemplates.HelmValues, model), ct);

            await emitter.EmitAsync(
                Path.Combine(dubboHelm, "templates", "nacos-deployment.yaml"),
                _engine.Render(DubboTemplates.NacosDeployment, model), ct);

            await emitter.EmitAsync(
                Path.Combine(dubboHelm, "templates", "dubbo-admin.yaml"),
                _engine.Render(DubboTemplates.DubboAdmin, model), ct);

            await emitter.EmitAsync(
                Path.Combine(dubboK8s, "namespace.yaml"),
                _engine.Render(DubboTemplates.Namespace, model), ct);

            await emitter.EmitAsync(
                Path.Combine(dubboK8s, "dubbo-mesh-config.yaml"),
                _engine.Render(DubboTemplates.MeshConfig, model), ct);

            await emitter.EmitAsync(
                Path.Combine(domainRoot, "infrastructure", "dubbo", "docker-compose.yaml"),
                _engine.Render(DubboTemplates.DockerCompose, model), ct);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Kafka Templates
    // ═══════════════════════════════════════════════════════════════════
    private static class KafkaTemplates
    {
        // ── Per-service ────────────────────────────────────────────────

        public const string Topics = """
            # Kafka Topics for {{ Service.PascalName }}
            # Auto-generated by MicroGen — Strimzi KafkaTopic CRDs
            apiVersion: kafka.strimzi.io/v1beta2
            kind: KafkaTopic
            metadata:
              name: {{ Service.KebabName }}-events
              namespace: {{ Service.DomainName | string.downcase }}-kafka
              labels:
                strimzi.io/cluster: {{ Service.DomainName | string.downcase }}-kafka
                app.kubernetes.io/name: {{ Service.KebabName }}
                app.kubernetes.io/part-of: {{ Service.DomainName | string.downcase }}
            spec:
              partitions: 6
              replicas: {{ Config.Apache.KafkaReplicationFactor }}
              config:
                retention.ms: "604800000"
                cleanup.policy: delete
                min.insync.replicas: "2"
                compression.type: lz4
            ---
            apiVersion: kafka.strimzi.io/v1beta2
            kind: KafkaTopic
            metadata:
              name: {{ Service.KebabName }}-commands
              namespace: {{ Service.DomainName | string.downcase }}-kafka
              labels:
                strimzi.io/cluster: {{ Service.DomainName | string.downcase }}-kafka
                app.kubernetes.io/name: {{ Service.KebabName }}
            spec:
              partitions: 3
              replicas: {{ Config.Apache.KafkaReplicationFactor }}
              config:
                retention.ms: "259200000"
                cleanup.policy: delete
                min.insync.replicas: "2"
                compression.type: lz4
            ---
            apiVersion: kafka.strimzi.io/v1beta2
            kind: KafkaTopic
            metadata:
              name: {{ Service.KebabName }}-dlq
              namespace: {{ Service.DomainName | string.downcase }}-kafka
              labels:
                strimzi.io/cluster: {{ Service.DomainName | string.downcase }}-kafka
                app.kubernetes.io/name: {{ Service.KebabName }}
            spec:
              partitions: 3
              replicas: {{ Config.Apache.KafkaReplicationFactor }}
              config:
                retention.ms: "2592000000"
                cleanup.policy: compact
                min.insync.replicas: "1"
            """;

        public const string ProducerConfig = """
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Service.KebabName }}-kafka-producer
              namespace: {{ Service.KebabName }}
              labels:
                app.kubernetes.io/name: {{ Service.KebabName }}
                app.kubernetes.io/component: kafka-producer
            data:
              kafka-producer.properties: |
                bootstrap.servers={{ Service.DomainName | string.downcase }}-kafka-bootstrap:9092
                key.serializer=org.apache.kafka.common.serialization.StringSerializer
                value.serializer=io.confluent.kafka.serializers.KafkaAvroSerializer
                schema.registry.url=http://{{ Service.DomainName | string.downcase }}-schema-registry:8081
                acks=all
                retries=3
                retry.backoff.ms=1000
                linger.ms=5
                batch.size=16384
                buffer.memory=33554432
                compression.type=lz4
                enable.idempotence=true
                max.in.flight.requests.per.connection=5
                client.id={{ Service.KebabName }}-producer
            """;

        public const string ConsumerConfig = """
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Service.KebabName }}-kafka-consumer
              namespace: {{ Service.KebabName }}
              labels:
                app.kubernetes.io/name: {{ Service.KebabName }}
                app.kubernetes.io/component: kafka-consumer
            data:
              kafka-consumer.properties: |
                bootstrap.servers={{ Service.DomainName | string.downcase }}-kafka-bootstrap:9092
                key.deserializer=org.apache.kafka.common.serialization.StringDeserializer
                value.deserializer=io.confluent.kafka.serializers.KafkaAvroDeserializer
                schema.registry.url=http://{{ Service.DomainName | string.downcase }}-schema-registry:8081
                group.id={{ Service.KebabName }}-consumer-group
                auto.offset.reset=earliest
                enable.auto.commit=false
                max.poll.records=500
                max.poll.interval.ms=300000
                session.timeout.ms=30000
                heartbeat.interval.ms=10000
                isolation.level=read_committed
                client.id={{ Service.KebabName }}-consumer
            """;

        // ── Per-domain ─────────────────────────────────────────────────

        public const string HelmChart = """
            apiVersion: v2
            name: {{ Domain.KebabName }}-kafka
            description: Apache Kafka cluster for the {{ Domain.PascalName }} domain (Strimzi operator)
            type: application
            version: 1.0.0
            appVersion: "{{ Config.Apache.KafkaVersion }}"
            keywords:
              - kafka
              - strimzi
              - event-streaming
              - {{ Domain.KebabName }}
            maintainers:
              - name: TheWatch Platform Team
            """;

        public const string HelmValues = """
            # Kafka cluster configuration for {{ Domain.PascalName }}
            kafka:
              version: "{{ Config.Apache.KafkaVersion }}"
              replicas: {{ Config.Apache.KafkaReplicas }}
              replicationFactor: {{ Config.Apache.KafkaReplicationFactor }}
              storage:
                type: persistent-claim
                size: 10Gi
                storageClass: standard
              resources:
                requests:
                  cpu: "500m"
                  memory: "1Gi"
                limits:
                  cpu: "2"
                  memory: "4Gi"
              jmx:
                enabled: true
              listeners:
                - name: plain
                  port: 9092
                  type: internal
                  tls: false
                - name: tls
                  port: 9093
                  type: internal
                  tls: true

            schemaRegistry:
              version: "{{ Config.Apache.SchemaRegistryVersion }}"
              replicas: 1
              resources:
                requests:
                  cpu: "250m"
                  memory: "512Mi"
                limits:
                  cpu: "1"
                  memory: "1Gi"

            kafkaUI:
              enabled: {{ Config.Apache.KafkaUI }}
              replicas: 1

            namespace: {{ Domain.KebabName }}-kafka
            """;

        public const string ClusterTemplate = """
            # Strimzi Kafka Cluster for {{ Domain.PascalName }}
            apiVersion: kafka.strimzi.io/v1beta2
            kind: Kafka
            metadata:
              name: {{ Domain.KebabName }}-kafka
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: {{ Domain.KebabName }}-kafka
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            spec:
              kafka:
                version: {{ "{{" }} .Values.kafka.version {{ "}}" }}
                replicas: {{ "{{" }} .Values.kafka.replicas {{ "}}" }}
                listeners:
                  - name: plain
                    port: 9092
                    type: internal
                    tls: false
                  - name: tls
                    port: 9093
                    type: internal
                    tls: true
                    authentication:
                      type: tls
                config:
                  offsets.topic.replication.factor: {{ "{{" }} .Values.kafka.replicationFactor {{ "}}" }}
                  transaction.state.log.replication.factor: {{ "{{" }} .Values.kafka.replicationFactor {{ "}}" }}
                  transaction.state.log.min.isr: 2
                  default.replication.factor: {{ "{{" }} .Values.kafka.replicationFactor {{ "}}" }}
                  min.insync.replicas: 2
                  log.retention.hours: 168
                  log.segment.bytes: 1073741824
                  auto.create.topics.enable: false
                  num.partitions: 6
                storage:
                  type: jbod
                  volumes:
                    - id: 0
                      type: persistent-claim
                      size: {{ "{{" }} .Values.kafka.storage.size {{ "}}" }}
                      class: {{ "{{" }} .Values.kafka.storage.storageClass {{ "}}" }}
                      deleteClaim: false
                resources:
                  requests:
                    cpu: {{ "{{" }} .Values.kafka.resources.requests.cpu {{ "}}" }}
                    memory: {{ "{{" }} .Values.kafka.resources.requests.memory {{ "}}" }}
                  limits:
                    cpu: {{ "{{" }} .Values.kafka.resources.limits.cpu {{ "}}" }}
                    memory: {{ "{{" }} .Values.kafka.resources.limits.memory {{ "}}" }}
                metricsConfig:
                  type: jmxPrometheusExporter
                  valueFrom:
                    configMapKeyRef:
                      name: {{ Domain.KebabName }}-kafka-metrics
                      key: kafka-metrics-config.yml
              zookeeper:
                replicas: 0
              entityOperator:
                topicOperator:
                  resources:
                    requests:
                      cpu: "100m"
                      memory: "256Mi"
                    limits:
                      cpu: "500m"
                      memory: "512Mi"
                userOperator:
                  resources:
                    requests:
                      cpu: "100m"
                      memory: "256Mi"
                    limits:
                      cpu: "500m"
                      memory: "512Mi"
            ---
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Domain.KebabName }}-kafka-metrics
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            data:
              kafka-metrics-config.yml: |
                lowercaseOutputName: true
                lowercaseOutputLabelNames: true
                rules:
                  - pattern: "kafka.server<type=(.+), name=(.+), clientId=(.+), topic=(.+), partition=(.*)><>Value"
                    name: "kafka_server_$1_$2"
                    type: GAUGE
                    labels:
                      clientId: "$3"
                      topic: "$4"
                      partition: "$5"
                  - pattern: "kafka.server<type=(.+), name=(.+)><>Value"
                    name: "kafka_server_$1_$2"
                    type: GAUGE
            """;

        public const string SharedTopics = """
            # Domain-wide shared Kafka topics for {{ Domain.PascalName }}
            apiVersion: kafka.strimzi.io/v1beta2
            kind: KafkaTopic
            metadata:
              name: {{ Domain.KebabName }}-audit-log
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                strimzi.io/cluster: {{ Domain.KebabName }}-kafka
            spec:
              partitions: 6
              replicas: {{ "{{" }} .Values.kafka.replicationFactor {{ "}}" }}
              config:
                retention.ms: "2592000000"
                cleanup.policy: compact,delete
                min.insync.replicas: "2"
            ---
            apiVersion: kafka.strimzi.io/v1beta2
            kind: KafkaTopic
            metadata:
              name: {{ Domain.KebabName }}-dead-letter
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                strimzi.io/cluster: {{ Domain.KebabName }}-kafka
            spec:
              partitions: 3
              replicas: {{ "{{" }} .Values.kafka.replicationFactor {{ "}}" }}
              config:
                retention.ms: "7776000000"
                cleanup.policy: compact
                min.insync.replicas: "1"
            ---
            apiVersion: kafka.strimzi.io/v1beta2
            kind: KafkaTopic
            metadata:
              name: {{ Domain.KebabName }}-health-events
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                strimzi.io/cluster: {{ Domain.KebabName }}-kafka
            spec:
              partitions: 3
              replicas: {{ "{{" }} .Values.kafka.replicationFactor {{ "}}" }}
              config:
                retention.ms: "86400000"
                cleanup.policy: delete
            """;

        public const string SchemaRegistry = """
            # Confluent Schema Registry for {{ Domain.PascalName }}
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.KebabName }}-schema-registry
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: schema-registry
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-kafka
            spec:
              replicas: {{ "{{" }} .Values.schemaRegistry.replicas {{ "}}" }}
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-schema-registry
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-schema-registry
                spec:
                  containers:
                    - name: schema-registry
                      image: confluentinc/cp-schema-registry:{{ "{{" }} .Values.schemaRegistry.version {{ "}}" }}
                      ports:
                        - containerPort: 8081
                      env:
                        - name: SCHEMA_REGISTRY_HOST_NAME
                          valueFrom:
                            fieldRef:
                              fieldPath: metadata.name
                        - name: SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS
                          value: "{{ Domain.KebabName }}-kafka-bootstrap:9092"
                        - name: SCHEMA_REGISTRY_LISTENERS
                          value: "http://0.0.0.0:8081"
                        - name: SCHEMA_REGISTRY_SCHEMA_COMPATIBILITY_LEVEL
                          value: "BACKWARD"
                      resources:
                        requests:
                          cpu: {{ "{{" }} .Values.schemaRegistry.resources.requests.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.schemaRegistry.resources.requests.memory {{ "}}" }}
                        limits:
                          cpu: {{ "{{" }} .Values.schemaRegistry.resources.limits.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.schemaRegistry.resources.limits.memory {{ "}}" }}
                      livenessProbe:
                        httpGet:
                          path: /
                          port: 8081
                        initialDelaySeconds: 30
                        periodSeconds: 10
                      readinessProbe:
                        httpGet:
                          path: /
                          port: 8081
                        initialDelaySeconds: 15
                        periodSeconds: 5
            ---
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.KebabName }}-schema-registry
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            spec:
              selector:
                app: {{ Domain.KebabName }}-schema-registry
              ports:
                - port: 8081
                  targetPort: 8081
            """;

        public const string KafkaUI = """
            # Provectus Kafka UI for {{ Domain.PascalName }}
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.KebabName }}-kafka-ui
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: kafka-ui
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-kafka
            spec:
              replicas: 1
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-kafka-ui
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-kafka-ui
                spec:
                  containers:
                    - name: kafka-ui
                      image: provectuslabs/kafka-ui:latest
                      ports:
                        - containerPort: 8080
                      env:
                        - name: KAFKA_CLUSTERS_0_NAME
                          value: "{{ Domain.PascalName }}"
                        - name: KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS
                          value: "{{ Domain.KebabName }}-kafka-bootstrap:9092"
                        - name: KAFKA_CLUSTERS_0_SCHEMAREGISTRY
                          value: "http://{{ Domain.KebabName }}-schema-registry:8081"
                        - name: KAFKA_CLUSTERS_0_METRICS_PORT
                          value: "9404"
                      resources:
                        requests:
                          cpu: "100m"
                          memory: "256Mi"
                        limits:
                          cpu: "500m"
                          memory: "512Mi"
            ---
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.KebabName }}-kafka-ui
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            spec:
              selector:
                app: {{ Domain.KebabName }}-kafka-ui
              ports:
                - port: 8080
                  targetPort: 8080
            """;

        public const string Namespace = """
            apiVersion: v1
            kind: Namespace
            metadata:
              name: {{ Domain.KebabName }}-kafka
              labels:
                app.kubernetes.io/name: kafka
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            """;

        public const string StrimziOperator = """
            # Strimzi Kafka Operator for {{ Domain.PascalName }}
            apiVersion: v1
            kind: ServiceAccount
            metadata:
              name: strimzi-cluster-operator
              namespace: {{ Domain.KebabName }}-kafka
              labels:
                app: strimzi
            ---
            apiVersion: rbac.authorization.k8s.io/v1
            kind: ClusterRoleBinding
            metadata:
              name: strimzi-cluster-operator-{{ Domain.KebabName }}
              labels:
                app: strimzi
            subjects:
              - kind: ServiceAccount
                name: strimzi-cluster-operator
                namespace: {{ Domain.KebabName }}-kafka
            roleRef:
              kind: ClusterRole
              name: strimzi-cluster-operator-global
              apiGroup: rbac.authorization.k8s.io
            ---
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: strimzi-cluster-operator
              namespace: {{ Domain.KebabName }}-kafka
              labels:
                app: strimzi
            spec:
              replicas: 1
              selector:
                matchLabels:
                  name: strimzi-cluster-operator
              template:
                metadata:
                  labels:
                    name: strimzi-cluster-operator
                spec:
                  serviceAccountName: strimzi-cluster-operator
                  containers:
                    - name: strimzi-cluster-operator
                      image: quay.io/strimzi/operator:0.44.0
                      ports:
                        - containerPort: 8080
                          name: http
                      env:
                        - name: STRIMZI_NAMESPACE
                          value: "{{ Domain.KebabName }}-kafka"
                        - name: STRIMZI_FULL_RECONCILIATION_INTERVAL_MS
                          value: "120000"
                        - name: STRIMZI_LOG_LEVEL
                          value: "INFO"
                        - name: STRIMZI_KAFKA_IMAGES
                          value: "{{ Config.Apache.KafkaVersion }}=quay.io/strimzi/kafka:0.44.0-kafka-{{ Config.Apache.KafkaVersion }}"
                      resources:
                        requests:
                          cpu: "200m"
                          memory: "384Mi"
                        limits:
                          cpu: "1"
                          memory: "512Mi"
                      livenessProbe:
                        httpGet:
                          path: /healthy
                          port: http
                        initialDelaySeconds: 10
                        periodSeconds: 30
                      readinessProbe:
                        httpGet:
                          path: /ready
                          port: http
                        initialDelaySeconds: 10
                        periodSeconds: 30
            """;

        public const string DockerCompose = """
            # Kafka local development stack for {{ Domain.PascalName }}
            version: "3.9"

            services:
              kafka:
                image: bitnami/kafka:{{ Config.Apache.KafkaVersion }}
                ports:
                  - "9092:9092"
                  - "9094:9094"
                environment:
                  KAFKA_CFG_NODE_ID: "1"
                  KAFKA_CFG_PROCESS_ROLES: "broker,controller"
                  KAFKA_CFG_CONTROLLER_QUORUM_VOTERS: "1@kafka:9093"
                  KAFKA_CFG_LISTENERS: "PLAINTEXT://:9092,CONTROLLER://:9093,EXTERNAL://:9094"
                  KAFKA_CFG_ADVERTISED_LISTENERS: "PLAINTEXT://kafka:9092,EXTERNAL://localhost:9094"
                  KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP: "CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,EXTERNAL:PLAINTEXT"
                  KAFKA_CFG_CONTROLLER_LISTENER_NAMES: "CONTROLLER"
                  KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE: "false"
                  KAFKA_CFG_NUM_PARTITIONS: "6"
                  KAFKA_CFG_DEFAULT_REPLICATION_FACTOR: "1"
                  KAFKA_KRAFT_CLUSTER_ID: "{{ Domain.KebabName }}-kafka-dev"
                volumes:
                  - kafka-data:/bitnami/kafka
                healthcheck:
                  test: ["CMD-SHELL", "kafka-broker-api-versions.sh --bootstrap-server localhost:9092"]
                  interval: 10s
                  timeout: 10s
                  retries: 5

              schema-registry:
                image: confluentinc/cp-schema-registry:{{ Config.Apache.SchemaRegistryVersion }}
                ports:
                  - "8081:8081"
                environment:
                  SCHEMA_REGISTRY_HOST_NAME: "schema-registry"
                  SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS: "kafka:9092"
                  SCHEMA_REGISTRY_LISTENERS: "http://0.0.0.0:8081"
                depends_on:
                  kafka:
                    condition: service_healthy

              kafka-ui:
                image: provectuslabs/kafka-ui:latest
                ports:
                  - "8180:8080"
                environment:
                  KAFKA_CLUSTERS_0_NAME: "{{ Domain.PascalName }}-dev"
                  KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: "kafka:9092"
                  KAFKA_CLUSTERS_0_SCHEMAREGISTRY: "http://schema-registry:8081"
                depends_on:
                  kafka:
                    condition: service_healthy

            volumes:
              kafka-data:
            """;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  OpenWhisk Templates
    // ═══════════════════════════════════════════════════════════════════
    private static class OpenWhiskTemplates
    {
        // ── Per-service ────────────────────────────────────────────────

        public const string Actions = """
            # OpenWhisk Actions for {{ Service.PascalName }}
            # Auto-generated by MicroGen
            {{~ for tag in Service.Tags ~}}
            {{~ if tag.Name != null ~}}
            ---
            apiVersion: openwhisk.apache.org/v1
            kind: Action
            metadata:
              name: {{ Service.KebabName }}-{{ tag.Name | string.downcase }}-handler
              namespace: {{ Service.DomainName | string.downcase }}-openwhisk
              labels:
                app.kubernetes.io/name: {{ Service.KebabName }}
                app.kubernetes.io/component: {{ tag.Name | string.downcase }}
            spec:
              runtime: dotnet:10
              main: {{ Service.PascalName }}::{{ Service.PascalName }}.Functions.{{ tag.Name }}Handler::Main
              limits:
                timeout: 60000
                memory: 256
                concurrency: 10
              annotations:
                web-export: "true"
                require-whisk-auth: "true"
                provide-api-key: "false"
              parameters:
                - key: serviceName
                  value: {{ Service.KebabName }}
                - key: domainName
                  value: {{ Service.DomainName | string.downcase }}
            {{~ end ~}}
            {{~ end ~}}
            """;

        public const string Triggers = """
            # OpenWhisk Triggers for {{ Service.PascalName }}
            {{~ for tag in Service.Tags ~}}
            {{~ if tag.Name != null ~}}
            ---
            apiVersion: openwhisk.apache.org/v1
            kind: Trigger
            metadata:
              name: {{ Service.KebabName }}-{{ tag.Name | string.downcase }}-kafka-trigger
              namespace: {{ Service.DomainName | string.downcase }}-openwhisk
            spec:
              feed: /whisk.system/messaging/kafkaFeed
              parameters:
                - key: brokers
                  value: "{{ Service.DomainName | string.downcase }}-kafka-bootstrap:9092"
                - key: topic
                  value: "{{ Service.KebabName }}-events"
                - key: isJSONData
                  value: "true"
            ---
            apiVersion: openwhisk.apache.org/v1
            kind: Trigger
            metadata:
              name: {{ Service.KebabName }}-{{ tag.Name | string.downcase }}-cron-trigger
              namespace: {{ Service.DomainName | string.downcase }}-openwhisk
            spec:
              feed: /whisk.system/alarms/alarm
              parameters:
                - key: cron
                  value: "0 */5 * * * *"
                - key: trigger_payload
                  value: '{"service":"{{ Service.KebabName }}","tag":"{{ tag.Name | string.downcase }}"}'
            {{~ end ~}}
            {{~ end ~}}
            """;

        public const string Rules = """
            # OpenWhisk Rules for {{ Service.PascalName }}
            {{~ for tag in Service.Tags ~}}
            {{~ if tag.Name != null ~}}
            ---
            apiVersion: openwhisk.apache.org/v1
            kind: Rule
            metadata:
              name: {{ Service.KebabName }}-{{ tag.Name | string.downcase }}-kafka-rule
              namespace: {{ Service.DomainName | string.downcase }}-openwhisk
            spec:
              trigger: {{ Service.KebabName }}-{{ tag.Name | string.downcase }}-kafka-trigger
              action: {{ Service.KebabName }}-{{ tag.Name | string.downcase }}-handler
              status: active
            ---
            apiVersion: openwhisk.apache.org/v1
            kind: Rule
            metadata:
              name: {{ Service.KebabName }}-{{ tag.Name | string.downcase }}-cron-rule
              namespace: {{ Service.DomainName | string.downcase }}-openwhisk
            spec:
              trigger: {{ Service.KebabName }}-{{ tag.Name | string.downcase }}-cron-trigger
              action: {{ Service.KebabName }}-{{ tag.Name | string.downcase }}-handler
              status: active
            {{~ end ~}}
            {{~ end ~}}
            """;

        public const string Packages = """
            # OpenWhisk Package for {{ Service.PascalName }}
            apiVersion: openwhisk.apache.org/v1
            kind: Package
            metadata:
              name: {{ Service.KebabName }}
              namespace: {{ Service.DomainName | string.downcase }}-openwhisk
              labels:
                app.kubernetes.io/name: {{ Service.KebabName }}
                app.kubernetes.io/part-of: {{ Service.DomainName | string.downcase }}
            spec:
              parameters:
                - key: serviceName
                  value: "{{ Service.KebabName }}"
                - key: domainName
                  value: "{{ Service.DomainName | string.downcase }}"
                - key: serviceUrl
                  value: "http://{{ Service.KebabName }}.{{ Service.KebabName }}.svc.cluster.local"
              actions:
            {{~ for tag in Service.Tags ~}}
            {{~ if tag.Name != null ~}}
                - name: {{ tag.Name | string.downcase }}-handler
            {{~ end ~}}
            {{~ end ~}}
            """;

        // ── Per-domain ─────────────────────────────────────────────────

        public const string HelmChart = """
            apiVersion: v2
            name: {{ Domain.KebabName }}-openwhisk
            description: Apache OpenWhisk serverless platform for the {{ Domain.PascalName }} domain
            type: application
            version: 1.0.0
            appVersion: "{{ Config.Apache.OpenWhiskVersion }}"
            keywords:
              - openwhisk
              - serverless
              - faas
              - {{ Domain.KebabName }}
            maintainers:
              - name: TheWatch Platform Team
            dependencies:
              - name: openwhisk
                version: "1.0.*"
                repository: "https://openwhisk.apache.org/charts"
            """;

        public const string HelmValues = """
            # OpenWhisk configuration for {{ Domain.PascalName }}
            openwhisk:
              whisk:
                ingress:
                  apiHostName: "{{ Domain.KebabName }}-openwhisk.thewatch.local"
                  apiHostPort: 443
                  type: NodePort
                limits:
                  actionsInvokesPerminute: 120
                  actionsInvokesConcurrent: 30
                  triggersFiresPerminute: 60
                  actionsSequenceMaxlength: 50
                  actions:
                    time:
                      min: "100ms"
                      max: "5m"
                      std: "1m"
                    memory:
                      min: "128m"
                      max: "512m"
                      std: "256m"
                    concurrency:
                      min: 1
                      max: 30
                      std: 10
                runtimes:
                  dotnet:
                    - kind: "dotnet:10"
                      default: true
                      image:
                        prefix: "openwhisk"
                        name: "action-dotnet-v10"
                        tag: "latest"
                  nodejs:
                    - kind: "nodejs:22"
                      default: true
                      image:
                        prefix: "openwhisk"
                        name: "action-nodejs-v22"
                        tag: "latest"
                  python:
                    - kind: "python:3.13"
                      default: true
                      image:
                        prefix: "openwhisk"
                        name: "action-python-v3.13"
                        tag: "latest"

              invoker:
                replicaCount: {{ Config.Apache.OpenWhiskInvokerReplicas }}
                resources:
                  requests:
                    cpu: "500m"
                    memory: "1Gi"
                  limits:
                    cpu: "2"
                    memory: "4Gi"

              controller:
                replicaCount: 1
                resources:
                  requests:
                    cpu: "250m"
                    memory: "512Mi"
                  limits:
                    cpu: "1"
                    memory: "2Gi"

              couchdb:
                persistence:
                  enabled: true
                  size: 5Gi

              kafka:
                external: true
                bootstrapServers: "{{ Domain.KebabName }}-kafka-bootstrap:9092"

            namespace: {{ Domain.KebabName }}-openwhisk
            """;

        public const string Deployment = """
            # OpenWhisk Core Deployment for {{ Domain.PascalName }}
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.KebabName }}-openwhisk-controller
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: openwhisk-controller
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-openwhisk
            spec:
              replicas: {{ "{{" }} .Values.openwhisk.controller.replicaCount {{ "}}" }}
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-openwhisk-controller
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-openwhisk-controller
                spec:
                  containers:
                    - name: controller
                      image: openwhisk/controller:{{ Config.Apache.OpenWhiskVersion }}
                      ports:
                        - containerPort: 8080
                          name: http
                      env:
                        - name: KAFKA_HOSTS
                          value: "{{ Domain.KebabName }}-kafka-bootstrap:9092"
                        - name: CONFIG_whisk_couchdb_host
                          value: "{{ Domain.KebabName }}-couchdb"
                        - name: CONFIG_whisk_couchdb_port
                          value: "5984"
                        - name: JAVA_OPTS
                          value: "-Xmx1g"
                      resources:
                        requests:
                          cpu: {{ "{{" }} .Values.openwhisk.controller.resources.requests.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.openwhisk.controller.resources.requests.memory {{ "}}" }}
                        limits:
                          cpu: {{ "{{" }} .Values.openwhisk.controller.resources.limits.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.openwhisk.controller.resources.limits.memory {{ "}}" }}
                      livenessProbe:
                        httpGet:
                          path: /ping
                          port: http
                        initialDelaySeconds: 30
                        periodSeconds: 10
                      readinessProbe:
                        httpGet:
                          path: /ping
                          port: http
                        initialDelaySeconds: 15
                        periodSeconds: 5
            ---
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.KebabName }}-openwhisk-invoker
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: openwhisk-invoker
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-openwhisk
            spec:
              replicas: {{ "{{" }} .Values.openwhisk.invoker.replicaCount {{ "}}" }}
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-openwhisk-invoker
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-openwhisk-invoker
                spec:
                  containers:
                    - name: invoker
                      image: openwhisk/invoker:{{ Config.Apache.OpenWhiskVersion }}
                      ports:
                        - containerPort: 8080
                      env:
                        - name: KAFKA_HOSTS
                          value: "{{ Domain.KebabName }}-kafka-bootstrap:9092"
                        - name: INVOKER_OPTS
                          value: "-Xmx2g"
                      resources:
                        requests:
                          cpu: {{ "{{" }} .Values.openwhisk.invoker.resources.requests.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.openwhisk.invoker.resources.requests.memory {{ "}}" }}
                        limits:
                          cpu: {{ "{{" }} .Values.openwhisk.invoker.resources.limits.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.openwhisk.invoker.resources.limits.memory {{ "}}" }}
            ---
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.KebabName }}-openwhisk-controller
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            spec:
              selector:
                app: {{ Domain.KebabName }}-openwhisk-controller
              ports:
                - port: 8080
                  targetPort: 8080
            ---
            apiVersion: apps/v1
            kind: StatefulSet
            metadata:
              name: {{ Domain.KebabName }}-couchdb
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: couchdb
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-openwhisk
            spec:
              replicas: 1
              serviceName: {{ Domain.KebabName }}-couchdb
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-couchdb
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-couchdb
                spec:
                  containers:
                    - name: couchdb
                      image: apache/couchdb:3.4
                      ports:
                        - containerPort: 5984
                      env:
                        - name: COUCHDB_USER
                          value: "whisk_admin"
                        - name: COUCHDB_PASSWORD
                          valueFrom:
                            secretKeyRef:
                              name: {{ Domain.KebabName }}-openwhisk-secrets
                              key: couchdb-password
                      volumeMounts:
                        - name: couchdb-data
                          mountPath: /opt/couchdb/data
              volumeClaimTemplates:
                - metadata:
                    name: couchdb-data
                  spec:
                    accessModes: ["ReadWriteOnce"]
                    resources:
                      requests:
                        storage: {{ "{{" }} .Values.openwhisk.couchdb.persistence.size {{ "}}" }}
            ---
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.KebabName }}-couchdb
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            spec:
              selector:
                app: {{ Domain.KebabName }}-couchdb
              ports:
                - port: 5984
                  targetPort: 5984
            """;

        public const string Namespace = """
            apiVersion: v1
            kind: Namespace
            metadata:
              name: {{ Domain.KebabName }}-openwhisk
              labels:
                app.kubernetes.io/name: openwhisk
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            """;

        public const string DockerCompose = """
            # OpenWhisk local development stack for {{ Domain.PascalName }}
            version: "3.9"

            services:
              openwhisk:
                image: openwhisk/standalone:nightly
                ports:
                  - "3233:3233"
                environment:
                  JAVA_OPTS: "-Xmx2g"
                  CONFIG_whisk_timeLimit_max: "5 m"
                  CONFIG_whisk_memory_max: "512 m"
                volumes:
                  - openwhisk-data:/data
                healthcheck:
                  test: ["CMD", "curl", "-sf", "http://localhost:3233/api/v1"]
                  interval: 15s
                  timeout: 5s
                  retries: 10
                  start_period: 60s

            volumes:
              openwhisk-data:
            """;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Dubbo Templates
    // ═══════════════════════════════════════════════════════════════════
    private static class DubboTemplates
    {
        // ── Per-service ────────────────────────────────────────────────

        public const string ProviderConfig = """
            # Dubbo Provider Configuration for {{ Service.PascalName }}
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Service.KebabName }}-dubbo-provider
              namespace: {{ Service.KebabName }}
              labels:
                app.kubernetes.io/name: {{ Service.KebabName }}
                app.kubernetes.io/component: dubbo-provider
            data:
              dubbo.yaml: |
                dubbo:
                  application:
                    name: {{ Service.KebabName }}
                    version: "1.0.0"
                    owner: thewatch
                    environment: production
                    metadata-type: remote
                  protocol:
                    name: tri
                    port: 50051
                    serialization: protobuf
                    threads: 200
                    dispatcher: all
                  registry:
                    address: nacos://{{ Service.DomainName | string.downcase }}-nacos:8848
                    group: {{ Service.DomainName | string.downcase }}
                    parameters:
                      namespace: {{ Service.DomainName | string.downcase }}
                  metadata-report:
                    address: nacos://{{ Service.DomainName | string.downcase }}-nacos:8848
                  config-center:
                    address: nacos://{{ Service.DomainName | string.downcase }}-nacos:8848
                  provider:
                    filter: sentinel
                    retries: 2
                    timeout: 3000
                    loadbalance: roundrobin
                    services:
            {{~ for tag in Service.Tags ~}}
            {{~ if tag.Name != null ~}}
                      {{ tag.Name }}Service:
                        interface: app.thewatch.{{ Service.KebabName }}.api.{{ tag.Name }}Service
                        version: "1.0.0"
                        group: {{ Service.DomainName | string.downcase }}
                        methods:
            {{~ for op in tag.Operations ~}}
                          - name: {{ op.PascalOperationId }}
                            timeout: 5000
                            retries: 1
            {{~ end ~}}
            {{~ end ~}}
            {{~ end ~}}
            """;

        public const string ConsumerConfig = """
            # Dubbo Consumer Configuration for {{ Service.PascalName }}
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Service.KebabName }}-dubbo-consumer
              namespace: {{ Service.KebabName }}
              labels:
                app.kubernetes.io/name: {{ Service.KebabName }}
                app.kubernetes.io/component: dubbo-consumer
            data:
              dubbo-consumer.yaml: |
                dubbo:
                  application:
                    name: {{ Service.KebabName }}-consumer
                  registry:
                    address: nacos://{{ Service.DomainName | string.downcase }}-nacos:8848
                    group: {{ Service.DomainName | string.downcase }}
                  consumer:
                    check: false
                    timeout: 5000
                    retries: 2
                    loadbalance: roundrobin
                    filter: sentinel
                    cluster: failover
                    references: {}
            """;

        // ── Per-domain ─────────────────────────────────────────────────

        public const string HelmChart = """
            apiVersion: v2
            name: {{ Domain.KebabName }}-dubbo
            description: Apache Dubbo service registry (Nacos) for the {{ Domain.PascalName }} domain
            type: application
            version: 1.0.0
            appVersion: "{{ Config.Apache.DubboVersion }}"
            keywords:
              - dubbo
              - nacos
              - service-mesh
              - rpc
              - {{ Domain.KebabName }}
            maintainers:
              - name: TheWatch Platform Team
            """;

        public const string HelmValues = """
            # Dubbo/Nacos configuration for {{ Domain.PascalName }}
            nacos:
              version: "{{ Config.Apache.NacosVersion }}"
              mode: cluster
              replicas: 3
              storage:
                type: persistent-claim
                size: 5Gi
                storageClass: standard
              auth:
                enabled: true
                token: "thewatch-nacos-{{ Domain.KebabName }}"
              resources:
                requests:
                  cpu: "250m"
                  memory: "512Mi"
                limits:
                  cpu: "1"
                  memory: "2Gi"

            dubboAdmin:
              enabled: true
              version: "0.6"
              replicas: 1
              resources:
                requests:
                  cpu: "100m"
                  memory: "256Mi"
                limits:
                  cpu: "500m"
                  memory: "512Mi"

            dubbo:
              version: "{{ Config.Apache.DubboVersion }}"
              protocol: tri
              port: 50051

            namespace: {{ Domain.KebabName }}-dubbo
            """;

        public const string NacosDeployment = """
            # Nacos Server StatefulSet for {{ Domain.PascalName }}
            apiVersion: apps/v1
            kind: StatefulSet
            metadata:
              name: {{ Domain.KebabName }}-nacos
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: nacos
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-dubbo
            spec:
              replicas: {{ "{{" }} .Values.nacos.replicas {{ "}}" }}
              serviceName: {{ Domain.KebabName }}-nacos-headless
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-nacos
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-nacos
                spec:
                  containers:
                    - name: nacos
                      image: nacos/nacos-server:v{{ "{{" }} .Values.nacos.version {{ "}}" }}
                      ports:
                        - containerPort: 8848
                          name: client
                        - containerPort: 9848
                          name: grpc
                        - containerPort: 9849
                          name: raft
                      env:
                        - name: MODE
                          value: "{{ "{{" }} .Values.nacos.mode {{ "}}" }}"
                        - name: NACOS_REPLICAS
                          value: "{{ "{{" }} .Values.nacos.replicas {{ "}}" }}"
                        - name: NACOS_AUTH_ENABLE
                          value: "{{ "{{" }} .Values.nacos.auth.enabled {{ "}}" }}"
                        - name: NACOS_AUTH_TOKEN
                          valueFrom:
                            secretKeyRef:
                              name: {{ Domain.KebabName }}-nacos-secrets
                              key: auth-token
                        - name: SPRING_DATASOURCE_PLATFORM
                          value: "embedded"
                        - name: JVM_XMS
                          value: "256m"
                        - name: JVM_XMX
                          value: "1g"
                      resources:
                        requests:
                          cpu: {{ "{{" }} .Values.nacos.resources.requests.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.nacos.resources.requests.memory {{ "}}" }}
                        limits:
                          cpu: {{ "{{" }} .Values.nacos.resources.limits.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.nacos.resources.limits.memory {{ "}}" }}
                      livenessProbe:
                        httpGet:
                          path: /nacos/v1/console/health/liveness
                          port: 8848
                        initialDelaySeconds: 30
                        periodSeconds: 10
                      readinessProbe:
                        httpGet:
                          path: /nacos/v1/console/health/readiness
                          port: 8848
                        initialDelaySeconds: 15
                        periodSeconds: 5
                      volumeMounts:
                        - name: nacos-data
                          mountPath: /home/nacos/data
              volumeClaimTemplates:
                - metadata:
                    name: nacos-data
                  spec:
                    accessModes: ["ReadWriteOnce"]
                    resources:
                      requests:
                        storage: {{ "{{" }} .Values.nacos.storage.size {{ "}}" }}
                    storageClassName: {{ "{{" }} .Values.nacos.storage.storageClass {{ "}}" }}
            ---
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.KebabName }}-nacos
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            spec:
              selector:
                app: {{ Domain.KebabName }}-nacos
              ports:
                - name: client
                  port: 8848
                  targetPort: 8848
                - name: grpc
                  port: 9848
                  targetPort: 9848
            ---
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.KebabName }}-nacos-headless
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            spec:
              clusterIP: None
              selector:
                app: {{ Domain.KebabName }}-nacos
              ports:
                - name: client
                  port: 8848
                - name: grpc
                  port: 9848
                - name: raft
                  port: 9849
            """;

        public const string DubboAdmin = """
            # Dubbo Admin Console for {{ Domain.PascalName }}
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.KebabName }}-dubbo-admin
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
              labels:
                app.kubernetes.io/name: dubbo-admin
                app.kubernetes.io/part-of: {{ Domain.KebabName }}-dubbo
            spec:
              replicas: {{ "{{" }} .Values.dubboAdmin.replicas {{ "}}" }}
              selector:
                matchLabels:
                  app: {{ Domain.KebabName }}-dubbo-admin
              template:
                metadata:
                  labels:
                    app: {{ Domain.KebabName }}-dubbo-admin
                spec:
                  containers:
                    - name: dubbo-admin
                      image: apache/dubbo-admin:{{ "{{" }} .Values.dubboAdmin.version {{ "}}" }}
                      ports:
                        - containerPort: 8080
                      env:
                        - name: admin.registry.address
                          value: "nacos://{{ Domain.KebabName }}-nacos:8848"
                        - name: admin.config-center
                          value: "nacos://{{ Domain.KebabName }}-nacos:8848"
                        - name: admin.metadata-report.address
                          value: "nacos://{{ Domain.KebabName }}-nacos:8848"
                      resources:
                        requests:
                          cpu: {{ "{{" }} .Values.dubboAdmin.resources.requests.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.dubboAdmin.resources.requests.memory {{ "}}" }}
                        limits:
                          cpu: {{ "{{" }} .Values.dubboAdmin.resources.limits.cpu {{ "}}" }}
                          memory: {{ "{{" }} .Values.dubboAdmin.resources.limits.memory {{ "}}" }}
                      livenessProbe:
                        httpGet:
                          path: /
                          port: 8080
                        initialDelaySeconds: 30
                        periodSeconds: 10
                      readinessProbe:
                        httpGet:
                          path: /
                          port: 8080
                        initialDelaySeconds: 15
                        periodSeconds: 5
            ---
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.KebabName }}-dubbo-admin
              namespace: {{ "{{" }} .Values.namespace {{ "}}" }}
            spec:
              selector:
                app: {{ Domain.KebabName }}-dubbo-admin
              ports:
                - port: 8080
                  targetPort: 8080
            """;

        public const string Namespace = """
            apiVersion: v1
            kind: Namespace
            metadata:
              name: {{ Domain.KebabName }}-dubbo
              labels:
                app.kubernetes.io/name: dubbo
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            """;

        public const string MeshConfig = """
            # Dubbo Mesh Configuration for {{ Domain.PascalName }}
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Domain.KebabName }}-dubbo-mesh-config
              namespace: {{ Domain.KebabName }}-dubbo
              labels:
                app.kubernetes.io/name: dubbo-mesh
                app.kubernetes.io/part-of: {{ Domain.KebabName }}
            data:
              mesh-config.yaml: |
                # Dubbo Mesh / xDS integration
                dubbo:
                  mesh:
                    enabled: true
                    mode: universal
                    xds:
                      address: "xds://{{ Domain.KebabName }}-dubbo-control-plane:5678"
                      cluster: {{ Domain.KebabName }}
                    sidecar:
                      enabled: true
                      image: apache/dubbo-agent:{{ Config.Apache.DubboVersion }}
                      resources:
                        requests:
                          cpu: "50m"
                          memory: "64Mi"
                        limits:
                          cpu: "200m"
                          memory: "128Mi"
                    routing:
                      rules:
                        - match:
                            headers:
                              x-canary: "true"
                          route:
                            - destination:
                                subset: canary
                                weight: 100
                        - route:
                            - destination:
                                subset: stable
                                weight: 100
                    observability:
                      tracing:
                        enabled: true
                        provider: opentelemetry
                        endpoint: "http://otel-collector:4317"
                      metrics:
                        enabled: true
                        port: 9090
                        path: /metrics
                    loadBalancing:
                      strategy: roundrobin
                      healthCheck:
                        enabled: true
                        interval: 5s
                        timeout: 2s
                        unhealthyThreshold: 3
                    circuitBreaker:
                      enabled: true
                      maxConnections: 1000
                      maxRequests: 500
                      maxRetries: 3
                      consecutiveErrors: 5
                      interval: 10s
                      baseEjectionTime: 30s
            """;

        public const string DockerCompose = """
            # Dubbo local development stack for {{ Domain.PascalName }}
            version: "3.9"

            services:
              nacos:
                image: nacos/nacos-server:v{{ Config.Apache.NacosVersion }}
                ports:
                  - "8848:8848"
                  - "9848:9848"
                environment:
                  MODE: standalone
                  NACOS_AUTH_ENABLE: "false"
                  JVM_XMS: "256m"
                  JVM_XMX: "512m"
                volumes:
                  - nacos-data:/home/nacos/data
                healthcheck:
                  test: ["CMD", "curl", "-sf", "http://localhost:8848/nacos/v1/console/health/liveness"]
                  interval: 10s
                  timeout: 5s
                  retries: 5
                  start_period: 30s

              dubbo-admin:
                image: apache/dubbo-admin:0.6
                ports:
                  - "8280:8080"
                environment:
                  admin.registry.address: "nacos://nacos:8848"
                  admin.config-center: "nacos://nacos:8848"
                  admin.metadata-report.address: "nacos://nacos:8848"
                depends_on:
                  nacos:
                    condition: service_healthy

            volumes:
              nacos-data:
            """;
    }
}
