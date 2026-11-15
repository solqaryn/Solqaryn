# SOLQARYN V16 — C05-01.M02.05: eventos, idempotencia, correlacion y limites de design system

**Clasificacion:** R0 documental sin runtime. **Microtarea:** C05-01.M02.05, PLAN fila 732. **Autor:** PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Repositorio:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT. **Fuente:** V5 FINAL STANDARD, M01.01-06 aprobados, M02.01-04 como contratos, COLA G/O y DAG F/G vigentes.

## Proposito y alcance

Especificar envelopes, versionado, correlacion, idempotencia, privacidad, cuotas y fallo seguro para la evoluciοn FUTURA de design tokens, temas y componentes. No se instala broker, bus, outbox, dispatcher, webhook, worker, subscriber, API, base, EF migration, CSS, frontend, secretos o telemetria runtime. Ningun evento de este documento fue publicado o consumido. Los eventos no conceden roles ni eligen un tenant.

Se preservan DS01-DS14 e INV01-INV18 de M02.01, los contratos API01-08/DTO/E01-12/V01-16 de M02.02, DB01-12 y migraciones conceptuales de M02.03 y OPT01-14/V01-18 de M02.04; estos documentos son preparaciones, no ejecucion productiva. La autoridad real corresponde a JWT/membresia/RBAC de backend en cada transicion futura.

## Eventos propuestos EV01-EV12 (todos NO_IMPLEMENTADOS)

| ID | Evento hipotetico | Clave/propietario y contenido permitido |
| --- | --- | --- |
| EV01 | DesignTokenRevisionProposed | Catalogo plataforma, tokenKey y revision; nunca CSS arbitrario |
| EV02 | DesignTokenRevisionPublished | Manifest SHA versionado de DS01/DS02, solo catalogo aprobado |
| EV03 | ComponentPatternRevisionPublished | patternKey/version y referencias a tokens existentes |
| EV04 | ThemePresetRevisionApproved | presetId/version y manifest accesible verificado, no grants |
| EV05 | TenantThemeAssignmentRequested | tenant backend, actor, revision esperada e intentId; NO activa tema |
| EV06 | TenantThemeAssignmentValidated | Resultado de validaciones y estado de solicitud, no switch automatico |
| EV07 | TenantThemeAssignmentActivated | Solo tras commit CAS y permisos comprobados, nunca por entrega at-least-once |
| EV08 | TenantThemeAssignmentRejected | Codigo seguro y correlacion opaca sin revelar otro tenant |
| EV09 | TenantThemeRollbackProposed | Revision anterior verificada y motivo, ninguna restauracion SQL |
| EV10 | TenantThemeRollbackConfirmed | Requiere publicacion transaccional y audit trail ya probado |
| EV11 | TenantBrandAssetReferenceChanged | ID opaco de asset propio y clasificacion; sin URL externa libre |
| EV12 | ThemeSessionEpochInvalidated | Tenant/epoch, indica limpieza de cache y stale responses; no JWT ni PII |

Los nombres y campos son contratos FUTUROS, no topics/schemas/productos existentes ni endpoints. La pantalla no puede publicar ni reinterpretar un evento como autorizacion.

## Envelope conceptual y campos prohibidos

Envelope permitido FUTURO: `eventId` opaco, `schemaVersion` allowlisted, `eventName` tipado, `tenantId` verificado por servidor, `actorRef` minimizado, `resourceRef` propio, `assignmentRevision` monotona, `idempotencyKey` tenant-scoped, `correlationId` opaco, `causationId` opcional, `occurredUtc`, `sourceSha` y `payloadDigest` saneado. Identidades de catalogo global deben llevar scope plataforma sin mezclar datos tenant. Nunca enviar JWT, cookie, password, token, DSN, secret, PII innecesaria, CSS/JS libre, payload bruto ni informacion de B a A.

## Reglas ID01-ID14 de orden, duplicados y seguridad

1. **ID01:** la idempotencia se particiona por tenant verificado, evento/accion, recurso y version de schema.
2. **ID02:** un `eventId` repetido con mismo digest/version se procesa una sola vez, sin duplicar DS06/DS10.
3. **ID03:** mismo idempotency key con digest/payload/tenant diferente se rechaza, nunca reutiliza respuesta de B.
4. **ID04:** revision esperada CAS del assignment se verifica en transaccion real antes de cualquier cambio.
5. **ID05:** evento fuera de orden no reduce revision activa ni reactiva preset retirado.
6. **ID06:** intento DS09 previo a READY/A5 no publica DS06 ni activa branding.
7. **ID07:** revocacion de rol/membresia entre proposal y activation requiere reautorizacion backend vigente.
8. **ID08:** cambio tenant A→B invalida epoch y cache A; evento atrasado A no modifica DOM en sesion B.
9. **ID09:** delivery at-least-once no significa exactly-once; exigir constraint durable y dedupe al consumidor, con CI real futura.
10. **ID10:** timeout/fallo de transporte no se comunica como exito confirmado; consultar estado autoritativo.
11. **ID11:** payload schema desconocido, hash invalido o corrupto se pone en cuarentena segura sin DLQ ejecutable desde documento.
12. **ID12:** sin conexion a autoridad/DB/cache o bajo caida de broker futuro, se conserva estado disabled/actual, no se concede grant.
13. **ID13:** correlacion y observabilidad no registran credenciales, datos de B, CSS privado ni links a assets ajenos.
14. **ID14:** cualquier replay, compensacion o rollback conserva audit trail append-only y nunca elimina hechos de otra empresa.

Los limites numericos deben medirse y aprobarse en una futura implementacion: cap por tamano de payload, profundidad de objetos, frecuencia por tenant, cardinalidad de etiquetas, retencion de dedupe e intervalos de reintento/backoff. **NO_MEDIDO** en este documento; ninguna cifra aqui habilita una capacidad runtime. En caso de saturacion, fallar cerrado y preservar la revision vigente.

## Casos futuros F01-F10 (positivos y negativos sin ejecutar)

| Caso | Positivo FUTURO | Negativo FUTURO |
| --- | --- | --- |
| F01 | Propuesta en A registrada una vez | Replay con digest diferente bloqueado |
| F02 | Revision nueva sustituye la antigua despues de CAS | Evento stale no retrocede revision |
| F03 | Autorizacion RBAC actual confirmada | Actor revocado tras propuesta no activa tema |
| F04 | Tenant A aplica su preset saneado | EventId/assignment de B no se acepta en A |
| F05 | Asset de A aprobado y referenciado | URL externa de B o payload script rechazada |
| F06 | Epoch B descarta respuesta de A | Cache obsoleta no reinyecta colores/logo A |
| F07 | Publicacion valida preserva DS10 audit trail | Timeout no muestra exito antes de commit real |
| F08 | Evento de rollback con revision previa verificada | Intent de rollback desconocido/forzado denegado |
| F09 | Proveedor reintenta un evento idempotentemente | Duplicado no dispara doble revision/auditoria |
| F10 | Transporte falla y se conserva estado seguro | Ausencia de broker/cache no concede permisos |

**NO_EJECUTADOS:** ningun evento real, HTTP, MySQL 8.4, Angular/Playwright, worker/outbox ni integracion; los oraculos son condiciones para pruebas futuras. No confundir codigo de ejemplo con un sistema de entrega habilitado.

## A0-A5, impacto y rollback

**A0:** diff limitado al documento de esta microtarea y seccion autocontenida CHANGELOG, fuentes M02 inspeccionadas; ninguna modificacion de Domain/Application/Infrastructure/API/Angular/config o DB. **A1:** consistencia estatica de EV01-12, ID01-14 y F01-10 con casos positivos y negativos, todos futuros. **A2:** aislamiento A/B, JWT/RBAC/revocacion, idempotencia, privacidad/PII, schema deny-by-default y cambio de epoch como contratos sin prueba runtime. **A3:** builds, DB, Playwright y proveedores NO_APLICA a Markdown-only; C05-01 M03 y otras etapas implementaran y probaran los eventos si son autorizados. **A4:** Git dev fast-forward CAS, Scope Lock STRICT, verificador V5 de CHANGELOG completed/success exact SHA y readback PLAN/COLA/DAG/BITACORA. **A5:** solo otra de las cinco Tasks Javier diferente de la autora puede revisar y aprobar; LISTO de documento no abre QA/main/PROD.

Rollback de este changeset = git revert normal en dev y evento compensatorio; no modificar filas BITACORA historicas, no parar ejecutores ni instalar webhooks. Seguridad y derechos de usuarios sin cambio.
