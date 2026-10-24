# P0-012.B — Certificación de autoridad documental comercial (entregable para revisión)

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH_BASE=dev
MICROTAREA=P0-012.B
DEPENDENCIA=P0-012.A
DOCUMENTO_RECTOR_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
SEGUIMIENTO_SHEET_ID=1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU

## Objeto y alcance de la certificación

Esta entrega aborda la microtarea oficial «CERTIFICAR: Resolver conflicto de autoridad documental». **No declara LISTO ni supone aprobación independiente**: la transición requiere revisión ajena, CI del SHA final, merge protegido en `dev` y readback del Plan Maestro, COLA y Bitácora. La fuente ejecutable sigue siendo exclusivamente el Plan Comercial Inicial Unificado V3; el Maestro previo y las diez Tasks retiradas no recuperan autoridad.

## Evidencias observadas antes del cambio

- `dev` base: `e1d7afd82ea814cc70121b866b0ce86843eb8d10` (PR #3735 fusionado). El PR #3734 ya dejó `P0-012.A` integrado con merge `17ca160ae5f2d1e20ef27b1820e670fbfb1099d8`.
- Documento rector nativo leído desde la conexión Javier: [Plan Comercial Inicial Unificado V3](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit).
- Sheet institucional leído desde la misma conexión: [STANDARD DE AUTOMATIZACIONES EN SOLQARYN](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `PLAN_MAESTRO!A24:M25` y `COLA!A24:N25`. `P0-012.A=LISTO`, `P0-012.B=PENDIENTE`, `DEPENDENCIAS_OK=SI`. Los valores no se presentan como prueba de cierre de B.
- Gobierno vivo: `docs/PROJECT_SCOPE_LOCK.md`, `AGENTS.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md`, `docs/AUTOMATION_3X3_OPERATING_CONTRACT.md`. Flags declarados: `COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY`, `LEGACY_MASTER_EXECUTABLE=FALSE`, `AUTOMATION_RUNTIME=ACTIVE_DEV`, `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, `VAEP_DISPATCH_AUTHORIZED=FALSE`, `JULES_WORKERS_AUTHORIZED=0`.
- [Scope Lock CI del HEAD base](https://github.com/solqaryn/Solqaryn/actions/runs/40059039668/job/120768326824): `verify-solqaryn-project-scope` SUCCESS; seguridad multiempresa y backend también SUCCESS para ese SHA. Esta evidencia **no** sustituye CI del PR nuevo.
- `docs/VAEP_AUTHORITY.md` está prohibido como plano de control por `scripts/verify-project-scope.mjs`. Las referencias históricas de autoridad VAEP y Word previas se conservan únicamente como trazabilidad; no son entradas de ejecución.

## Controles de regresión añadidos

`scripts/validate-automation-authority.mjs` ahora inspecciona el texto **operativo**, previo al primer encabezado explícito de archivo histórico, de `AGENTS.md`, `PROJECT_SCOPE_LOCK.md`, `AUTOMATION_STATUS.md` y `AUTOMATION_AUTHORITY.md`. Rechaza: (1) IDs de microtareas `SQ-000` a `SQ-999` en autoridad operativa, y (2) enlaces Google Docs/Sheets activos con ID ajeno al documento rector y al seguimiento institucional. Conservar referencias en apartados de archivo histórico no las ejecuta.

Se agregan tres mutaciones negativas causales: introducir `SQ-123` ejecutable en AGENTS, apuntar al Word fuente previo desde Scope Lock y sustituir el Sheet institucional por otro ID en Status. Las tres deben fallar en `node scripts/validate-automation-authority.mjs --self-test`. El check obligatorio `SOLQARYN Project Scope Lock` ya llama a ese comando en CI.

## Dependencias y criterios pendientes de cierre

- Ejecutar/consultar CI sobre el **SHA definitivo del PR P0-012.B** y confirmar los tres checks obligatorios; documentar fallos reales si surgen.
- Revisar independientemente el diff y validar que no queden referencias ejecutivas al Maestro histórico en las superficies relevantes. El análisis está limitado a los encabezados normativos de cuatro archivos; no equivale a un escaneo universal de datos privados o todos los repositorios.
- Verificar que no se ha reinstalado `docs/VAEP_AUTHORITY.md` ni un motor VAEP/Jules; preservar QA/main/PROD y secretos.
- Solo después de revisión, merge y readback: registrar `PLAN_MAESTRO!H25:M25`, confirmar `COLA!E25/G25/N25` por fórmula y crear evento idempotente en BITACORA con hora `America/Tegucigalpa` y UTC original en Z.

## Impacto y reversión

Cambio de validación normativa y documento de evidencia; **sin modificaciones** a Angular, API, Dominio, Application, Infrastructure, MySQL, JWT/RBAC/tenancy, endpoints, integraciones, datos o despliegues. Reversión: revertir el commit de controles mediante PR protegido si produce falsos positivos, sin reactivar fuentes históricas. La fuente comercial y los IDs institucionales no se alteran.
