# Punto 19 — Tenancy fail-closed en las lanes provider

Fecha: 2026-10-04 (hora local)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Cobertura reforzada

- La auditoría tenant es obligatoria en CI mediante `--require-certified`; una dimensión ausente hace fallar la lane.
- Identidad/empresa, scope vigente, autorización del recurso, persistencia, reportes, archivos, claves de caché y procesamiento background tenant-bound devuelven `PASS`.
- El Outbox se reconoce como procesador por empresa y se distingue de un worker hospedado: existe un `OutboxRetryProcessor` tenant-bound y hay cero `BackgroundService`/`IHostedService`.
- El audit `--require-certified` exige todas las dimensiones y falla cerrado si falta alguna. En la evidencia exact-head actual pasó en ambas lanes; no se limita a una revisión estática opcional.
- La lane certificada Oracle EF10/net10 pasó 2336/2336 unitarias y 22/22 integraciones; la integración MySQL Pomelo pasó 28/28.

## Evidencia exact-head

- Fase 6 exact-head run `37309393715`, HEAD `eb505b3ae36232d6456f538246b876bec0cac938`: todos los jobs success; `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- Pomelo ejecutó `priority3_tenant_isolation_audit.py --require-certified`: `PRIORITY3_MULTITENANT_CERTIFICATION=PASS`; identity/company, scope vigente, autorización del recurso, persistencia, reportes, archivos, cache y background tenant-aware todos `PASS`; `TENANT_BACKGROUND_RUNTIME_COUNT=1`, `TENANT_BACKGROUND_HOSTED_WORKER_COUNT=0`, `TENANT_BACKGROUND_OUTBOX_PROCESSOR_COUNT=1`; integración 28/28.
- Oracle EF10/net10 repitió `PRIORITY3_MULTITENANT_CERTIFICATION=PASS` y las ocho dimensiones `TENANT_CHECK` en `PASS`; runtime background 1, hosted workers 0, Outbox processor 1; 2336/2336 unitarias y 22/22 integraciones.
- [Fase 6 exact-head](https://github.com/solqaryn/Solqaryn/actions/runs/37309393715); aceptación DEV del mismo HEAD también success: [run](https://github.com/solqaryn/Solqaryn/actions/runs/37309393739).

## Resultado

**Punto 19: CERRADO.** No se ejecutó Fase 7 ni se cambió el TargetFramework productivo. Si se incorpora un worker hospedado, deberá permanecer dentro de este gate y aportar aislamiento tenant antes de su adopción.

MAPA_ARQUITECTURA: SIN_CAMBIO.
