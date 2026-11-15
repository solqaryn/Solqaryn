# SOLQARYN V5 — C14-01.M03.02: caso de uso Application para aceptación funcional

**Autor:** PRIMARY DEV 05 Javier, TASK_ID `6adc2e55f47c81919f6374e561687076`; repo `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. Fuentes: STANDARD V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA, AGENTS.md y Skill canónica del HEAD.

## A0 — Implementación y límites

Se agregó `V5FunctionalAcceptanceApplicationService` (.NET 10, capa Application) como caso de uso de solo lectura que compone `V5FunctionalAcceptanceDomainRules` de C14-01.M03.01. El backend llamante es responsable de proporcionar identidad, tenant, rol/permiso efectivo, propiedad persistida y catálogo de caso/acción confiables, nunca tomados sin validar de claims o campos de frontend. La selección `ClientRequestedTenantId` solo puede coincidir con `VerifiedActor.TenantId`, y un lote requiere entre 1 y 64 elementos sin nulos, catálogo no vacío y controles individuales. Se detiene ante la primera denegación sin procesar otros casos.

No crea DI en API, controlador, consulta de BD, transacción comercial, cobro, reembolso, JWT, permiso, migración, worker, webhooks, proveedor, nuevo frontend, rol o promoción de QA/main/PROD. Domain mantiene independencia de Infrastructure. Impacto: dos archivos nuevos C# (servicio y xUnit), documento causal y sección de CHANGELOG. Rollback seguro: `git revert` convencional y controles de regresión, nunca force-push o reescritura BITACORA.

## A1 — Nueve pruebas reales xUnit, sin certificar productos externos

Escenarios de `V5FunctionalAcceptanceApplicationServiceTests`: dos casos permitidos terminan en `EXTERNAL_ACCEPTANCE_EVIDENCE_AND_A5_REQUIRED`; actor nulo/revocado, propiedad desconocida o permiso denegado; tenant B enviado por cliente; catálogo de casos/acciones nulo/vacío; lote sin items, excesivo o más allá del presupuesto; segundo item nulo o de empresa B con conteo acotado; SHA incorrecto y acción no autorizada; evidencia privada, operación de producción o falta de caso negativo; reversión/idempotencia ausentes y evidencia insegura. Los `Result` nunca autorizan acceso, cobros, QA ni aceptación final.

## A2 — Seguridad

El permiso debe ser resuelto desde backend y pertenencia/ownership vigente, no desde `Request` o JWT descontextualizado. Catálogos son de servidor. Cada observación se contrasta contra tenant, SHA, acción, caso, privacidad, rollback, idempotencia y consecuencias productivas usando Domain. No se confunde preflight con pruebas reales HTTP, MySQL, PSP, documentación fiscal o autorización del propietario. Fail-closed ante cualquier ausencia.

## A3 — Prueba e integración causal

Se requiere compilación/test xUnit .NET 10 en GitHub-hosted, además de seguridad multiempresa y regressiones backend. Solo se integra Application -> Domain: sin UI, endpoints ni migraciones, los checks Angular, MySQL, Playwright, browser y PSP no representan pruebas de este changeset y se consignan NO_APLICA. Ninguna ejecución CANCELLED/SKIPPED/IN_PROGRESS se computa PASS exigido.

## A4/A5 — Evidencia y salida

El changeset se publica en `dev` con fast-forward/CAS, encabezado CHANGELOG y viñetas juntas, sin sobreescribir otros autores. Pasar a VALIDANDO antes de auditar A1–A3. Registrar EVENT_ID y readback en BITACORA A:Z, PLAN_MAESTRO y COLA. Solo marcar LISTO_DEV con checks causales del SHA exacto `completed/success` (o descendiente con blobs idénticos demostrados), incluyendo Scope Lock y V5. La revisión A5 es posterior, verdaderamente independiente de otra Task Javier; no promoción QA/main/PROD, eventos o infraestructura.
