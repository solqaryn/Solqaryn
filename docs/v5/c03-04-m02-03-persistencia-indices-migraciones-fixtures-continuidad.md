# SOLQARYN V5 — C03-04.M02.03 contrato de persistencia y rollback

**Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Destino:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT. **Unidad:** "Definir persistencia, índices, migraciones y datos de prueba" de C03-04 M02. **Tipo de entrega:** diseño DOCUMENTAL, no migration EF, DDL, backup, restore ni conexión a Aiven.

**Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (PLAN/COLA/DAG/BITACORA). Preflight individual M02.03: PLAN PENDIENTE sin owner, COLA G/O SI, DAG F VALIDADO y G SI; los seis entregables M01.01–06 están LISTO con A5 independiente. No asumir que M02.01/02 habiliten tablas o contratos ejecutables.

## 1. Línea base y autoridad de datos

- `scripts/backup_dev.sh`, blob `d2e0ca8bf48dc8c21fcbfd5ab5256c770bdbdcf3`: validaciones de entorno, **no** respaldo certificado.
- `scripts/restore_dev.sh`, blob `9be58822ae5c6189e7a84cf785998bf42a1b576c`: exige condición destructiva, **no** restauración realizada.
- `.github/workflows/backup-restore-dev.yml`, blob `cee9cdd1e22d006c21d9a23cc40ead6223d7295c`: especifica CI efímera; YAML no demuestra PASS.
- `docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md`, blob `81e8b11557912fffcd741ff0f34faf69e0ed6f47`: guía DEV sin privilegios implícitos a PROD.
- `docs/ROLLBACK_RUNBOOK.md`, blob `b7424464a5a79f0d5ff858a7ec4d2356a30355f0`: procedimiento de reversión, sin ejecución en esta microtarea.
- Contratos `C03-04.M02.01` (entidades propuestas) y `C03-04.M02.02` (API/DTO propuestos) son referencias **documentales**, con sus A5 independientes todavía pendientes; no se activan sus consumidores por esta entrega.

No confundir entidades reales existentes de otras capas con los **agregados FUTUROS_NO_IMPLEMENTADOS** de este contrato. Tampoco presumir que el rol SQL de auditoría permite DDL: las migraciones futuras se ejecutarán únicamente con autorización y entorno efímero adecuado.

## 2. Modelo de almacenamiento propuesto (sin crear tablas)

| Proyección futura | Propietario y clave natural | Índices/restricciones que deberán verificarse |
| --- | --- | --- |
| `IncidentCase` | `TenantId, IncidentId`, estado/version/creador con membresía actual | PK opaca, índice `TenantId,State,CreatedUtc`, version token optimista |
| `IncidentScope` | `TenantId, IncidentId, ResourceId` | FK compuesta tenant+incidente; nunca referencia recurso de otro tenant |
| `IncidentEvidence` | `TenantId, IncidentId, EvidenceId` | Hash/manifest y clasificación; PII cifrada/externa, jamás secreto bruto |
| `ContainmentProposal` | `TenantId, IncidentId, ProposalId` | Versión y autoridad; no equivale a revocación JWT ejecutada |
| `OperatorDecision` | `TenantId, IncidentId, DecisionId` | Actor con membresía/rol actuales, evidencia de aprobación separada |
| `BackupRequest` | `TenantId, Environment, RequestId` | `Environment` allowlisted CI/DEV, estado no certificado hasta job real |
| `BackupManifest` | `TenantId, BackupId, ManifestVersion` | Checksum íntegro y referencia opaca a artefacto externo |
| `RestoreTrial` | `TenantId, BackupId, TrialId` | Destino efímero, autorización destructiva, job SHA y hashes |
| `RollbackPlan` | `TenantId, IncidentId, RollbackId` | Referencia a version anterior; nunca operación automática |
| `RecoveryAuditEntry` | `TenantId, IncidentId, Sequence` | Append-only, integridad de secuencia, sin borrar entradas anteriores |
| `RecoveryIdempotency` | `TenantId, ActorId, Operation, Key, Version` | Índice único compuesto, TTL/limpieza sólo con política aprobada |

**No implementar ninguna tabla** bajo esta M02.03. El modelo final debe revisar si IDs opacos por tenant requieren claves compuestas y evitar identidad global que habilite lecturas cross-tenant. Las referencias de respaldo o artefacto no transportan el contenido del backup ni secretos. Los nombres, columnas e índices son candidatos que requieren diseño EF/MySQL independiente.

## 3. Reglas de integridad e índices (futuras)

- DB01: toda consulta, clave foránea y actualización incluye `TenantId` derivado de autoridad server-side, no de DTO.
- DB02: claves compuestas `TenantId,Id` impiden cruzar incidentes, evidencias, backups y decisiones de dos empresas.
- DB03: uso de `Version` optimista para prevención de sobrescritura; conflicto se devuelve 409 sin pérdida de historia.
- DB04: relación de `RecoveryIdempotency` con actor+tenant+operación+versión: replay cruzado no comparte resultado.
- DB05: evidencias inmutables por auditoría append-only; una corrección agrega nuevo evento compensatorio.
- DB06: ningún campo de log o DTO guarda JWT, contraseñas, URI firmadas, secretos, PII cruda o excepciones.
- DB07: un backup no se declara válido si no se verifican hash, manifiesto, timestamp y compatibilidad.
- DB08: una restauración exige destino CI/DEV descartable y autorización destructiva fuera de este contrato.
- DB09: índices/constraints de tenant deben someterse a pruebas MySQL 8.4 efímeras, no deducirse de LINQ.
- DB10: estados de recuperación o restauración no se obtienen desde Sheets de automatizaciones.
- DB11: fallos parciales conservan `PENDIENTE/ERROR`, no `PASS`, y registran correlación sin información sensible.
- DB12: no se reescriben eventos BITACORA, ni se duplican escrituras tras errores de CAS.
- DB13: snapshots para restauración sólo se refieren al entorno exacto aprobado; prohibido PROD implícito.
- DB14: toda migración tiene `up`, `down` seguro/compensación y prueba de preservar datos de B al operar en A.

## 4. Plan de migración y reversión FUTURO_NO_EJECUTADO

1. Inventariar las tablas actuales, ownership y FKs en un MySQL 8.4 **efímero** sin acceder a Aiven real.
2. Prototipar nuevas entidades EF 10 y restricciones tenant-first en un scope **M03 autorizado**.
3. Preparar fixtures sintéticos de empresa A y B (sin datos productivos), usuarios activos/revocados y rol sin permiso.
4. Ejecutar migración en CI y comprobar SQL real, índices, FK, unicidad, concurrentes e idempotencia.
5. Ensayar recuperación frente a transacción fallida, rollback de esquema y comparación de hashes/filas en A/B.
6. Documentar resultados, logs saneados, SHA exacto y medidas RPO/RTO **medidas**, manteniendo el gate hasta A0–A5.

**Rollback de ESTE documento:** git revert normal del commit Markdown, preservando historial y BITACORA. No existe ningún rollback MySQL que ejecutar ahora.

## 5. Fixtures y oráculos FUTUROS_NO_EJECUTADOS

| Caso | Clase | Resultado esperado de una prueba posterior |
| --- | --- | --- |
| P01 | + | Incidente A con ownership válido crea FK solo A, versión inicial y auditoría |
| P02 | + | Dos solicitudes idénticas A/actor/versión producen una sola mutación |
| P03 | + | Restore CI efímero compara hash y conserva datos sintéticos A+B |
| P04 | + | Reversión transaccional impide escrituras parciales y conserva evidencia |
| N01 | - | Sesión A intenta FK/referencia B → DENY, cero datos B |
| N02 | - | Role legacy admin sin RolPermiso vigente → DENY |
| N03 | - | Membresía revocada después de emitir JWT → DENY |
| N04 | - | URI externa, secreto, PII o payload SQL en manifiesto → rechazo |
| N05 | - | Clave idempotente de A reutilizada en B → nunca comparte resultado |
| N06 | - | Versión obsoleta o escritor concurrente → conflicto sin overwrite |
| N07 | - | Hash de backup incorrecto/manifest ausente → no restauración |
| N08 | - | Entorno producción o fuera de allowlist → NO_GO |
| N09 | - | Falta autorización destructiva → NO_GO antes de I/O |
| N10 | - | Job CI `cancelled`, `skipped` o `in_progress` → jamás PASS |
| N11 | - | Rollback elimina o altera auditoría histórica → rechazar |
| N12 | - | Índice tenant omitido permite cross-tenant → prueba falla y bloquea avance |

**Los 16 escenarios son únicamente una especificación**, ninguno fue ejecutado por esta microtarea. No hay hechos sobre RPO/RTO, migración o backup real.

## 6. Cierre A0–A5 y dependencias

**A0:** revisar solo dos Markdown y fuentes Git reales. **A1:** comprobar consistencia de 11 agregados, 14 reglas, seis pasos de migración y 4P/12N futuros. **A2:** JWT/RBAC/tenant A/B fail-closed; ninguna tabla ni dato real. **A3:** consistencia con M01.01–06 LISTO+A5 y M02 conceptual sin presuponer A5 de otros autores. **A4:** git fast-forward CAS sobre HEAD actual, Scope Lock y V5 `completed/success` exact SHA, PLAN/COLA/BITACORA readback. **A5:** revisión independiente por otra Task Javier; no certifica implementación futura ni QA/main/PROD.

M02.04–06/M03+ requieren preflight propio y pruebas causales cuando sean código. No instalar nuevos esquemas, endpoints, workers, webhooks, eventos, secretos, hosts, pagos ni infra bajo esta unidad.