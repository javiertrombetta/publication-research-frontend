#!/usr/bin/env bash
#
# Deploys the web application to Azure Container Apps, into the same environment as the API.
#
# Safe to run again: every step either creates something or updates it to match. Nothing is deleted.
#
# Usage:
#   az login
#   az account set --subscription "<your subscription>"
#   ./azure/deploy.sh
set -euo pipefail

LOCATION="${LOCATION:-australiaeast}"
RESOURCE_GROUP="${RESOURCE_GROUP:-publication-research}"
ENVIRONMENT_NAME="${ENVIRONMENT_NAME:-publication-research-env}"
APP_NAME="${APP_NAME:-publication-research-frontend}"
BACKEND_APP_NAME="${BACKEND_APP_NAME:-publication-research-backend}"
IMAGE="${IMAGE:-docker.io/javiertrombetta/publication-research-frontend:latest}"
PRIVACY_POLICY_URL="${PRIVACY_POLICY_URL:-https://www.ais.ac.nz/}"

echo "==> Subscription"
az account show --query "{name:name, id:id}" -o tsv

echo "==> Resource group: $RESOURCE_GROUP ($LOCATION)"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

echo "==> Container Apps environment: $ENVIRONMENT_NAME"
# Shared with the API. Whichever of the two scripts runs first creates it.
if ! az containerapp env show --name "$ENVIRONMENT_NAME" --resource-group "$RESOURCE_GROUP" --output none 2>/dev/null; then
  az containerapp env create \
    --name "$ENVIRONMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output none
fi

# Where the API is. Read from the deployment rather than asked for, so the two cannot drift apart,
# and overridable for the case where the API is somewhere else entirely.
API_BASE_URL="${API_BASE_URL:-}"
if [ -z "$API_BASE_URL" ]; then
  API_FQDN="$(az containerapp show \
    --name "$BACKEND_APP_NAME" --resource-group "$RESOURCE_GROUP" \
    --query "properties.configuration.ingress.fqdn" -o tsv 2>/dev/null || true)"

  if [ -z "$API_FQDN" ]; then
    echo "error: the API is not deployed in $RESOURCE_GROUP, and no API_BASE_URL was given." >&2
    echo "       Deploy the backend first, or export API_BASE_URL=https://..." >&2
    exit 1
  fi

  API_BASE_URL="https://$API_FQDN"
fi

echo "==> Talking to the API at $API_BASE_URL"

ENV_VARS=(
  "ASPNETCORE_ENVIRONMENT=Production"
  "PORT=8080"
  "Api__BaseUrl=$API_BASE_URL"
  "Institution__PrivacyPolicyUrl=$PRIVACY_POLICY_URL"
)

echo "==> Container app: $APP_NAME"
if az containerapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --output none 2>/dev/null; then
  az containerapp update \
    --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" \
    --image "$IMAGE" \
    --set-env-vars "${ENV_VARS[@]}" --output none
else
  az containerapp create \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --environment "$ENVIRONMENT_NAME" \
    --image "$IMAGE" \
    --target-port 8080 \
    --ingress external \
    --cpu 0.5 --memory 1.0Gi \
    --min-replicas 0 --max-replicas 1 \
    --env-vars "${ENV_VARS[@]}" \
    --output none
fi

FQDN="$(az containerapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" \
  --query "properties.configuration.ingress.fqdn" -o tsv)"

echo
echo "Site: https://$FQDN"
echo
echo "Run the API's deploy script again so it knows this address: it needs it for CORS and for the"
echo "links in verification and password-reset emails."
