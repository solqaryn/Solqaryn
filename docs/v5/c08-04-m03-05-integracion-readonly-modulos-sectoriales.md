# SOLQARYN V17 — C08-04.M03.05 integración de lectura segura por módulo

**Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Repo:** `solqaryn/Solqaryn@dev`; **PLAN fila 1278**. **Riesgo:** R2 (tenant/RBAC/proveedor). Exclusividad: candado V17 `SQV17_C08_04_M03_05` (2000001278); un solo EN_PROGRESO propio.

## A0 — alcance efectivo y fronteras
Añade `C0804SectorModuleReadOnlyIntegrationBoundary` y el contrato `IC0804SectorModuleReadinessSource` en Application, reutilizando el `C0804SectorModuleAuthorizationFence` de M03.03 y Domain de M03.01. Solamente un `Inspect` autorizado con tenant verificado, RBAC vigente, SHA exacto, módulo ordinal y revisión actual puede alcanzar el puerto de **sondeo read-only**. Mutaciones, solicitudes de dispatch, grants y clientes que elevan privilegios son rechazados antes de hacer I/O.

La salida de sondeo es un código opaco y **nunca** activa módulo, concede acceso, encola worker, llama proveedor mutante, escribe SQL ni certifica proveedor real. El contrato abstrae una futura integración opcional; no existe implementación concreta/DI, background scheduler, colas productivas, credenciales, webhooks, migraciones ni tráfico proveedor live. Una transacción real necesitaría un nuevo scope con autorización actual y CAS durable con bitácora.

## A1–A2 — oráculos ejecutables
`C0804SectorModuleReadOnlyIntegrationBoundaryTests` contiene ocho pruebas xUnit con caso positivo in-process y negativos: intento de activación y dispatch cliente, tenant B, identidad revocada, snapshot sin verificación, revisión obsoleta, respuesta cross-tenant, transporte no read-only, SHA extraño, proveedor que lanza error sin filtrar secreto, cancelación antes de I/O e intención inválida. La suite debe ejecutarse en GitHub-hosted .NET 10 Release, sin aceptar SKIPPED ni contar pruebas authored como aprobadas hasta CI real.

## A3–A5, rollback y límites
A3 = .NET Application real/in-process con xUnit; **MySQL Category=Integration, JWT HTTP, Angular/Playwright y proveedor real NO_APLICA** porque el diff no integra esas superficies. No simular pruebas de red/DB ni atribuir seguridad productiva a los dobles. A4 = éxito de Scope Lock, V5, backend Release, seguridad multiempresa y autorización de SHA causal y readback PLAN/COLA/DAG/BITACORA. A5 debe efectuarlo otra Task Javier distinta.

Rollback: revert normal del commit en dev sin force, conservando eventos históricos. Prohibido QA/main/PROD, infraestructura real, secretos y modificación de cuentas.
