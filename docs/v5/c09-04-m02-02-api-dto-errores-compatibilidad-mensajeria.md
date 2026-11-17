# SOLQARYN V19 — C09-04.M02.02: contratos API/DTO, errores, validación y compatibilidad multicanal

**ID:** C09-04.M02.02 · PLAN_MAESTRO fila 1413 · **Autor:** PRIMARY DEV05, TASK_ID `6adc2e55f47c81919f6374e561687076` · **Proyecto:** `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

**Alcance:** propuesta contractual M02 **R0 documental** sobre dominio de riesgo **R2** (JWT/RBAC, tenant A/B, consentimiento, proveedores y mensajería comercial). No implementa endpoints, DTO, base de datos, webhooks, workers, eventos, SMTP/WhatsApp ni canales de campañas. No autentica ni envía solicitudes. M02.01 define las 16 entidades conceptuales y 20 invariantes; M01.01–06 preserva NR01–NR14, P01–P16/N01–N16 y G01–G09.

## A0 — API existentes, exactamente como declara el código de dev

| Fuente / blob | Superficie efectivamente declarada | Alcance y cautela |
| --- | --- | --- |
| `backend/src/API/Controllers/MensajesOutboxController.cs` — `b6d3fa67c9ecdb35072fa59dad1e71c1e351be81` | `POST api/outbox/tenants/{empresaId:int}` con `[Authorize]`, permiso Configuración/Crear, `Idempotency-Key`, `RegistrarMensajeOutboxRequest` | Verifica empresa de ruta contra `TenantPermissionContext.RequireEmpresaIdAsync`; acepta intención en outbox, no envío comercial |
| `backend/src/Application/Interfaces/IMensajeOutboxService.cs` — `1f149f093a38e11b71f759864733a0533c6b5e20` | Record `RegistrarMensajeOutboxRequest(TipoEvento, PayloadJson, TipoAgregado?, IdAgregado?, CorrelationId?)` | Request existente, no DTO de campaña/consentimiento |
| `backend/src/API/Controllers/EmailEmpresarialController.cs` — `e7acfb132c9aedf0e22be9cb3acd6e86e4718833` | `POST email-empresarial/tenants/{empresaId}`, `GET /{mensajeId:guid}`, `GET` paginado | Protege tenant y permisos Configuración Crear/Ver. El proxy navegador puede prefijar /api, pero el controlador backend no; no asumir contrato público adicional |
| `backend/src/API/Controllers/WhatsAppController.cs` — `efc1e79625e62925efe7d1e47040eae91fb12dc6` | `POST whatsapp/webhook` anónimo con cabecera `X-WhatsApp-Verify-Token`; `GET whatsapp/publico`; `POST whatsapp/iniciar-whatsapp` bajo auth/permiso | El webhook **actual** compara token y acepta, **NO prueba HMAC ni anti-replay**; iniciar responde CONFIGURADA_SIN_SESION con QR nulo cuando procede, no crea sesión real |
| `backend/src/Application/Services/MensajeOutboxService.cs` — `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Valida membresía server-side e idempotencia en el stage de intención | Una consulta/DTO no sustituye índice único ni test de carrera real |

**Errores observados por inspección estática del controlador outbox:** ausencia de `Idempotency-Key` → ProblemDetails 400 código `OUTBOX_IDEMPOTENCY_KEY_REQUIRED`; ruta tenant distinta → 403 `OUTBOX_TENANT_CONTEXT_MISMATCH`; membresía insuficiente → 403 `OUTBOX_TENANT_FORBIDDEN`; argumento inválido → 400 `OUTBOX_INVALID_REQUEST`; clave conflictiva → 409 `OUTBOX_IDEMPOTENCY_CONFLICT`; colisión de índice → 409 `OUTBOX_IDEMPOTENCY_RACE`. Éstos son comportamientos **declarados en código**, no respuestas HTTP ejecutadas en esta entrega. La ruta utiliza `application/problem+json` para los errores tratados y `ApiResponse<MensajeOutboxRegistroResponse>` al aceptar (EventoId, EmpresaId, Estado).

**Compatibilidad real existente:** el email empresarial también declara errores `EMAIL_TENANT_CONTEXT_MISMATCH`, `EMAIL_IDEMPOTENCY_KEY_REQUIRED`, `EMAIL_IDEMPOTENCY_CONFLICT`, `EMAIL_TENANT_FORBIDDEN`, `EMAIL_INVALID_REQUEST`, `EMAIL_IDEMPOTENCY_RACE`, `EMAIL_NOT_FOUND`, `EMAIL_INVALID_QUERY` y GET tenant-scoped. No cambiar códigos HTTP, formatos o prefix routing bajo este changeset.

## Contrato propuesto de API comercial: interfaces futuras I01–I12 (NO_IMPLEMENTADAS)

| ID | Solicitud lógica futura / campos permitidos | Respuesta y obligación de rechazo |
| --- | --- | --- |
| I01 | Identidad actor + tenant verificado exclusivamente server-side | Denegar A→B aunque empresaId de URL/DTO parezca válido |
| I02 | `Finalidad` tipada: transaccional o comercial según política posterior | Prohibir reutilización de opt-in de finalidad diferente |
| I03 | `Canal` allowlist y selección explícita (nunca auto-fallback) | Rechazar canal no habilitado o sin consentimiento |
| I04 | `DestinatarioRef` opaco + pertenencia/sucursal | Bloquear contacto legado NULL, empresa ajena, id inexistente |
| I05 | `TemplateRef` y `TemplateVersion` acotadas al tenant | Prohibir interpolación arbitraria, plantilla ajena/PII B |
| I06 | `IdempotencyKey` obligatoria + huella de solicitud normalizada | Mismo scope+payload devuelve estado previo; payload distinto falla |
| I07 | `CorrelationId` opaco | Diagnóstico solamente; nunca grant ni token |
| I08 | `ConsentEvidenceRef` o resultado de validación server-side | Consentimiento ausente, revocado o vencido impide marketing |
| I09 | `IntentId` y `DeliveryState` normalizados | Distinguir staged, accepted-provider, delivered, bounced, dead-letter |
| I10 | `ProviderRef` y `ProviderMessageRef` opacos | Ningún token/cuerpo/credencial en JSON, trazas o error |
| I11 | Firma+timestamp+nonce+provider para webhook futuro | **HMAC y anti-replay futuros**, no atribuirlos al webhook actual |
| I12 | Consulta, supresión, auditoría tenant-scoped con filtros y paginación | Denegar resultados B, opt-out respetado y sin payload sensible |

Los nombres I01–I12 son **valores conceptuales, NO rutas REST, esquemas OpenAPI ni DTO instanciados**. Ningún endpoint de campaña nuevo queda aprobado y los `StatusCodes` de estas interfaces propuestas están **POR_DEFINIR**; no inventar 200/403/409 para rutas aún inexistentes.

## Validador contractual y matriz de error para implementar más adelante

**Servidor:** resolver actor y membresía activa, tenant autorizado, recurso del mismo owner, permiso y finalidad, consentimiento por destinatario/canal, idempotencia tenant-scoped, plantilla/versiones allowlisted, exclusión durable y clasificación de estado; bloquear si falta alguno. Nunca confiar en JWT desactualizado, guard Angular, `X-Empresa-Id` o `empresaId` de body sin revalidación.

**Compatibilidad:** conservar rutas, prefijos, `Idempotency-Key`, `ProblemDetails` y códigos existentes. Introducir extensiones sólo aditivas y versionadas en microtareas autorizadas; clientes antiguos no deben recibir una respuesta que revele otros tenants. Un `CorrelationId` no cambia autorización. Campos desconocidos/contradictorios y secretos en parámetros se rechazarán según contrato que se diseñe y pruebe; no alterar parsers existentes ahora.

**Problem envelope futuro** (no serializado en este scope): `code` estable no sensible, `traceId` opaco, descripción genérica, HTTP según endpoint real; nunca stack, proveedor interno, DSN, payload, token ni PII. Política de cancelación no convierte excepciones en un envío efectuado. Código de HMAC y replay dependerá del proveedor efectivo en otra unidad, no del token actual.

## Verificación futura obligatoria (P01–P09 / N01–N14)

**Positivos FUTUROS_NO_EJECUTADOS:** P01 actor tenant A autorizado; P02 mismo idempotency key/payload sin duplicación; P03 consentimiento por finalidad/canal; P04 destinatario/sucursal válidos; P05 template sin PII ajena; P06 consulta con paginación y tenant; P07 envío simulado sin secreto; P08 aceptado-proveedor separado de entregado; P09 opt-out preserva auditoría legítima.

**Negativos FUTUROS_NO_EJECUTADOS:** N01 A→B; N02 rol/membresía revocada; N03 contacto legacy NULL; N04 opt-out; N05 finalidad cambiada; N06 idempotency key con payload distinto; N07 dos escritores; N08 template de B; N09 canal fallback no autorizado; N10 firma inválida; N11 replay; N12 secretos/log PII; N13 proveedor indisponible; N14 exportación cross-tenant. Ningún caso fue ejecutado. Acreditar después con .NET/xUnit, HTTP/JWT/RBAC A/B, MySQL 8.4 efímero y proveedor simulado/real autorizado según cambio. Los métodos actuales NO constituyen garantía de consentimiento comercial o HMAC.

## Gates, A0–A5 y rollback

- **A0:** sólo este archivo `docs/v5/` y sección autocontenida `CHANGELOG_AI.md`; sin alteración de API, código o dependencias.
- **A1:** trazabilidad static/source exacta de superficies existentes y propuestas; matrices P/N futuras no pasan como tests.
- **A2:** tenancia, consentimiento, JWT/RBAC, PII, idempotencia, firma y replay como condiciones fail-closed de contrato, no certificación runtime.
- **A3:** MySQL, HTTP, Angular, SMTP y WhatsApp reales **NO_APLICA al changeset documental**.
- **A4:** commit fast-forward en dev, check `verify-solqaryn-project-scope` y V5 provenance **completed/success en SHA exacto**; diff, blobs y PLAN/COLA/DAG/BITACORA readback.
- **A5:** revisión independiente por otra Task Javier, distinta de DEV05, sin autopase; LISTO no significa A5_PASS.
- **DAG:** sólo fórmulas institucionales determinan sucesores; no forzar SI, no habilitar worker, webhook, migración, secrets, QA/main/PROD.
- **Rollback:** `git revert` normal selectivo en dev tras comprobar HEAD/concurrencia; BITACORA intacta y datos legítimos preservados.

**Decisiones abiertas:** contrato definitivo de consentimiento/finalidad, proveedor HMAC, retención, idempotencia DB y claims, versión de DTO y códigos nuevos, límites de paginación y cuotas. Resolver con evidencia causal en sus propias microtareas sin inventar autorización.
