# SOLQARYN V5 FINAL — C03-04.M02.05: eventos, idempotencia, correlación y límites de continuidad

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Fecha:** 2026-11-14, America/Tegucigalpa. **Rama:** solqaryn/Solqaryn@dev. **PROJECT_SCOPE_LOCK=STRICT**.

**Entrega DOCUMENTAL_NO_IMPLEMENTADA:** contratos futuros de incidentes, revocaciones, respaldos, restauración y rollback. NO se crean eventos runtime, webhooks, Tasks, workers, jobs, disparadores, SQL, API o servicios. No se ejecutan backup, restore, migraciones, cambios de permisos ni accesos productivos. No QA/main/PROD. Todas las pruebas propuestas están FUTURAS_NO_EJECUTADAS.

## 1. Autoridad, preflight y fuentes

La autoridad es el [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) para C03-04.M02.05 y la [hoja institucional PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). PLAN fila 552 PENDIENTE sin owner; COLA G/O=SI y DAG F=VALIDADO/G=SI, con C03-04.M01.01–06+A5. M02.01–04 son documentos relacionados; no equivalen a respaldo/restauración aprobados ni liberan por sí mismos M02.06/M03.

Archivos reales referenciados en el inventario C03-04.M01.03 y el diseño M02.01: scripts/backup_dev.sh (restricciones entorno), scripts/restore_dev.sh (permiso destructivo y destino descartable), .github/workflows/backup-restore-dev.yml (MySQL 8.4 efímero en CI), docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md y docs/ROLLBACK_RUNBOOK.md. La existencia de archivos y workflows no es evidencia de ejecución ni aprobación. Para cualquier restauración futura exigir manifest, hash, entorno real y job concluido, además de controles tenant A/B y aprobación explícita.

## 2. Señales conceptuales EV01–EV14 (NO_PUBLICADAS)

| ID | Señal propuesta | Prueba causal futura exigida |
| --- | --- | --- |
| EV01 | IncidenteDetectado | empresa/entorno, owner, timestamp y clase sin PII |
| EV02 | IncidenteClasificado | rol y política comprobados, versión |
| EV03 | RevocacionSolicitada | membresía activa y scope, sin token en payload |
| EV04 | ContencionAutorizada | permiso y aprobación específica, sin acción implícita |
| EV05 | BackupSolicitado | dataset, tenant, entorno permitido y base SHA |
| EV06 | BackupVerificado | artefacto cifrado, manifest y checksum verificados |
| EV07 | BackupRechazado | hash/permiso/secret faltante, sin falsa certificación |
| EV08 | RestoreSolicitado | destino efímero allowlisted y autorización destructiva |
| EV09 | RestoreIniciado | exclusividad real del destino y job nativo |
| EV10 | RestoreVerificado | esquema, filas, FK, hash y aislamiento A/B comprobados |
| EV11 | RestoreFallido | motivo seguro y estado parcial, no PASS |
| EV12 | RollbackSolicitado | versión previa, owner y compensación aprobada |
| EV13 | RollbackVerificado | evidencia de efecto y auditoría preservada |
| EV14 | IncidenteCerrado | autoridad de cierre y revisión independiente |

Estos son nombres de contrato y NO clases, eventos disparados, topics, transportes, workflows activos ni Tasks. No se autoriza hacer que una Task programe o dispare otra fuera del schedule físico.

## 3. Envelope y correlación con límites de confianza

Campos propuestos: SchemaVersion, EventId opaco, EventKind, EmpresaId verificado, EnvironmentScope, ResourceReference, OccurredAtUtc, CorrelationId saneado, CausationId, OperatorReference mínima, ExpectedVersion, ArtifactHash, ProvenanceSha, NativeJobId cuando exista, OutcomeCode, RetentionClass, PayloadDigest. No incluir contraseña, cadena de conexión, tokens JWT, cookies, passphrase de cifrado, dump de datos ni PII en logs.

La autoridad de empresa y rol nace de membresía server-side vigente; el parámetro suministrado por UI, ruta, header o documento nunca concede acceso. Un backup A no puede exponerse/restaurarse en B. Un artefacto sin tenant verificable permanece en cuarentena. CorrelationId permite rastrear, no autorizar. Separar hora del evento, hora de job, última señal y hora de verificación; autorreporte institucional no equivale a ejecución nativa. CI pertinente requiere head_sha exacto, status completed y conclusion success. CANCELLED, SKIPPED, FAILURE, QUEUED e IN_PROGRESS jamás son PASS requerido.

## 4. Contratos de idempotencia ID01–ID10 (PROPUESTOS)

| ID | Regla futura | Fallo que impide |
| --- | --- | --- |
| ID01 | Identidad empresa+entorno+operación+artefacto+solicitud | clave global compartida A/B |
| ID02 | Payload canónico unido a idempotency key | cambiar destino con la misma clave |
| ID03 | CAS atómico en versión real/lease real | dos restores simultáneos ganadores |
| ID04 | Recibo durable sólo tras commit efectivo | registrar éxito sin artefacto |
| ID05 | Reintento finito sólo ante fallos transitorios | loop de reintentos en checksum inválido |
| ID06 | Identificador nativo de ejecución verificable | atribuir run_id ficticio |
| ID07 | Hash y destino inmutables durante retry | restaurar base distinta en un reintento |
| ID08 | Lease sin inferencia arbitraria de caducidad | reclamar owner con ejecución activa |
| ID09 | Corrección compensatoria append-only | reescribir BITACORA o borrar historial |
| ID10 | Documento no cuenta como efecto runtime | declarar restore por crear Markdown |

Una lectura+escritura Sheets no acredita transacción distribuida. La implementación futura debe probar exclusión sobre recursos reales y concurrencia; no instala un lock artificial ni cambia permisos.

## 5. Límites LM01–LM12 y denegación segura

| ID | Límite | Regla |
| --- | --- | --- |
| LM01 | Entorno y host permitidos | denegar PROD, host remoto o destino sin permiso |
| LM02 | Timeout finito | conservar estado NO_VERIFICADO sin falsa salida PASS |
| LM03 | RPO | calcular desde artefacto original y hora UTC real |
| LM04 | RTO | medir ejecución nativa end-to-end, no estimar del YAML |
| LM05 | Retención | cifrar, minimizar, cumplir política sin borrado arbitrario |
| LM06 | Número de reintentos | backoff acotado con causa transitoria |
| LM07 | Cuota de almacenamiento y tamaño | rechazar sin comprar ni activar servicio |
| LM08 | Permiso de lectura de evidencias | no revelar datos de tenant B a operador A |
| LM09 | Deduplicación de alertas | no generar notificaciones infinitas |
| LM10 | Integridad criptográfica | hash o manifest inválido bloquea restore |
| LM11 | Rollback proporcional | no borrar historial ni afectar otra empresa |
| LM12 | Autoría y A5 independiente | jamás autoaprobación o autorización PROD implícita |

La plataforma no aplica en esta entrega valores numéricos nuevos; RPO/RTO, cuotas y umbrales requieren medición y aprobación de la implementación futura.

## 6. Oráculos positivos y negativos FT01–FT12 (FUTUROS_NO_EJECUTADOS)

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| FT01 | Operador autorizado registra incidente propio | operador A lee detalle de B |
| FT02 | Membresía revocada impide renovación/acceso | JWT antiguo conserva permiso efectivo |
| FT03 | Backup de DEV produce manifest, hash y cifrado | archivo corrupto se declara válido |
| FT04 | Restore en MySQL 8.4 descartable conserva filas/FK | intento a PROD sin permiso se ejecuta |
| FT05 | Host allowlisted permite operación autorizada | host remoto o path arbitrario se conecta |
| FT06 | Timeout conserva estado reanudable/idempotente | retry duplica acción o inventa éxito |
| FT07 | Dos writers compiten y gana uno mediante CAS real | ambos creen obtener lease válido |
| FT08 | Rollback agrega evidencia y conserva datos originales | revert borra auditoría o datos de B |
| FT09 | Logs exponen sólo códigos/correlación saneada | logs contienen secretos/PII/JWT |
| FT10 | RPO/RTO se miden con job real | documento se presenta como drill PASS |
| FT11 | Check exact-SHA completed/success permite gate | skipped/cancelled/in_progress se cuenta PASS |
| FT12 | Reviewer A5 distinto audita el diff | mismo autor aprueba, o promociona QA/PROD |

Fixtures futuros: dos empresas A/B, miembros activos/revocados, artefacto con hash correcto e incorrecto, destino loopback/PROD simulado, dos concurrent writers, errores transitorios/permanentes, recuperación parcial y caso de PII en logs. No se ejecutaron HTTP, .NET, MySQL, CI de restore ni Playwright por esta documentación.

## 7. Impacto, A0–A5 y rollback

**Impacto actual:** exclusivamente nuevo Markdown en docs/v5 y entrada autocontenida en CHANGELOG_AI.md. Sin cambios de código, infra, credenciales, cuentas, scripts, workflows, MySQL, despliegues, contratos activos de autorización ni datos productivos. Rollback inmediato: git revert normal del changeset en dev y evento BITACORA aditivo, preservando HEAD concurrente.

**A0:** fuentes V5, gate M01+A5, coherencia EV01–14/ID01–10/LM01–12/FT01–12, no secretos, no headings vacíos. **A1:** positivos y negativos definidos como FUTUROS_NO_EJECUTADOS; no PASS funcional. **A2:** especificación tenant A/B, JWT/RBAC, permisos destructivos y destino fail-closed, sin atribuir pruebas runtime. **A3:** reconciliar PLAN/COLA/DAG/BITACORA y límites de contratos existentes. **A4:** publicar por fast-forward/CAS contra HEAD, verificar diff dos Markdown, blobs, Scope Lock y V5 completed/success en SHA exacto, readback institucional. **A5:** revisión real por otra Task Javier distinta de la autora, posterior a LISTO_DEV; no habilita QA/main/PROD.
