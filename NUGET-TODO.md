# TheWatch — NuGet Strategy & TODO

> How we create, version, and distribute NuGet packages from this solution, and what remains to be done.

---

## Current State

| Area | Status |
|------|--------|
| Packable NuGet packages | **None** — no project has `<IsPackable>true</IsPackable>` or pack metadata |
| Central package management | **None** — no `Directory.Build.props` or `Directory.Packages.props` |
| Private NuGet feed | **None** — only `nuget.org` and a local Hangfire Pro folder in `nuget.config` |
| NuGet publish CI/CD | **None** — GitHub Actions build/test/deploy but never `dotnet pack` or `dotnet nuget push` |
| Version strategy | **None** — projects inherit SDK defaults (`1.0.0`); no `<Version>` or `<VersionPrefix>` set anywhere |

---

## Package Candidates

### Tier 1 — Internal Infrastructure (publish first)

These are consumed by every service and client in the solution. Packaging them lets us version shared code independently and enables external consumers (partner integrations, third-party responder apps).

| Package ID | Project | Target(s) | Dependencies |
|------------|---------|-----------|--------------|
| `TheWatch.Contracts.Abstractions` | `TheWatch.Contracts.Abstractions/` | net10.0 | None (zero deps) |
| `TheWatch.Shared` | `TheWatch.Shared/` | net10.0; net10.0-android; net10.0-ios | Serilog, JWT, EF Core, SignalR, Azure/GCP SDKs |
| `TheWatch.Generators` | `TheWatch.Generators/` | netstandard2.0 | Microsoft.CodeAnalysis (analyzer package — special packaging rules) |

> **TheWatch.Generators** is a Roslyn Source Generator. It must be packaged with `<IncludeBuildOutput>false</IncludeBuildOutput>` and the analyzer DLL placed in the `analyzers/dotnet/cs` folder inside the `.nupkg`. See [Roslyn analyzer packaging docs](https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions).

### Tier 2 — Service Contracts (publish second)

Each contract library exposes DTOs, typed `HttpClient` wrappers, and interface definitions for one microservice. They depend only on `TheWatch.Contracts.Abstractions`. These are the primary packages external consumers would reference.

| Package ID | Project |
|------------|---------|
| `TheWatch.Contracts.AuthSecurity` | `TheWatch.Contracts.AuthSecurity/` |
| `TheWatch.Contracts.CoreGateway` | `TheWatch.Contracts.CoreGateway/` |
| `TheWatch.Contracts.DisasterRelief` | `TheWatch.Contracts.DisasterRelief/` |
| `TheWatch.Contracts.DoctorServices` | `TheWatch.Contracts.DoctorServices/` |
| `TheWatch.Contracts.FamilyHealth` | `TheWatch.Contracts.FamilyHealth/` |
| `TheWatch.Contracts.FirstResponder` | `TheWatch.Contracts.FirstResponder/` |
| `TheWatch.Contracts.Gamification` | `TheWatch.Contracts.Gamification/` |
| `TheWatch.Contracts.Geospatial` | `TheWatch.Contracts.Geospatial/` |
| `TheWatch.Contracts.MeshNetwork` | `TheWatch.Contracts.MeshNetwork/` |
| `TheWatch.Contracts.Surveillance` | `TheWatch.Contracts.Surveillance/` |
| `TheWatch.Contracts.VoiceEmergency` | `TheWatch.Contracts.VoiceEmergency/` |
| `TheWatch.Contracts.Wearable` | `TheWatch.Contracts.Wearable/` |

**Option**: Publish a meta-package `TheWatch.Contracts` that pulls in all 12 + Abstractions for consumers who want the full set.

### Tier 3 — Optional / Later

| Package ID | Project | Notes |
|------------|---------|-------|
| `TheWatch.Aspire.ServiceDefaults` | `TheWatch.Aspire.ServiceDefaults/` | Only useful if distributing Aspire templates |
| `TheWatch.Mobile.Sdk` | *(does not exist yet)* | Subset of Mobile services for third-party MAUI apps |

### Not Packaged (ever)

- **Microservices** (P1–P11, Geospatial) — deployed as containers, not packages
- **Dashboard / Admin / Admin.RestAPI / Admin.CLI** — deployed as apps
- **Test projects** — already marked `<IsPackable>false</IsPackable>`
- **Aspire AppHost** — orchestrator, not distributable

---

## TODO Checklist

### Phase 1: Central Package Management

- [ ] **N-01.** Create `Directory.Build.props` at solution root with shared metadata:
  ```xml
  <Project>
    <PropertyGroup>
      <Authors>TheWatch Project</Authors>
      <Company>TheWatch</Company>
      <Copyright>Copyright (c) 2026 TheWatch Project</Copyright>
      <PackageLicenseExpression>UNLICENSED</PackageLicenseExpression>
      <PackageProjectUrl>https://github.com/your-org/thewatch</PackageProjectUrl>
      <RepositoryUrl>https://github.com/your-org/thewatch</RepositoryUrl>
      <RepositoryType>git</RepositoryType>
      <PackageIcon>icon.png</PackageIcon>
      <PackageReadmeFile>README.md</PackageReadmeFile>
    </PropertyGroup>
  </Project>
  ```
- [ ] **N-02.** Create `Directory.Packages.props` at solution root to enable Central Package Management (CPM). Consolidate all `PackageReference` versions from 45 `.csproj` files into one place. Key packages to centralize:
  - Serilog family (4.x, 9.x, 3.x, 6.x)
  - Microsoft.Extensions.* (10.0.x)
  - Microsoft.EntityFrameworkCore.* (10.0.x)
  - Microsoft.AspNetCore.* (10.0.x)
  - Aspire.* (9.2.x)
  - System.IdentityModel.Tokens.Jwt (8.x)
  - Confluent.Kafka (2.x)
  - Hangfire.* (1.8.x / 3.0.4)
  - xunit / FluentAssertions / NSubstitute
  - All other shared dependencies
- [ ] **N-03.** Strip `Version=` from every `<PackageReference>` in all 45 `.csproj` files (CPM requires versions only in `Directory.Packages.props`).
- [ ] **N-04.** Verify solution builds after CPM migration: `dotnet build TheWatch.sln`

### Phase 2: Package Metadata & Configuration

- [ ] **N-05.** Add NuGet metadata to `TheWatch.Contracts.Abstractions.csproj`:
  ```xml
  <PropertyGroup>
    <PackageId>TheWatch.Contracts.Abstractions</PackageId>
    <Description>Base contract interfaces for TheWatch microservices.</Description>
    <IsPackable>true</IsPackable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  ```
- [ ] **N-06.** Add NuGet metadata to all 12 `TheWatch.Contracts.*.csproj` files (same pattern, unique Description per package).
- [ ] **N-07.** Add NuGet metadata to `TheWatch.Shared.csproj`. Note: multi-TFM package (net10.0, net10.0-android, net10.0-ios) — verify `dotnet pack` produces a single `.nupkg` with all TFM `lib/` folders.
- [ ] **N-08.** Add Roslyn analyzer packaging to `TheWatch.Generators.csproj`:
  ```xml
  <PropertyGroup>
    <PackageId>TheWatch.Generators</PackageId>
    <IsPackable>true</IsPackable>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <DevelopmentDependency>true</DevelopmentDependency>
    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
    <TargetsForTfmSpecificContentInPackage>
      $(TargetsForTfmSpecificContentInPackage);_AddAnalyzersToOutput
    </TargetsForTfmSpecificContentInPackage>
  </PropertyGroup>
  <Target Name="_AddAnalyzersToOutput">
    <ItemGroup>
      <TfmSpecificPackageFile Include="$(OutputPath)\TheWatch.Generators.dll"
        PackagePath="analyzers/dotnet/cs" />
    </ItemGroup>
  </Target>
  ```
- [ ] **N-09.** Explicitly mark non-packable projects (`<IsPackable>false</IsPackable>`): all microservices (P1–P11), Geospatial, Dashboard, Admin, Admin.RestAPI, Admin.CLI, Aspire AppHost. Test projects already have this.
- [ ] **N-10.** Create `icon.png` (128x128 minimum) for package icon. Add as `<None Include="icon.png" Pack="true" PackagePath="\" />` in `Directory.Build.props` or each packable project.

### Phase 3: Versioning Strategy

- [ ] **N-11.** Choose and implement a versioning scheme:

  **Recommended: SemVer with shared version prefix**
  ```
  Directory.Build.props:
    <VersionPrefix>1.0.0</VersionPrefix>

  CI (pre-release builds):
    dotnet pack -p:VersionSuffix=preview.$(BUILD_NUMBER)
    → TheWatch.Contracts.CoreGateway.1.0.0-preview.42.nupkg

  Release builds:
    dotnet pack
    → TheWatch.Contracts.CoreGateway.1.0.0.nupkg
  ```

  **Alternative: Independent versioning per package** — more flexible but harder to manage. Only choose this if Contracts and Shared will evolve on very different cadences.

- [ ] **N-12.** Add `<PackageReleaseNotes>` or a CHANGELOG.md approach. Consider [MinVer](https://github.com/adamralph/minver) or [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) for automatic version derivation from git tags.

### Phase 4: Private Feed Setup

- [ ] **N-13.** Decide on feed hosting:

  | Option | Pros | Cons |
  |--------|------|------|
  | **GitHub Packages** | Free for private repos, integrated with Actions | 500MB free, then paid; GitHub-ecosystem lock-in |
  | **Azure Artifacts** | 2GB free, integrates with Azure DevOps | Requires Azure account |
  | **AWS CodeArtifact** | Integrates with AWS infra already in Terraform | Requires AWS credential setup |
  | **Self-hosted (BaGet)** | Full control, no vendor lock-in | Must operate + secure the server |
  | **nuget.org** | Public, free, standard | Packages are public (may not be desired) |
  | **Local folder feed** | Simplest for dev | Not suitable for CI/CD or team sharing |

- [ ] **N-14.** Configure chosen feed in `nuget.config`:
  ```xml
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="hangfire-local" value="E:\json_output\Nugets\Hangfire" />
    <add key="thewatch-internal" value="https://YOUR_FEED_URL/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <thewatch-internal>
      <add key="Username" value="..." />
      <add key="ClearTextPassword" value="%NUGET_FEED_TOKEN%" />
    </thewatch-internal>
  </packageSourceCredentials>
  ```
- [ ] **N-15.** Add feed credentials as GitHub Actions secret (`NUGET_FEED_TOKEN`) for CI publishing.

### Phase 5: CI/CD Pipeline

- [ ] **N-16.** Create `.github/workflows/nuget-publish.yml`:
  ```yaml
  name: NuGet Publish
  on:
    push:
      tags: ['v*']          # Release on version tag
    workflow_dispatch:       # Manual trigger

  jobs:
    pack:
      runs-on: ubuntu-latest
      steps:
        - uses: actions/checkout@v4
        - uses: actions/setup-dotnet@v4
          with:
            dotnet-version: '10.0.x'

        - name: Pack
          run: |
            dotnet pack TheWatch.Contracts.Abstractions/ -c Release -o ./nupkgs
            dotnet pack TheWatch.Shared/ -c Release -o ./nupkgs
            dotnet pack TheWatch.Generators/ -c Release -o ./nupkgs
            for dir in TheWatch.Contracts.*/; do
              dotnet pack "$dir" -c Release -o ./nupkgs
            done

        - name: Push
          run: |
            dotnet nuget push ./nupkgs/*.nupkg \
              --source "thewatch-internal" \
              --api-key ${{ secrets.NUGET_FEED_TOKEN }} \
              --skip-duplicate
  ```
- [ ] **N-17.** Add pre-release publishing on `develop` branch merges (append `-preview.{run_number}` suffix).
- [ ] **N-18.** Add `dotnet pack` step to existing `ci.yml` as a validation (pack but don't push, just verify packages build).

### Phase 6: Dependency Graph Cleanup

- [ ] **N-19.** Once Contracts are published as NuGet packages, decide whether consuming services (P1–P11, Admin.RestAPI) should:
  - **Option A**: Keep `<ProjectReference>` (monorepo workflow — build everything together). This is the default and simplest for a single-team project.
  - **Option B**: Switch to `<PackageReference>` for contract libraries (forces versioned contracts, enables independent service deployment). Better for multi-team scenarios.

  **Recommendation**: Stay with ProjectReference in the monorepo. Publish packages for **external** consumers only. Internal services keep building from source.

- [ ] **N-20.** Convert `TheWatch.Generators` from `<ProjectReference>` to `<PackageReference>` in all consuming projects **if** the generator package is published. This would require:
  - Removing `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"` from ProjectReference
  - Adding `<PackageReference Include="TheWatch.Generators" />` with analyzer attributes
  - Testing that source generation still works from the package

- [ ] **N-21.** Audit transitive dependency exposure. `TheWatch.Shared` has heavy dependencies (EF Core, Serilog, Azure SDKs, GCP SDKs). Consider:
  - Marking implementation-only deps as `<PrivateAssets>all</PrivateAssets>` so they don't flow to consumers
  - Splitting `TheWatch.Shared` into smaller packages if it grows further:
    - `TheWatch.Shared.Core` (auth extensions, DTOs, base classes)
    - `TheWatch.Shared.Azure` (Azure provider stubs)
    - `TheWatch.Shared.Gcp` (GCP provider stubs)
    - `TheWatch.Shared.Cloudflare` (Cloudflare provider stubs)

### Phase 7: Package Quality

- [ ] **N-22.** Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` on all packable projects so IntelliSense XML docs are included in `.nupkg`.
- [ ] **N-23.** Add `<EnablePackageValidation>true</EnablePackageValidation>` (.NET 8+) to detect breaking API changes between versions.
- [ ] **N-24.** Add Source Link for debugger source stepping:
  ```xml
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.*" PrivateAssets="All" />
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  ```
- [ ] **N-25.** Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` for packable projects to catch XML doc warnings and API compatibility issues at build time.

---

## Package Dependency Graph

```
TheWatch.Contracts.Abstractions        (zero deps)
  ├── TheWatch.Contracts.AuthSecurity
  ├── TheWatch.Contracts.CoreGateway
  ├── TheWatch.Contracts.DisasterRelief
  ├── TheWatch.Contracts.DoctorServices
  ├── TheWatch.Contracts.FamilyHealth
  ├── TheWatch.Contracts.FirstResponder
  ├── TheWatch.Contracts.Gamification
  ├── TheWatch.Contracts.Geospatial
  ├── TheWatch.Contracts.MeshNetwork
  ├── TheWatch.Contracts.Surveillance
  ├── TheWatch.Contracts.VoiceEmergency
  └── TheWatch.Contracts.Wearable

TheWatch.Shared                        (heavy deps — EF Core, Serilog, cloud SDKs)
  └── consumed by all microservices, Dashboard, Mobile, Admin

TheWatch.Generators                    (analyzer package — Roslyn Source Generator)
  └── consumed as analyzer by all services + clients
```

---

## Publish Order

Packages must be published bottom-up respecting the dependency tree:

1. `TheWatch.Contracts.Abstractions` (no deps)
2. All 12 `TheWatch.Contracts.*` packages (depend on Abstractions)
3. `TheWatch.Shared` (independent — no contract deps)
4. `TheWatch.Generators` (independent — analyzer package)

Steps 2–4 can be parallel since they only depend on step 1.

---

## Quick Reference: Key Commands

```bash
# Pack all packable projects
dotnet pack TheWatch.sln -c Release -o ./nupkgs

# Pack a single project
dotnet pack TheWatch.Contracts.CoreGateway/ -c Release -o ./nupkgs

# Pack with pre-release suffix
dotnet pack -c Release -o ./nupkgs -p:VersionSuffix=preview.42

# Push to feed
dotnet nuget push ./nupkgs/*.nupkg --source thewatch-internal --api-key YOUR_KEY

# List packages on feed
dotnet nuget list source thewatch-internal

# Verify package contents
dotnet nuget verify ./nupkgs/TheWatch.Contracts.CoreGateway.1.0.0.nupkg
unzip -l ./nupkgs/TheWatch.Contracts.CoreGateway.1.0.0.nupkg
```

---

## Priority Summary

| Phase | Items | Effort | Impact |
|-------|-------|--------|--------|
| 1. Central Package Management | N-01 to N-04 | Medium | High — version consistency across 45 projects |
| 2. Package Metadata | N-05 to N-10 | Low | Required before any packing |
| 3. Versioning | N-11 to N-12 | Low | Required for reproducible builds |
| 4. Feed Setup | N-13 to N-15 | Low–Medium | Required for distribution |
| 5. CI/CD Pipeline | N-16 to N-18 | Medium | Automated publishing |
| 6. Dependency Cleanup | N-19 to N-21 | Medium | Cleaner package graph, smaller downloads |
| 7. Package Quality | N-22 to N-25 | Low | Polish — IntelliSense, Source Link, validation |

**Suggested starting point**: Phase 1 (CPM) has the highest immediate value — it eliminates version drift across 45 projects regardless of whether you ever publish packages.

---

*Created: 2026-02-26*
