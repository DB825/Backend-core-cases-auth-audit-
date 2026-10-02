#!/usr/bin/env bash
# Captures the live OpenAPI spec from CaseAuth.Api and regenerates the Angular client from it.
# Run this after changing any controller/contract so the spec and client stay in sync.
set -euo pipefail
cd "$(dirname "$0")/.."

PORT=5099
BASE_URL="http://127.0.0.1:$PORT"
TMP_DB="$(mktemp -u /tmp/caseauth-openapi-XXXXXX.db)"
TMP_UPLOADS="$(mktemp -d /tmp/caseauth-openapi-uploads-XXXXXX)"

echo "==> Building CaseAuth.Api"
dotnet build src/CaseAuth.Api/CaseAuth.Api.csproj -c Release --nologo

echo "==> Starting CaseAuth.Api temporarily on $BASE_URL"
API_PID=""
cleanup() {
  [ -n "$API_PID" ] && kill "$API_PID" 2>/dev/null || true
  rm -f "$TMP_DB"
  rm -rf "$TMP_UPLOADS"
}
trap cleanup EXIT

ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="$BASE_URL" \
ConnectionStrings__Sqlite="Data Source=$TMP_DB" \
Storage__LocalDiskRoot="$TMP_UPLOADS" \
  dotnet src/CaseAuth.Api/bin/Release/net8.0/CaseAuth.Api.dll &
API_PID=$!

echo "==> Waiting for the API to come up"
for _ in $(seq 1 30); do
  if curl -sf "$BASE_URL/swagger/v1/swagger.json" -o openapi/openapi.json; then
    echo "==> Spec captured to openapi/openapi.json"
    break
  fi
  sleep 1
done

if [ ! -s openapi/openapi.json ]; then
  echo "Failed to capture the OpenAPI spec - is the API failing to start? Check the output above." >&2
  exit 1
fi

kill "$API_PID" 2>/dev/null || true
API_PID=""

echo "==> Generating the Angular client"
cd clients/angular
npm install
npm run generate

echo "==> Done. Generated client is in clients/angular/src/"
