# SOLQARYN V5 FINAL — C02-02.M02.03: persistencia, índices y migración conceptual de placement

**Responsable:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15.

**Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); PLAN/COLA/DAG institucional por ID; Skill canónica, `AGENTS.md`; M01.06 de C02-02, y contratos M02.01/M02.02 ya publicados. Esta entrega define un **contrato de diseño**, sin modificación de tablas, objetos EF, migraciones reales, conexiones, DSN ni bases de datos.

## 1. Fronteras presentes y futuras

La línea base actual usa ASP.NET Core net10.0, EF Core 10.0.12 y `MySql.EntityFrameworkCore` 10.0.9, con `AppDbContext`, `StorageTenantContext`, membresía `UsuarioEmpresa` y controles del backend. No existe autorización aquí para crear un DbContext dedicado, un selector de servidor activo, sincronización de filas, doble escritura, backup o cutover. `MySQL 8.4` identifica la tecnología objetivo de pruebas futuras, nunca una migración ejecutada.

El futuro almacenamiento shared/dedicated preservará al `EmpresaId` como frontera de propiedad, pero **no** aceptará `EmpresaId`, host, DSN o target enviados por navegador para decidir conexión. Una tabla conceptual de mapping jamás equivale a configuración de producción.

## 2. Modelo de persistencia propuesto, sin crear DDL

| Familia | Clave propuesta | FK, ownership y política |
| --- | --- | --- |
| PlacementPolicy | PolicyId | Política del sistema/tenant y permisos administrativos verificados; versión de reglas |
| PlacementAssignment | AssignmentId y EmpresaId | 0..1 asignación activa por empresa, `Revision` monotónica y estado |
| StorageBinding | BindingId | Alias opaco a configuración server-only; nunca almacenar DSN de cliente |
| PlacementRevision | AssignmentId + Version | Historial append-only con actor, SHA y cambios redactados |
| MigrationIntent | IntentId, EmpresaId | Referencia a assignment origen/destino y clave idempotente |
| MigrationCheckpoint | IntentId + Sequence | Checkpoint inmutable, status, hash de corrida, sin filas sensibles |
| DataConsistencyProof | ProofId + IntentId | Conteos, FK, digest agregados y fecha por tenant |
| CutoverGuard | EmpresaId + FenceEpoch | Bloqueo/versionado de cutover; un solo ganador |
| RestoreEvidence | SnapshotRef + ProofId | Restore efímero con suma de integridad, permisos y retención |
| RollbackDecision | DecisionId + IntentId | Aprobación, target previo, fecha y prueba posterior |
| PlacementAuditEvent | EventId + IdempotencyKey | Resultado redactado, correlación y autoridad backend |

El conjunto es **hipotético** y será sujeto a decisión arquitectónica, protección de datos y revisión futura de schema. No duplicar entidades actuales ni crear relaciones que rompan FKs existentes.

## 3. Índices y unicidad tentativos IDX01–IDX10

| Índice futuro | Propósito/negativa |
| --- | --- |
| IDX01 `(EmpresaId, ActiveFlag)` | Restringir a una asignación activa por empresa; sin combinación que exponga otro tenant |
| IDX02 `(AssignmentId, Revision)` | Revisión única monotónica, sin overwrite histórico |
| IDX03 `(BindingId)` | Alias opaco estable, no URL ni DSN |
| IDX04 `(IntentId, EmpresaId)` | Consulta de intent obligatoriamente acotada por tenant |
| IDX05 `(IntentId, Sequence)` | Idempotencia de checkpoint y orden de reanudación |
| IDX06 `(EmpresaId, IdempotencyKey)` | Evitar intentos duplicados de una empresa |
| IDX07 `(EmpresaId, FenceEpoch)` | Excluir cutovers obsoletos |
| IDX08 `(IntentId, ProofId)` | Vincular pruebas al intento correcto |
| IDX09 `(EmpresaId, RecordedAtUtc)` | Paginación auditada sin lectura cross-tenant |
| IDX10 `(SnapshotRef, VerifiedAtUtc)` | Evidencia de restore sin rutas de backup |

No se afirma que MySQL soporte un índice parcial idéntico al diseño de otras bases: la unicidad de asignación activa deberá validarse con una estrategia compatible con MySQL 8.4 y ensayos concurrenciales antes de adoptar DDL.

## 4. Plan de migración FUTURO, no ejecutado

| Etapa | Precondición y evidencia exigida | Aborto/rollback |
| --- | --- | --- |
| MIG01 Descubrimiento | Estado actual de esquema, FKs, nulos y ownership por empresa | Si hay huérfanos, no aplicar |
| MIG02 Diseño aditivo | Migration EF revisada, script y reversión acordados en repositorio | Sin DDL en bases reales |
| MIG03 Simulación | MySQL 8.4 efímero con migraciones, índices y seeds A/B | Revertir contenedor efímero |
| MIG04 Snapshot | Backup de datos de prueba, hash, restore verificado | Sin restore correcto, NO_GO |
| MIG05 Dual-read plan | Estrategia de no cruzar tenants y evitar fallback con destino fallido | Mantener ruta actual |
| MIG06 Reconciliación | Conteos, FKs, checksum, jobs idempotentes, integridad por empresa | Abortar ante divergencia |
| MIG07 Cutover plan | Fence/version esperado, permisos temporales y ventana aprobada | Revertir sólo con target anterior probado |
| MIG08 Auditoría/handoff | Eventos saneados, evidencia exact-SHA, A5 independiente y readbacks | Sin QA/PROD sin orden separada |

Ninguna etapa MIG01–MIG08 se efectuó contra Aiven, Render, Hostinger, Cloudflare o datos privados en esta microtarea. No hay migración EF nueva que revertir.

## 5. Datos de prueba sintéticos futuros FIX01–FIX12

| ID | Dataset o caso esperado | Denegación que debe comprobarse |
| --- | --- | --- |
| FIX01 | Empresa A shared válida | A no lee asignación B |
| FIX02 | Empresa B dedicated válido de prueba | B no obtiene DSN A |
| FIX03 | Empresa sin binding vigente | No abrir conexión default ajena |
| FIX04 | Membresía revocada | No consultar intent |
| FIX05 | Dos asignaciones activas concurrentes | Únicidad y CAS rechazan una |
| FIX06 | Revisiones fuera de secuencia | No sobrescribir estado nuevo |
| FIX07 | Retry igual con misma idempotency | No duplicar checkpoint |
| FIX08 | Replay alterado con misma idempotency | Rechazar conflicto |
| FIX09 | Restore efímero íntegro | Detectar checksum corrupto |
| FIX10 | FKs huérfanas en conjunto aislado | Impedir cutover |
| FIX11 | Target de prueba caído | No fallback a B o conexión insegura |
| FIX12 | Rollback de prueba con target previo | Denegar reversión sin aprobación |

**NO EJECUTADOS:** estas doce entradas son obligaciones futuras de tests positivos/negativos, no pruebas xUnit reales, MySQL/Docker en ejecución ni certificado de seguridad.

## 6. Invariantes de seguridad SEC01–SEC12

1. SEC01 JWT, RBAC y membresía vigente se validan en servidor por cada acción.
2. SEC02 Empresa A/B se filtra en toda consulta, exportación y migración.
3. SEC03 Ningún DSN, host, token, password ni PII se serializa a API/log.
4. SEC04 Mapeo ausente o corrupto devuelve NO_GO, no fallback ajeno.
5. SEC05 Cutover requiere fence de versión, exclusión de escritores y checkpoint idempotente.
6. SEC06 Snapshot y restore se ensayan antes de liberar cambio técnico.
7. SEC07 Cambios de integridad o nulos no reconciliados bloquean migración.
8. SEC08 Migraciones destructivas precisan autorización separada, rollback y backup.
9. SEC09 A5 de la misma Task autora no satisface revisión independiente.
10. SEC10 Check cancelado, fallido, omitido o pendiente no es PASS requerido.
11. SEC11 UI, pestaña Sheets o documentación no crea acceso cross-tenant.
12. SEC12 QA/main/PROD, secretos, DNS y datos reales quedan fuera de este cambio.

## 7. A0–A5, salida y rollback

**A0** diferencia dos Markdown y comprueba fuentes y ausencia de cambios de schema/secretos. **A1** verifica la coherencia de once entidades, diez índices conceptuales, ocho pasos y doce datasets futuros sin afirmar ejecución. **A2** revisa fail-closed, tenant A/B, permisos, idempotencia y estrategias de regresión. **A3** contrasta contratos C02-02.M02.01/M02.02 y bloqueos de M01.06, y distingue diseño de persistencia efectiva. **A4** requiere commit fast-forward/CAS en `dev`, check-runs exact-SHA `verify-solqaryn-project-scope` y V5 procedencia completados con éxito, readback institucional. .NET/Angular/MySQL de ejecución son **NO_APLICA al diff Markdown-only**, sin llamarlos PASS. **A5** independiente por otra Task Javier antes de usar esta entrega como gate técnico.

**Rollback de este changeset:** `git revert` normal de documento/CHANGELOG sin reescribir historia. Un `git revert` NO restaura MySQL y no constituye una restauración de datos. Sin ejecución sobre QA/main/PROD, servidores, cuentas ni servicios de pago.
