# SOLQARYN V5 — C09-04.M01.01: objetivo, exclusiones y no-regresión de mensajería multicanal

**Microtarea:** C09-04.M01.01 · PLAN_MAESTRO fila 1406 · **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a · **Fecha:** 2026-11-14 · **Repositorio:** solqaryn/Solqaryn, rama dev · **PROJECT_SCOPE_LOCK=STRICT**.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA, unidad C09-04 M01 (alcance y preparación). **Resultado futuro del lote:** mensajería mediante adaptadores de email, WhatsApp y proveedores adicionales, con contratos observables de webhook, replay, privacidad y no duplicación. Esta entrega **no implementa ni activa** nuevos proveedores, eventos, webhooks, dispatcher o automatizaciones.

## Objetivo, superficies y límites explícitos

Definir el límite verificable de la mensajería multicanal comercial en una plataforma multiempresa: una intención de comunicación debe estar vinculada a un tenant autenticado y autorizado, a un destinatario elegible, una finalidad legítima, un canal explícito y una clave de idempotencia. El envío real será responsabilidad de una fase de implementación posterior con permisos específicos, configuración segura y pruebas funcionales/integración.

**En alcance documental:** inventariar límites actuales, proponer invariantes de aislamiento y consentimiento, explicitar superficies API/Domain/Application/Infrastructure y futura UI, y definir verificaciones de aceptación para etapas sucesivas. En esta microtarea no se deciden transportes concretos ni se cambian contratos existentes. Una precondición DAG VALIDADO/SI autoriza esta **preparación**, no el envío ni la reconfiguración de proveedores.

**Fuera de alcance:** enviar correo o WhatsApp, invocar APIs externas, crear campañas, registrar contactos reales, almacenar tokens, instalar colas/brokers, modificar MySQL, desplegar workers, abrir webhooks, construir un event dispatcher, cambiar RBAC/JWT o configurar DNS, claves SMTP, META, R2, QA/main/PROD. La orden V5 prohíbe explícitamente implementar eventos por ahora.

## Línea base técnica observada (HEAD dev de preflight cf04aad8e006465ced4b2a92a7dd108edd9fc064)

| Componente existente | Blob observado | Lo que sí acredita / lo que no acredita |
| --- | --- | --- |
| `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Modelo de intención durable con EmpresaId e idempotencia; no demuestra dispatch global |
| `backend/src/Domain/Entities/EmailEmpresarial.cs` | `718421df52d525712ee9512d0c164ffebfcf80a5` | Agregado de correo por empresa; no demuestra campañas CRM |
| `backend/src/Domain/Entities/ConfiguracionWhatsAppEmpresa.cs` | `e861f83314287f59b00290f579cae0247d95a780` | Referencias opacas de secretos por empresa; no autoriza mostrar credenciales |
| `backend/src/Application/Services/MensajeOutboxService.cs` | `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Validación de autoridad previa al staging; no acredita ejecución externa |
| `backend/src/Application/Services/OutboxRetryPolicy.cs` | `a9bcacda24a7ba031a271b527d8ca2043a8510b9` | Política determinista de reintentos; no garantiza entrega exactamente una vez |
| `backend/src/API/Controllers/MensajesOutboxController.cs` | `b6d3fa67c9ecdb35072fa59dad1e71c1e351be81` | API autenticada y permiso declarado, requiere pruebas A/B por endpoint |
| `backend/src/API/Controllers/EmailEmpresarialController.cs` | `e7acfb132c9aedf0e22be9cb3acd6e86e4718833` | Contrato HTTP de correo por empresa, no plataforma universal de marketing |
| `backend/src/API/Controllers/WhatsAppController.cs` | `efc1e79625e62925efe7d1e47040eae91fb12dc6` | Boundary HTTP WhatsApp, no proveedor de sesión conectado por inferencia |
| `backend/src/Infrastructure/Services/SmtpEmailService.cs` | `7ff845fb6571b66c5da5d41be85b79ade92e27cf` | Transporte SMTP transaccional ya existente; no opt-in comercial implícito |

La lectura de fuentes y hashes es **inspección estática**, no un informe de ejecución real de entregas, webhook, SMTP o MySQL. Arquitectura actual: ASP.NET Core 10, Domain ← Application ← Infrastructure, API composition root, MySQL EF Core 10, frontend Angular 22.2.1. No cambiar paquetes ni proveedor.

## Invariantes de no-regresión NR01–NR14

| ID | Invariante exigible al implementar | Escenario negativo que deberá rechazarse |
| --- | --- | --- |
| NR01 | Toda intención conserva empresa emisora validada server-side | Empresa A envía usando identidad de B |
| NR02 | Cualquier recurso destino pertenece al mismo tenant o a un vínculo expresamente autorizado | Referencia cruzada A/B sin relación válida |
| NR03 | JWT, membresía y permiso del canal se validan en backend y no sólo en guards UI | Usuario sin rol envía vía API directa |
| NR04 | La clave idempotente está acotada por empresa, canal, evento y destino | Repetición idéntica provoca doble envío |
| NR05 | Un reintento no borra evidencia histórica ni emite un efecto nuevo sin exclusión atómica | Worker duplicado toma dos veces el mismo evento |
| NR06 | Consentimiento/finalidad/opt-out se verifican antes de contacto comercial | Persona retirada recibe mensaje promocional |
| NR07 | El origen de un contacto con EmpresaId NULL legado no adquiere tenant por suposición | Reasignación silenciosa del cliente legado |
| NR08 | Templates, adjuntos y enlaces nunca filtran PII ni secretos de otro tenant | Plantilla A renderiza datos de B |
| NR09 | Credenciales de transporte permanecen en almacén seguro, no en payload/log/API | Token mostrado en respuestas o bitácora |
| NR10 | Los webhooks futuros exigirán firma, ventana temporal, replay guard e identidad de proveedor | Firma incorrecta/expirada aceptada |
| NR11 | Los fallos de canal se clasifican sin ciclo infinito ni reenvío sin límites | Backoff infinito o dead-letter ignorado |
| NR12 | La auditoría usa correlación sin payload sensible en texto claro | Se registra cuerpo o dato sensible completo |
| NR13 | Operaciones de supresión/exportación son tenant-scoped y trazables | Exportación global sin autorización explícita |
| NR14 | Fallo de proveedor no altera transacción comercial ni habilita un canal fallback sin consentimiento | Se cambia silenciosamente de WhatsApp a email |

## Contratos de aceptación futura, sin atribución de ejecución

- **Positivos previstos:** P01 alta de intención autorizada tenant A; P02 replay mismo idempotency-key devuelve estado previo; P03 rechazo de reintento terminal conservando bitácora; P04 opt-in verificable canal/finalidad; P05 auditoría mínima correlacionada; P06 credenciales referenciadas y no expuestas; P07 selección de canal explícita; P08 notificación transaccional tenant-bound sin cambiar operaciones comerciales.
- **Negativos previstos:** N01 A→B rechazado; N02 falta de permiso rechazada; N03 opt-out bloquea campaña; N04 dato legacy NULL sin resolver se bloquea; N05 clave idempotente colisionada con payload diferente se rechaza; N06 webhook con HMAC inválido se rechaza; N07 replay fuera de ventana se rechaza; N08 proveedor inactivo no genera fallback; N09 PII en log se rechaza; N10 token en payload se rechaza; N11 reintento excedido se aísla; N12 exportación cruzada se deniega.

**Estado de todos estos casos:** `FUTUROS_NO_EJECUTADOS`. No confundir criterios propuestos con pruebas .NET/Angular/Playwright/MySQL, simulaciones de transporte o certificación de disponibilidad real.

## Dependencias, decisiones y plan de verificación posterior

- El DAG dinámico para esta unidad indica preparación documental `VALIDADO/SI`. El preflight lee PLAN_MAESTRO!H1406/I1406, COLA!G1406/O1406 y DAG_TECNICO!F1367/G1367. Esa evidencia no sustituye A0–A4 ni revisión A5 por un TASK_ID Javier distinto.
- Las unidades M01.02–M01.05 deberán mapear actores/fronteras, inspeccionar pruebas y rutas completas, identificar riesgos y convertir invariantes en criterios observables. M03 exigirá código real, tenant A/B y CI dirigida, sin promociones prematuras.
- **Decisiones abiertas:** granularidad de consentimiento y finalidad, vínculo comercial de contacto, política de retención y supresión, separación mensajes transaccionales/marketing, transporte permitido por tenant, cuotas y estrategia de entrega y dead-letter. No fijar proveedores nuevos mediante este documento.
- **Evidencia futura exigible:** tests de idempotencia concurrente, autorización negativa/positiva tenant A/B, aislamiento de búsquedas, rechazo por opt-out, protección de secretos, firma y replay de futuros webhooks, pruebas de integración efímeras y observabilidad segura.
- **Rollback:** revertir exclusivamente este commit documental en `dev` mediante `git revert` normal, sin force-push, sin modificar la bitácora histórica; conservar las decisiones para auditar el cambio.

**Resultado de esta M01.01:** contrato de objetivos, exclusiones y no-regresión documentado. **No** representa un sistema de mensajería multicanal implementado ni autorización para eventos, pagos, integraciones de producción o QA/main/PROD.
