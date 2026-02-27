# TheWatch Microservices – Documentation Hub

This folder centralizes the living documentation for the TheWatch platform: what each service does, how to run the stack locally, and where to find security and compliance references.

## How to use this documentation

- Start with the **Architecture and service map** in [`microservices.md`](./microservices.md).
- Follow **developer setup** steps below for local work.
- Consult **security & compliance** references that remain in the existing [`docs`](https://github.com/erhebend-tai/TheWatch-Microservices/tree/main/docs) directory.
- Use the **Operations** section for build, test, and deployment entry points.

## Quick start for contributors

1. Install .NET 10 (preview) and Docker (for local containers).  
2. Restore dependencies:  
   ```bash
   dotnet restore TheWatch.sln
   ```
3. Build locally (mirrors CI’s per-project approach):  
   ```bash
   dotnet build TheWatch.Shared/TheWatch.Shared.csproj -c Release
   dotnet build TheWatch.P1.CoreGateway/TheWatch.P1.CoreGateway.csproj -c Release
   # repeat for other services as needed, or build the solution if resources allow
   ```
4. Run the Aspire orchestration for an end-to-end dev stack:  
   ```bash
   dotnet run --project TheWatch.Aspire.AppHost/TheWatch.Aspire.AppHost.csproj
   ```
5. Execute targeted tests when changing a service:  
   ```bash
   dotnet test TheWatch.P1.CoreGateway.Tests/TheWatch.P1.CoreGateway.Tests.csproj
   ```

> For signing, branch protection, and contributor policies, see [`docs/developer-setup.md`](https://github.com/erhebend-tai/TheWatch-Microservices/blob/main/docs/developer-setup.md).

## Architecture at a glance

- **Microservices (P1–P11)**: service-specific APIs with per-service databases; see [`microservices.md`](./microservices.md) for a map.
- **Shared libraries**: `TheWatch.Shared` (cross-cutting concerns) and `TheWatch.Contracts.*` (typed contracts).
- **Eventing & real time**: Kafka-based event bus, SignalR hubs for live updates, and mesh networking for offline resilience.
- **Client surfaces**: MAUI mobile app, web Dashboard, and Admin API/CLI.
- **Infrastructure**: Aspire AppHost for local orchestration, Docker Compose/Helm/Terraform for deployments, SBOM generation via `scripts/generate-sbom.sh`.

## Security, compliance, and policy references

- Security policies: [`docs/policies`](https://github.com/erhebend-tai/TheWatch-Microservices/tree/main/docs/policies)
- Vulnerability management: [`docs/vulnerability-management-policy.md`](https://github.com/erhebend-tai/TheWatch-Microservices/blob/main/docs/vulnerability-management-policy.md)
- Incident response: [`docs/incident-response-plan.md`](https://github.com/erhebend-tai/TheWatch-Microservices/blob/main/docs/incident-response-plan.md)
- SSDF attestation: [`docs/ssdf-attestation.md`](https://github.com/erhebend-tai/TheWatch-Microservices/blob/main/docs/ssdf-attestation.md)
- Data classification: [`docs/data-classification-matrix.md`](https://github.com/erhebend-tai/TheWatch-Microservices/blob/main/docs/data-classification-matrix.md)

## Operations and deployment entry points

- **Container builds**: `docker-compose.yml` for local services; production images built via `.github/workflows/docker-publish.yml`.
- **Kubernetes/Helm**: charts in `helm/`; Terraform in `terraform/`.
- **SBOM & provenance**: `scripts/generate-sbom.sh` and `.github/workflows/slsa-provenance.yml`.
- **CI**: `.github/workflows/ci.yml` builds per project and runs service-level tests on `main`/`develop`.

## Documentation publishing (GitHub Pages)

The workflow `.github/workflows/docs-pages.yml` publishes this `documentation/` folder (and any files under `docs/`) to GitHub Pages on every push to `main` that touches documentation. No manual steps are required—updating Markdown here automatically refreshes the published site.
