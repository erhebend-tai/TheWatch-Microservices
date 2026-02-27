# Contributing to TheWatch

Thank you for your interest in contributing to TheWatch. This document provides
guidelines and requirements for contributions to ensure consistency, quality, and
compliance with applicable security standards.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Security Requirements](#security-requirements)
- [Testing Requirements](#testing-requirements)

---

## Code of Conduct

All contributors must adhere to the project [Code of Conduct](CODE_OF_CONDUCT.md).
Respectful, professional interaction is expected in all project spaces.

---

## Getting Started

1. Fork and clone the repository.
2. Follow the setup instructions in [`docs/developer-setup.md`](docs/developer-setup.md),
   including commit signing configuration.
3. Create a feature branch from `develop`:
   ```bash
   git checkout -b feature/your-feature-name develop
   ```

### Prerequisites

- .NET 10 SDK (preview)
- Docker Desktop
- Git with commit signing configured (GPG or SSH)

---

## Development Workflow

1. **Branch from `develop`** — All feature work branches from `develop`.
2. **Build & test locally** — Verify your changes build and pass tests before
   pushing.
3. **Open a pull request** — Target the `develop` branch. Production releases
   merge from `develop` to `main` through the release process.
4. **Address review feedback** — All PRs require review before merge.

### Building

```bash
# Restore dependencies
dotnet restore TheWatch.sln

# Build a specific project (preferred, avoids OOM)
dotnet build TheWatch.P1.CoreGateway/TheWatch.P1.CoreGateway.csproj -c Release

# Run service-level tests
dotnet test TheWatch.P1.CoreGateway.Tests/TheWatch.P1.CoreGateway.Tests.csproj
```

### Running Locally

```bash
# Via Aspire (recommended for local development)
dotnet run --project TheWatch.Aspire.AppHost/TheWatch.Aspire.AppHost.csproj

# Via Docker Compose
docker compose up -d
```

---

## Coding Standards

- **Language**: C# / .NET 10
- **Style**: Follow the existing code style in the repository. The CI pipeline
  enforces formatting via `dotnet format`.
- **Naming**: Use PascalCase for public members, camelCase for local variables
  and parameters.
- **Architecture**: Follow the established microservice patterns — each service
  has its own project, contract library, and test project.
- **Shared Code**: Cross-cutting concerns go in `TheWatch.Shared`. Service
  contracts go in the corresponding `TheWatch.Contracts.*` project.

---

## Commit Guidelines

- **Sign all commits** — GPG or SSH signing is required. See
  [`docs/developer-setup.md`](docs/developer-setup.md) for setup instructions.
- **Use conventional commit messages**:
  ```
  type(scope): short description

  Optional longer description.
  ```
  Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `security`
- **Keep commits focused** — One logical change per commit.

---

## Pull Request Process

1. **Fill out the PR template** — Provide a clear description, link related
   issues, and complete the compliance checklist.
2. **All CI checks must pass** — Build, test, security scanning, and format
   checks.
3. **At least one approving review** is required before merge.
4. **Squash merge** into `develop` to keep a clean commit history.

### PR Checklist

Before submitting, confirm:

- [ ] Code compiles without warnings
- [ ] New or changed functionality has corresponding tests
- [ ] Tests pass locally
- [ ] No secrets, credentials, or PII in the changeset
- [ ] Documentation updated if applicable
- [ ] Commits are signed

---

## Security Requirements

All contributions must comply with the project security standards:

- **No hardcoded secrets** — Use environment variables or secret management.
  Gitleaks runs on every PR.
- **Input validation** — Validate all external inputs.
- **Authentication & authorization** — Follow established JWT/RBAC patterns in
  `TheWatch.Shared`.
- **Dependency management** — Use Central Package Management
  (`Directory.Packages.props`). New dependencies require review.
- **Container security** — Dockerfiles must use non-root user (UID 1001) per
  DISA STIG V-222425.

For security vulnerabilities, follow the reporting process in
[`SECURITY.md`](SECURITY.md).

---

## Testing Requirements

- **Unit tests** are required for new business logic. Each service has a
  corresponding `TheWatch.P*.Tests` project.
- **Use xUnit** as the test framework with **FluentAssertions** for assertions.
- **Test naming**: `MethodName_StateUnderTest_ExpectedBehavior`
- **Run tests locally** before opening a PR:
  ```bash
  dotnet test TheWatch.P1.CoreGateway.Tests/TheWatch.P1.CoreGateway.Tests.csproj
  ```
- **Integration tests** in `TheWatch.Integration.Tests` require Docker for
  infrastructure dependencies.
