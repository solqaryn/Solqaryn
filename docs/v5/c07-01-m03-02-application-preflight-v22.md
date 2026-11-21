# SOLQARYN V22 — C07-01.M03.02, caso de uso de Application para catálogo universal

**Criterio propio:** PLAN_MAESTRO!D1023 «Implementar caso de uso, servicio o aplicación con límites claros». **Autor:** PRIMARY DEV 04 JAVIER Task \`6aeacb7820ac8191b7d768a6f33dfa09\`. **Riesgo R2**, rama dev, PROJECT_SCOPE_LOCK=STRICT. No confundir PLAN E (meta acumulativa M03) con la entrega atómica de esta .02.

## A0 — Implementación real y alcance
Nuevo \`backend/src/Application/AutomationV5/C0701CatalogApplicationService.cs\`, en Application, separado de \`C0701CatalogDomainRules\` (M03.01 aprobada) y de Infrastructure/API: servicio asíncrono de preflight que obtiene (por dos interfaces explícitas) actor autenticado/tenant/rol y recurso leído **por tenant servidor**, verifica igualdad de tenant frente a hints de cliente, rechazo de claims/grants suministrados por cliente, frescura de JWT/RBAC/membresía, ownership, versión CAS y rechazo de controladores con escritura directa. Invoca las reglas Domain reales para SKU/tipo/precio/categoría/variante/branch/RBAC/tenant, siempre sin devolver información privada ni permitir escritura. Un resultado con precondiciones válidas **solo exige revalidación autorizada y transacción atómica futura**, no es autorización.

Interfaces \`IC0701VerifiedCatalogActorReader\` y \`IC0701VerifiedCatalogResourceReader\` son puertos **sin implementaciones live**; no se registran en DI y no crean endpoint ni repositorio. No se modifica SQL/EF/MySQL, controller, front Angular, API, esquemas, secretos ni servicios externos. Evita amplificar riesgo R3 y desacopla la arquitectura Domain→Application→Infrastructure.

## A1/A2 — Pruebas positivas, negativas, fronteras y errores
Nuevo \`backend/tests/Solqaryn.Tests/Application/C0701M0302CatalogApplicationServiceTests.cs\` con xUnit que comprueba: candidato válido devuelve únicamente revisión futura, sin grant; entradas nulas/alteradas no llaman adaptadores; supuestos permisos o escritura client-side se niegan; JWT sin firma, membresía, RBAC o rol revocado no consultan recurso; cliente tenant B no cambia A; lecturas de resource B o sin ownership denegadas sin filtrar datos; negativas del Domain persisten; fallos de autoridad/repository se encapsulan fail-closed; cancelación se propaga; versiones obsoletas y actor inexistente se rechazan. Los lectores falsos de pruebas solo prueban el **servicio en aislamiento**, NO prueban token JWT real, HTTP ni MySQL.

## A3/A4, límites y secuencia
**A3 MySQL/HTTP/Chromium NO_APLICA a esta microtarea de capa Application sin adaptador live, sin EF/DB y sin UI**, con LIMITACIÓN explícita: la autorización efectiva necesita integración JWT/RBAC A/B real en futura M03.03/04, persistencia MySQL efímera y tests Playwright cuando se implemente UI. No certificar DB ni seguridad end-to-end por stubs.

A0–A4 solo se completan con diff exacto, xUnit .NET10 GitHub-hosted completada SUCCESS, Scope Lock exact-SHA SUCCESS, V5 procedencia SUCCESS y readback PLAN/COLA/BITACORA. CI QUEUED/IN_PROGRESS/SKIPPED/CANCELLED/FAILED requerida no es PASS. Antes de esas pruebas mantener VALIDANDO con SHA/RUN causal.

## A5, reversión y restricciones
A5 independiente de autor DEV04 por otra Task Javier, manteniendo owner histórico, candado V17 y bitácora idempotente. Reversión por git revert normal de este único commit, sin borrar otras entregas ni alterar history. No precio comercial aprobado, impuestos/PSP, autorizaciones de producción, QA/main/PROD, force-push, migraciones o secretos.
