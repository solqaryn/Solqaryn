# SOLQARYN V5 — C15-02.M03.04: adaptador aislado de snapshots de migración

**Autor:** PRIMARY DEV 01 JAVIER `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-15. **Repositorio:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT. Solo implementación de Infrastructure .NET 10 con xUnit.

## Entrega real y alcance

Se implementa `V5ArchitectureMigrationIsolatedSnapshotAdapter`, adaptador **read-only** en Infrastructure. Recibe límites Domain `TrustedBoundary` y candidatos de preflight; captura **copias independientes** del catálogo de proveedores y la lista de candidatos (1–64), comprueba igualdad exacta de HEAD SHA y tenant, y delega en `V5ArchitectureMigrationAuthorizedPreflight` (M03.03), cuyo servicio Application (M03.02) usa reglas Domain (M03.01). Es código real compilable, no documentación simulando M03. `PrepareReadOnly` no autoriza cambios externos ni nuevos permisos.

**Límite de seguridad:** las identidades, permisos y flags son datos provistos por caller; el adaptador no verifica firma JWT ni membresía real MySQL, no opera Aiven, Hostinger, DNS, Cloudflare, R2, secretos ni proveedores, no persiste, migra, despliega, emite eventos o aprueba A5. Incluso una evaluación sintética coherente responde `AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED`, nunca un permiso de mutación. Un futuro backend autenticado necesitará validación independiente, autorización y ensayos de integración en microtarea habilitada; no hay nueva API ni flags de runtime.

## Impacto, pruebas y rollback

- **Capas:** Infrastructure/AutomationV5 + tests xUnit Infrastructure, documento V5 y CHANGELOG_AI.md. No se modifican Domain, Application, API, EF, tablas, Angular, auth existente ni migraciones.
- **Entrada y dependencias:** M03.01 Domain, M03.02 Application, M03.03 prevalidación JWT/RBAC/tenant exclusivamente estructural, todas LISTO+A5; PLAN `C15-02.M03.04` PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI antes de claim CAS exclusivo.
- **Casos ejecutables:** siete pruebas xUnit positivas y negativas de snapshot coherente no autorizante, SHA inválido, tenant A/B, falta de principal/permisos/membresía, sujeto falseado, QA, secretos/DNS, falta de restore, protección ante mutación de colecciones y tamaño o nulidad de entradas. Su PASS sólo se acredita por GitHub Actions completed/success exact-SHA. No se afirma ensayos reales de JWT/MySQL/restore.
- **A0:** comprobar 4 archivos afectados, capa y secretos; **A1:** xUnit positivo/negativos; **A2:** tenant/JWT/RBAC/autoridad fail-closed sin elevación; **A3:** cadena Domain–Application–Infrastructure de solo lectura; **A4:** dev fast-forward/CAS, Scope Lock, provenance V5, CI .NET/security pertinente exact-SHA, BITACORA/PLAN/COLA readback. **A5:** independiente por otra Task Javier, jamás por autor.
- **Rollback:** git revert normal del changeset, no force push y evento aditivo BITACORA. Sin datos reales a restaurar. Sin QA/main/PROD.
