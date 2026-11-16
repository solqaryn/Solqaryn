# SOLQARYN V17 — C09-03.M02.02: contratos API, DTO, errores y compatibilidad Social Hub
**Tipo:** R0 documental, propuesta de interfaces futuras; no se han creado endpoints, DTO .NET, tablas, jobs, proveedores ni despliegues. **Scope:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.
**Autor técnico:** PRIMARY DEV05 Javier, `6adc2e55f47c81919f6374e561687076`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO!A1377, COLA!A1377, DAG_TECNICO!A1338, contrato `C09-03.M02.01` y cierre M01 aprobado. La publicación es propuesta técnica: sus nombres son ilustrativos y **no representan rutas API existentes**.

## A0. Objetivo, límites e interfaces
Definir cómo futuras capas API/Application del Social Hub podrían expresar campañas, contenido, consentimiento, segmentos, calendario y métricas respetando el modelo lógico E01–E16 y los invariantes I01–I18 de M02.01. Las fronteras actuales permanecen: API autentica y compone, Application valida el caso de uso, Domain mantiene reglas y Infrastructure persiste/integra **solo en otra microtarea autorizada**. Nada en este documento habilita mensajes, activaciones de proveedores o migraciones.

El contexto de autorización (EmpresaId, usuario, roles, permisos y sucursales autorizadas) **siempre lo resuelve el backend** desde sesión vigente y fuente confiable; nunca se acepta del cuerpo o de un parámetro como decisión de seguridad. Un `CampaignId` o `MediaId` de otra empresa devuelve una denegación uniforme que no confirma su existencia. La UI no certifica permisos. La pertenencia debe reevaluarse en cada mutación y de nuevo antes de cualquier efecto externo.

## Catálogo de doce operaciones conceptuales (NO implementadas)
| Operación futura | Entrada/salida propuesta | Validación obligatoria |
| --- | --- | --- |
| OP01 ListarBorradores | FiltroCampania -> PáginaCampanias | Empresa actual, permiso lectura y filtros tipados |
| OP02 ConsultarCampania | CampaignId -> DetalleCampania | Ownership activo + versión sin exposición BOLA |
| OP03 CrearBorrador | CrearCampania -> DetalleCampania | Permiso crear, canal allowlist, sanitización |
| OP04 ActualizarPieza | CampaignId + PiezaContenido + IfMatch -> DetalleCampania | Permiso editar, medio propio, CAS versión |
| OP05 AprobarCampania | CampaignId + Aprobacion -> EstadoCampania | Rol independiente, consentimiento no eludible |
| OP06 DefinirSegmento | SegmentoCriterio -> SegmentoEstado | Sin SQL/JS arbitrario, límites de cardinalidad |
| OP07 RegistrarConsentimiento | ConsentimientoRegistro -> EstadoConsentimiento | Evidencia y finalidad/canal, auditoría minimizada |
| OP08 AplicarOptOut | OptOutRegistro -> EstadoConsentimiento | Prioridad inmediata, revocación irreversible por reintento |
| OP09 ProgramarCampania | AgendaCampania -> EstadoCampania | UTC, expiración, revisión, aprobación previa |
| OP10 CancelarCampania | CampaignId + IfMatch -> EstadoCampania | Permiso cancelar; detener intenciones pendientes |
| OP11 SolicitarPublicacion | PublicacionIntencion -> EstadoPublicacion | Consentimiento vigente, opt-out, idempotencia, cuota |
| OP12 ConsultarMetricas | FiltroMetrica -> MétricaAgregada | Empresa/RBAC, intervalo y tamaño mínimo de cohorte |

Estas operaciones son **nombres de contrato**, no URLs HTTP. No se obliga a abrir nuevas rutas ni se altera la API hoy publicada; su exposición requiere revisión de arquitectura, contratos reales y CI de seguridad en M03/M04.

## Doce DTO conceptuales y campos mínimos
| DTO propuesto | Campos (tipos semánticos, no clases ejecutables) | Restricciones |
| --- | --- | --- |
| D01 FiltroCampania | estado enum, desdeUtc?, hastaUtc?, cursor opaco, limite | paginación acotada, no EmpresaId confiable |
| D02 PáginaCampanias | items resumen[], cursorSiguiente?, total? | nunca IDs o contactos de otra empresa |
| D03 DetalleCampania | campaignId, canal, versión, estado, contentRef, fechaUtc? | sin credenciales, identidad del owner interna |
| D04 CrearCampania | título limitado, canal enum, finalidad, contentRef? | sanitizar texto y media de empresa vigente |
| D05 PiezaContenido | contentId, texto limitado, mediaId?, versión | XSS/SSRF/ownership validados en servidor |
| D06 Aprobacion | campaignId, versiónEsperada, motivo acotado | exige rol y separación de responsabilidades |
| D07 SegmentoCriterio | segmentoId?, filtros tipados allowlist[], versión | prohibido SQL, expresiones JS y segmentos B |
| D08 SegmentoEstado | segmentoId, snapshotRevision, conteoAproximado? | umbral privacidad y consentimiento al envío |
| D09 ConsentimientoRegistro | subjectRef opaco, canal, finalidad, estado, proofRef | evidencia verificable y marcas UTC, no PII en logs |
| D10 OptOutRegistro | subjectRef opaco, canal, finalidad?, causa enum | revocación prevalece sobre calendarización y caché |
| D11 AgendaCampania | campaignId, ejecutarUtc, expiraUtc, versiónEsperada | UTC real, rango válido, cancelación persistente |
| D12 PublicacionIntencion | campaignId, channelId, destinoRef opaco, claveIdempotencia | request digest independiente; devolver EstadoPublicacion opaco |

`EstadoCampania`, `EstadoConsentimiento`, `EstadoPublicacion`, `MétricaAgregada` y `FiltroMetrica` son vistas/resultados conceptuales de las operaciones; sus formatos reales deberán diseñarse sin secretos, PII superflua ni identificadores de otro tenant. La tabla no declara 12 clases existentes ni estructuras físicas.

## Errores tipados y resultado fail-closed propuestos
| Código de contrato | Estado HTTP orientativo | Conducta de seguridad |
| --- | --- | --- |
| E01 INVALID_INPUT | 400 | campos/tipos/longitudes inválidos, mensaje genérico |
| E02 AUTH_REQUIRED | 401 | sesión inexistente, expirada o revocada |
| E03 PERMISSION_DENIED | 403 | rol o permiso insuficiente sin datos |
| E04 RESOURCE_UNAVAILABLE | 404 | ID ajeno, ausente o de otro tenant indistinguibles |
| E05 OWNERSHIP_UNKNOWN | 403 | EmpresaId/ownership heredado NULL: denegar |
| E06 VERSION_CONFLICT | 409 | IfMatch no coincide, no sobrescribir |
| E07 CONSENT_REQUIRED | 409 | finalidad/canal no autorizados |
| E08 OPT_OUT_ACTIVE | 409 | baja o revocación superior al snapshot |
| E09 CONTENT_UNSAFE | 422 | media, texto, URL o referencia no segura |
| E10 SEGMENT_INVALID | 422 | filtro fuera del allowlist o límite |
| E11 SCHEDULE_INVALID | 422 | UTC/expiración/versión inválidos |
| E12 IDEMPOTENCY_CONFLICT | 409 | misma clave semántica, payload digest diferente |
| E13 RATE_LIMITED | 429 | cuota o ventana excedida, reintento controlado |
| E14 PROVIDER_DISABLED | 503 | proveedor no aprobado o deshabilitado |
| E15 TRANSIENT_FAILURE | 503 | error interno sin mostrar secretos; no duplicar efecto |
| E16 CANCELLED_OR_REVOKED | 409 | cancelación o revocación previa al efecto |

Los códigos HTTP son **propuesta para posterior implementación**, no prueba de que existen esos responses hoy. Correlación segura: errorCode estable, traceId opaco, timestamp UTC y texto sanitizado; no retornar token, contacto, stacktrace, SQL, SubjectRef externo ni distinción de existencia entre empresas.

## Compatibilidad, concurrencia, idempotencia y recuperación
- **Versionado:** introducir DTO aditivo/v1 y propiedades opcionales sólo bajo compatibilidad comprobada; no cambiar endpoints actuales ni serializar `EmpresaId` como fuente autorizante. `IfMatch`/versión impide sobrescrituras concurrentes. Valores NULL heredados de ownership no se infieren automáticamente.
- **Idempotencia de efectos:** para futura OP11, identidad estable `(EmpresaId, campaignId, channelId, destinoRef, claveIdempotencia)`; guardar aparte digest canónico del request. Misma clave+digest reenvía estado previo; misma clave+digest diferente produce E12 **sin publicar**. Un índice físico y la transacción/outbox real son trabajo separado M02.03/M03; no declararlos implementados aquí.
- **Consentimiento:** comprobación de finalidad+canal+destino, opt-out, autorización de usuario, versión de campaña y validez de proveedor antes de cada efecto externo. Snapshot anterior nunca prevalece sobre revocación. Si falta una comprobación, negar.
- **Errores de proveedor:** timeout, 429, interrupción o respuesta ambigua no autorizan reintento ciego. Estado indeterminado requiere conciliación con clave estable, sin duplicados ni PII en logs.
- **Cancelación/reversión:** cancelar intenciones pendientes; conservar evidencia de consentimiento, opt-out y auditoría append-only. No borrar contactos ni fingir retirada de mensajes ya entregados.
- **Compatibilidad de consumidores:** frontend debe tratar 401/403/404/409/429/503 sin revelar información, y limitar reintentos. La API actual, schemas EF, MySQL, JWT real y proveedores no se alteran.

## Doce pares de aceptación futura POS/NEG: 0/12 ejecutados
| Caso | Positivo a implementar más adelante | Negativo que debe detectar un defecto |
| --- | --- | --- |
| T01 | Usuario A lista solo sus campañas | B solicita CampaignId A y no observa existencia |
| T02 | Editor A crea borrador propio | usuario sin rol intenta crear o aprobar |
| T03 | Update de pieza usa versión vigente | versión obsoleta rechazada sin pisar datos |
| T04 | Medio activo A se vincula a su pieza | MediaId B, URL SSRF o HTML peligroso |
| T05 | Segmento tipado respeta límites | filtro SQL/JS o cardinalidad privada |
| T06 | Consentimiento por canal/finalidad se acredita | ausencia de prueba o canal distinto |
| T07 | Opt-out inmediato bloquea envío | snapshot obsoleto intenta reactivar contacto |
| T08 | UTC vigente permite programación | fecha vencida, versión antigua o revocada |
| T09 | Misma clave+digest devuelve misma intención | misma clave y digest distinto es conflicto |
| T10 | Aprobación y cuota permiten intención | usuario revocado, cuota agotada o proveedor off |
| T11 | Retry ambiguo conserva un resultado | timeout paralelo produce segundo envío |
| T12 | Métrica autorizada es agregada | cohortes mínimas o consulta B revela PII |

**Estado de pruebas:** 0/12 pares implementados o ejecutados, 0 endpoints probados, 0 pruebas MySQL 8.4/EF, 0 HTTP/JWT, 0 Playwright y 0 integraciones reales atribuibles a **esta** microtarea R0. Son criterios para posteriores fases R1/R2; ningún build verde ajeno cambia esa realidad.

## Evidencia A0–A5 y rollback
A0: verificar que sólo cambien este documento y una sección autocontenida en `CHANGELOG_AI.md`; prerequisitos M01, PLAN/COLA/DAG, Scope Lock y V17 antes de claim. A1: validar correspondencia de OP01–OP12, D01–D12, E01–E16, T01–T12 y casos negativos en revisión estática; no llamar PASS runtime a propuestas. A2: obligaciones tenant A/B, JWT/RBAC, BOLA, consentimiento, opt-out, XSS/SSRF, replay y concurrencia **declaradas**, no implementadas. A3: tests de .NET, UI, MySQL, proveedores y HTTP NO_APLICA a un diff exclusivamente Markdown, por causa técnica. A4: publicación en dev mediante fast-forward con SHA exacto, checks obligatorios Scope Lock y procedencia V5 en `completed/success`, BITACORA y PLAN/COLA/DAG readback. A5: sólo otra Task Javier distinta de DEV05 puede aprobar o rechazar mediante revisión independiente de documentos, criterios y evidencia. No habilitar runtime, QA/main/PROD ni secretos. Rollback mediante `git revert` normal y readback; no reescribir historial, borrar BITACORA ni tocar datos reales.
