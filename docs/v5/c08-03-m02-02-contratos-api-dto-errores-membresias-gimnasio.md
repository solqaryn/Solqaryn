# SOLQARYN V17 — C08-03.M02.02 — Contratos conceptuales de API, DTO, errores y validaciones para membresías de gimnasio

**Autor:** PRIMARY DEV 01 — JAVIER; TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-15. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, PLAN_MAESTRO fila 1233, `C08-03.M02.02`. **Entorno:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo:** R0, contrato documental exclusivamente; NO IMPLEMENTADO.

## 1. Alcance, predecesores y fronteras

Resultado exigido: definir contratos de API, DTO, errores, validaciones y compatibilidad para futuras membresías comerciales de gimnasio, plan/vigencia, perfil, suspensión y decisión de acceso. No se añade endpoint, controlador, DTO compilado, modelo EF, migración MySQL, lector, QR operativo, factura, PSP, worker, webhook, cambio JWT/RBAC, UI ni datos reales. Las rutas descritas son **propuestas futuras** y no un inventario de rutas actualmente disponibles.

Predecesores: `C08-03.M01.01–05` LISTO+A5 independientes y `C08-03.M01.06` LISTO+A5 independiente; contrato terminal de 16 barreras, 12 casos futuros y 8 reversas. Requiere PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI al CLAIM bajo candado `SQV17_C08_03_M02_02`. La preparación M02.01 se desarrolla de manera independiente y no se considera modelo físico ya publicado. D01–D08 de M01.04 siguen **PENDIENTES** y prohíben asumir proveedor de cobro, hardware, identificador de miembro, política de acceso o elección de persistencia definitiva.

La membresía de empresa del SaaS (`UsuarioEmpresa`, autenticación, roles y `SuscripcionesSaaS`) **no es** una membresía comercial de gimnasio. No derivar pertenencia ni permisos de `Cliente.EmpresaId` nullable; backend futuro deberá resolver tenant, sede, operador, recurso y titular con fuente autoritativa. Un contrato QR o una bandera frontend nunca otorga rol ERP, token o derecho físico.

## 2. Operaciones conceptuales O01–O08, NO implementadas

Todas las rutas a continuación son solo nombres de contrato propuestos `/api/gym/v1`; la URL real queda sujeta a arquitectura y revisión futura. Cada operación autenticada exige middleware/JWT válido cuando corresponda, autorización y aislamiento server-side por empresa y sede; denegación indistinguible de objetos ajenos sin revelar PII.

| ID | Método / operación futura | Entrada / salida propuesta | Reglas de seguridad y efectos |
| --- | --- | --- | --- |
| O01 | GET `/memberships` — listar | `MembershipQuery` → `MembershipPage` | Filtro tenant/sede siempre desde contexto servidor; paginación con límite, nunca listado global |
| O02 | GET `/memberships/{id}` — detalle | id opaco → `MembershipView` | Respuesta 404 indistinguible si no existe o pertenece a otro tenant |
| O03 | POST `/memberships/drafts` — borrador | `CreateMembershipDraft` → `MembershipView` | Rol de alta explícito; crea BORRADOR sin acceso vigente ni cargo |
| O04 | PATCH `/memberships/{id}/draft` — corregir borrador | `UpdateMembershipDraft` → `MembershipView` | Solo BORRADOR; versión ETag/If-Match; cambios validados y auditables |
| O05 | POST `/memberships/{id}/suspensions` — suspender | `SuspendMembership` → `MembershipView` | Revocación efectiva antes de nuevas decisiones; idempotencia obligatoria |
| O06 | POST `/memberships/{id}/renewal-requests` — solicitar renovación | `RequestRenewal` → `RenewalRequestView` | Solicitud PENDIENTE sin ampliar vigencia; pago/PSP solo tras autorización y evidencia futura |
| O07 | POST `/access-decisions` — evaluar check-in | `AccessDecisionRequest` → `AccessDecisionView` | Rechazar offline/QR replay; no activar puerta; consumo solo con transacción MySQL futura verificada |
| O08 | GET `/memberships/{id}/audit` — leer trazas | id + paginación → `AuditPage` | Solo rol de auditoría, redacción PII y correlaciones no sensibles |

**Ninguna operación se encuentra desplegada ni probada**; O07 no supone consentimiento para hardware, nuevos proveedores, workers, webhooks o cobros.

## 3. DTO y compatibilidad futura D01–D08

Los nombres siguientes son **esquemas lógicos**, no clases compiladas ni definiciones de EF. Identificadores `MembershipId`, `PlanId`, `BranchId` y `MemberId` serían opacos y nunca deben bastar como autorización.

| ID | DTO conceptual | Campos permitidos/obligatorios y límites |
| --- | --- | --- |
| D01 | `MembershipQuery` | sede autorizada opcional, estado de filtro en allowlist, cursor opaco, tamaño de página acotado |
| D02 | `MembershipView` | identificador opaco, sede/titular autorizados, plan comercial, estado, vigencia UTC, versión, sin JWT/QR/PII sensible |
| D03 | `CreateMembershipDraft` | titular resoluble, plan comercial permitido, sede, vigencia solicitada, idempotency key; sin estado ACTIVO impuesto por cliente |
| D04 | `UpdateMembershipDraft` | campos editables whitelist y versión obligatoria; prohíbe parche de EmpresaId, roles o efectivo pagado |
| D05 | `SuspendMembership` | razón allowlist, versión, idempotency key; actor tomado de JWT server-side |
| D06 | `RequestRenewal` | referencia comercial no secreta, vigencia solicitada y clave idempotente; jamás prueba de pago inventada |
| D07 | `AccessDecisionRequest` | recurso/sede propuestos, referencia opaca y nonce/TTL; credenciales de lector solo si integración futura aprobada |
| D08 | `AccessDecisionView` | `DENY` por defecto, decisión/razón segura, correlationId, tiempo UTC; nunca secreto QR, perfil ajeno o código de apertura |

Las salidas O01/O06/O08 usan envolturas versionadas de página/solicitud/auditoría derivadas de estos DTO, sin convertirlas en endpoints ejecutables. Versionar contratos de forma aditiva, rechazar campos desconocidos sensibles, mantener errores estables y proteger el esquema de retrocompatibilidad antes de migraciones.

## 4. Errores seguros ER01–ER12

| Error | HTTP futuro orientativo | Condición/rechazo esperado |
| --- | --- | --- |
| ER01 `AUTHENTICATION_REQUIRED` | 401 | token faltante, inválido o expirado |
| ER02 `ROLE_OR_MEMBERSHIP_DENIED` | 403 | rol revocado, empresa SaaS inactiva o permiso ausente |
| ER03 `RESOURCE_UNAVAILABLE` | 404 | inexistente **o** pertenencia de otro tenant: no confirmar existencia |
| ER04 `BRANCH_SCOPE_DENIED` | 403 | sede o recurso ajeno a permisos del actor |
| ER05 `INVALID_INPUT` | 400 | validación de tipo, longitud, allowlist o fechas |
| ER06 `VERSION_CONFLICT` | 409 | ETag o versión vencida; ningún write parcial |
| ER07 `STATE_TRANSITION_DENIED` | 409 | suspensión, borrador o estado incompatible |
| ER08 `MEMBERSHIP_EXPIRED` | 403 | borde de vigencia UTC `[inicio, fin)` vencido |
| ER09 `MEMBERSHIP_SUSPENDED` | 403 | suspensión o baja; denegación inmediata |
| ER10 `RENEWAL_NOT_VERIFIED` | 409 | renovación pendiente o evidencia comercial no verificada |
| ER11 `IDEMPOTENCY_CONFLICT` | 409 | misma clave con payload distinto o doble consumo |
| ER12 `ACCESS_DECISION_UNAVAILABLE` | 503 | lector offline, servicio caído o incertidumbre: **DENY** |

Nunca devolver PII, recursos ajenos, existencia de otro tenant, QR persistente, claims JWT, secretos o detalles del proveedor en errores. El status HTTP exacto es provisional y se someterá a tests de contrato y revisión de seguridad antes de implementación.

## 5. Reglas de validación V01–V16 (fail-closed)

| Regla | Invariante futura verificable |
| --- | --- |
| V01 | Tenant y sede resueltos por servidor, nunca por un `EmpresaId` manipulable |
| V02 | `UsuarioEmpresa` SaaS activo y rol requerido son distintos del derecho comercial gimnasio |
| V03 | Membresía con titular/sede/plan correctamente relacionados, sin `Cliente.EmpresaId` inferido |
| V04 | Alta crea BORRADOR, nunca ACTIVO sin evidencia comercial autorizada |
| V05 | Fechas UTC no ambiguas, zona IANA documentada, vigencia `[inicio, fin)` |
| V06 | Suspensión/revocación invalidan acceso/caché/QR antes de otra decisión |
| V07 | Rol de lectura no habilita alta, suspensión, renovación, auditoría ni check-in |
| V08 | Búsqueda y páginas limitadas, orden estables y filtros obligatorios por tenant/sede |
| V09 | ETag/versión evita escrituras perdidas y transiciones de estado contradictorias |
| V10 | Idempotency key con fingerprint; mismo reintento = mismo efecto; payload diferente = conflicto |
| V11 | Concurrencia y cupo/check-in deberán probar atomicidad real con MySQL 8.4 efímero |
| V12 | Token QR/lector futuro exige TTL, integridad y anti-replay; offline nunca permite acceso |
| V13 | Renovación no extiende vigencia sin confirmación comercial auténtica, jamás por callback no firmado |
| V14 | Logs mínimos de actor, tenant, motivo, UTC y correlación; no PII ni secretos |
| V15 | Errores de objetos A/B indistinguibles y sin filtración; roles revocados denegados |
| V16 | Cambios incompatibles y cancelación/timeout quedan DENY/PENDIENTE, nunca éxito falso |

## 6. Matriz futura de aceptación T01–T10 — NO_EJECUTADA

Estos son **oráculos para pruebas futuras**, no casos ejecutados, ni un test de integración o HTTP aprobado en esta microtarea.

| Caso | Positivo futuro | Negativo/frontera futuro |
| --- | --- | --- |
| T01 | Operador válido de A lista sólo miembros de A | B no aparece ni con filtros manipulados |
| T02 | Usuario de A consulta su perfil propio | ID B o UsuarioEmpresa inactivo: 404/403 seguro |
| T03 | Alta autorizada crea BORRADOR | Rol lectura o `EmpresaId` falso no crea nada |
| T04 | Edición de borrador con versión correcta | ETag obsoleto y campos prohibidos: rechazo sin write |
| T05 | Suspensión autoritativa corta nuevos accesos | QR cacheado tras revocación: DENY inmediato |
| T06 | Ventana UTC permite antes del fin | Exactamente en fin, DST ambiguo o reloj cliente alterado: DENY |
| T07 | Renovación verificada, si se autoriza PSP, puede tramitarse | Callback falso/replay o pago no verificado nunca activa |
| T08 | Check-in real futuro registra una sola operación | Dos requests paralelas + clave distinta: no doble consumo MySQL |
| T09 | Auditoría muestra sólo actor/tenant propios | PII, JWT, QR o tenant B no aparecen en respuesta/log |
| T10 | Timeout/lector offline responde DENY explícito | UI no muestra apertura exitosa ni inventa estado permitido |

El alcance futuro de `.NET 10` exige xUnit + auth/tenant A/B; EF/MySQL exige pruebas `Category=Integration` en MySQL 8.4 **efímero** con logs; UI Angular exige build, accesibilidad/Playwright Chromium. Sin acceso/permiso a un proveedor real no se declara test de PSP/hardware. No usar QA/main/PROD.

## 7. Impacto, calidad A0–A5 y rollback

- **A0:** únicamente este Markdown y una sección contigua de `CHANGELOG_AI.md`; requisitos M01.01–06/A5 y DAG revisados; no modifica Domain, Application, Infrastructure, API, MySQL, Angular, CI ni configuración de seguridad.
- **A1:** revisión estática de 8 operaciones, 8 DTO lógicos, 12 errores, 16 reglas y 10 pares de aceptación **futuros NO_EJECUTADOS**. No se presentan como asserts de xUnit ni HTTP.
- **A2:** comprobación de diseño fail-closed para tenant A/B, UsuarioEmpresa, JWT/RBAC, revocación, idempotencia, privacidad, QR y expiración UTC/DST. Ninguna protección runtime cambia.
- **A3:** validación conceptual de límites Domain → Application → Infrastructure/API y dependencias M01; pruebas reales HTTP, EF/MySQL, Angular/Playwright y PSP = `NO_APLICA_R0` motivado.
- **A4:** commit fast-forward/CAS en `dev`; confirmar Scope Lock y procedencia V5 `completed/success` en SHA exacto o equivalencia de blobs demostrada, más PLAN/COLA/BITACORA readback. Checks auxiliares pendientes o CANCELLED nunca PASS.
- **A5:** revisión técnica independiente por otra de las cinco Tasks Javier, distinta del autor. `LISTO_DEV` no equivale a `A5_PASS` ni autoriza gates sucesores por sí solo.

Rollback de este documento: `git revert` normal del commit causal en `dev`, conservación de BITACORA y recálculo de dependencias; jamás reset/force-push, secretos, datos reales o migración/despliegue. D01–D08 de M01.04 deben resolverse antes de implementar operaciones afectadas.
