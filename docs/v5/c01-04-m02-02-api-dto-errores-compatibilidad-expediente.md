# SOLQARYN V5 — C01-04.M02.02: contratos API, DTO, errores y compatibilidad del expediente universal

**Microtarea:** `C01-04.M02.02` · **Autor:** PRIMARY DEV 04 — JAVIER_CHATGPT, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09` · **Fecha:** 2026-11-14 (America/Tegucigalpa) · **Rama:** `solqaryn/Solqaryn@dev` · **PROJECT_SCOPE_LOCK=STRICT**.

**Naturaleza:** contrato exclusivamente **DOCUMENTAL/PROPUESTO** de operaciones, tipos, errores y compatibilidad para expediente y seguimiento universal futuro. No agrega ni modifica controlador, endpoint, DTO compilado, autorización, migración, EF, Angular, almacenamiento, worker, evento, Task o producto real. Las pruebas descritas son **FUTUROS_NO_EJECUTADOS**. No se autoriza QA/main/PROD ni se certifica aislamiento multiempresa o cero P0/P1 por este archivo.

## 1. Autoridad, prerequisitos y límites de superficie actual

Fuente única: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md`, Scope Lock y `docs/AUTOMATION_AUTHORITY.md` del HEAD dev.

Al reclamar este ID: PLAN_MAESTRO fila 261 `H=PENDIENTE/I` sin owner; COLA G/O=SI; DAG exacto `C01-04.M02.02` con F=VALIDADO/G=SI. El gate explícito del DAG es `C01-04.M01.01–06+A5`, cuyos artefactos figuran LISTO con revisión independiente; **M02.01 LISTO** ya existe, pero su A5 todavía es independiente y pendiente, por lo que nunca se usa como habilitación indebida de una fase técnica. M02.02 es documental independiente y no presupone implementación ni liberación de M02.06/M03.

| Documento Git observado | Blob de referencia | Alcance |
| --- | --- | --- |
| `c01-04-m01-02-actores-entradas-salidas.md` | `29184fdc66a5ccec8998c85851f1c505aa02ca80` | Siete clases de actor y EXP-IO01–10 |
| `c01-04-m01-05-aceptacion-expediente-timeline.md` | `1cad256ee30742ccc0384fe00eaff586a97add11` | AC01–AC18 y pruebas futuras |
| `c01-04-m01-06-bloqueo-rollback-liberacion.md` | `a377ec78cd6a38dee45a6cd874cb66abd2df4dd4` | Gate y riesgos de tenancy/propiedad/rollback |
| `c01-04-m02-01-modelo-entidades-ownership-invariantes-expediente.md` | `bd5c74657b43c5a0ccbabb308ccba08234eee495` | EX01–EX16, IN01–IN18, EV01–EV12 conceptuales |

El código real ya tiene `AuditoriaController`, `AuditoriaDto`, `RegistroAuditoria`, `CompraDocumento`, `ICompraDocumentoStorageService` y `ContextoTenantActual`; su existencia **no implica** que existan endpoints de expediente universal. En particular, la investigación pendiente de un posible filtro de tenant ausente en `AuditoriaRepository.GetFilteredAsync` es una brecha **no demostrada como vulnerabilidad**, y bloquea cualquier reutilización sin test A/B específico. Ninguna ruta de este documento es una URL real publicada.

## 2. Catálogo API01–API12 de operaciones propuestas, NO_IMPLEMENTADAS

| ID | Operación lógica futura | Entrada tipada y propietario confiable | Salida/denegación obligatoria |
| --- | --- | --- | --- |
| API01 | `ResolveExpedienteScope` | `resourceType` allowlisted y `resourceRef` opaco; tenant desde backend | Scope propietario o error genérico, nunca referencia B |
| API02 | `ReadTimelinePage` | `scopeId`, cursor opaco y límite, principal autenticado | Página ordenada UTC+ID sólo del owner permitido |
| API03 | `ReadTimelineEntry` | `entryRef` bajo scope propio verificado | Hecho y procedencia autorizados o 404 homogéneo |
| API04 | `AddObservation` | `expectedVersion`, texto saneado, actor desde servidor | Nueva observación sólo con grant y readback |
| API05 | `ProposeFormalDecision` | Código de decisión allowlisted, motivo y versión | Acción sujeta a RBAC; no aprobar por nota libre |
| API06 | `ReviewFormalDecision` | Revisor con permiso efectivo, estado y expectedVersion | Dictamen independiente cuando política lo exija |
| API07 | `ListAttachmentRefs` | Expediente + permiso de lectura verificado | Metadatos propios sin URL externa sensible |
| API08 | `AuthorizeAttachmentRead` | Adjunto opaco, owner, TTL y firma aplicable | Referencia temporal y restringida o denegación |
| API09 | `ListExpedienteRevisions` | Scope validado, cursor ligado a recurso y tenant | Versiones ordenadas, sin pérdida de historial |
| API10 | `GetExpedienteRevisionDiff` | Dos versiones del mismo scope | Diff redactado y autorizado; nunca PII cruzada |
| API11 | `LookupIdempotencyReceipt` | Key ligada a actor, tenant, operación y payload | Mismo resultado para mismo efecto; conflicto si difiere |
| API12 | `ReadAuditCorrelation` | Correlación opaca y permiso de auditoría | Sólo evidencia minimizada del ámbito propio |

Son nombres **lógicos, no rutas HTTP**, y ningún catálogo concede permisos implícitos. Para implementar una ruta sería necesario nuevo scope M03 con API backend .NET 10, servicios Application/Domain, EF/MySQL 8.4 y pruebas JWT/RBAC/tenant A/B; Angular 22.2.1 sólo presenta respuestas autorizadas. El plan comercial y un flag de UI no sustituyen un grant.

## 3. DTO conceptuales y reglas de interoperabilidad

`ExpedienteScopeRequestV1` propondría `resourceType`, `resourceRef` y `clientRequestId` opacos, nunca `EmpresaId` de autoridad del cliente. `TimelinePageV1` propondría `items`, `nextCursor`, `readRevision` y `observedAtUtc`; cada ítem tendría `entryId`, `kind`, `occurredAtUtc`, `sourceRef`, `actorRef` saneado, `version` y `redactionLevel`. `ObservationCommandV1` incluiría texto limitado, `expectedVersion`, `idempotencyKey` y correlación; el servidor completa actor/tenant/permiso.

`FormalDecisionCommandV1` requiere `decisionCode`, `reason`, `expectedVersion`, una acción RBAC específica y una política que distinga propuesta/revisión/aprobación. `AttachmentRefV1` sólo devuelve identificador opaco, tipo/tamaño autorizados y estado, no URL privada persistente. `RevisionDiffV1` expone campos permitidos y nunca secrets/tokens. Esos nombres son **diseño hipotético**, no clases en el repositorio.

| Política de compatibilidad | Respuesta a variación o consumidor antiguo |
| --- | --- |
| Cambios aditivos V1 | Sólo mediante contrato versionado y pruebas; no sustituir AuditoriaController |
| Tenant/claims | Resolver en backend desde membresía efectiva; jamás confiar en payload A/B |
| Cursor | Vincular scope, filtro, orden, versión y audiencia; rechazar cursor ajeno |
| Escritura | CAS de persistencia real; expectedVersion obsoleto = 409 sin mutación |
| Idempotencia | Misma key/payload/tenant = mismo resultado; payload distinto = conflicto |
| Enlaces de adjuntos | Revalidar owner y expiración al servir; no aceptar publicId legacy por sí solo |
| Consentimiento y minimización | Redactar PII y proteger evidencia original según audiencia |
| Errores y fallback | No caer a ámbito global si membership/storage falla; fail-closed |

## 4. Semántica de errores E01–E14 propuesta

| ID | Código futuro orientativo | Condición | Efecto seguro |
| --- | --- | --- | --- |
| E01 | 400 | Campo desconocido/tipo incorrecto/limit excedido | Rechazo antes de I/O |
| E02 | 401 | JWT inválido, firma errónea, expirado | Sin metadatos ni stack |
| E03 | 403 | RBAC ausente o membresía revocada | Denegación independiente de UI |
| E04 | 404 | Recurso B, sin owner o inexistente | Respuesta uniforme sin enumeración |
| E05 | 409 | Versión de observación/decisión obsoleta | Ninguna escritura parcial |
| E06 | 409 | IdempotencyKey igual con payload diferente | Ningún duplicado |
| E07 | 422 | ResourceType/decisionCode no allowlisted | No crear recurso dinámicamente |
| E08 | 400 | Cursor de otra empresa/filtro o manipulado | Sin resultados |
| E09 | 403 | Adjuntos con dueño ajeno o TTL vencido | No bytes/metadatos B |
| E10 | 413 | Contenido o adjunto excede límites | No almacenar carga |
| E11 | 429 | Cuota por actor y tenant | No afectar arbitrariamente empresa vecina |
| E12 | 503 | Servicio de autorización no disponible | Fail-closed, nada "pendiente aprobado" |
| E13 | 409 | Timeline ya registra evento origen | Reintento no duplica hecho |
| E14 | 422 | Intento de usar este diseño como PASS CI/A5/PROD | NO_GO de promoción |

Los números de estado son *propuestas*, no observaciones de peticiones actuales. No se exponen cabeceras JWT, secretos, SQL, PII ni información de tenant B en errores.

## 5. Oráculos T01–T14 positivos/negativos — FUTUROS_NO_EJECUTADOS

| Caso | Positivo esperado futuro | Negativo verificable |
| --- | --- | --- |
| T01 | Miembro A consulta timeline de recurso A | A obtiene contenido B por ID/slug |
| T02 | Orden UTC+ID da paginación estable | Timestamps iguales causan huecos/duplicados |
| T03 | Observación autorizada conserva actor y versión | Token revocado guarda texto |
| T04 | Decisión formal exige grant y revisión | Nota libre se convierte en decisión |
| T05 | Cursor firmado pertenece a scope A | Cursor de B se reutiliza en A |
| T06 | Adjunto propio con autorización/TTL se descarga | PublicId/URL de B filtra bytes |
| T07 | Reintento idéntico produce único efecto | Payload distinto con misma key pisa estado |
| T08 | Dos writers con version anterior generan conflicto | Ambos se creen ganadores |
| T09 | UI A→B descarta respuesta A tardía | Datos de A visibles en pantalla B |
| T10 | HTML malicioso aparece escapado | XSS inyecta contenido ejecutable |
| T11 | Error autorizado incluye correlación opaca | Logs/DTO contienen JWT o PII |
| T12 | AuditoriaController conserva roles originales | Nuevo contrato elude permiso existente |
| T13 | SHA+CI pertinente completed/success se acredita | Cancelled/skipped/pending se cuenta PASS |
| T14 | Reviewer A5 distinto conserva autor original | Autor hace auto-A5/promueve PROD |

Los 28 resultados propuestos no fueron ejecutados, y no acreditan .NET, Angular, Playwright, MySQL ni pentest. La implementación posterior requiere fixtures sintéticos A/B y pruebas positivas y negativas sobre código real.

## 6. Impacto, A0–A5 y rollback

**A0:** fuentes M01.02/M01.05/M01.06 y M02.01 exactas; catálogo API01–12, E01–14, T01–14, no-secrets, diff documental con CHANGELOG autocontenido. **A1:** casos documentales coherentes, tests dinámicos expresamente FUTUROS_NO_EJECUTADOS. **A2:** JWT, RBAC, tenant A/B, autorización/ownership de adjuntos, PII y deny-by-default no modificados. **A3:** conciliación PLAN/COLA/DAG/BITACORA, revisión de contratos existentes como referencias; .NET/API, Angular/Node, EF/MySQL/Playwright son `NO_APLICA_DOCUMENTAL` si diff sólo dos Markdown. **A4:** publicación Git por fast-forward/expected HEAD y readbacks, Scope Lock + V5 `completed/success` exact SHA. **A5:** pendiente para distinta Task Javier; no concesión QA/main/PROD.

**Rollback:** `git revert` normal del commit documental, readback de SHA/CI y BITACORA nueva, sin force-push/reset/alteración histórica. M02.03–05 son independientes según DAG actual y M02.06 terminal requiere sus propias A5, nunca por este archivo.
