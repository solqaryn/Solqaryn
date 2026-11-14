# SOLQARYN V5 — C09-04.M01.03: inventario técnico y brechas verificables de mensajería multicanal

**Microtarea:** C09-04.M01.03 · PLAN_MAESTRO fila 1408 · **Responsable:** PRIMARY DEV 06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a · **Fecha:** 2026-11-14 · `solqaryn/Solqaryn@dev` · PROJECT_SCOPE_LOCK=STRICT.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Resultado exigido del lote:** mensajería por adaptadores con contratos de webhooks, replay, privacidad y no duplicación. Esta M01.03 es lectura de contratos/código/evidencias: no implementa funciones, ni considera que un archivo existente pruebe integración E2E de campañas.

## Antecedentes y alcance

- `C09-04.M01.01`: `docs/v5/c09-04-m01-01-alcance-mensajeria-multicanal.md`, blob `dbbed9aa74fa10f4b5e44dadf62edd2b7983ab24`; define NR01–NR14 y separa email/WhatsApp actual de futuras campañas.
- `C09-04.M01.02`: `docs/v5/c09-04-m01-02-actores-interfaces-mensajeria-multicanal.md`, blob `ab286319a4903bc0eb861eee76873a259455f362`; mapea A01–A14, I01–I12 y F01–F09 como límites propuestos.
- **Fuente de verdad:** blobs del árbol Git `dev` consultado durante esta corrida, acompañados por lecturas de contenido de componentes primarios. No hubo invocación de endpoints, migración de esquema, SMTP, WhatsApp, MySQL, tests xUnit o Playwright; este inventario no es certificación operativa.

## Fuentes existentes y procedencia por blob (HEAD dev de preflight)

| Capa / ruta exacta | Blob Git | Evidencia concreta / límite |
| --- | --- | --- |
| Domain — `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Intención durable con empresa, correlación e idempotencia; no confirma envío |
| Domain — `backend/src/Domain/Entities/EmailEmpresarial.cs` | `718421df52d525712ee9512d0c164ffebfcf80a5` | Agregado de correo tenant-bound; no prueba consentimiento de campaña |
| Domain — `backend/src/Domain/Entities/ConfiguracionWhatsAppEmpresa.cs` | `e861f83314287f59b00290f579cae0247d95a780` | Referencias de secretos opacas; no exponer token real |
| Application — `backend/src/Application/Services/MensajeOutboxService.cs` | `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Verifica scope de usuario para registrar intención, no prueba workers activos |
| Application — `backend/src/Application/Services/EmailEmpresarialService.cs` | `a181ae87bf4eb4a9327c2588827d5338b9dfbff2` | Comprobación server-side de tenant e idempotencia por EmpresaId |
| Application — `backend/src/Application/Services/OutboxRetryPolicy.cs` | `a9bcacda24a7ba031a271b527d8ca2043a8510b9` | Backoff determinista acotado; no demuestra progreso del dispatcher |
| Application — `backend/src/Application/Services/OutboxRetryProcessor.cs` | `542c0a655a3ee0698e3d80f1854d43903b6470fa` | Contrato IOutboxEffectDispatcher y reintentos; no confirma proveedor externo conectado |
| API — `backend/src/API/Controllers/MensajesOutboxController.cs` | `b6d3fa67c9ecdb35072fa59dad1e71c1e351be81` | Ruta autenticada y permiso, no certifica todos los escenarios negativos |
| API — `backend/src/API/Controllers/EmailEmpresarialController.cs` | `e7acfb132c9aedf0e22be9cb3acd6e86e4718833` | API email empresarial por tenant, no CRM universal |
| API — `backend/src/API/Controllers/WhatsAppController.cs` | `efc1e79625e62925efe7d1e47040eae91fb12dc6` | Boundary WhatsApp con verificaciones, no canal de campañas habilitado |
| Infrastructure — `backend/src/Infrastructure/Services/SmtpEmailService.cs` | `7ff845fb6571b66c5da5d41be85b79ade92e27cf` | SMTP MailKit transaccional; no prueba consentimiento ni entrega de marketing |
| Infrastructure — `backend/src/Infrastructure/Services/WhatsAppPublicoService.cs` | `7607fa5822f12fdc087237575e8970da0a96c578` | Lecturas de configuración WhatsApp, no integración de mensajería universal |
| Infrastructure — `backend/src/Infrastructure/Repositories/MensajeOutboxRepository.cs` | `056d5aa380b53ed697ddcb6852df55394e6544a7` | Intención/persistencia y updates condicionales, no exactly-once de proveedor |
| Infrastructure — `backend/src/Infrastructure/Repositories/EmailEmpresarialRepository.cs` | `14b776398184153e87bd1a9458b1e918c675211f` | Consultas por EmpresaId, sin afirmar aislamiento total de plataforma |

El conjunto constituye **14 referencias de evidencia estática** verificadas por ruta/blob; los controladores y servicios corresponden a capacidades existentes de email/outbox/WhatsApp. No se inventa un `MarketingCampaignService`, endpoint de campañas, dispatcher de eventos nuevo o plataforma CRM completa. La presencia de IOutboxEffectDispatcher en Application **no autoriza desplegar ni activar** una nueva cadena de eventos (prohibición V5 vigente).

## Brechas de evidencia G01–G12 (no defectos confirmados)

| Brecha | Pregunta falsable | Evidencia que falta para certificación futura |
| --- | --- | --- |
| G01 | ¿Existe política persistente por tenant/canal/finalidad de consentimiento comercial? | Entidad y API con opt-in/opt-out, tests y tenant A/B |
| G02 | ¿El email transaccional se distingue obligatoriamente de campaña de marketing? | DTO/contexto y test de no promoción implícita |
| G03 | ¿Se detecta replay externo firmado y fuera de ventana? | Algoritmo y fixtures de firma/HMAC/caducidad |
| G04 | ¿Cada webhook de proveedor exige identidad tenant verificada? | Router, secreto referenciado y pruebas A/B |
| G05 | ¿Idempotencia incluye payload/actor/canal frente a reutilización maliciosa de clave? | Concurrent tests y colisiones en MySQL efímero |
| G06 | ¿Los workers futuros reclaman de manera exclusiva los mensajes? | Integración claim lease, recovery y doble worker |
| G07 | ¿Los límites de retry y dead-letter previenen reintento infinito? | Pruebas de caída parcial y recuperación sin duplicados |
| G08 | ¿Qué tratamiento recibe un contacto con EmpresaId NULL legado? | Política de ownership y negativos no atribución silenciosa |
| G09 | ¿Las plantillas ocultan información personal de otra empresa? | Escenarios cross-tenant de render y datos sensibles |
| G10 | ¿El opt-out bloquea igualmente API, cron y reintentos? | Pruebas server-side sobre todos los puntos de entrada |
| G11 | ¿La auditoría evita destinatarios, contenidos y credenciales en logs? | Redacción verificable de logs, errores y traces |
| G12 | ¿Hay autorización para habilitar un nuevo proveedor, cuota y rollback? | Orden específica, config, tests de fallo, límites y desactivación |

**Interpretación:** G01–G12 son **huecos entre el inventario y la certificación futura**, no hallazgos de vulnerabilidad ya demostrada. La validación futura debe incluir versiones reales .NET 10, EF Core/MySQL 8.4, Angular 22.2.1, JWT/RBAC, aislamiento A/B y auditoría de secretos. El gobierno del proyecto prohíbe aceptar pruebas `cancelled/skipped/in_progress` como PASS requerido.

## Hipótesis y criterios futuros P01–P08 / N01–N12

**Positivos pendientes:** P01 autorización tenant-scoped; P02 idempotencia estable frente a replay; P03 opt-in vigente por canal/finalidad; P04 plantilla depurada; P05 acuse de transporte vinculado a correlación; P06 retry controlado y reversible; P07 trazas de tenant mínimo; P08 supresión efectiva antes de enviar.

**Negativos pendientes:** N01 actor A intenta enviar por B; N02 JWT válido sin permiso; N03 sucursal ajena; N04 contacto legado sin ownership; N05 consentimiento revocado; N06 firma webhook inválida; N07 replay vencido; N08 payload contiene token; N09 idempotency-key reusada para contenido diferente; N10 retry supera máximo; N11 fallback automático no autorizado; N12 logs/exportación exponen PII de otro tenant.

**Estado:** `FUTUROS_NO_EJECUTADOS` en todos P/N. Ni inventariar blobs ni leer contratos valida que un proveedor, worker o esquema satisface estos casos. Los tests positivos/negativos y CI dirigida deben incorporarse en la fase técnica correspondiente y revisarse en el SHA exacto, con A5 independiente.

## Riesgos de integración, decisiones y reversibilidad

- **Interfaces de mayor riesgo:** consentimiento/PII, permisos de tenant en API y procesadores, transición del outbox a efecto externo, colisiones de idempotencia con varios workers, canal alterno y webhook no confiable.
- **Sin nuevas dependencias:** no se añade broker, cola de proveedor, SDK WhatsApp, gateway, secreto, tabla, worker ni cuenta. M01.04 deberá traducir brechas a riesgos/supuestos/decisiones; M01.05 formulará aceptación observable. M03 requerirá código real y CI según impacto.
- **A0–A4 de este alcance documental:** comparar diff y blobs, revisar coherencia del inventario, aislar reclamos de seguridad, validar referencias existentes, comprobar Scope Lock STRICT y control V5 de CHANGELOG `completed/success` en SHA publicado. Otras suites runtime no son prueba causal de cambios sólo Markdown.
- **Rollback:** `git revert` normal del commit documental en `dev`, sin force-push ni reescritura de BITACORA. Registrar A5_PENDIENTE para revisión por otra Task Javier; no trasladar a QA/main/PROD.

**Resultado:** inventario de 14 fuentes reales y matriz G01–G12, con separación explícita entre código existente, brechas de evidencia y capacidades futuras no certificadas.
