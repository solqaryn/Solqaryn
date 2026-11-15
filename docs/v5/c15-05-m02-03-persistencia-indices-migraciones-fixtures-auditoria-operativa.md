# SOLQARYN V5 — C15-05.M02.03: persistencia, índices, migraciones y datos sintéticos de auditoría operativa

**Responsable:** PRIMARY DEV 01 — JAVIER_CHATGPT, `6adbb9fff1548191a1bd5b30d9e9984a`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 14 noviembre 2026. **Tipo:** contrato documental M02.03; **NO_IMPLEMENTADO**. No se creó ni se modificó tabla, índice, migración EF, seed, servicio, API, evento, worker, webhook, configuración de proveedor, tarea física o dato real.

## 1. Procedencia y límites del diseño

Autoridad exclusiva: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Preflight de `C15-05.M02.03`: PLAN_MAESTRO H2314=PENDIENTE / I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI, seguido de compare-and-set a EN_PROGRESO y readback. La elegibilidad no reemplaza permiso DB/QA/main/PROD.

Contratos precedentes: `docs/v5/c15-05-m02-01-modelo-ownership-invariantes-auditoria-operativa.md` (OM01–OM14, INV01–INV16, AT01–AT12) y `docs/v5/c15-05-m02-02-api-dto-errores-compatibilidad-auditoria-operativa.md` (interfaces NO_IMPLEMENTADAS, riesgos JWT/RBAC/tenant, errores y compatibilidad). La realidad actual se reparte entre PLAN_MAESTRO H/I/K, COLA G/N/O, DAG_TECNICO F/G, BITACORA A:Z append-only, TAREAS_PROGRAMADAS AA:AN y GitHub Actions exact-SHA. Un diseño de persistencia no permite inferir un almacén nuevo desplegado.

## 2. Decisiones de persistencia y autoridad de datos (PD01–PD12)

| ID | Regla futura propuesta | Fuente / prueba negativa necesaria |
| --- | --- | --- |
| PD01 | PLAN_MAESTRO conserva autoridad exclusiva para estado y owner del microtask ID | Copia de lectura nunca cambia estado |
| PD02 | COLA es vista derivada y reactiva, jamás tabla que conceda SI manualmente | Vista obsoleta no autoriza claim |
| PD03 | DAG_TECNICO identifica predecesores exactos, validación F y gate G | F=VALIDADO sin G=SI no habilita |
| PD04 | BITACORA preserva 26 columnas, EVENT_ID e IDEMPOTENCY_KEY únicos y orden histórico | Reprocesar el mismo evento no duplica filas |
| PD05 | Registro de CI enlaza check, SHA, fecha y fuente GitHub verificable | Check de SHA viejo no certifica nuevo HEAD |
| PD06 | RUN_ID proveedor sólo si se acredita por Automations.peek, distinto de CORRIDA_KEY_LOCAL | Autorreporte nunca se anuncia como nativo |
| PD07 | PLAN I identifica Task Javier permitida y cambio de estado requiere exclusión demostrable | Dos escritores no pueden atribuirse un único ID |
| PD08 | Relevo guarda owner anterior, motivo, ausencia de carrera y SHA antes/después | Diez minutos de antigüedad no acreditan abandono |
| PD09 | Revisión A5 enlaza autor, revisor distinto, SHA, evidencia y decisión | Autor no puede autoaprobar |
| PD10 | Acceso a registros evita secretos, prompts, correo personal y PII; minimización por propósito | Exportación en bruto se deniega |
| PD11 | Datos multiempresa, si existiesen, se aíslan con tenant ID backend y permiso independiente | No confiar en tenant indicado sólo por cliente |
| PD12 | Retención, copia, purga, recuperación y restauración necesitan política aprobada y ensayos | No se afirma cumplimiento de backup/restore real |

**Elección presente:** seguir con las fuentes institucionales actuales, sin duplicar autoridades en MySQL. Una eventual proyección consultable sería **read model**, no sistema de registro primario; su actualización podría retrasarse y siempre debería exponer `observedAtUtc` y origen. Ninguna proyección será utilizada para conceder permisos, activar Tasks, afirmar CI PASS o liberar una dependencia.

## 3. Modelo físico opcional futuro, no adoptado

Si una orden posterior autoriza una base de consulta, evaluar entidades `OperationalAuditProjection` (microtask_id, estado, owner_task_id, observed_at, source_revision), `OperationalAuditEventProjection` (event_id, microtask_id, occurred_at, operation, payload_digest), `OperationalCheckProjection` (microtask_id, commit_sha, check_name, status, conclusion, checked_at) y `OperationalReviewProjection` (microtask_id, author_task_id, reviewer_task_id, reviewed_sha, decision, reviewed_at). Estos nombres son **borradores lógicos**, no tablas existentes y no deben generar migración por esta microtarea. Una fuente sin versión o sin tiempo de observación permanece `UNKNOWN`, no se convierte en cero ni PASS.

Usar EF Core 10 con proveedor MySQL **sólo si** la gobernanza autoriza implementación futura y la arquitectura canónica lo mantiene. Registrar `TenantId` únicamente cuando exista dominio/propiedad verificados; no fabricar vínculo tenant para las seis Tasks institucionales. No almacenar secretos, tokens JWT completos, saldos, adjuntos, correos, prompts, dump de BITACORA ni snapshots con PII en columnas de auditoría.

## 4. Índices candidatos condicionados IX01–IX06

| ID | Campos propuestos | Objetivo y limitación |
| --- | --- | --- |
| IX01 | `microtask_id` único en proyección actual | Evita duplicados de entidad; **no** concede lock CAS global |
| IX02 | `(microtask_id, occurred_at, event_id)` en eventos | Trazar evolución causal y paginar estable |
| IX03 | `event_id` único e `idempotency_key` única si se replica | Dedupe, política de conflictos debe ser explícita |
| IX04 | `(commit_sha, check_name, checked_at)` en comprobaciones | Agrupar evidencia por SHA exacto |
| IX05 | `(microtask_id, reviewed_sha, reviewed_at)` en revisiones A5 | Unir decisión a artefacto correcto sin sustituir revisión independiente |
| IX06 | `(owner_task_id, observed_at)` en última señal de proyección | Detección auxiliar; señal sin run nativo no prueba actividad |

Estos índices **no se crearon ni se probaron**. Deben revisarse cardinalidad, selectividad, colación, claves compuestas, coste de escrituras y acceso por tenant antes de DDL. Una clave única en un read model no garantiza exclusión entre proveedor de Tasks, Sheets y GitHub; la transición de owner exige un mecanismo CAS real y verificación bajo competencia.

## 5. Migración y reversión FUTURA, sujeta a nueva orden

- **MIG01 / preflight:** copia verificable de fuentes, control de versiones, plan de minimización de PII, inventario de ids y autorización separada de lectura.
- **MIG02 / diseño:** modelar esquema aditivo y mappings, verificar política `TenantId`, restricciones y retención; revisión A5 independiente cuando corresponda.
- **MIG03 / ensayo:** ejecutar únicamente en base MySQL efímera de CI autorizada, con datos sintéticos, test de concurrencia de dos escritores, aislamiento empresas A/B y restauración.
- **MIG04 / despliegue:** no autorizado aquí. Requerir procedimiento de seguridad, ventana de mantenimiento, respaldo, rollback, aprobaciones y pruebas exact-SHA.
- **MIG05 / reconciliación:** comparar count, digests e IDs entre read model y Sheets/GitHub para detectar desalineación; jamás corregir estado de PLAN por importación automática.
- **MIG06 / rollback:** desactivar futuras lecturas del read model, recuperar contrato anterior, realizar rollback aditivo verificado y no destruir BITACORA; no hacer reset Git.

**Estado actual de MIG01–MIG06: NO_EJECUTADAS.** Sin SQL, operaciones Aiven, datos reales ni releases de ambiente.

## 6. Fixtures sintéticos FX01–FX12, todos NO_EJECUTADOS

| ID | Prueba futura positiva | Control negativo que debe fallar cerrado |
| --- | --- | --- |
| FX01 | ID presente coincide PLAN/COLA/DAG | ID huérfano tratado como elegible |
| FX02 | Estado PENDIENTE sin owner se reclama una vez | Dos Tasks obtienen ambas éxito |
| FX03 | VALIDANDO conserva owner y CI pendiente | VALIDANDO se convierte en LISTO automáticamente |
| FX04 | Evento único se agrega una vez | Duplicado con misma clave crea nueva fila |
| FX05 | Verificación `completed/success` del SHA original | `cancelled/skipped/in_progress` se considera PASS |
| FX06 | A5 registra distinto autor/revisor | Self-review firma A5_PASS |
| FX07 | Snapshot local con origen y hora | Reporte propio se confunde con run nativo |
| FX08 | Paginación por microtask ID y evento | Cursor invalidado devuelve registros de otra empresa |
| FX09 | Registro con tenant y rol verificados | Tenant A lee registros del tenant B |
| FX10 | Replay idempotente preserva orden | Replay cambia historiales o responsables |
| FX11 | Relevo documentado sin ejecución activa | Run en curso pierde propietario por falsa orfandad |
| FX12 | Rollback aditivo conserva evidencia original | Deshacer borra historial o usa force-push |

Los fixtures son **descripciones de ensayo futuro**, no datos insertados, builds, tests xUnit, pruebas MySQL, Playwright, mediciones de cuota, ni certificación de ausencia de errores. Requerir CI con servicio efímero y exact-SHA al implementar.

## 7. Análisis de impacto, controles A0–A5 y seguimiento

- **Componente:** auditoría operativa C15-05; impacto sólo documental M02.03 y `CHANGELOG_AI.md`.
- **Dependencias:** M01.01–06 con A5 precedente; M02.01 y M02.02 documentales, M02.04/05/06 posteriores; releer DAG/PLAN/COLA por ID tras cada transición.
- **Interfaces/API/clientes:** ninguna clase, ruta, DTO ejecutable o consumidor modificado.
- **Datos:** ninguna migración, fila de negocio, índice, tabla, proveedor ni transacción real modificada.
- **Seguridad:** JWT/RBAC, empresas A/B, roles, Scope Lock y seis Tasks Javier se preservan; no se crean secretos, tokens ni reglas de acceso.
- **Observabilidad:** diseño diferencia fuente nativa de autorreporte; no crea monitor en tiempo real.
- **A0:** validar 12 reglas PD, seis índices, seis hitos MIG y doce fixtures sintéticos con alcance declarado.
- **A1:** comprobar consistencia documental de positivos/negativos, no afirmar pruebas de código.
- **A2:** revisar prohibiciones de PII, privilegios, tenancy y falsas señales de exclusividad; ningún pentest realizado.
- **A3:** trazabilidad con STANDARD V5, M02.01/M02.02, PLAN/COLA/DAG/BITACORA; CI .NET/MySQL no causal `NO_APLICA_DOCUMENTAL`.
- **A4:** publicar exclusivamente dos archivos Markdown en dev mediante fast-forward CAS, revisar diff y exigir Scope Lock y procedencia V5 `completed/success` exact-SHA, además de readbacks institucionales.
- **A5:** otra Task Javier independiente del autor con revisión de diff, fuente, SHA y CI, antes de promover cualquier dependencia que la requiera.

Rollback documental: `git revert` normal en dev, nuevo SHA y controles, BITACORA append-only; nunca force-push ni borrado histórico. Una implementación real de persistencia necesitará autorización y pruebas nuevas. Sin autorización expresa, sólo este contrato documental puede considerarse LISTO_DEV, **no una migración realizada**.
