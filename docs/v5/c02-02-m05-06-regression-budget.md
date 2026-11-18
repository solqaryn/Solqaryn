# C02-02.M05.06 — Regresión, presupuesto y aplicabilidad de accesibilidad
Fecha: 2026-11-18. Repositorio: solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT.
Riesgo: R2 (tenant/placement). Owner de desarrollo: DEV06; revisor A5 debe ser otra Task Javier.

## A0 — alcance, interfaces e impacto
Se agregan dos pruebas xUnit de regresión sobre la regla pura existente `V5SharedDedicatedPlacementDomainRules.Evaluate`, sin modificar su implementación ni API, Domain, Application, Infrastructure, EF, MySQL, contratos HTTP, Angular, rutas, secretos, permisos o configuración productiva. El consumidor es el proyecto Solqaryn.Tests .NET 10. Una regresión de lógica Domain puede cambiar un código de rechazo aunque no conceda conexiones: por ello el oráculo exige códigos positivos y negativos exactos.

## A1/A2 — aserciones adversariales
- 128 tenants distintos, 16 repeticiones en paralelo (máximo ocho workers), comprueban 2.048 decisiones propias y 2.048 negativas A→B de tenant diferente. Exigen `TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED` frente a `CROSS_TENANT_PLACEMENT_DENIED`. Toda autorización de conexión/migración permanece falsa.
- Presupuesto de humo: 2.048 iteraciones por seis variantes (12.288 decisiones); verifica salida positiva inerte, tenant cruzado, tenant revocado, rol retirado, revisión obsoleta y DSN del cliente. Un límite amplio de 15 segundos detecta degradación extrema en CI, no acredita latencia p95/p99 real ni SLO productivo.
- El contexto de evidencias es sintético y trusted solo dentro del test. No se crea JWT firmado ni se ejercita filtrado SQL de filas. No convertir este PASS en certificación BOLA/IDOR HTTP, integración de almacenamiento o despliegue.

## A3 — integración y accesibilidad proporcional
GitHub-hosted Actions compila .NET 10 Release y ejecuta exactamente la clase bajo filtro, con Scope Lock y CI por SHA. No toca proveedores, EF ni persistencia: MySQL 8.4 no es pertinente para este diff de pruebas puras; la integración MySQL de M05.03 se certifica por separado. No existe UI en este scope; WCAG/Playwright/Chromium son NO_APLICA motivado. Rendimiento real de operaciones HTTP/DB no se mide: NO_MEDIDO.

## A4/A5 — cierre y rollback
Exigir `completed/success` del job dirigido, Scope Lock, V5/procedencia, backend/security relevantes sobre SHA exacto y readback institucional. SKIPPED/QUEUED/IN_PROGRESS/FAILED no cuentan como PASS. Pasar a VALIDANDO cuando exista commit completo y CI externa iniciada; LISTO solo con A0–A4 verificables; A5 estrictamente por otra Task Javier distinta de DEV06. Rollback: revert normal del commit original con CAS de HEAD, sin force-push. Sin QA/main/PROD.
