#!/usr/bin/env bash
set -euo pipefail

REPO_URL="https://raw.githubusercontent.com/Pulsy-Global/atria/main/deploy/docker"
REPO_ROOT_URL="https://raw.githubusercontent.com/Pulsy-Global/atria/main"
TARGET_DIR="${PWD}/atria-oss"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if ! docker compose version >/dev/null 2>&1; then
  echo "docker compose not found. Please install Docker Compose v2.20+."
  exit 1
fi

COMPOSE_VERSION="$(docker compose version --short 2>/dev/null || true)"
if [[ "${COMPOSE_VERSION}" =~ v?([0-9]+)\.([0-9]+)\.([0-9]+) ]]; then
  COMPOSE_MAJOR="${BASH_REMATCH[1]}"
  COMPOSE_MINOR="${BASH_REMATCH[2]}"
  COMPOSE_PATCH="${BASH_REMATCH[3]}"
  REQUIRED_VERSION="2.20.0"
  if (( COMPOSE_MAJOR < 2 || (COMPOSE_MAJOR == 2 && COMPOSE_MINOR < 20) )); then
    echo "Docker Compose ${COMPOSE_VERSION} detected. Please upgrade to v2.20+ for 'include' support."
    exit 1
  fi
else
  echo "Could not determine Docker Compose version from '${COMPOSE_VERSION}'. Please install Docker Compose v2.20+."
  exit 1
fi

mkdir -p \
  "${TARGET_DIR}/base" \
  "${TARGET_DIR}/prod" \
  "${TARGET_DIR}/config/nats" \
  "${TARGET_DIR}/config/observability/grafana/dashboards" \
  "${TARGET_DIR}/config/observability/grafana/provisioning/dashboards" \
  "${TARGET_DIR}/config/observability/grafana/provisioning/datasources" \
  "${TARGET_DIR}/config/observability/loki" \
  "${TARGET_DIR}/config/observability/otel-collector" \
  "${TARGET_DIR}/config/observability/prometheus" \
  "${TARGET_DIR}/data/k3s"

# Helper function: copy if local file exists, otherwise download
get_file() {
  local rel_path="$1"
  local target="$2"
  local local_file="${SCRIPT_DIR}/${rel_path}"

  if [[ -f "${local_file}" ]]; then
    cp "${local_file}" "${target}"
  else
    curl -fsSL "${REPO_URL}/${rel_path}" -o "${target}"
  fi
}

get_repo_file() {
  local rel_path="$1"
  local target="$2"
  local local_file="${SCRIPT_DIR}/../../${rel_path}"

  if [[ -f "${local_file}" ]]; then
    cp "${local_file}" "${target}"
  else
    curl -fsSL "${REPO_ROOT_URL}/${rel_path}" -o "${target}"
  fi
}

get_file "base/docker-compose.infra.yml" "${TARGET_DIR}/base/docker-compose.infra.yml"
get_file "base/docker-compose.observability.yml" "${TARGET_DIR}/base/docker-compose.observability.yml"
get_file "base/docker-compose.functions.yml" "${TARGET_DIR}/base/docker-compose.functions.yml"
get_file "prod/docker-compose.yml" "${TARGET_DIR}/prod/docker-compose.yml"
get_file "prod/docker-compose.prod.yml" "${TARGET_DIR}/prod/docker-compose.prod.yml"
get_file "prod/.env.example" "${TARGET_DIR}/prod/.env"
get_repo_file "config/nats/nats-server.cfg" "${TARGET_DIR}/config/nats/nats-server.cfg"
get_repo_file "config/nats/dashboard-config.json" "${TARGET_DIR}/config/nats/dashboard-config.json"

OBSERVABILITY_FILES=(
  "grafana/dashboards/atria-api-orchestrator.json"
  "grafana/dashboards/atria-collector.json"
  "grafana/dashboards/atria-delivery.json"
  "grafana/dashboards/atria-ingestor.json"
  "grafana/dashboards/atria-overview.json"
  "grafana/dashboards/atria-runtime.json"
  "grafana/provisioning/dashboards/dashboards.yml"
  "grafana/provisioning/datasources/metrics.yml"
  "loki/config.yaml"
  "otel-collector/config.yaml"
  "prometheus/alerts.yml"
  "prometheus/prometheus.yml"
)

for observability_file in "${OBSERVABILITY_FILES[@]}"; do
  get_repo_file \
    "config/observability/${observability_file}" \
    "${TARGET_DIR}/config/observability/${observability_file}"
done

read -r -s -p "NATS password (default: natsadmin): " NATS_PASS_INPUT
echo
read -r -s -p "Postgres password (default: Atria): " POSTGRES_PASS_INPUT
echo
echo

read -r -p "Enable serverless functions? (Installs k3s + Fission) [y/N]: " ENABLE_FUNCTIONS
echo

NATS_PASS_INPUT="${NATS_PASS_INPUT:-natsadmin}"
POSTGRES_PASS_INPUT="${POSTGRES_PASS_INPUT:-Atria}"

ENV_TMP="$(mktemp)"
awk -v nats_pass="${NATS_PASS_INPUT}" -v pg_pass="${POSTGRES_PASS_INPUT}" '
  /^NATS_PASS=/ { print "NATS_PASS=" nats_pass; next }
  /^POSTGRES_PASSWORD=/ { print "POSTGRES_PASSWORD=" pg_pass; next }
  { print }
' "${TARGET_DIR}/prod/.env" > "${ENV_TMP}"
mv "${ENV_TMP}" "${TARGET_DIR}/prod/.env"

if [[ "${ENABLE_FUNCTIONS}" =~ ^[Yy]$ ]]; then
  ENV_TMP="$(mktemp)"
  awk '
    /^ATRIA_FUNCTIONS_ENABLED=/ { print "ATRIA_FUNCTIONS_ENABLED=true"; next }
    { print }
  ' "${TARGET_DIR}/prod/.env" > "${ENV_TMP}"
  mv "${ENV_TMP}" "${TARGET_DIR}/prod/.env"
fi

cat <<'EOF'
Files ready in ./atria-oss
Next steps:
  1) Edit ./atria-oss/prod/.env (images, ports, ATRIA_ROOT if needed)
EOF

if [[ "${ENABLE_FUNCTIONS}" =~ ^[Yy]$ ]]; then
  cat <<'EOF'
  2) cd ./atria-oss/prod && docker compose --profile functions up -d

Services (default ports):
  - API: http://localhost:4300
  - SPA: http://localhost:7150
  - Grafana: http://localhost:3000
  - Functions: http://localhost:31314 (k3s + Fission)
EOF
else
  cat <<'EOF'
  2) cd ./atria-oss/prod && docker compose up -d

Services (default ports):
  - API: http://localhost:4300
  - SPA: http://localhost:7150
  - Grafana: http://localhost:3000

Note: To enable serverless functions later, run:
  cd ./atria-oss/prod && docker compose --profile functions up -d
EOF
fi
