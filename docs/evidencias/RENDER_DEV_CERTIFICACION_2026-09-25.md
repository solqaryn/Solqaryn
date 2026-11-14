# Certificación Render DEV — 2026-09-25

Estado: **PASS / CERRADO**

## Ownership y servicio

- Workspace: `SOLQARYN`
- Workspace ID: `tea-dapjfom7bikc73fncodg`
- Email: `solqaryn.platform@outlook.com`
- Servicio: `solqaryn-api-dev`
- Service ID: `srv-daqvla49v7es738up9vg`
- Repositorio: `https://github.com/solqaryn/Solqaryn`
- Rama: `dev`
- Auto deploy gate: `checksPass`
- Runtime: Docker
- Dockerfile: `./backend/Dockerfile`
- Health: `/health/ready`
- URL: `https://solqaryn-api-dev-fxx8.onrender.com`
- Maintenance: OFF
- Suspensión: NO

## Evidencia de base y salud

Los logs actuales del servicio muestran explícitamente:

- database: `solqaryn_dev`;
- server: `solqaryn-mysql-solqaryn.h.aivencloud.com`;
- `GET /health/ready`: HTTP 200 de forma repetida.

## Decisión operativa

Render DEV queda certificado como recurso canónico bajo SOLQARYN. No crear otro servicio DEV. Un recurso Render DEV legacy de cuenta personal solo podrá eliminarse cuando se identifique en dicha cuenta y se compruebe que no tiene consumidores.
