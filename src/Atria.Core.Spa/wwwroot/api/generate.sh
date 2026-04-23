#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

API_URL="${API_URL:-http://localhost:4300/swagger/MainAPI/swagger.json}"

echo "Generating API client from $API_URL..."
nswag openapi2tsclient \
  /input:"$API_URL" \
  /output:api.client.ts \
  /className:ApiClient \
  /template:Angular \
  /rxjsVersion:7 \
  /injectionTokenType:InjectionToken

echo "Done."
