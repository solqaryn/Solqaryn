# SOLQARYN V5 — C15-04.M03.04: adaptador aislado de observaciones UX

**Autor:** PRIMARY DEV 01 JAVIER `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-15. Rama `dev`, PROJECT_SCOPE_LOCK=STRICT. Alcance de código real: .NET 10 Infrastructure, xUnit, documento y CHANGELOG.

## Contrato de implementación

Se añade `V5UxAuditIsolatedSnapshotAdapter` para capturar de forma defensiva un snapshot de `V5UxAuditApplicationService.Request`: conjuntos autorizados de superficies y criterios, y la lista de observaciones. El adaptador **no** confía en referencias mutables del llamador: copia las colecciones y rechaza cambios de SHA o tenant observado antes de delegar en `V5UxAuditAuthorizedApplicationService` (M03.03), que valida autoridad estructural y utiliza el servicio M03.02 y reglas M03.01. El resultado sólo sirve para prevalidación **read-only**; ni la identidad ni la evidencia se autentican aquí de forma real.

Una lectura coherente no certifica mediciones externas: devuelve `EXTERNAL_EVIDENCE_AND_A5_REQUIRED`; permisos, JWT, membresía, métricas WCAG/Playwright y autoridad de tenant deben provenir de backend autenticado y ensayos pertinentes futuros. **Sin** escritura institucional, persistencia, EF/MySQL, base de datos, secretos, eventos, despliegue, QA/main/PROD o cambios de producción. El trabajo no altera balances, clientes ni grants.

## Impacto, pruebas y salida A0–A5

- Predecessores `C15-04.M03.01–03` LISTO+A5 independientes; PLAN H=PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI antes de reclamación CAS exclusiva. Los gates de M03.05/06 deben recalcularse; nunca forzarlos.
- **A0:** cuatro archivos: adaptador Infrastructure, xUnit, documento y nueva sección autocontenida CHANGELOG. Sin modificar código de autorización existente. **A1:** siete tests xUnit de prevalidación sin permisos, SHA obsoleto, tenant A/B, catálogo privado, presupuesto violado y mutación de colecciones. **A2:** DENY por defecto JWT/RBAC/tenant, cero grants. **A3:** Domain → Application → Infrastructure (sin circularidad), ninguna escritura a proveedor o Sheets. **A4:** Git fast-forward CAS, Scope Lock y check provenance V5 SHA exacto, backend/security GitHub-hosted completed/success, readbacks PLAN/COLA/BITACORA.
- **A5:** pendiente de otra Task Javier distinta del autor; no autoriza QA/main/PROD ni sustituye WCAG o pruebas reales de navegador. Rollback por `git revert` normal con historia institucional aditiva, sin reescritura.

Las mediciones son sintéticas en tests y no son evidencia de rendimiento de clientes reales.
