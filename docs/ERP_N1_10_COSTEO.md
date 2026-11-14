# ERP-N1.10 — Costeo empresarial

## Estado

**CERRADO / CERTIFICADO** en `Desarrollo`.

Baseline funcional certificado: `142435e063767e6106bdc8dad2ccb9dd7645f137`.

Este documento consolida el cierre de ERP-N1.10 A–H y es la referencia canónica de la capacidad de costeo empresarial implementada en Solqaryn.

## Objetivo funcional

Solqaryn dispone de una política de costeo única para la empresa activa, con historial temporal inmutable y tres métodos canónicos:

- Promedio ponderado.
- FIFO.
- Costo estándar.

El cambio de política no reescribe historia ni recalcula silenciosamente costos ya materializados. La mutación abre una nueva versión y cierra la vigente dentro de una transacción.

## Autoridad y modelo

- `PoliticaCosteoInventario` es la autoridad temporal de la política por empresa.
- La política vigente se resuelve por empresa activa; ausencia o ambigüedad relevante se trata fail-closed.
- El historial conserva vigencias y motivo del cambio.
- La operación idempotente que solicita el mismo método vigente no genera una versión artificial ni auditoría duplicada.
- Los contratos de dominio para Promedio, FIFO y Estándar permanecen explícitos; FIFO conserva linaje/capas y Estándar conserva la semántica de costo/variación definida por el dominio.

## Persistencia y migración

ERP-N1.10.C materializó la persistencia de la política y contratos asociados mediante cambios aditivos y migración controlada.

Reglas de transición:

1. Instalación nueva puede inicializar una política segura de Promedio cuando existe una única empresa activa válida.
2. Upgrade con cero o múltiples empresas activas no inventa una autoridad: falla cerrado y requiere reconciliación explícita.
3. No se fabrica backfill FIFO ni costo estándar histórico inexistente.
4. La migración, snapshot EF, fresh install y upgrade fueron validados en entornos de Desarrollo/CI; Producción no fue modificada.

## Backend y API

Superficie HTTP:

- `GET /costeo-inventario/politica-vigente`
- `GET /costeo-inventario/politicas`
- `GET /costeo-inventario/metodos`
- `PUT /costeo-inventario/politica-vigente`

El historial admite paginación y filtros temporales UTC. Los rangos inválidos y métodos fuera del catálogo se rechazan antes de consultar o mutar persistencia cuando corresponde.

La mutación de política:

- valida método y motivo;
- bloquea/serializa la autoridad vigente en persistencia;
- cierra la versión anterior;
- crea la nueva versión;
- persiste dentro de transacción;
- registra auditoría estricta para cambios reales.

## RBAC, auditoría y seguridad

El controlador exige autenticación y permisos relacionales:

- Lecturas: `MovimientosInventario / Ver`.
- Cambio de política: `MovimientosInventario / Editar`.

No existe bypass administrativo específico de esta capacidad.

Las mutaciones reales se registran con auditoría estricta, incluyendo entidad, referencia, valores anteriores/nuevos y motivo. La idempotencia evita auditoría duplicada cuando no existe cambio semántico.

La superficie hereda los controles transversales de la plataforma: JWT/autorización, correlation id, manejo uniforme de excepciones, security headers y health/readiness.

## Frontend y UX

ERP-N1.10.E incorporó:

- consulta de política vigente;
- catálogo de métodos desde backend;
- historial paginado/filtrado;
- formulario de cambio de política;
- separación de permisos Ver/Editar;
- validación de motivo;
- bloqueo de cambio redundante al mismo método;
- filtros UTC;
- estados de loading/error/vacío y locators estables para automatización.

La UI no replica una lista autoritativa independiente: consume el catálogo expuesto por backend.

## QA y certificación

Sobre el baseline funcional `142435e063767e6106bdc8dad2ccb9dd7645f137` finalizaron correctamente los gates relevantes:

- Desarrollo — Compilación y pruebas `#32134812652`: **SUCCESS**.
- Fase 8 — Validación completa automatizada `#32134812633`: **SUCCESS**.
- M10 — UI/UX empresarial y accesibilidad `#32134812567`: **SUCCESS**.
- Desarrollo — aceptación funcional integral `#32134812695`: **SUCCESS**.
- M13 — Auditoría integral y certificación final `#32134812757`: **SUCCESS**.
- Recuperación de migración MySQL `#32134812773`: **SUCCESS**.
- M11 backup/restore y controles transversales asociados al mismo HEAD: **SUCCESS**.

Cobertura causal incluye backend Release, unitarias, contrato HTTP/RBAC, frontend lint/build productivo, integración MySQL/migraciones, seguridad HTTP, Playwright y regresión transversal.

## Rollback y recuperación

La estrategia es **forward-only** para datos ya migrados:

- No ejecutar reversión destructiva en Producción.
- Ante defecto de aplicación, detener nuevas mutaciones de política y corregir mediante migración/changeset forward.
- Conservar intacto el historial temporal existente.
- Restaurar desde backup únicamente bajo el runbook institucional de recuperación y con evidencia de integridad, nunca como mecanismo ordinario de cambio de política.
- Un cambio funcional de método se revierte creando una nueva versión explícita de política; no editando ni eliminando historia.

## Definition of Done

ERP-N1.10 queda cerrado porque A–H cubren preflight, dominio, persistencia/migración, backend/API, frontend/UX, RBAC/auditoría/seguridad, QA/CI y documentación/certificación; los gates funcionales y transversales del baseline están verdes y no existe pendiente técnico conocido entre N1.9.F y N1.10.F.

No reabrir ERP-N1.10 salvo regresión causal demostrada. El siguiente trabajo debe seleccionarse por dependencia/elegibilidad desde VAEP v2.