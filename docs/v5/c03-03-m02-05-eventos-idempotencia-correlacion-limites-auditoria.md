# SOLQARYN V5 FINAL — C03-03.M02.05: contratos de eventos, idempotencia, correlación y límites de auditoría

**Entrega:** especificación documental INERTE. **Autor:** PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio/rama:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Autoridad:** Standard V5 FINAL y Sheet PLAN_MAESTRO/COLA/DAG_TECNICO. **Dependencia:** M01.01–06 de C03-03 LISTO con A5 y DAG F=VALIDADO/G=SI. **Prohibición expresa:** NO IMPLEMENTAR EVENTOS TODAVÍA; este diseño no instala dispatcher, webhook, cola, worker, comunicación entre Tasks, polling o server runtime permanente.

## 1. Naturaleza y fronteras de confianza

La auditoría de seguridad necesita observabilidad fiable de autorizaciones y denegaciones, cambios críticos y accesos excepcionales, evitando secre­tos y registros cruzados de tenants. El sistema vivo contiene `AuditoriaController`, `AuditoriaService`, `RegistroAuditoria`, `AuditoriaRepository` y `CorrelationIdMiddleware`. El método `RegistrarAsync` puede capturar fallos y no acredita por sí mismo durabilidad; `RegistrarEstrictoAsync` propaga errores. La cabecera `X-Correlation-ID` se sanea (ASCII seguro, longitud máxima 64), pero **no es una credencial, una clave idempotente ni garantía de entrega**.

Se distinguen cuatro conceptos: (1) **decisión de autorización** calculada y aplicada solo por el backend; (2) **evento auditable** que registra una decisión pasada sin conceder nuevos permisos; (3) **identificador de correlación** para enlazar diagnósticos, sin semántica de deduplicación; y (4) **identificador idempotente del efecto** gestionado por el servidor cuando un caso de uso futuro lo requiere. Ninguno puede provenir de un flag cliente con efecto de autoridad.

## 2. Catálogo propuesto de eventos futuros (NO emitidos)

| ID | Evento lógico propuesto | Productor y límites futuros |
| --- | --- | --- |
| EV01 | `AuthorizationDecisionRecorded` | Puerta de autorización backend, resultado allow/deny sin JWT ni roles completos |
| EV02 | `CriticalChangeAuditCommitted` | Servicio de cambio crítico tras confirmación transaccional de efecto + auditoría |
| EV03 | `CriticalChangeAuditRejected` | Fallo estricto impide mutación, registra señal segura si es posible |
| EV04 | `TenantAuditReadRequested` | Consulta autorizada y minimizada, sin listar IDs B |
| EV05 | `TenantAuditReadDenied` | Intento de acceso a recurso ajeno sin descubrir su existencia |
| EV06 | `ExceptionalReadGrantApproved` | Plataforma + MFA/reauth + aprobador distinto, scope exacto, TTL breve |
| EV07 | `ExceptionalReadGrantRevoked` | Revocación de grant, invalida cualquier permiso cacheado |
| EV08 | `AuditRetentionExecutionProposed` | Marcador de solicitud; sin política aprobada no hay borrado |
| EV09 | `AuditRetentionExecutionDenied` | Rechazo por falta de permiso/política |
| EV10 | `AuditDeliveryFailureObserved` | Señal de fallo con código saneado, no copia de payload |
| EV11 | `AuditPolicyVersionChanged` | Referencia de versión de política y autoridad, no secretos ni configuración global |
| EV12 | `AuditAccessExportDenied` | Rechazo por falta de grant de exportación o alcance/volumen excesivo |

Los doce son **nombres de contrato**, no eventos publicados, tablas outbox, WebSocket, Kafka, HTTP ni cola real. No se activa integración transversal.

## 3. Sobre conceptual mínimo y restricciones

| Atributo | Fuente y regla |
| --- | --- |
| `schemaVersion` | Enum de versión soportada; desconocido = rechazar/aislar para decisión crítica |
| `eventType` | Catálogo cerrado EV01–EV12, no ruta ejecutable |
| `eventId` | UUID/ID opaco generado por servidor; identifica evidencia, no permiso |
| `tenantScope` | Empresa verificada o ámbito plataforma explícito, jamás `NULL` como fallback |
| `actorRef` | Referencia minimizada derivada del principal autenticado |
| `operationRef` | ID de efecto definido por servidor, sin aceptar clave client-supplied de confianza |
| `correlationId` | ID saneado ASCII ≤64; diagnóstico, no deduplicación |
| `resourceRef` | Hash/opaco, tenant-bound, no exponer propietario de B |
| `actionCode` | Código categorizado, sin payload arbitrario |
| `outcome` | allowed/denied/error y policyVersion histórica, sin autoridad nueva |
| `occurredAtUtc` | Fecha del servidor, no marca de cliente |
| `retentionClass` | Clase hipotética con política previamente aprobada |
| `redactionVersion` | Reglas de minimización aplicadas, nunca payload original |
| `payloadDigest` | Digest de campos públicos/redactados, no JSON de credenciales |
| `causationRef` | Identificador opaco de causa, sin enlazar datos cross-tenant |
| `deliveryAttempt` | Contador **solo si** existe dispatcher autorizado futuro; no habilitado aquí |

El sobre no incluye JWT, cookie, contraseña, secreto de recuperación, dirección privada, before/after sin redacción, cadena MySQL, token SMTP ni headers de autorización. Eventos de plataforma no deben mezclarse con consultas empresariales por un `TenantId=null` ambiguo.

## 4. Idempotencia, reintentos y orden contractual futuro

| Invariante | Regla |
| --- | --- |
| ID01 | `correlationId` no deduplica operaciones; solicitudes distintas pueden compartirlo |
| ID02 | `operationRef` se vincula a actor/tenant/acción/versión, nunca únicamente a header cliente |
| ID03 | Retries de escritura crítica exigen semántica transaccional y detección de efecto ya cometido |
| ID04 | Entrega duplicada de evento futuro no debe duplicar cargo, grant, revocación ni auditoría estricta |
| ID05 | Evento de auditoría no reejecuta ni autoriza operación; replays se tratan como lectura histórica |
| ID06 | Eventos fuera de orden deben respetar `policyVersion` y `occurredAtUtc` sin revivir grants |
| ID07 | Si falla persistencia obligatoria, rollback/denegación; no afirmar ACK sin commit |
| ID08 | Si falla la variante best-effort, evidencia de fallo honesta, nunca éxito durable inventado |
| ID09 | Eventos no tienen acceso a identidades de otras Tasks ni programan reejecuciones de automatizaciones |
| ID10 | No usar nombres EV para justificar permisos IAM, conexión a proveedor o salida PROD |

Sin infraestructura desplegada, no se afirma exactamente-once, orden total, garantía de entrega, latencia, retención temporal ni cuotas satisfechas.

## 5. Límites hipotéticos de seguridad y disponibilidad

**Por validar antes de cualquier implementación:** límites de payload por clase y campo; tamaño de consulta y cursor ligado al tenant; rate limit por actor/tenant/acción sin contadores compartidos; deduplicación durable si el caso lo necesita; TTL de grants excepcionales; correlación saneada; rechazo de evento malformado; backpressure sin pérdida silenciosa de auditoría estricta; DLQ aislada y sin secretos; autorización de operadores de plataforma separada de enterprise; métricas agregadas sin PII. Estos controles **no existen por redactarlos**. Un eventual dispatcher necesita autorización nueva, cuota, cancelación, monitoreo, rollback e idempotencia persistente, y queda expresamente fuera de esta microtarea.

## 6. Oráculos positivos y negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo esperado en una futura prueba real | Negativo exigido |
| --- | --- | --- |
| PN01 | Registro permitido A produce sobre saneado de A | JWT A no produce evento con recurso/identidad B |
| PN02 | Denegación 403 registra razón mínimamente descriptiva | Error/stack no revela grants privados |
| PN03 | Cambio crítico y auditoría strict confirman misma transacción | Caída DB no deja cambio aplicado sin evidencia |
| PN04 | Correlation ID seguro enlaza logs | CRLF/65 caracteres no inyectan logs |
| PN05 | Mismo operationRef seguro evita efecto duplicado | Mismo correlationId no se toma como idempotency key |
| PN06 | Grant excepcional con MFA/aprobador distinto caduca | Autoaprobación o TTL vencido se rechaza |
| PN07 | Revocación invalida siguiente acceso | Evento retrasado no vuelve a autorizar grant revocado |
| PN08 | Consulta A paginada entrega solo scope A | Cursor/ID B no revela filas ni conteo |
| PN09 | Error de schema desconocido queda aislado | Evento crítico desconocido no se descarta silenciosamente |
| PN10 | Política de retención aprobada limita borrado | Solicitud sin autoridad no elimina ni exporta |
| PN11 | Reintento tras timeout preserva semántica de resultado | ACK emitido sin COMMIT no se considera durabilidad |
| PN12 | Auditoría de lectura excepcional no incluye datos sensibles | Token/contraseña en old/new no termina en tabla/log/cola |

Son **12 pares de condiciones futuras**; NO constituyen tests Node, xUnit, HTTP, MySQL, queue o integración ejecutados. Implementaciones M03/M05 requerirán entorno efímero, pruebas de fallo, seguridad A/B y SHA exacto.

## 7. A0–A4 documental y salida segura

- **A0:** verificar Plan V5, DAG, predecesores y compatibilidad con M01/M02.02, diff documental, ausencia de secretos y el veto a runtime de eventos.
- **A1:** revisar 12 eventos, 16 campos, diez invariantes y 12 pares P/N marcados FUTUROS_NO_EJECUTADOS.
- **A2:** tenant A/B, JWT/RBAC/PII, grants excepcionales, redacción, idempotencia sin privilegio y auditoría strict fail-closed.
- **A3:** alineación documental con Domain/Application/Infrastructure, `AuditoriaService`, `CorrelationIdMiddleware` y futuros consumidores; builds .NET/Angular, MySQL y Playwright NO_APLICA a diff solo Markdown (no PASS).
- **A4:** commit fast-forward/CAS de dos Markdown en dev, GitHub Scope Lock y V5 provenance `completed/success` en SHA exacto, comparación de blobs y readback PLAN/COLA/DAG/BITACORA.
- **A5:** revisión independiente por una de las otras cinco Tasks Javier, distinta de DEV02, antes de habilitar dependencias sujetas al gate. LISTO no autoriza QA/main/PROD.

**Rollback documental:** `git revert` normal con evento institucional compensatorio, sin borrar BITACORA histórica. Cero nuevas Tasks, webhooks, workers, outbox, relays, colas, SQL, IAM o cambios productivos.
