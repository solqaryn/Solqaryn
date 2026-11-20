# C11-03.M05.06 — regresion proporcional de Outbox y observabilidad V22

- PLAN D1687: ejecutar regresion, rendimiento y accesibilidad proporcionales. Fase M05 y E1687 son acumulativas; esta entrega es TEST-ONLY, no certifica relay, mensajeria externa, eventos de dominio o toda M05.
- R2 por Oracle EF/MySQL/tenancy. Se ejecuta un xUnit Category=Integration nuevo sobre servicio MensajeOutboxService, repositorio real, EF Core y MySQL 8.4 efimero, con base aleatoria y borrado en finally; sin Aiven ni datos productivos.
- A1: transaccion que se revierte deja cero eventos en otra conexion; COMMIT persiste un solo evento; 24 replays de misma clave y payload preservan EventoId y unicidad; conflicto de payload divergente y denegacion tenant B, que revierte solo B sin afectar A.
- Rendimiento: cronometraje real de 24 replays y guard de bloqueo patologico 120 s para CI; solo diagnostico, no p95/p99 ni Web Vitals ni SLO real. A11Y NO_APLICA porque el diff no modifica UI ni DOM; ninguna prueba de teclado o Chromium se atribuye a este cambio backend-only.
- A2: aislamiento A/B validado en MySQL fisico con fixture IUsuarioScopeService; autenticacion JWT firmada, RBAC de BD real, endpoint HTTP, colas externas y entrega real NO_APLICA a D1687 test-only; LIMITACION_FASE no equiparar fixture a produccion.
- A3: GitHub-hosted ubuntu, MySQL 8.4 Docker efimero, .NET10 Release xUnit y TRX exactamente 1 total / 1 PASS / 0 FAIL / 0 NOT_EXECUTED, esperando su conclusion real. No falsificar skipped/in_progress.
- A4: Scope Lock exact-SHA, V5 provenance, seguridad multiempresa y checks obligatorios pertinentes completos. Sin CI concluida, queda VALIDANDO con SOURCE_SHA y RUN verificados. A5 independiente por otra Task Javier distinta del autor DEV06.
- Cambios aditivos: test, workflow y este documento, sin runtime, migraciones, scripts productivos, secretos ni QA/main/PROD. CHANGELOG_AI.md (historial >1MB) se conserva intacto para no truncar evidencia; el registro de este incremento queda aqui y en BITACORA.
- Rollback: git revert aditivo del commit cuando sea necesario, preservando historia; no fuerza reescritura.
