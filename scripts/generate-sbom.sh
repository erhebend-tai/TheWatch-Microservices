#!/usr/bin/env bash
# =============================================================================
# generate-sbom.sh — Aggregate SBOM Generation for TheWatch Emergency Platform
# =============================================================================
# Item 327: Generate per-project and aggregate Software Bill of Materials (SBOM)
# for the full TheWatch microservices solution using CycloneDX and SPDX formats.
#
# Compliance References:
#   NIST SP 800-53 SR-4  — Provenance (supply chain integrity of components)
#   NIST SSDF PS.2       — Protect all forms of code from unauthorized access
#                          and tampering; verify integrity of acquired software
#
# Usage:
#   ./generate-sbom.sh [--output-dir <dir>] [--skip-spdx] [--verbose]
#
# Prerequisites:
#   - .NET SDK 10.0+ installed
#   - dotnet-CycloneDX tool (auto-installed if missing)
#   - cyclonedx-cli (auto-installed if missing)
#   - Solution restored (dotnet restore TheWatch.sln)
# =============================================================================
set -euo pipefail

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_DIR="${SCRIPT_DIR}"
OUTPUT_DIR="${SOLUTION_DIR}/dist/sbom"
AGGREGATE_FILE="thewatch-aggregate-sbom.json"
AGGREGATE_SPDX_FILE="thewatch-aggregate-sbom.spdx.json"
SKIP_SPDX=false
VERBOSE=false
CYCLONEDX_TOOL="dotnet-CycloneDX"
CYCLONEDX_CLI="cyclonedx"
TIMESTAMP="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"

# All projects to generate SBOMs for (14 services + Shared + Generators = 16 total)
# SR-4: Enumerating every deployable unit ensures provenance traceability
PROJECTS=(
  "TheWatch.P1.CoreGateway"
  "TheWatch.P2.VoiceEmergency"
  "TheWatch.P3.MeshNetwork"
  "TheWatch.P4.Wearable"
  "TheWatch.P5.AuthSecurity"
  "TheWatch.P6.FirstResponder"
  "TheWatch.P7.FamilyHealth"
  "TheWatch.P8.DisasterRelief"
  "TheWatch.P9.DoctorServices"
  "TheWatch.P10.Gamification"
  "TheWatch.P11.Surveillance"
  "TheWatch.Geospatial"
  "TheWatch.Dashboard"
  "TheWatch.Admin.RestAPI"
  "TheWatch.Shared"
  "TheWatch.Generators"
)

# ---------------------------------------------------------------------------
# Argument parsing
# ---------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
  case "$1" in
    --output-dir)
      OUTPUT_DIR="$2"
      shift 2
      ;;
    --skip-spdx)
      SKIP_SPDX=true
      shift
      ;;
    --verbose)
      VERBOSE=true
      shift
      ;;
    -h|--help)
      echo "Usage: $0 [--output-dir <dir>] [--skip-spdx] [--verbose]"
      echo ""
      echo "Options:"
      echo "  --output-dir <dir>  Output directory for SBOM files (default: dist/sbom/)"
      echo "  --skip-spdx         Skip SPDX format generation (CycloneDX only)"
      echo "  --verbose           Enable verbose output"
      echo ""
      echo "Generates per-project CycloneDX JSON SBOMs, merges them into an aggregate"
      echo "SBOM, and optionally produces SPDX format alongside."
      exit 0
      ;;
    *)
      echo "ERROR: Unknown argument: $1"
      exit 1
      ;;
  esac
done

# ---------------------------------------------------------------------------
# Logging helpers
# ---------------------------------------------------------------------------
log()  { echo "[SBOM] $(date +%H:%M:%S) $*"; }
vlog() { [[ "$VERBOSE" == "true" ]] && log "$*" || true; }
err()  { echo "[SBOM] ERROR: $*" >&2; }

# ---------------------------------------------------------------------------
# Tool installation
# ---------------------------------------------------------------------------
install_cyclonedx_tool() {
  # PS.2: Ensure SBOM tooling is available and verified before generating
  # provenance artifacts
  if dotnet tool list -g | grep -qi "cyclonedx"; then
    vlog "dotnet-CycloneDX already installed"
  else
    log "Installing dotnet-CycloneDX global tool..."
    dotnet tool install --global CycloneDX 2>/dev/null || \
      dotnet tool update --global CycloneDX
    log "dotnet-CycloneDX installed successfully"
  fi
}

install_cyclonedx_cli() {
  # cyclonedx-cli is required for merging multiple SBOMs into an aggregate
  if command -v cyclonedx &>/dev/null; then
    vlog "cyclonedx-cli already available on PATH"
    return 0
  fi

  # Try dotnet tool install
  if dotnet tool list -g | grep -qi "cyclonedx-cli"; then
    vlog "cyclonedx-cli already installed as dotnet tool"
    return 0
  fi

  log "Installing cyclonedx-cli..."
  # Attempt installation via dotnet global tool
  if dotnet tool install --global CycloneDX.Cli 2>/dev/null; then
    log "cyclonedx-cli installed via dotnet tool"
    return 0
  fi

  # Fallback: download pre-built binary from GitHub releases
  log "Attempting binary download of cyclonedx-cli..."
  local CLI_VERSION="0.27.1"
  local OS_SUFFIX
  case "$(uname -s)" in
    Linux*)  OS_SUFFIX="linux-x64" ;;
    Darwin*) OS_SUFFIX="osx-x64" ;;
    MINGW*|MSYS*|CYGWIN*) OS_SUFFIX="win-x64.exe" ;;
    *) err "Unsupported OS for cyclonedx-cli binary download"; return 1 ;;
  esac

  local DOWNLOAD_URL="https://github.com/CycloneDX/cyclonedx-cli/releases/download/v${CLI_VERSION}/cyclonedx-${OS_SUFFIX}"
  local INSTALL_PATH="${HOME}/.dotnet/tools/cyclonedx"
  curl -sSL -o "${INSTALL_PATH}" "${DOWNLOAD_URL}"
  chmod +x "${INSTALL_PATH}"
  log "cyclonedx-cli ${CLI_VERSION} downloaded to ${INSTALL_PATH}"
}

# ---------------------------------------------------------------------------
# SBOM generation — per-project
# ---------------------------------------------------------------------------
generate_project_sbom() {
  local project="$1"
  local project_dir="${SOLUTION_DIR}/${project}"
  local csproj="${project_dir}/${project}.csproj"
  local output_file="${OUTPUT_DIR}/${project}.cdx.json"

  if [[ ! -f "$csproj" ]]; then
    err "Project file not found: ${csproj} — skipping"
    return 1
  fi

  vlog "Generating CycloneDX SBOM for ${project}..."

  # SR-4: Each component's dependencies are individually enumerated for
  # provenance verification and supply-chain risk assessment
  dotnet CycloneDX "${csproj}" \
    --output "${OUTPUT_DIR}" \
    --filename "${project}.cdx.json" \
    --json \
    --exclude-dev \
    --set-type application \
    --set-name "TheWatch.${project}" \
    --set-version "$(git describe --tags --always 2>/dev/null || echo '0.0.0-local')" \
    ${VERBOSE:+--verbose} \
    2>&1 | { [[ "$VERBOSE" == "true" ]] && cat || cat >/dev/null; }

  if [[ -f "$output_file" ]]; then
    vlog "  -> ${output_file} ($(wc -c < "$output_file") bytes)"
    return 0
  else
    err "Failed to generate SBOM for ${project}"
    return 1
  fi
}

# ---------------------------------------------------------------------------
# SBOM merge — aggregate
# ---------------------------------------------------------------------------
merge_sboms() {
  log "Merging per-project SBOMs into aggregate..."

  local sbom_files=()
  for project in "${PROJECTS[@]}"; do
    local f="${OUTPUT_DIR}/${project}.cdx.json"
    if [[ -f "$f" ]]; then
      sbom_files+=("--input-file" "$f")
    fi
  done

  if [[ ${#sbom_files[@]} -eq 0 ]]; then
    err "No SBOM files found to merge"
    return 1
  fi

  # SR-4: The aggregate SBOM provides a complete, unified view of all
  # third-party and transitive dependencies across the entire TheWatch platform
  local aggregate_path="${OUTPUT_DIR}/${AGGREGATE_FILE}"

  # Use cyclonedx-cli merge to combine all per-project SBOMs
  # Try the dotnet tool invocation first, then fall back to bare command
  if dotnet tool list -g | grep -qi "cyclonedx-cli"; then
    dotnet cyclonedx merge \
      "${sbom_files[@]}" \
      --output-file "${aggregate_path}" \
      --output-format json \
      --name "TheWatch Emergency Response Platform" \
      --version "$(git describe --tags --always 2>/dev/null || echo '0.0.0-local')" \
      --hierarchical
  elif command -v cyclonedx &>/dev/null; then
    cyclonedx merge \
      "${sbom_files[@]}" \
      --output-file "${aggregate_path}" \
      --output-format json \
      --name "TheWatch Emergency Response Platform" \
      --version "$(git describe --tags --always 2>/dev/null || echo '0.0.0-local')" \
      --hierarchical
  else
    err "cyclonedx-cli not found. Cannot merge SBOMs."
    return 1
  fi

  if [[ -f "$aggregate_path" ]]; then
    log "Aggregate SBOM: ${aggregate_path} ($(wc -c < "$aggregate_path") bytes)"
  else
    err "Failed to create aggregate SBOM"
    return 1
  fi
}

# ---------------------------------------------------------------------------
# SPDX generation
# ---------------------------------------------------------------------------
generate_spdx() {
  if [[ "$SKIP_SPDX" == "true" ]]; then
    vlog "SPDX generation skipped (--skip-spdx)"
    return 0
  fi

  local aggregate_cdx="${OUTPUT_DIR}/${AGGREGATE_FILE}"
  local spdx_output="${OUTPUT_DIR}/${AGGREGATE_SPDX_FILE}"

  if [[ ! -f "$aggregate_cdx" ]]; then
    err "Aggregate CycloneDX SBOM not found; cannot convert to SPDX"
    return 1
  fi

  log "Converting aggregate SBOM to SPDX format..."

  # PS.2: Producing SBOMs in multiple standard formats (CycloneDX + SPDX) ensures
  # broad tooling compatibility and satisfies diverse compliance requirements
  if dotnet tool list -g | grep -qi "cyclonedx-cli"; then
    dotnet cyclonedx convert \
      --input-file "${aggregate_cdx}" \
      --output-file "${spdx_output}" \
      --output-format spdxjson
  elif command -v cyclonedx &>/dev/null; then
    cyclonedx convert \
      --input-file "${aggregate_cdx}" \
      --output-file "${spdx_output}" \
      --output-format spdxjson
  else
    err "cyclonedx-cli not found. Cannot convert to SPDX."
    return 1
  fi

  if [[ -f "$spdx_output" ]]; then
    log "SPDX SBOM: ${spdx_output} ($(wc -c < "$spdx_output") bytes)"
  else
    err "Failed to generate SPDX SBOM"
    return 1
  fi
}

# ---------------------------------------------------------------------------
# Summary statistics
# ---------------------------------------------------------------------------
print_summary() {
  local aggregate_path="${OUTPUT_DIR}/${AGGREGATE_FILE}"

  echo ""
  echo "============================================================"
  echo " TheWatch SBOM Generation Summary"
  echo " Generated: ${TIMESTAMP}"
  echo "============================================================"
  echo ""

  # Count per-project SBOMs generated
  local sbom_count=0
  local failed_count=0
  for project in "${PROJECTS[@]}"; do
    if [[ -f "${OUTPUT_DIR}/${project}.cdx.json" ]]; then
      ((sbom_count++))
    else
      ((failed_count++))
    fi
  done
  echo " Per-project SBOMs generated: ${sbom_count}/${#PROJECTS[@]}"
  if [[ $failed_count -gt 0 ]]; then
    echo " Failed/skipped:              ${failed_count}"
  fi
  echo ""

  # Extract stats from aggregate SBOM using python or jq if available
  if [[ -f "$aggregate_path" ]]; then
    echo " Aggregate SBOM: ${aggregate_path}"
    echo " File size:      $(du -h "$aggregate_path" | cut -f1)"

    # Attempt to extract component count and license summary
    if command -v python3 &>/dev/null; then
      python3 - "${aggregate_path}" <<'PYEOF'
import json, sys, collections

try:
    with open(sys.argv[1], "r") as f:
        sbom = json.load(f)
    components = sbom.get("components", [])
    print(f" Total packages: {len(components)}")

    # Count unique licenses
    license_counts = collections.Counter()
    for comp in components:
        for lic_entry in comp.get("licenses", []):
            lic = lic_entry.get("license", {})
            license_id = lic.get("id", lic.get("name", "Unknown"))
            if license_id:
                license_counts[license_id] += 1

    if license_counts:
        print(f" Unique licenses: {len(license_counts)}")
        print(" Top licenses:")
        for lic_id, count in license_counts.most_common(10):
            print(f"   {lic_id}: {count} packages")
    else:
        print(" License data: not available in SBOM")
except Exception as e:
    print(f" (Could not parse aggregate SBOM: {e})")
PYEOF
    elif command -v jq &>/dev/null; then
      local total_packages
      total_packages="$(jq '.components | length' "$aggregate_path" 2>/dev/null || echo 'N/A')"
      echo " Total packages: ${total_packages}"

      echo " Unique licenses:"
      jq -r '[.components[]?.licenses[]?.license.id // .components[]?.licenses[]?.license.name // "Unknown"] | group_by(.) | map({license: .[0], count: length}) | sort_by(-.count) | .[:10][] | "   \(.license): \(.count) packages"' \
        "$aggregate_path" 2>/dev/null || echo "   (Could not extract license data)"
    else
      echo " (Install python3 or jq to see package/license stats)"
    fi
  fi

  echo ""

  # SPDX summary
  local spdx_path="${OUTPUT_DIR}/${AGGREGATE_SPDX_FILE}"
  if [[ -f "$spdx_path" ]]; then
    echo " SPDX SBOM:      ${spdx_path}"
    echo " SPDX file size: $(du -h "$spdx_path" | cut -f1)"
  elif [[ "$SKIP_SPDX" == "true" ]]; then
    echo " SPDX SBOM:      skipped"
  else
    echo " SPDX SBOM:      not generated (conversion failed)"
  fi

  echo ""
  echo " Output directory: ${OUTPUT_DIR}/"
  ls -lh "${OUTPUT_DIR}/" 2>/dev/null | tail -n +2
  echo ""
  echo "============================================================"
  echo " Compliance: NIST SR-4 (Provenance) | SSDF PS.2 (Integrity)"
  echo "============================================================"
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
main() {
  log "Starting SBOM generation for TheWatch Emergency Response Platform"
  log "Solution directory: ${SOLUTION_DIR}"
  log "Output directory:   ${OUTPUT_DIR}"
  log "Projects:           ${#PROJECTS[@]}"
  echo ""

  # Create output directory
  mkdir -p "${OUTPUT_DIR}"

  # Install tooling
  install_cyclonedx_tool
  install_cyclonedx_cli

  echo ""
  log "=== Phase 1: Per-Project SBOM Generation ==="
  echo ""

  # Generate per-project SBOMs
  local success_count=0
  local fail_count=0
  for project in "${PROJECTS[@]}"; do
    log "[$((success_count + fail_count + 1))/${#PROJECTS[@]}] ${project}"
    if generate_project_sbom "$project"; then
      ((success_count++))
    else
      ((fail_count++))
    fi
  done

  echo ""
  log "Per-project generation complete: ${success_count} succeeded, ${fail_count} failed"

  if [[ $success_count -eq 0 ]]; then
    err "No SBOMs were generated. Aborting."
    exit 1
  fi

  echo ""
  log "=== Phase 2: Aggregate SBOM Merge ==="
  merge_sboms

  echo ""
  log "=== Phase 3: SPDX Conversion ==="
  generate_spdx

  # Print summary
  print_summary

  log "SBOM generation complete."
}

main "$@"
