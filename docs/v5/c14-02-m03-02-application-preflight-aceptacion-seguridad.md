# SOLQARYN V5 — C14-02.M03.02: preflight de Application para aceptación de seguridad

**Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT (TASK_ID `6af16bb9492c8191b8e8c7d606f11507`). **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Destino:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## Implementación real

`V5SecurityAcceptanceApplicationService` es código de capa Application en .NET 10, con pruebas xUnit propias. Compone la política fail-closed de Domain `V5SecurityAcceptanceDomainRules` (blob `76750b5aa7bc7aa51090169022f34586892add44`) y comprueba consistencia de SHA, fuente de evidencia no sensible y límites de JWT/RBAC, tenant A/B, BOLA, host, descargas, jobs, caché y solicitud de restauración.

Esta implementación **no autentica** las afirmaciones que recibe como entrada. Un snapshot consistente produce `TRUSTED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED`, jamás permiso real: las propiedades `AllowsResourceAccess`, `AllowsRelease` y `AllowsInstitutionalWrite` son siempre false. El adaptador futuro deberá comprobar claims JWT, membresía viva, scope de servidor, recursos persistidos, grants de servicios, CI exact-SHA y evidencia de pruebas negativas antes de emitir decisión real.

## Preflight y dependencias

En PLAN_MAESTRO fila 2067 la tarea era PENDIENTE libre; COLA G/O=SI y DAG F=VALIDADO/G=SI habilitaron una reclamación exclusiva. La fase `C14-02.M02.01–06+A5` y `C14-02.M03.01+A5` se validaron mediante sus gates calculados, sin inventar liberaciones de M03.03. Scope limitado a aplicación pasiva sin DI, controller, middleware, EF, migraciones ni backup.

## Pruebas de código y seguridad

La suite `V5SecurityAcceptanceApplicationServiceTests` contiene pruebas positivas de preflight no autorizante y negativas de SHA, evidencia insegura, JWT/membresía/permiso ausente, tenant externo, client spoofing, host inconsistente, enlace caducado, filtro bypass, job sin grant y restore sin autorización.

**El contrato no certifica pentest, UI, HTTP, MySQL ni vulnerabilidades resueltas.** Solo GitHub Actions reales de backend/tests, seguridad, Scope Lock y procedencia CHANGELOG sobre SHA causales pueden acreditar A1–A4. Si CI required sigue queued/in_progress, conservar VALIDANDO; FAIL/CANCELLED/SKIPPED nunca PASS.

## Análisis de impacto y rollback

Modifica únicamente Application, su suite xUnit, documento en docs/v5 y CHANGELOG_AI.md. Domain existente no se edita; no se registra servicio en DI. No cambia endpoints, rutas Angular, persistencia, JWT/RBAC real, flujos BOLA/IDOR, workers, eventos, Cloudflare, QA/main/PROD ni secretos. Rollback mediante git revert normal en dev. A5 debe revisarlo otra Task Javier diferente de la autora.
