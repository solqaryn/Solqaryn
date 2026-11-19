# C03-04.M06.01 — Auditoria causal: pruebas reales vs. simulaciones (V22)

PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
MICROTAREA_ID=C03-04.M06.01
TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09
RISK=R0_DOCUMENTAL_READ_ONLY
SOURCE=PLAN_MAESTRO!D572;V15/V17/V22
A5=REVISION_INDEPENDIENTE_JAVIER

## Objetivo atomico

Revisar que las pruebas de C03-04.M05.01 a M05.06 correspondan al alcance real y distinguir explicitamente CI real de bases de datos o API simuladas. Esta auditoria lee la evidencia existente; NO ejecuta migraciones, restores, operaciones productivas ni libera controles de las fases M06 restantes.

## Matriz de procedencia y cobertura

| ID | SHA fuente de evidencia | Comprobacion existente | Clasificacion y limitacion |
| --- | --- | --- | --- |
| C03-04.M05.01 | a2aa4ff18010d88330d326061211d30fb02ccbd3 | 27/27 xUnit del contrato de dominio, positivos/negativos, Scope Lock y seguridad | **Real .NET en GitHub**, pero datos y autoridad en memoria; NO JWT HTTP/MySQL real ni restauracion |
| C03-04.M05.02 | c33d3b80bc38e009790c3071b6f82983f9de5b77 | 22/22 pruebas de contrato API/DTO y denegaciones segun PLAN/CI | **Prueba de contrato real**, sin garantizar servidor HTTP autenticado ni integracion productiva |
| C03-04.M05.03 | e8232053e1680303abd2a6fa7a6c5a4ec3774b19 | 3/3 integraciones aisladas MySQL 8.4, tenantes A/B, rechazo host/port, rollback y revocacion | **MySQL efimero realmente ejecutado**; no sustituye servicio JWT real ni backup/restore |
| C03-04.M05.04 | 6b523ce077f534e6d6326165c82ec812f247a929 | 17/17 xUnit contract/acceptance, positivos/negativos y control de estado | **xUnit real sobre modelo/fixtures**; sin certificacion HTTP/JWT/restore vivo |
| C03-04.M05.05 | 204a4f972e3d6935ec553f82c36777271002b0fe | 35/35 xUnit adversariales tenant, permisos, revocacion y restore denegado | **Asserts reales, autoridad de fixture**; no inferir persistencia o RBAC integrado |
| C03-04.M05.06 | 4b4163c55b002ffe02c05f60240fa059b834a492 | 10/10 xUnit Release, TRX, servidor GitHub-hosted MySQL 8.4.13, aislamiento A/B, revocacion, rollback, concurrent reads, 20k reglas sin efectos | **Integracion SQL real + dominio puro**: permite solo preflight fail-closed, NO ejecucion de restore, mutacion de incidente o JWT HTTP firmado |

Los conteos M05.01–05 se cotejaron contra PLAN_MAESTRO K/L y registros CI existentes, sin nueva ejecucion retroactiva de aquellos SHAs. M05.06 se verifico adicionalmente contra el log original del job 135845972811, run 44604929709 (TRX 10 PASS, 0 FAIL/skip), y contra los blobs de cuatro archivos exactos preservados en dev. Los resultados de MySQL 8.4.13 pertenecen al servicio efimero de GitHub Actions; no son pruebas de Aiven ni produccion.

## Gaps que NO pueden calificarse PASS

1. No hay certificacion integrada de JWT firmado, HTTP autorizado, revocacion efectiva de sesiones o grants productivos de este flujo. Los objetos `ServerAuthority` de las pruebas de dominio son fuentes controladas.
2. Un preflight `READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT` **no autoriza mutaciones ni restauracion**: las propiedades `AuthorizesIncidentMutation` y `AuthorizesBackupRestore` son siempre false. La ejecucion real requiere comprobaciones de seguridad por servicio confiable, CAS, idempotencia y auditoria.
3. El rollback SQL ensayado sobre una tabla fixture no equivale a restaurar un backup real: checksum de backup, RTO/RPO, restauracion de archivos, aprobaciones operativas y trazas de retorno siguen SIN CERTIFICAR.
4. La meta acumulativa de PLAN E, runbooks y autorizaciones para C10/C12 y R3 no se libera por esta revision individual. DAG_TECNICO/COLA deben conservar exclusivamente sus formulas, sin forzar SI.

## Evidencia por tecnologia / A0–A5

- A0: PLAN D572 pide auditar pruebas vs mocks; el diff de esta tarea es **solo markdown/changelog** y no modifica APIs, UI, DB, permisos, jobs ni autenticacion.
- A1: matriz de seis unidades con SHAs distintos, resultados y limitaciones explicitas; falsos positivos evitados al separar fuente sintetica, MySQL efimero, CI .NET y restaure real.
- A2: NO_APLICA a cambio ejecutable; no hay efectos runtime, JWT, RBAC ni tenant introducidos. Las brechas de la fase permanecen abiertas.
- A3: NO_APLICA a nuevos tests/integraciones para **este diff documental**. La integracion efimera previa de M05.03 y M05.06 se cita sin atribuir ejecucion nueva.
- A4: requerir `verify-solqaryn-project-scope` y check V5 completed SUCCESS del SHA nuevo, diff documental y readback PLAN/COLA/BITACORA antes de LISTO; SKIPPED/IN_PROGRESS no se atribuyen PASS.
- A5: revisor independiente de Task Javier diferente del autor DEV04; no aprobar propio trabajo.

Rollback: `git revert` del commit dev aprobado, preservando changelog previo, BITACORA y los estados institucionales. No alterar qa/main/PROD, servicios de pago ni secretos.
