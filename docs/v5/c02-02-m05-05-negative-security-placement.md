# SOLQARYN V19 — C02-02.M05.05: seguridad negativa shared/dedicated placement

**Ámbito:** SOLQARYN `dev`, riesgo V15 **R2**, TASK_ID autor `6adc2e4bd8a48191ac7d8641ed95a1a3`; claim V17 exclusivo sobre PLAN_MAESTRO fila 354. Prerrequisitos verificados al reclamar: COLA G/O=SI, DAG F=VALIDADO/G=SI, PLAN H=PENDIENTE/I vacío. Esta entrega no otorga permiso QA/main/PROD.

## A0 — cambio y límites

Repara el caso de uso Application `C0202PlacementApplicationGate`: excepciones de un lector backend de evidencias confiables no deben escapar con diagnóstico del proveedor ni transformarse en autorización. El lector puede fallar por I/O/estado; en tal caso la respuesta es `TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED`, sin contenido privado. Una cancelación realmente solicitada por el cliente se sigue propagando. No se modifican DTO HTTP, API controllers, Domain, DI, roles, tokens, configuración, datos, esquema, EF, router, migraciones, proveedores o secretos. Consumidores actuales: suites Application de placement M03 y la nueva matriz negativa M05.05.

## A1 y A2 — matriz ejecutable

La suite `C0202M0505PlacementNegativeSecurityTests` prueba fallos del proveedor con información simulada, cancelación no solicitada, cancelación real, cinco inconsistencias de tenant/SHA/ownership, cinco revocaciones o pérdida de rol, cuatro solicitudes de abuso DSN/fallback/cutover/DDL y alias objetivo suplantado, más un control positivo que **no** autoriza conectar, migrar, desplegar ni cortar servicio. Se exigen asserts que fallan si un error permite acceso o si la excepción del lector escapa sin sanitización. Los fixtures son sintéticos; no contienen credenciales reales.

## A3 y A4 — evidencia y cierre

Diferencia exclusivamente Application + xUnit + documento + CHANGELOG; SQL/MySQL 8.4/EF/HTTP/Chromium/servicios productivos **NO_APLICA al diff**, sin sustituir las integraciones reales de tareas posteriores. GitHub-hosted .NET 10 Release, backend, seguridad multiempresa, autorización, Scope Lock y V5/procedencia deben quedar completed/success sobre SOURCE_SHA exacto (o equivalencia de blobs integral probada). Si CI está QUEUED/IN_PROGRESS, conservar VALIDANDO, SHA, RUN_ID y owner; nunca asumir PASS. A5 debe ser revisión independiente por otra Task Javier del allowlist, posterior a A0–A4. Rollback normal con `git revert`, nunca force-push ni datos productivos.
