# SOLQARYN V17 — C08-03.M02.05 — Contrato conceptual de eventos, idempotencia, correlación y límites de membresías de gimnasio

**Autor:** PRIMARY DEV 01 JAVIER; TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-15. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, PLAN_MAESTRO C08-03.M02.05 fila 1236. **Riesgo:** R0, documentación de diseño sin runtime. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Alcance, dependencias y límites

Definir contratos FUTUROS de eventos, correlación, idempotencia, límites y reversión asociados a membresías comerciales de gimnasio, vigencia, suspensión, acceso y perfil. Esta microtarea **NO CREA ni EMITE** eventos, workers, brokers, outbox, colas, webhooks, dispositivos, QR ejecutable, API, tablas, migraciones EF/MySQL, cambios JWT/RBAC, jobs, pagos o PSP. El ERP existente conserva autoridad económica, y la membresía de empresa SaaS `UsuarioEmpresa` no equivale al abono comercial de gimnasio.

Antecedentes: C08-03.M01.01–06 LISTO+A5; M02.01 modelo de veinte entidades/veinte invariantes; M02.02 contratos conceptuales API/DTO/errores; M02.03 proyecciones/índices/migraciones futuros. M02.04 (configuración) pertenece a otra Task y no se edita. Cada antecedente tiene sus puertas propias: `PLAN/COLA/DAG` y A5 independientes donde sean exigibles. D01–D08 de M01.04 permanecen PENDIENTES, no autorizar hardware, pago, TTL definitivo, identidad o política de entrada a partir de esta propuesta.

## 2. Catálogo de eventos FUTUROS EV01–EV16 — NO EMITIDOS

| ID | Evento conceptual | Payload permitido futuro | Denegación / observabilidad mínima |
| --- | --- | --- | --- |
| EV01 | `MembershipDraftCreated` | tenant, ref opaca, versión, tiempo UTC | Borrador no implica vigencia ni cobro |
| EV02 | `MembershipDraftChanged` | ref, versión anterior/nueva | CAS fallido nunca emite éxito |
| EV03 | `MembershipActivated` | estado, referencia ERP verificada | Sin validación comercial no activar |
| EV04 | `MembershipSuspended` | versión, motivo allowlist, instante | Revocación visible antes de nuevos accesos |
| EV05 | `MembershipCancelled` | estado terminal, correlación | Sin reapertura por replay |
| EV06 | `MembershipValidityRecomputed` | inicio/fin UTC e IANA, versión | DST ambiguo no amplía periodo |
| EV07 | `MembershipRenewalRequested` | solicitud PENDIENTE, operación opaca | Callback no firmado no autoriza |
| EV08 | `MembershipRenewalConfirmed` | conciliación ERP auténtica | No emitir por intento, doble cobro o timeout |
| EV09 | `MemberAccessEvaluated` | sede/recurso y decisión DENY/ALLOW sanos | Ningún secreto QR/PII ni decisión solo UI |
| EV10 | `MemberAccessRejected` | categoría de rechazo, correlationId | No revelar si existe objeto de B |
| EV11 | `AccessCredentialRevoked` | alias hash, versión y fecha UTC | Token cacheado no revive acceso |
| EV12 | `AccessCredentialExpired` | referencia opaca, TTL server-side | Reloj cliente no extiende validez |
| EV13 | `MemberBranchEntitlementChanged` | sede, área, versión y derecho | Sede B no se autoriza por plan de A |
| EV14 | `AccessDecisionConflictDetected` | fingerprint opaco, versión | Duplicado o cupo concurrente no aumenta derecho |
| EV15 | `GymAuditRecorded` | actor pseudónimo, tenant, UTC, motivo | JWT/QR, PAN, correo y biometría ausentes |
| EV16 | `GymReconciliationPending` | estado pendiente y presupuesto limitado | Sin transacción confirmada no se emite completo |

**Todos son NO_EMITIDOS y NO_EJECUTADOS.** Un nombre que expresa estado «confirmado» solo puede publicarse *después* de commit y conciliación de un origen autoritativo real. Ni UI, lector, QR, mensaje ni pago sin verificar constituye fuente de autorización.

## 3. Envoltura de evento futura — SIN CÓDIGO

```text
GymEnvelopeV1 {
  eventId: opaqueUniqueId,
  eventType: allowlist(EV01..EV16),
  schemaVersion: 1,
  occurredAtUtc: ISO8601_UTC,
  tenantRef: serverResolvedOpaque,
  branchRef: serverResolvedOpaque,
  membershipRef: opaque,
  aggregateVersion: positiveLong,
  correlationId: opaque,
  causationId: opaqueOptional,
  actorRef: pseudonymousAuthorizedPrincipal,
  idempotencyFingerprint: nonReversibleDigest,
  outcome: enumSafe,
  payload: schemaAllowlistedNoSecrets
}
```

Ningún campo del request del cliente/lector puede reemplazar tenant, sede, actor, rol o titular autoritativos. `eventId`, `correlationId` y `causationId` se usarían para rastrear, **nunca** para otorgar permisos. Nunca transportar JWT, refresh token, QR real, PAN, contraseña, documento de identidad, PII de A/B, biometría, callback PSP ni secreto de dispositivo.

## 4. Invariantes de idempotencia, privacidad y temporalidad INV01–INV18

| ID | Invariante futura verificable |
| --- | --- |
| INV01 | Fuente real del tenant/sede resuelta por backend, no por token QR o input |
| INV02 | Evento A jamás aparece o afecta consumidor de tenant B |
| INV03 | Identidad empresarial SaaS/roles se validan separada de abono gimnasio |
| INV04 | `eventId` único y (tenant, agregado, versión) no duplican efectos |
| INV05 | Misma idempotency key + fingerprint convergen a mismo resultado |
| INV06 | Misma key con distinto payload retorna conflicto sin escribir |
| INV07 | Reintento de renovación no factura dos veces; ERP sigue autoridad |
| INV08 | Suspensión, expiración y revocación invalidan cache/QR antes de permitir |
| INV09 | Ventana UTC `[inicio, fin)` y zona IANA explícita, DST no amplía vigencia |
| INV10 | Evento fuera de orden no regresa agregado a versión anterior |
| INV11 | CAS/versión entre check-ins concurrentes produce como máximo efecto autorizado |
| INV12 | Afirmación comercial `Paid` no se infiere de solicitud ni callback no firmado |
| INV13 | Auditoría mínima con actor pseudónimo, tenant, motivo, UTC, sin PII |
| INV14 | Dispositivo offline/timeout no interpreta ausencia de respuesta como ALLOW |
| INV15 | Worker futuro valida rol/tenant/estado de nuevo, mensaje no es autorización |
| INV16 | Presupuesto de reintento finito, backoff y cuarentena; sin loop infinito |
| INV17 | Consumidor desconocido/schema no soportado rechaza sin efectos secundarios |
| INV18 | Flag desactivado conserva ERP, acceso y UI actuales, sin activación implícita |

Ni `eventId` ni idempotencia lógica garantizan entrega «exactly once» entre DB, broker y PSP. Un diseño outbox transaccional será evaluado **solo cuando otra microtarea de implementación esté autorizada**, con rollback y pruebas reales; no existe uno nuevo por este documento.

## 5. Contratos operativos y límites FUTUROS L01–L10

| ID | Regla y límite no cuantificado aún |
| --- | --- |
| L01 | Límite de payload/schemas por versión, negar campos desconocidos sensibles |
| L02 | TTL de alias QR/credencial determinado por seguridad aprobada, no inventado |
| L03 | Rate limit por tenant/sede/dispositivo autenticado, sin fuga B |
| L04 | Reintentos finitos, DLQ/cuarentena futura sin crear worker presente |
| L05 | Orden por agregado/versión; out-of-order no revierte estado terminal |
| L06 | Corrección por compensación explícita, auditable, idempotente |
| L07 | Retención mínima, sin PII ni secretos y con borrado legal autorizado |
| L08 | Feature flags tipados no equivalen a permisos JWT/RBAC |
| L09 | Adaptador de PSP/hardware únicamente si proveedor y permiso están verificados |
| L10 | Integración transaccional con MySQL 8.4 efímero y negative tests antes de go-live |

Los valores numéricos de TTL, cuotas, payload y retención siguen **NO_MEDIDO/PENDIENTE**, nunca se inventan sin decisión y medición.

## 6. Escenarios futuros positivos y negativos T01–T12 — NO_EJECUTADOS

| Caso | Positivo futuro | Negativo/frontera futuro |
| --- | --- | --- |
| T01 | Evento miembro A se consume en A | Nunca entregar/crear objeto de B |
| T02 | Draft registrado conserva único evento | Reintento con mismo key no duplica |
| T03 | Key igual+payload igual converge | Key igual+payload distinto rechaza |
| T04 | Suspensión bloquea acceso desde tiempo efectivo | Evento tardío/QR cacheado no revive |
| T05 | Expiración UTC en [inicio,fin) opera | En fin exacto/DST ambiguo => DENY |
| T06 | Renovación conciliada ERP emite una confirmación | Callback falso/replay no factura ni activa |
| T07 | Actor con rol/sede correctos recibe evento saneado | Rol revocado u operador B no consume |
| T08 | Dos accesos autorizados seriados respetan cupo | Carrera paralela no supera cupo en MySQL efímero |
| T09 | Mensaje versión n+1 se procesa tras n | Out-of-order o versión obsoleta no revierte |
| T10 | Logs muestran correlationId opaco y UTC | Sin JWT, QR, biometría, PII, PAN ni datos B |
| T11 | Error transitorio se cuarentena según presupuesto | No ciclo infinito ni falso `Confirmed` |
| T12 | Flag apagada preserva sistemas actuales | Documento no activa lector, pago, API ni worker |

**Resultado real de esta tarea documental:** cero eventos emitidos, cero tests runtime ejecutados, cero migraciones/DB, cero operaciones de PSP y hardware. Los escenarios son un contrato futuro.

## 7. Evidencia A0–A5, interfaces, rollback y NO-GO

- **A0:** PLAN/COLA/DAG, M01.01–06, M02.01–04, Scope Lock, ownership; diff limitado a este Markdown y sección contigua de CHANGELOG.
- **A1:** revisión estática de EV01–EV16, INV01–INV18, L01–L10 y T01–T12; los oráculos NO_EJECUTADOS nunca se presentan como tests.
- **A2:** tenant A/B, SaaS vs abono gimnasio, JWT/RBAC revocado, TTL/DST, concurrencia, idempotencia, secretos/PII y rechazo offline diseñados sin tocar código.
- **A3:** `.NET`/API/MySQL/EF/Angular/Chromium/PSP/hardware/colas/worker = `NO_APLICA_R0` por diff exclusivamente documental; la implementación futura exige CI e integración verdaderas.
- **A4:** GitHub dev fast-forward/CAS, SHA/blobs preservados, Scope Lock y V5 `completed/success` por SHA exacto, PLAN/BITACORA readback; CI queued/in_progress/skipped/failed no es PASS.
- **A5:** revisor distinto de PRIMARY DEV 01 entre las cinco Tasks Javier restantes; ningún auto-PASS ni autorización para QA/main/PROD.

**Rollback:** `git revert` normal de este commit bajo HEAD actual y auditoría conservada. Nunca force-push, reset, borrar BITACORA, modificar fórmulas G/O, activar infraestructura productiva ni crear Tasks o webhooks. M02.06 y M03 seguirán bajo predecesoras y A5 independientes.
