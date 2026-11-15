# SOLQARYN V16 — C06-01.M02.05: eventos, idempotencia, correlación y límites

**ID:** C06-01.M02.05 · **PLAN_MAESTRO:** fila 876 · **Autor:** PRIMARY DEV 05 JAVIER (6adc2e55f47c81919f6374e561687076) · **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA · **Rama:** solqaryn/Solqaryn@dev · PROJECT_SCOPE_LOCK=STRICT · **Riesgo:** V15 R0 documental (implementación posterior de eventos/PII se clasificará R2).

## 1. Alcance y fuentes comprobables

Esta unidad describe **solo** futuros contratos de eventos, límites de privacidad, idempotencia y correlación de la landing institucional SOLQARYN: propuesta de valor, planes, sectores, demo, contacto, legal y SEO. **No** crea ni activa dispatcher, workers, webhooks, Tasks, colas, endpoints API, formularios, Angular, MySQL/EF, SMTP, analítica/cookies, dominio, indexación, datos, secretos o publicación real. No introduce acceso a ERP ni autorización por header, slug o navegador. Seguridad JWT/RBAC y tenant A/B permanecen en backend.

Referencias documentales originales leídas desde dev: `C06-01.M01.01` blob `96d8280c96125af30b8f3f580c092460cb397b84` (NR01–NR12), `M01.02` `abc1084b024d32391559f02c365cf4111c5f739a` (actores e interfaces), `M01.03` `e3d90f0f7f49da2360b11120139ff4003d4ae50e` (doce fuentes y B01–B08), `M01.04` `c2b970feee1b2ab1d62349f21af394655070a77a` (D01–D08 **PENDIENTES**), `M01.05` `c6e254f6728a00a220216875341f8dd61065e936` (AC01–AC14 y N01–N14 **FUTUROS_NO_EJECUTADOS**), `M01.06` `5f13fec349767c5b6deccd8857eafd33109ae49d` (BL01–BL16, REV01–REV08). `C06-01.M02.02` es contrato API/DTO sin implementación, publicado en `be44d4af533ac25f05c83ebc791692c6863cfb39`. DAG y COLA son calculados, no se fuerzan.

Arquitectura: Angular 22.2.1 standalone + Zone.js, API ASP.NET Core 10, Domain ← Application ← Infrastructure y MySQL/EF Oracle 8.4; JWT/RBAC, auditoría y aislamiento A/B rigen cualquier implementación futura. No reemplazar el ERP, su router ni `tienda/*`.

## 2. Doce eventos conceptuales EV01–EV12 — NO_IMPLEMENTADOS

| Clave | Emisión futura permitida tras validación | Datos seguros y rechazo |
| --- | --- | --- |
| EV01 ContenidoPublicado | Editor autenticado y versión aprobada | Id y versión pública, sin tenant/PII |
| EV02 ContenidoRetirado | Administrador editorial RBAC | Motivo normalizado y versión, sin resurrección de contenido |
| EV03 PlanAprobado | Autoridad comercial con D01 aprobada | Catálogo versionado; jamás precio inventado |
| EV04 LegalAprobado | Autoridad legal con D02 aprobada | Jurisdicción, versión y fecha de aprobación verificables |
| EV05 DemoSolicitada | API futura con consentimiento válido | Identificador opaco, no tenant real ni credenciales |
| EV06 DemoRechazada | Rate limit/validador futuro | Motivo neutro, sin enumerar usuarios |
| EV07 ContactoRecibido | Backend con DTO mínimo y antiabuso | Referencia interna opaca sin mensaje/email en bus |
| EV08 ContactoLimitado | Política antispam futura | Contador agregado, nunca IP/PII cruda |
| EV09 ConsentimientoRevocado | Actor autorizado con prueba causal | No enviar SMTP ni reactivar solicitud revocada |
| EV10 DemoExpirada | Limpiador de fixtures sintéticos | TTL auditable sin recursos de PROD |
| EV11 SeoPublicoVersionado | Editor aprobado, rutas allowlisted | Canonical/robots/sitemap sin rutas ERP ni tokens |
| EV12 PublicacionRevertida | Flujo de reversión autorizado | SHA y versión editorial, sin permisos QA/main/PROD |

Estos eventos no existen en runtime por aparecer en una tabla. Ninguna fuente pública puede actuar como productor autorizado sin comprobación server-side y un contrato ejecutable versionado. Nunca traducir eventos a una Task/scheduler o dispatcher automáticamente.

## 3. Catorce invariantes de idempotencia ID01–ID14 — NO_IMPLEMENTADOS

| ID | Criterio verificable futuro | Fallo adversarial que debe bloquear |
| --- | --- | --- |
| ID01 | eventId opaco generado por servidor, único | Segundo efecto para el mismo eventId |
| ID02 | idempotencyKey con ámbito, tipo y ventana finita | Replay entre tenant A y B |
| ID03 | Productor interno con identidad/autenticidad verificadas | Evento fabricado anónimamente |
| ID04 | Consentimiento versionado para lead/contacto/demo | Persistencia sin consentimiento o revocado |
| ID05 | expectedVersion y CAS editorial seguro | Sobrescritura de publicación obsoleta |
| ID06 | correlationId aleatorio, no identificante | JWT, email, IP y mensaje en logs |
| ID07 | Ventana de deduplicación y TTL explícitos | Clave caducada reutilizada sin validación |
| ID08 | Atomicidad persistencia/outbox o compensación demostrable | Evento publicado tras rollback de datos |
| ID09 | Reintentos limitados/backoff con contador | Email/lead duplicado por retry infinito |
| ID10 | Dead letter privado con retención aprobada | Payload PII/secretos expuesto |
| ID11 | Rate limit antispam sin enumerar clientes | Envío masivo y 429 burlado |
| ID12 | Secuencia y versión monotónicas | Revocación vencida que reabre evento |
| ID13 | Integración externa desactivada sin permiso/secreto | SMTP/tercero activo por configuración implícita |
| ID14 | Prueba negativa de propiedad, rol y tenant A/B | Recurso de B accesible a A por slug/header |

**No** se afirma que CAS/outbox/HMAC, MySQL, colas, permisos o cifrado se hayan implementado. `eventId` y `correlationId` tampoco son tokens de autorización.

## 4. Sobre conceptual EventoPublicoV1 (sin serializador)

```text
EventoPublicoV1:
  eventId: opaque_server_generated
  eventType: allowlist_EV01_to_EV12
  schemaVersion: v1
  occurredAtUtc: trusted_server_timestamp
  correlationId: random_opaque_without_PII
  sourceRevision: approved_editorial_version
  scope: PUBLIC_EDITORIAL_or_PRIVATE_INTERNAL
  idempotencyDigest: server_derived_no_raw_email_or_token
  retentionClass: policy_approved
  restrictedPayloadRef?: internal_pointer_not_public_url
```

El campo `scope` no concede permisos; un consumidor futuro debe revalidar JWT/RBAC, pertenencia y ownership si accede a recursos empresariales. No exponer contenido del formulario en colas, trazas, métricas o respuestas públicas. Cualquier nuevo proveedor exige autorización y seguridad por separado.

## 5. Diez pares de aceptación P01–P10 / N01–N10 — FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo/frontera futuro |
| --- | --- | --- |
| P01/N01 | Editor autorizado publica contenido una vez | Anónimo no publica ni abre ERP |
| P02/N02 | Plan con aprobación comercial emite versión | Precio inventado no genera evento |
| P03/N03 | Legal aprobado registra versión/jurisdicción | Placeholder no se distribuye |
| P04/N04 | Demo consentida produce ID opaco | Token revocado o falta de consentimiento deniega |
| P05/N05 | Contacto validado crea referencia mínima | XSS y correo inválido son rechazados |
| P06/N06 | Replay idempotente evita dos efectos | Clave de A no sirve para B |
| P07/N07 | Transacción confirmada emite evento | Excepción/rollback no deja evento publicado |
| P08/N08 | Antispam aplica límite y espera | Reintentos no eluden 429 ni envían SMTP |
| P09/N09 | Demo expira y borra fixtures aislados | No lee datos reales ni revive tenant |
| P10/N10 | CI causal y revert futuro validados | FAIL/CANCELLED/SKIPPED/IN_PROGRESS no PASS |

**Cantidad de tests HTTP, MySQL, Angular, Playwright, integración o SMTP ejecutados en esta entrega documental: 0.** Las pruebas de aceptación detalladas son FUTURAS, no ejecutadas. La futura implementación R2 deberá aportar casos positivos, negativos, frontera, revocación, cross-tenant A/B, cancelación, replay, límites y, si modifica EF/outbox, MySQL 8.4 EFÍMERO real, sin proveedores de pago ni datos productivos. Si afecta UI, comprobar con Chromium/Playwright.

## 6. Riesgos, rollback, gates y estado

- D01–D08 siguen **PENDIENTES**; no autorizar marketing, precios, legal, consentimiento, indexación, cookies, SMTP o demo por este contrato.
- M02.05 es documento independiente permitido por DAG vigente; no habilita terminal M02.06 ni M03/M04/QA/main/PROD sin PLAN/COLA/DAG y A5 reales de predecesoras.
- Fail-closed: nunca tratar una cabecera de tenant, host, slug, idempotencyKey o identificador opaco como grant de plataforma o de otro cliente.
- Observabilidad mínima: correlación opaca, retención aprobada, redacción de PII y trazabilidad de denegaciones, sin reidentificación ni detalles de errores al público.
- **Rollback de este diff:** `git revert` normal del único commit documental (archivo y sección contigua de CHANGELOG), sin force-push ni reescritura de BITACORA. No hay base de datos o proveedor nuevo que restaurar.

## 7. Verificación proporcional V15 A0–A5

**A0:** revisar alcance, requisitos, 12 eventos, 14 controles, 10 pares P/N y 2 archivos Markdown del commit, sin secretos ni cabeceras vacías. **A1:** inspección coherencia evento/consumidor/rechazo, criterios positivos y negativos documentados pero **NO_EJECUTADOS** en runtime. **A2:** inspección de límites JWT/RBAC, tenant A/B, consentimiento, PII, replay y fail-closed escritos, sin certificar seguridad real. **A3:** `NO_APLICA` a build .NET, MySQL/EF, HTTP, Angular, Playwright y SMTP porque el changeset no cambia runtime. **A4:** Scope Lock y procedencia V5 `completed/success` sobre SHA causal o equivalencia de blobs demostrada, diff real, PLAN/COLA/BITACORA con readbacks. **A5:** revisión independiente por otra de las cinco Tasks Javier, distinta de DEV 05; puede rechazar.

**ESTADO:** sólo contrato R0 de planificación técnica. No hay eventos, colas o dispatcher ejecutables, ni autorización QA/main/PROD.
