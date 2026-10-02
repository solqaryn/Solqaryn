# Security / RLS evidence root

Estado: `IMPLEMENTATION_REVIEWED`

Este documento es el índice canónico para evidencia de seguridad/aislamiento usada por los cierres `SEC_AUDIT` de las matrices N8. No sustituye los tests ni los runs de CI: los enlaza y limita sus claims.

## Reglas de certificación

1. Un PASS requiere evidencia causal del candidate funcional o equivalencia demostrada; un receipt aislado no basta.
2. Las dimensiones mínimas son `TENANT_SCOPE`, `RBAC`, `APPROVALS` y `AUDITABILITY`.
3. `APPROVALS` significa verificar controles de aprobación/privilegio donde exista un workflow material que los requiera. No se infiere un esquema four-eyes global si el producto no lo implementa. Para un candidate que no toca estados de aprobación, puede declararse `N/A_CAUSAL` sólo si el diff y los contratos demuestran que no introduce ni modifica dicho workflow.
4. `RLS` no se declara globalmente por nombre. Se distingue:
   - `DB_RLS`: aislamiento impuesto por el motor/DB.
   - `APP_RLS`: aislamiento impuesto por filtros/tenant scope/autorización en aplicación.
   - `REPORT`: evidencia documental; nunca reemplaza prueba runtime.
5. No se permite `N/A` por comodidad. Debe quedar causa, alcance y evidencia.
6. Secretos, PII o credenciales reales no se copian a este documento ni al repo.

## Inventario base

La enumeración transversal está en `N8_15_F_SEC_AUDIT_INVENTARIO.md`: 49 artefactos/49 MATRIX_ID, sin duplicados ni reportes faltantes en ese corte. Los breakpoints y clases `REPORT / DB_RLS / APP_RLS` de ese inventario deben conservarse como fuente de cobertura y no como afirmación de runtime global.

## Evidencia causal vigente para N8.18.F

Functional candidate: `c0d453957cea0e8600f2bbbc5a15e8e230a39eea`.

El candidate modifica únicamente `frontend/src/app/shared/navigation/navigation-contract.json`: `/centro-reportes` pasa de `Finanzas|ReportesAdministrativos` a `ReportesAdministrativos` y agrega `requires_admin: true`. No hay cambios de backend, DB, schema, migrations, secrets, logging, tenant model ni approval state machine.

### TENANT_SCOPE

- Exact-candidate `ERP-N1.2 - Certificación Almacenes`, run `35058750263`, success: job `API, jerarquía, RBAC, auditoría, MySQL, Angular y E2E`.
- El workflow ejecuta backend Release/tests, runtime MySQL, frontend build/lint y E2E; incluye jerarquía y permisos en su superficie causal.
- Clasificación para N8.18.F: `PASS` para no-regresión de aislamiento/tenant aplicable al refactor de navegación.

### RBAC

- Exact-candidate `ERP-N1.1 - Certificación Sucursales`, run `35058750310`, success: `API, RBAC, auditoría, MySQL, Angular y E2E`.
- Exact-candidate `ERP-N1.2 - Certificación Almacenes`, run `35058750263`, success.
- El candidate de N8.18.E endurece el contrato de `/centro-reportes` a `ReportesAdministrativos` + `requires_admin:true`; no amplía autoridad.
- Clasificación: `PASS` / fail-closed respecto del delta N8.18.E.

### APPROVALS

- El diff funcional N8.18.E no toca ninguna entidad, endpoint, estado o workflow de aprobación; sólo endurece navegación/autorización de un reporte administrativo.
- No se formula claim de four-eyes/global approvals.
- Clasificación para **el delta N8.18**: `N/A_CAUSAL`, porque no existe modificación material de workflow de aprobación en el candidate. La dimensión queda evaluada, no omitida.

### AUDITABILITY

- Runs `35058750310` y `35058750263` incluyen explícitamente auditoría en sus jobs causales y terminan success sobre el exact functional candidate.
- `Backend Release y pruebas` y `MySQL, backend, Angular y Playwright M9` también terminan success en el mismo candidate.
- Clasificación: `PASS` para no-regresión del candidate.

### SECRETS / CONFIG / DEPENDENCIES

- `Fase 2 - Auditoría de configuración y dependencias`, run `35058750327`, success sobre el exact functional candidate.
- Sus checks exactos incluyen configuración/aislamiento/endurecimiento y auditorías de dependencias productivas .NET/npm.
- El candidate no introduce archivos de config ni secretos.
- Clasificación: `PASS` para N8.18.F.

## Workflow rojo investigado y no causal

`ERP-N0.4 - Certificación RBAC relacional`, run `35058750259`, termina failure únicamente en `Snapshot EF consistente`. Antes de ese paso quedan success: restore/build/tests backend, esquema pre-N0.4, seed legacy, preflight, aplicación exacta N0.4, aislamiento histórico y postcheck/preservación. Como N8.18.E no modifica DB/schema/migrations, este snapshot drift no es una regresión causal de seguridad del candidate N8.18 y no invalida N8.18.F. Debe tratarse como deuda de workflow/schema separada, no como excusa para false PASS ni como bloqueo de trabajo no relacionado.

## Criterio de cierre N8.18.F

N8.18.F puede certificarse únicamente si su REVIEW_FIRST final queda P0=0/P1=0, los runs causales anteriores permanecen success para `c0d453...`, el HEAD posterior a este documento es sólo evidencia/documentación equivalente al candidate funcional y el receipt/readback preservan esa distinción.
