# SOLQARYN V5 FINAL — C01-04.M02.05: eventos conceptuales, idempotencia, correlación y límites del expediente universal

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Fecha:** 2026-11-14, America/Tegucigalpa. **Rama:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

**Salida atómica:** definir contratos FUTUROS_NO_IMPLEMENTADOS para eventos conceptuales, deduplicación, correlación, aislamiento y límites del expediente, cronología, observaciones, decisiones, adjuntos y versiones. **No** se crea evento operativo, worker, broker, webhook, cola, dispatcher, callback entre Tasks, endpoint, tabla, migration, proveedor, permiso o servicio. Ninguna Task despierta a otra. No QA/main/PROD ni datos o secretos reales.

## 1. Autoridad y preflight causal

Fuente única: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) para C01-04.M02.05 y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Preflight individual: PLAN PENDIENTE sin responsable, COLA G/O=SI, DAG F=VALIDADO/G=SI, con C01-04.M01.01–06 y A5 independientes. Los contratos documentales M02.01–04 son material complementario, no ejecución funcional ni un atajo a M02.06/M03.

Evidencia real observada: backend/src/Domain/Entities/RegistroAuditoria.cs tiene CorrelationId, Fecha, Entidad, ReferenciaId, EmpresaId nullable y Resultado; backend/src/Domain/Entities/CompraDocumento.cs tiene referencias de archivo asociadas a compra; backend/src/Domain/Security/ContextoTenantActual.cs exige membresía verificada y propietario de empresa, pero no constituye por sí solo el expediente universal. docs/v5/c01-04-m02-01-modelo-entidades-ownership-invariantes-expediente.md define EX01–EX16 como conceptos y no como entidades EF. No asumir que un registro auditable existente publica un evento del expediente ni que se entregan mensajes a servicios externos.

## 2. Señales conceptuales EV01–EV14 (NO_PUBLICADAS)

| ID | Evento de dominio futuro / condición observada | Resultado observable propuesto |
| --- | --- | --- |
| EV01 | ExpedienteAbierto | Referencia a recurso y ámbito autorizado, versión inicial |
| EV02 | ExpedienteVinculado | Enlace autorizado a origen, sin cambiar entidad comercial original |
| EV03 | EntradaTimelineAnadida | Evento inmutable con FechaUtc, Id y versión para orden estable |
| EV04 | ObservacionRegistrada | Actor y rol efectivos, texto minimizado, permiso comprobado |
| EV05 | ObservacionRectificada | Nueva versión con motivo, sin alterar prueba original |
| EV06 | DecisionPropuesta | Permiso y política requeridos, estado no aprobado |
| EV07 | DecisionRevisada | Revisor facultado y decisión vinculada a versión original |
| EV08 | DecisionAprobada | Condición de revisión satisfecha; aprobación no deriva del texto libre |
| EV09 | AdjuntoVinculado | Metadatos privados y propietario confirmado; no contenido ni URL pública |
| EV10 | AdjuntoVersionado | Hash y versión verificables, retención preservada |
| EV11 | AccesoDenegado | Código seguro y correlación sin filtrar PII/JWT |
| EV12 | VersionConflicto | Escritura obsoleta rechazada sin cambio parcial |
| EV13 | ReversionCompensada | Nueva traza compensatoria, sin borrar historia |
| EV14 | ReconciliacionPendiente | Dato fuente ambiguo o legacy en cuarentena, no visible por defecto |

Los nombres anteriores no son clases, eventos HTTP, topics ni esquemas de transporte instalados. El sistema futuro puede escoger un mecanismo compatible con la arquitectura real mediante un cambio distinto aprobado; este documento **no** lo selecciona ni activa. Ninguna emisión se interpreta como autorización de un nuevo tenant o elevación de rol.

## 3. Envelope lógico y correlación por ámbito

Campos propuestos (versionados, inmutables y mínimos): EventId opaco, ContractVersion, EmpresaId verificado, RecursoTipo, RecursoId, ExpedienteId, EntryId/Version, EventKind, OccurredAtUtc, CausationId, CorrelationId saneado, ActorReference no identificable públicamente, Outcome, PayloadHash, RetentionClass y ProvenanceReference.

La fuente de autoridad para EmpresaId, sucursal, usuario y rol es el backend y su contexto de membresía activa; jamás confiar en IDs enviados por el navegador, headers, claims no reconciliados o valores persistidos sin ownership demostrado. Si el evento está vinculado a una transacción, la fuente se conserva sin modificaciones: un evento proyectado no equivale a efecto financiero, aprobación comercial o cambio de negocio.

Correlación: una solicitud puede originar varias entradas bajo el mismo CorrelationId; cada evento mantiene EventId distinto y CausationId hacia el hecho anterior. No usar CorrelationId como token de autorización ni como clave global de acceso a logs entre empresas. Exponer códigos de error y correlaciones saneadas al cliente, nunca errores internos, JWT, credenciales, PII, IP detalladas o payload crudo de auditoría.

## 4. Contratos de idempotencia ID01–ID10 (DISEÑO)

| ID | Contrato de deduplicación futuro | Rechazo necesario |
| --- | --- | --- |
| ID01 | Clave por EmpresaId + recurso + operación | Igual clave en empresa B no hereda respuesta A |
| ID02 | Payload canónico y hash vinculados a clave | Mismo token con body distinto produce conflicto |
| ID03 | Resultado observable único por operación efectiva | Reintento no crea segunda observación/decisión |
| ID04 | Escritura con versión CAS en datastore | Dos writers de versión anterior no ganan ambos |
| ID05 | Entrada de cronología sólo tras commit exitoso | Error transaccional no crea evento fantasma |
| ID06 | Evento fallido conserva estado auditable | Reintento no reordena ni borra hecho |
| ID07 | Backoff/retry futuro con techo explícito | Fallo permanente no genera cola infinita |
| ID08 | Retención de recibo sensible a negocio y plan | Expiración no permite replay inseguro |
| ID09 | Almacenamiento y lookup con tenant compuesto | Clave global o índice no filtra empresas |
| ID10 | Publicación eventual sólo tras diseño de outbox/consumidor | Documento no constituye evento entregado |

Garantía futura a elegir y probar: efecto único bajo transacción, entrega eventualmente repetible con consumidor idempotente o rechazo seguro cuando no haya certeza; **no declarar exactly-once distribuido** sin evidencia. Todo contrato real de integración exige pruebas de crash/replay, orden, expiración, concurrencia y rollback en infraestructura efímera autorizada.

## 5. Límites LM01–LM12 y denegación por defecto

| ID | Límite futuro | Comportamiento de seguridad |
| --- | --- | --- |
| LM01 | Tamaño máximo del payload | Rechazar sin truncar evidencia obligatoria |
| LM02 | Máximo de adjuntos/tamaño por ámbito | Validar MIME, contenido, recurso y ownership |
| LM03 | Cuota de entradas por recurso/tiempo | Responder throttling con correlación segura |
| LM04 | Edad máxima de cursor | Cursor vencido se rechaza, no recupera otro tenant |
| LM05 | Retención y minimización de datos | Evitar PII/secreto en envelope y logs |
| LM06 | Ventana de deduplicación por operación | Expiración sin duplicar efectos irreversibles |
| LM07 | Reintentos por fallo transitorio | Backoff finito, sin recursión ni loop perpetuo |
| LM08 | Profundidad de correlación | Impedir cadena causal cíclica o infinita |
| LM09 | Ordenamiento por (FechaUtc, Id) | Evitar empate no determinista/duplicados |
| LM10 | Permisos y sucursal efectivos | Deny si rol/empresa/recurso no coinciden |
| LM11 | Conflicto de versiones concurrentes | Una escritura gana CAS; segunda no pisa la primera |
| LM12 | Límite de exposición de auditoría | Vista reducida, reautorizada y aislada por EmpresaId |

Los valores numéricos de límites deben medirse contra necesidades comerciales, capacidad y protección de datos. El diseño no introduce cuotas nuevas en runtime, ni revoca ni incrementa derechos por flags. Ningún mecanismo externo se provisiona.

## 6. Doce escenarios verificables FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| FT01 | Usuario A crea una observación en recurso A y obtiene un efecto | A reenvía recurso B conocido: no observa ni crea entrada |
| FT02 | Mismo payload/clave retorna recibo original sin duplicar | Igual clave, body distinto se rechaza con conflicto |
| FT03 | Dos escritores CAS sobre versión N dejan uno N+1 | Ambos sobrescriben la misma revisión |
| FT04 | Entrada con FechaUtc coincidente mantiene orden por Id | Cursor omite/duplica al empatar timestamp |
| FT05 | Reversión agrega hecho compensatorio auditable | Rollback elimina auditoría o la entidad fuente |
| FT06 | Adjunto propio revalida permisos al descargar | PublicId de B entrega bytes al usuario A |
| FT07 | Membresía vigente autoriza acción específica | Membresía revocada o rol sin permiso autoriza |
| FT08 | Pérdida de conexión permite retry finito sin duplicar | Timeout crea dos decisiones o bucle permanente |
| FT09 | Evento mínimo carece de datos personales | JWT/PII/error interno aparece en log/trace |
| FT10 | Configuración restrictiva aplica cuota | Override tenant supera límite plataforma |
| FT11 | Validación CI sobre SHA exacto acredita solo checks pertinentes | SKIPPED/CANCELLED/IN_PROGRESS se presenta como PASS |
| FT12 | Documento deja pendiente implementación y A5 independiente | Se instala webhook o se promueve a producción por leerlo |

Fixture propuesto: empresas A y B, miembros activos/revocados, recurso propio/ajeno, idempotency key repetida con payload igual/distinto, dos writers de versión N, un adjunto privado, dos registros UTC iguales, error transitorio y uno permanente. Datos sintéticos; ninguna prueba HTTP, xUnit, SQL ni Angular se ejecuta en esta entrega.

## 7. Impacto, pruebas futuras y rollback

**Capas:** diseño documental en docs/v5 y CHANGELOG_AI.md. Domain, Application, Infrastructure, API, Angular, migraciones y servicios permanecen intactos. **Dependencias:** M01.01–06+A5; M02.01–04 de consulta conceptual no sustituyen el gate. **Datos:** no hay DDL, nueva tabla, outbox, backfill ni proceso de retención ejecutado. **Seguridad:** JWT/RBAC y tenancy A/B prevalecen, denegación por defecto y minimización. **Operación:** no worker, webhook, dispatcher, Tasks nuevas, servicios de pago ni observabilidad instalada. **Rollback inmediato:** revert normal exclusivamente del changeset documental en dev, preservando BITACORA histórica y cambios concurrentes.

Pruebas futuras cuando exista runtime: xUnit de operaciones A/B y membresía revocada, tests integración MySQL 8.4 efímero sobre claves compuestas/idempotencia y transacciones, tests de errores/reintentos, version CAS, seguridad de adjuntos, y Playwright si se agrega consumidor visual. No marcar las anteriores como ejecutadas.

## 8. Auditorías A0–A5 y liberación

- **A0:** comprobar EV01–EV14, ID01–ID10, LM01–LM12, FT01–FT12, fuentes, coherencia y ausencia de secretos/archivos runtime; verificar CHANGELOG autocontenido.
- **A1:** escenarios positivos/negativos falsables redactados; **FUTUROS_NO_EJECUTADOS**, no PASS funcional.
- **A2:** límites diseñados JWT/RBAC/tenant A/B, cache, idempotencia y no filtrado de credenciales; pruebas runtime **NO_APLICA** al diff Markdown.
- **A3:** contrastar STANDARD y PLAN/COLA/DAG del ID exacto, ownership y BITACORA, sin inferir evento real ni liberar M02.06/M03.
- **A4:** commit fast-forward/CAS en dev, únicamente este documento + CHANGELOG, evidencia Git blob, Scope Lock y V5 completed/success del SHA exacto, readback PLAN/COLA/DAG/BITACORA. Cancelled/Skipped/Queued no son PASS requerido.
- **A5 independiente:** una Task Javier distinta de la autora debe inspeccionar diff, SHA, oráculos y controles reales; nunca autoaprobar. LISTO_DEV documental no autoriza QA/main/PROD.
