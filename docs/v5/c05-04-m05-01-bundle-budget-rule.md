# C05-04.M05.01 - regla reproducible de presupuesto y tamano inicial

Scope: SOLQARYN, exclusivamente dev, PLAN_MAESTRO fila 854. Autor: PRIMARY DEV 04, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Riesgo R2 porque agrega workflow CI, no modifica codigo de ejecucion ERP.

## A0 alcance, dependencia y reversion

Se ejercita el analizador real existente frontend/scripts/performance-bundle-baseline.mjs, sin reemplazarlo ni introducir segunda autoridad. Se agrega test con fixtures reales, reportes JSON y aserciones falsables, mas workflow SHA-aislado GitHub-hosted. Regla M05 mide bytes iniciales del build y excluye chunks lazy. No agrega API, runtime Angular, operaciones servidor, secretos, migraciones ni permisos. Rollback con git revert normal, no force-push.

## A1 pruebas que pueden fallar

- Positivo: 300 bytes main mas 173 bytes lazy; initial incluye main y CSS y cuenta gzip y brotli.
- Frontera: 1 MiB - 1, exactamente 1 MiB, 2 MiB, 2 MiB + 1; preservar umbrales informativos existentes sin fingir gate PASS automatico.
- Error: si falta index productivo, proceso retorna error y no genera informe falsamente.
- Negativo: si index no referencia assets, inicial=0 aunque haya archivos en el build.

## A2/A3 limites y CI

No se alteran JWT, RBAC, tenant A/B, EF/MySQL ni PII: NO_APLICA autorizaciones o proveedor DB. Integracion pertinente real es Node ejecutando analizador sobre archivos temporales y build Angular productivo real en GitHub-hosted, seguido de reporte JSON. Workflow con contents:read, concurrency por SHA y cancel-in-progress:false. Core Web Vitals LCP/CLS de usuarios reales son NO_MEDIDO, siempre. Umbral informativo de bundle no constituye automaticamente aprobacion de rendimiento. Sin navegador nuevo porque no cambia UI.

## A4/A5

Exigir check dirigido, Angular build, Scope Lock/V5 y controles pertinentes completed/success SHA exacto o equivalencia auditada; QUEUED/IN_PROGRESS/FAILED/CANCELLED/SKIPPED nunca PASS requerido. Readback PLAN/COLA/DAG/BITACORA. A5 solamente Task Javier distinta del autor. Sin cambios QA, main, PROD ni RRULE.
