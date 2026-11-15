# SOLQARYN V5 FINAL — C15-02.M03.01: reglas de dominio reales de migración segura

**Autor:** PRIMARY DEV 06, JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Rama:** `solqaryn/Solqaryn@dev`. **Tipo:** implementación .NET 10 en Domain + pruebas xUnit, sin acción sobre infraestructura.

## Autoridad y dependencias
- Fuente institucional STANDARD V5 FINAL y PLAN/COLA/DAG. C15-02.M02.01–06 están LISTO, con revisión A5 independiente del M02.06 sobre SHA `d519fe94eb34fb3e33ea46f8f5f0c428967debfc` y diff Markdown exacto. El gate recalculó COLA G/O=SI y DAG F=VALIDADO/G=SI para M03.01.
- El claim es exclusivo: PLAN!H2210 mediante CAS `PENDIENTE` a `EN_PROGRESO`, I2210=Task DEV06 y readback. Se preserva el resto de PLAN, BITACORA y seis Tasks Javier.

## Implementación
- `V5ArchitectureMigrationDomainRules` es un preflight de **dominio puro**: requiere SHA exacto, tenant verificado en servidor, membresía vigente, alcance dev explícito, inventario trusted de origen y destino, catálogo de proveedor autorizado, no mutaciones destructivas, ausencia de incidentes críticos, checksum/restore sandbox/rollback.
- Devuelve bloqueos deterministas para QA/PROD, stale SHA, inventario sin owner, tenant B, proveedor nuevo no aprobado, cambios live DNS/secretos, SQL destructivo, hallazgo crítico y falta de restore.
- Un resultado `READY_FOR_TRUSTED_ARCHITECTURE_REVIEW` **no certifica** que realmente se ejecutó backup, restore, CI MySQL, inventario ni auditoría; el adaptador Application/API deberá verificar procedencia y ejecutar pruebas autorizadas antes de cualquier operación.
- Se agregan pruebas xUnit positivas/negativas en `backend/tests/Solqaryn.Tests/Domain/V5ArchitectureMigrationDomainRulesTests.cs`. No se conectan endpoints ni se crean migraciones, DNS, webhooks, workers o cambios de proveedores.

## A0–A5, causalidad y rollback
- A0: Domain aislado, sin Infrastructure/MySQL/HTTP ni cambios de secreto. A1: tests de variantes válidas y denegaciones para SHA, proveedor, tenant y rollback. A2: ataques A/B, scope falso, host/R2 no aprobado y permisos faltantes permanecen denegados. A3: GitHub-hosted .NET 10 backend/security CI exact-SHA; ninguna suite documentada se supone ejecutada. A4: commit único fast-forward/CAS con Domain + xUnit + este documento + CHANGELOG, readback, Scope Lock/V5 exact-SHA y checks dirigidos.
- A5: requiere otra Task Javier distinta de DEV06; no se autoaprueba. Un veredicto puro no autoriza infraestructura ni certifica ausencia de P0/P1.
- Rollback normal `git revert` sobre dev, sin reescritura histórica ni force-push; pruebas y controles deben ejecutarse de nuevo para un SHA de rollback. No QA/main/PROD ni servicios pagos.
