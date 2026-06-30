---
title: GeoSense AI
emoji: 🌍
colorFrom: blue
colorTo: green
sdk: docker
app_port: 8000
pinned: false
---

# GeoSense AI

Topography, soil, and risk analysis microservice for Planora (FastAPI + Google
Earth Engine). Temporary deployment for testing/demo purposes.

- UI: `/ui`
- Swagger: `/docs`
- Health: `/api/v1/health`

## Required secrets (Space settings → Variables and secrets)

| Name | Type | Notes |
|---|---|---|
| `GEE_PROJECT` | Variable | GEE project ID |
| `GEE_SERVICE_ACCOUNT_EMAIL` | Variable | Service account email |
| `GEE_SERVICE_ACCOUNT_KEY_JSON` | Secret | Full contents of the service-account JSON key file |

Optional (S3 report storage / completion webhooks): `AWS_ACCESS_KEY_ID`,
`AWS_SECRET_ACCESS_KEY`, `AWS_REGION`, `AWS_S3_BUCKET`, `WEBHOOK_URL`,
`SHARED_SECRET`. The app degrades gracefully (reports as "degraded" in
`/api/v1/health`) if any of these are missing.
