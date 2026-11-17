# SOLQARYN V19 — C09-04.M02.06: dependencias técnicas y contrato de salida M02

**Identidad y alcance:** C09-04.M02.06 · PLAN_MAESTRO fila 1417 · autor PRIMARY DEV03 JAVIER (TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`) · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT` · fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`).

**Clasificación:** R0 por changeset exclusivamente documental; el dominio futuro es R2 por tenant, consentimiento, concurrencia, integraciones, JWT/RBAC y MySQL. Esta entrega **NO** agrega ni activa entidades, API, workers, webhooks, flujos SMTP/WhatsApp, callbacks, eventos, tareas programadas, colas, proveedores, feature flags, base de datos, migraciones, claves o permisos. Ninguna salida documental autoriza QA, main, producción o un envío.

## A0 — Línea base verificable de M02 y relación causal

Las cinco entradas siguientes constaban como **LISTO** en PLAN_MAESTRO y recibieron **A5 independiente por otro TASK_ID Javier**, previo a este CLAIM. Los commits indicados son los orígenes documentales de cada contrato, no certificaciones de código de integración.

| ID / fila | Documento publicado bajo `docs/v5/` | SHA de origen | Puerta independiente |
| --- | --- | --- | --- |
| M02.01 / 1412 | `c09-04-m02-01-entidades-ownership-invariantes-mensajeria.md` | `aa2c1a6c46f4ac652a6ae502d1862336b0ed6827` | A5 DEV01, 16 entidades conceptuales y 20 invariantes |
| M02.02 / 1413 | `c09-04-m02-02-api-dto-errores-compatibilidad-mensajeria.md` | `ad16fdfd2af51198721289d4f4c31f7c02093daf` | A5 DEV06, HTTP existente separado de API futura |
| M02.03 / 1414 | `c09-04-m02-03-persistencia-indices-migraciones-datos-prueba.md` | `297c9296e5282a4f2d73aee378e18dae17ecb22c` | A5 DEV05, T01–T10 futuros y sin MySQL ejecutado |
| M02.04 / 1415 | `c09-04-m02-04-configuracion-flags-mensajeria.md` | `5333f0e485326f8755edb4bf367b6ffb37587035` | A5 DEV06, flags CFG/CF no activos |
| M02.05 / 1416 | `c09-04-m02-05-eventos-idempotencia-correlacion-limites-mensajeria.md` | `89c4384b2ffb5c948acf997c4fecae547b0b144a` | A5 DEV03, EV01–EV16/EI01–EI20 futuros, no ejecutados |

El contrato M01.01–.06 conserva sus propias puertas y las referencias NR, P/N, actores, fronteras y gates G01–G09. En el inicio de M02.06, el DAG técnico de este ID informó `ESTADO_REVISION=VALIDADO` y `GATE_TECNICO_CALCULADO=SI`, y COLA G/O=SI, con H=PENDIENTE, I vacía. Son precondiciones del **CLAIM**, no resultado ni autorización para M03. La salida M02 se contabiliza sólo cuando M02.06 complete A0–A4 sobre su propio SHA y A5 independiente de otro TASK_ID, con readback de DAG/COLA; nunca cambiar las fórmulas manualmente.

## Arquitectura real y dependencias futuras

| Límite | Evidencia real del HEAD leído | Trabajo futuro condicionado |
| --- | --- | --- |
| Domain | `MensajeOutbox.cs` blob `891572626c19cd6cf8f522fca5ec876333286ffb`, `EmailEmpresarial.cs` blob `718421df52d525712ee9512d0c164ffebfcf80a5` | M03.01: validar reglas, estados terminales y ownership server-side sin introducir dependencias de Infrastructure |
| Application | `MensajeOutboxService.cs` blob `a00d353c474d7d7f785a58f443c66c5f12a542f7`, `OutboxRetryPolicy.cs` blob `a9bcacda24a7ba031a271b527d8ca2043a8510b9` | M03.02: servicio idempotente, límites/cancelación, correlación y errores |
| API/JWT/RBAC/tenant | `MensajesOutboxController.cs` blob `b6d3fa67c9ecdb35072fa59dad1e71c1e351be81` | M03.03: rechazar tenant B, membresía revocada, rol insuficiente y consentimiento caducado con HTTP real cuando se implemente |
| Infrastructure/EF/MySQL | M02.03 documenta configuración/índices existentes y separa una intención durable de una entrega real | M03.04: transacciones e índice único compuesto, claim CAS realmente exclusivo con MySQL 8.4 efímero y rollback no destructivo |
| Proveedor/transporte | `WhatsAppController.cs` blob `efc1e79625e62925efe7d1e47040eae91fb12dc6` implementa un token de verificación; **no** equivale a autenticidad HMAC del payload | M03.05: adaptadores y workers sólo tras opt-in, firma, anti-replay, retry limitado y aislamiento |
| Observabilidad | Modelo de estados, UTC y CorrelationId descrito por el outbox vigente | M03.06: logs estructurados opacos, resultados de entrega diferenciados, redacción de PII y errores fail-closed |

Las fases futuras M03.01–06 son unidades técnicas independientes sujetas al gate vigente del DAG. Ni el diseño conceptual EV/CFG/CF ni la lista de fuentes implementan por sí mismos esos contratos. El frontend nunca concede permisos del backend y ningún token, DSN, secreto, correo o dato productivo aparece en esta especificación.

## Contrato de salida y pruebas negativas obligatorias posteriores

**Entrada de implementación:** (1) M02.01–06 con LISTO y revisión A5 cuando sea exigible por su SHA, (2) DAG F/G y COLA G/O calculados realmente, (3) ausencia de escritor/lease concurrente, (4) Scope Lock STRICT, (5) clasificación R2 por alcance real. Si cualquiera falla, NO_GO sólo a la microtarea afectada, sin saltar gates.

**Reglas fail-closed de M03:** (a) empresa autorizada desde sesión server-side y referencia del mismo tenant, jamás ruta/cabecera arbitraria; (b) JWT/RBAC, membresía y consentimiento por finalidad, canal y destinatario revalidados inmediatamente antes de efectos; (c) clave idempotente y digest canónico con unicidad durable, no duplicar envíos bajo carreras; (d) claim exclusivo y transición atómica con auditoría causal; (e) distinguir petición, aceptación del proveedor, entrega y rebote; (f) webhook sólo tras HMAC sobre bytes reales, ventana temporal y anti-replay durable; (g) retries acotados, dead-letter sin reenvío oculto, sin fallback entre canales; (h) trazabilidad UTC sin PII, token, cuerpo o secreto en logs; (i) cancelación y rollback preservan historial legítimo, sin falsear efectos externos.

**Matriz de aceptación futura (NO_EJECUTADA en este changeset):** positivos P01 tenant A autorizado, P02 idempotencia mismo contenido sin doble efecto, P03 opt-in y finalidad vigentes, P04 retry finito, P05 correlación opaca; negativos N01 tenant A→B, N02 rol revocado, N03 opt-out, N04 clave reutilizada con payload distinto, N05 dos workers compitiendo, N06 HMAC inválida, N07 replay de nonce, N08 timeout tras acuse previo, N09 dead-letter reabierto sin autorización, N10 secreto en respuesta/log, N11 referencia NULL heredada sin tenant. Especificación de oráculos, **no pruebas, mediciones ni contratos desplegados**. Los M03 que cambien .NET/API necesitan xUnit y rechazos reales; EF/DB exige migraciones y pruebas Category=Integration ejecutadas sobre MySQL 8.4 efímero, no sólo un job verde; UI, si se modifica, exige Angular/Playwright causal. Los proveedores externos sólo se certifican si hay acceso y entorno permitido.

## A0–A5, observabilidad y reversión

- **A0:** diff de este ID limitado a este documento y sección autocontenida de `CHANGELOG_AI.md`. Sin migraciones, rutas, DTO, servicios, workers ni cambios arquitectónicos.
- **A1:** comprobar referencias, cinco fuentes documentales originales, cinco A5 de Task distinta, DAG, criterios positivos/negativos futuros explícitamente NO_EJECUTADOS; sin contabilizar tests inexistentes.
- **A2:** revisión documental de fronteras tenant A/B, revocación, opt-in, JWT/RBAC, secretos, idempotencia y replay; seguridad HTTP/runtime **NO_APLICA** al diff documental, nunca certificado por él.
- **A3:** MySQL, integración real, proveedores, browser y flujos de envío **NO_APLICA** a este cambio exclusivamente R0; pasan a gates obligatorios cuando una futura tarea modifique runtime.
- **A4:** publicar fast-forward serial sobre `dev`, conservar SHA/blobs, exigir `verify-solqaryn-project-scope` y control V5 de CHANGELOG ambos `completed/success` del SHA exacto, además de PLAN H/I, COLA/DAG y BITACORA A:Z con readback. `QUEUED/IN_PROGRESS/SKIPPED/CANCELLED/FAIL` nunca es PASS de check requerido.
- **A5:** revisor de otra de las cinco Tasks Javier, distinto de DEV03; LISTO_DEV sin A5 no desbloquea por sí solo M03 ni autoriza QA/main/PROD.

**Rollback:** `git revert` ordinario del changeset documental tras revalidar HEAD y ausencia de conflicto. Preservar BITACORA histórica, estados de autores, mensajes y datos legítimos. Ni reset ni force-push. **Métricas no observadas:** `NO_MEDIDO`; no afirmar envíos, HMAC, WCAG ni integraciones no comprobadas.
