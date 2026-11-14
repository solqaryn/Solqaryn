# SOLQARYN V5 — C09-03.M01.01: alcance y no-regresión de Social Hub y campañas

**Microtarea:** C09-03.M01.01 · PLAN_MAESTRO fila 1370 · Autor: PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507` · Fecha: 2026-11-14 · Repositorio `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. El resultado **futuro** del lote C09-03 es «Campañas, contenido, segmentos, calendarios y métricas»; evidencia futura: consentimiento, límites y opt-out. En esta unidad M01.01 se fija únicamente el **objetivo, el no-alcance y los criterios de no-regresión**, sin instalar un Social Hub ni activar campañas.

## Objetivo verificable y fronteras

Documentar las condiciones bajo las cuales una futura empresa usuaria podrá administrar contenido y campañas con segmentación limitada al tenant, planificación de publicaciones, consentimiento verificable por canal, preferencia de baja y métricas agregadas compatibles con minimización de datos. Cualquier envío o publicación posterior debe requerir autorización backend real, configuración tipada segura y pruebas con escenarios negativos. El futuro Social Hub no debe convertirse en un motor de ejecución de scripts, SQL o consultas entre tenants.

**Dependencias preparatorias:** C06-03 (superficies públicas), C09-01 (CRM), C01/C06/C08 (contexto de plataforma, experiencia y sectores). `DAG_TECNICO!F1331=VALIDADO` y `G1331=SI` junto con `COLA!G1370/O1370=SI` autorizan exclusivamente este trabajo documental. No equivalen a autorización de canales, proveedores, despliegues, M01.06 terminal, migraciones o gestión de datos reales.

## Línea base observada en dev

| Fuente leída en repositorio original | Blob Git | Hecho acreditado y límite |
| --- | --- | --- |
| `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | Modelo Cliente con `EmpresaId` nullable legado; **no** garantiza un consentimiento publicitario ni un segmento global |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía empresarial; **no** concede por sí misma permiso de campaña |
| `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Contrato de intención durable; **no** certifica un dispatcher de campaña ni entregas externas |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Modelo de registro de auditoría; **no** demuestra métricas comerciales completas |
| `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Guarda de experiencia Angular, nunca sustituto del permiso verificado en API |
| `docs/v5/c09-01-m01-01-alcance-crm-universal.md` | `4d35d8113cf1a1718dd7f487c50a06f51c1848b5` | Contrato documental de CRM, sin autorización de envío |
| `docs/v5/c09-04-m01-01-alcance-mensajeria-multicanal.md` | `dbbed9aa74fa10f4b5e44dadf62edd2b7983ab24` | Frontera documental de mensajería, sin activar transporte |

La lectura estática de estas rutas **no** constituye un inventario exhaustivo; C09-03.M01.03 deberá ampliarlo. La plataforma actual usa .NET 10, MySQL/EF Core 10, Angular 22.2.1, JWT y RBAC; esta entrega no cambia paquetes, esquema, controladores ni servicios.

## No-alcance de esta unidad

- No crear API, rutas Angular, tablas, migraciones, UI de campañas, segmentadores live, colas, crons, workers, webhooks, eventos, proveedores sociales, servicios externos ni Tasks.
- No importar contactos, clasificar perfiles reales, procesar PII, enviar email/WhatsApp, publicar redes sociales, ejecutar marketing ni generar métricas con datos de clientes.
- No modificar consentimientos existentes, secretos, tokens OAuth, IAM/JWT/RBAC, tenant routing, datos productivos, precios, pagos, CDN, DNS, QA, main o PROD.
- No inferir que `EmpresaId=NULL`, una sesión, un host, una cabecera `X-Empresa-Id`, un slug o un enlace de invitación autorizan lectura de otros tenants.
- No interpretar el contrato documental como un `PASS` de pruebas de runtime, cumplimiento legal, entregabilidad, moderación o revisión A5.

## Invariantes NR01–NR16 exigibles en fases futuras

| ID | Regla no negociable |
| --- | --- |
| NR01 | Cada campaña, pieza, segmento y métrica conserva un propietario `EmpresaId` comprobado en backend |
| NR02 | Tenant A no puede leer, editar, programar, exportar ni inferir datos de B ni con IDs conocidos |
| NR03 | JWT, membresía activa y permiso empresarial por acción se validan server-side; la guarda UI no es autoridad |
| NR04 | `Cliente.EmpresaId=NULL` legado exige resolución acreditada o denegación, nunca propiedad global |
| NR05 | Un contacto comercial no constituye consentimiento para campaña, canal ni finalidad concreta |
| NR06 | Cada destinatario requiere consentimiento vigente, finalidad, canal y mecanismo de opt-out comprobable |
| NR07 | Solicitud de baja, bloqueo o revocación tiene prioridad sobre un calendario o reintento pendiente |
| NR08 | Segmentos futuros usan criterios tipados/allowlist; nunca SQL/JS/código arbitrario del usuario |
| NR09 | Contenido editable exige normalización, defensa XSS, autorización de medios y revisión de enlaces |
| NR10 | Publicación y envío futuros exigen idempotencia acotada por empresa/canal/campaña/destino |
| NR11 | Una programación se ejecuta sólo con permiso y consentimiento vigentes al momento real de la acción |
| NR12 | Tokens, HMAC, firmas, claves OAuth y secretos nunca se almacenan en documentos ni se filtran en logs |
| NR13 | Logs y auditoría registran actor, tenant, correlación y transición minimizando PII |
| NR14 | Métricas agregadas no facilitan reidentificación, cross-tenant drilldown ni exportación no autorizada |
| NR15 | Fallos de proveedores, cuotas, rate limits o desconexiones no activan reintentos ilimitados ni borran evidencia |
| NR16 | Revertir una futura función impide nuevos efectos externos, conserva auditoría y no altera ventas, créditos ni pagos |

## Matriz mínima de aceptación futura — NO EJECUTADA

| Escenario | Resultado futuro exigible |
| --- | --- |
| P01: empresa con rol autorizado crea un borrador para su propio tenant | Borrador auditado, sin publicación automática |
| P02: suscripción con consentimiento por canal y finalidad | Envío futuro únicamente después de preflight vigente |
| P03: opt-out de destinatario antes de la hora programada | Excluido incluso cuando existía una tarea programada |
| P04: reintento idéntico de intención | No se duplica publicación ni envío |
| P05: consulta de métricas agregadas dentro del tenant | Sólo agregados permitidos y sin PII ajena |
| N01: tenant A solicita campaña, segmento o estadística de B | Denegación server-side y sin filtración |
| N02: usuario sin permiso llama endpoint directamente | Denegación, independientemente del guard Angular |
| N03: `EmpresaId=NULL` y ownership no reconciliado | Denegación por defecto |
| N04: consentimiento revocado o canal no autorizado | No se emite campaña |
| N05: doble ejecución, replay, HMAC inválido o idempotency key reciclada | Rechazo/dedupe y auditoría |
| N06: HTML/script malicioso en contenido | Rechazo/sanitización probada |
| N07: cuotas agotadas, proveedor sin credenciales o tiempo de espera | Error seguro sin reintento infinito |
| N08: exportación de destinatarios fuera de tenant/RBAC | Denegación sin exponer correos/teléfonos |

**Estado de esta matriz:** `FUTUROS_NO_EJECUTADOS`; no son pruebas aprobadas. Las etapas M02–M06 deberán incorporar contratos, pruebas positivas/negativas, integración real, consentimiento, accesibilidad y rollback según alcance.

## Comprobaciones y reversibilidad de la entrega documental

- **A0:** contrastar título, ID, fuentes y límites; el diff sólo puede añadir este documento y su sección autocontenida en CHANGELOG.
- **A1–A3:** revisión de escenarios, aislamiento tenant/RBAC y enlaces a contratos (documental). `NO_APLICA` para HTTP/MySQL/browser/entregabilidad, porque no hay modificación runtime. No atribuir PASS a escenarios futuros.
- **A4:** comprobar publicación fast-forward, blob y diff exacto, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre el SHA real; COLA/PLAN/BITACORA deben concordar antes de marcar LISTO DEV.
- **A5:** revisión técnica futura por otra de las cinco Tasks Javier, nunca autoaprobación. El cierre DEV no habilita QA/main/PROD.
- **Rollback documental:** `git revert` del commit sobre `dev` con nueva evidencia y sin force-push; no borrar BITACORA ni reescribir historial.
