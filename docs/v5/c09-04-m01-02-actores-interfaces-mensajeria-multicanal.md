# SOLQARYN V5 — C09-04.M01.02: actores, entradas, salidas y fronteras de mensajería multicanal

**Microtarea:** C09-04.M01.02 · PLAN_MAESTRO fila 1407 · **Responsable:** PRIMARY DEV 06 JAVIER (TASK_ID 6af16bbad26881918c283abe06e1f32a) · **Fecha:** 2026-11-14 · `solqaryn/Solqaryn@dev` · PROJECT_SCOPE_LOCK=STRICT.

**Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Antecesora específica:** `C09-04.M01.01`, documento `docs/v5/c09-04-m01-01-alcance-mensajeria-multicanal.md`, que fija NR01–NR14. **Naturaleza:** preparación **documental**, no endpoints, workers, webhooks, colas ni comunicaciones nuevas.

## Modelo de actores A01–A14

| Actor | Operación prevista | Regla de autoridad |
| --- | --- | --- |
| A01 propietario de empresa | Define política de mensajes de su empresa | Validar EmpresaId y RBAC en API |
| A02 administrador delegado | Gestiona plantillas y permisos acotados | Nunca adquiere control de empresa ajena |
| A03 operador comercial | Propone envío de seguimiento | Requiere rol, finalidad y consentimiento |
| A04 operador de pedidos | Solicita aviso transaccional | Evento de negocio no equivale a opt-in comercial |
| A05 cliente destinatario | Recibe o revoca consentimiento | Identidad y pertenencia comprobables |
| A06 representante/sucursal | Origina comunicación desde sucursal | Sucursal pertenece a empresa vigente |
| A07 servicio Application | Valida caso de uso y autorización | No confiar en EmpresaId del navegador |
| A08 repositorio de intención | Persiste clave y estado idempotente | Aislamiento físico/lógico por tenant |
| A09 procesador de reintentos futuro | Reintenta efectos durables | Límite, claim exclusivo, dead-letter |
| A10 adaptador SMTP existente | Transporta email en límites aprobados | Configuración secreta fuera de payload |
| A11 adaptador WhatsApp futuro | Entrega canal cuando autorizado | Referencia secreta, nunca token en texto |
| A12 proveedor externo futuro | Devuelve acuse, error o webhook | Verificación criptográfica y anti-replay |
| A13 auditor de tenant | Consulta trazas de su empresa | Payload mínimo sin PII de terceros |
| A14 soporte de plataforma | Diagnostica sin actuar como usuario final | Elevación temporal explícita y auditada, fail-closed |

El nombre de actor no presupone que exista hoy un rol, componente, worker o endpoint operativo para cada caso. A09, A11, A12 y flujos de autorización asociados son **contratos propuestos**; A10 refiere al SMTP transaccional existente, no a marketing activado.

## Interfaces I01–I12 y entradas/salidas planeadas

| Interfaz | Entrada futura | Salida deseada | Riesgo / rechazo obligatorio |
| --- | --- | --- | --- |
| I01 solicitud de intención | `empresaId, actorId, canal, destinatario, clave` | Identificador de intención durable | Empresa A no puede exigir envío por B |
| I02 autorización server-side | Identidad JWT, membresía y permiso | Decisión de acceso justificada | Solo guard Angular insuficiente |
| I03 resolver contexto/sucursal | Empresa, sucursal y recurso | Contexto empresarial verificado | Sucursal ajena o legacy NULL no atribuido |
| I04 finalidad y consentimiento | Finalidad, categoría, sujeto, fecha | Resultado permitido/denegado | Opt-out no genera envío comercial |
| I05 plantilla/render seguro | Plantilla, variables limitadas, locale | Cuerpo validado y redaccion segura | Plantilla cruza datos tenant A/B |
| I06 idempotencia persistente | Tenant, canal, idempotency-key, hash | Estado previo o creación única | Igual clave + payload distinto rechazar |
| I07 estado outbox | Intención, correlación, estado | Estado rastreable sin payload secreto | No doble reclamo concurrente |
| I08 transporte email | Intención autorizada, referencia SMTP | Confirmación o error normalizado | No aceptar SMTP como proof de marketing |
| I09 transporte WhatsApp | Intención autorizada, secret ref | Acuse o error normalizado | Sin fallback ni sesiones QR inseguras |
| I10 callback/webhook futuro | Firma, marca temporal, cuerpo, correlación | Acuse idempotente o 4xx/5xx | HMAC incorrecta o replay se rechaza |
| I11 consulta/auditoría | Tenant autenticado y filtro de estado | Eventos mínimos tenant-scoped | Ningún log global por defecto |
| I12 opt-out y supresión | Sujeto, canal, finalidad, tenant | Estado efectivo y auditado | No reactivar consentimiento sin prueba |

**Contrato de datos común futuro:** `CorrelationId` nunca es token de autorización; `EmpresaId` del request es dato reclamado, no autoridad; la clave idempotente exige alcance por tenant y payload; los mensajes a proveedores usan credenciales opacas configuradas server-side. Los DTO definitivos se definen en etapa de código, no en esta M01.02.

## Fronteras de confianza F01–F09

1. **F01 Browser → API:** validar JWT/CSRF según contexto, permisos y pertenencia en backend; el frontend sólo orienta la experiencia.
2. **F02 API → Application:** denegar por defecto si falla la membresía o finalidad; no aceptar id de empresa como sustituto de auth.
3. **F03 Application → Repository/MySQL:** consultas tenant-scoped, idempotencia durable y consistencia de transacciones; no cambios de esquema aquí.
4. **F04 Outbox → futuro dispatcher:** reclamar una vez, retry limitado y observabilidad sin secretos; no asumir ejecución automática actualmente.
5. **F05 Transport → SMTP/WhatsApp:** secretos fuera de repo, tokens no retornados, no fallback a otro canal no consentido.
6. **F06 Proveedor → futuro webhook:** firma HMAC, ventana temporal, validación de origen y protección contra replay.
7. **F07 Contacto/PII → render/plantilla:** minimizar datos, proteger preferencias, retirar materiales sensibles al auditar.
8. **F08 Tenant A → Tenant B:** impedir referencias cruzadas de clientes, sucursales, plantillas, registros y consentimientos.
9. **F09 Soporte plataforma → cuenta tenant:** apoyo técnico trazable y autorizado, sin impersonación ni lectura arbitraria.

## Fuentes reales del código leídas en dev durante el preflight

| Ruta exacta | Blob | Límite verificable |
| --- | --- | --- |
| `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Modelo de intención, no envío probado |
| `backend/src/Domain/Entities/EmailEmpresarial.cs` | `718421df52d525712ee9512d0c164ffebfcf80a5` | Agregado por tenant, no CRM |
| `backend/src/Domain/Entities/ConfiguracionWhatsAppEmpresa.cs` | `e861f83314287f59b00290f579cae0247d95a780` | Referencias secretas opacas |
| `backend/src/Application/Services/MensajeOutboxService.cs` | `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Staging sujeto a autoridad de usuario |
| `backend/src/Application/Services/OutboxRetryPolicy.cs` | `a9bcacda24a7ba031a271b527d8ca2043a8510b9` | Política de reintentos, no garantía exactly-once |
| `backend/src/API/Controllers/MensajesOutboxController.cs` | `b6d3fa67c9ecdb35072fa59dad1e71c1e351be81` | Entrada API `Authorize`/permiso |
| `backend/src/API/Controllers/EmailEmpresarialController.cs` | `e7acfb132c9aedf0e22be9cb3acd6e86e4718833` | Contrato HTTP email por empresa |
| `backend/src/API/Controllers/WhatsAppController.cs` | `efc1e79625e62925efe7d1e47040eae91fb12dc6` | Boundary WhatsApp, no proveedor conectado por inferencia |
| `backend/src/Infrastructure/Services/SmtpEmailService.cs` | `7ff845fb6571b66c5da5d41be85b79ade92e27cf` | SMTP MailKit existente, no campañas habilitadas |

## Matriz de escenarios futuros, no ejecutados

**Positivos P01–P08:** P01 intención tenant A con rol válido; P02 idempotencia repetida sin envío duplicado; P03 opt-in por canal; P04 plantilla segura sin cruce de PII; P05 acuse correlacionado; P06 trazabilidad de reintento; P07 consulta auditoría sólo de tenant autorizado; P08 supresión y revocación efectiva.

**Negativos N01–N12:** N01 solicitud A→B; N02 permiso ausente; N03 sucursal ajena; N04 contacto legacy NULL sin reconciliar; N05 opt-out comercial; N06 HMAC inválido; N07 replay fuera de ventana; N08 token secreto en salida; N09 idempotency-key reutilizada con cuerpo distinto; N10 retry fuera de cuota; N11 fallback implícito de canal; N12 exportación de trazas de tenant ajeno.

**Estado de todos P/N:** `FUTUROS_NO_EJECUTADOS`. Ni lectura de blobs ni este Markdown constituyen ejecución de .NET 10, Angular 22.2.1, MySQL 8.4, SMTP real, WhatsApp o Playwright. No se han creado mensajes, tareas recurrentes ni dispatcher por este cambio.

## Cierre, dependencia y reversibilidad

Este artefacto satisface la **identificación documental** de actores, entradas, salidas, superficies y fronteras de C09-04.M01.02. La especificación funcional detallada sigue pendiente de fases posteriores. El DAG F/G VALIDADO/SI permite únicamente el trabajo de preparación; cualquier ejecución de nuevos eventos/webhooks sigue prohibida por la orden V5.

Para el cierre DEV exigir Scope Lock y control de procedencia del CHANGELOG `completed/success` en el SHA publicado, readback PLAN/COLA/BITACORA, y registrar A5_PENDIENTE. A5 solo por otra Task Javier, nunca por la autora. Rollback mediante `git revert` normal del commit documental, no force-push, sin borrar bitácora. Sin cambios en QA/main/PROD.
