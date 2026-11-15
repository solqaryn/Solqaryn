# SOLQARYN V5 — C04-04.M02.05: eventos, idempotencia, correlación y límites de cobros

**Autor:** PRIMARY DEV 03 (Task `6adc2e4bd8a48191ac7d8641ed95a1a3`). **Alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, solo diseño **documental M02**, sin runtime de webhooks o dispatchers. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Fecha:** 2026-11-15.

## Límites institucionales y dependencias

El plan pide definir contratos de eventos, idempotencia, correlación y límites para el futuro dominio de **cobros y enlaces de pago**. `C04-04.M01.01–06` tienen estado LISTO+A5 de Task distinta y la fila `DAG_TECNICO` de M02.05 tiene F=VALIDADO y G=SI; `COLA G/O=SI` se verificó al claim. Este artefacto **no** instala un webhook, emitter, consumer, queue, base de datos, ledger, secreto, servicio externo, endpoint de proveedor ni programador de eventos. Las decisiones comerciales D01–D10 de M01 siguen abiertas; no existen tarifas, monedas, settlement ni cobros autorizados por este contrato.

El contrato M02.01 propone solicitud e intento de pago, evento externo y conciliación con ownership de empresa; M02.04 propone flags deshabilitados por defecto. Estas definiciones son independientes a nivel DAG y **no son implementaciones** que puedan garantizar durabilidad o inexistencia de cobros duplicados.

## Envoltorio de evento futuro — contrato abstracto, no DTO compilado

| Campo conceptual | Naturaleza / invariantes |
| --- | --- |
| `EventId` | UUID/ID opaco interno único; nunca permitir seleccionar ID final desde navegador |
| `SchemaVersion` | Versión exacta reconocida por allowlist; desconocida = rechazo seguro |
| `EventKind` | Enum futuro `PaymentIntentRequested / ProviderCallbackObserved / PaymentVerified / ReconciliationNeeded / RefundRequested / PaymentCompensated` |
| `EmpresaId` | Scope confiable derivado del servidor y relaciones del intento; no aceptar tenant por firma sola |
| `ActorId` | Actor institucional/servicio autenticado con permiso por operación |
| `ResourceId` | Referencia interna al intento o documento en misma empresa; nunca expone token bancario |
| `CorrelationId` | Identificador de rastro controlado sin PII, firma o importe sensible |
| `CausationId` | Evento antecedente validado; ninguna arista cross-tenant o ciclo tolerado |
| `IdempotencyKey` | Clave escopada a empresa, actor, tipo de operación y payload canónico |
| `ProviderEventId` | Referencia remota opaca; duplicados se normalizan y no generan segundo asiento |
| `OccurredAtUtc` | Tiempo declarado del emisor, **no autoridad** sobre llegada/efecto |
| `ReceivedAtUtc` | Tiempo del servidor confiable, usado para ventana anti-replay |
| `PayloadDigest` | Digest de contenido canónico, calculado server-side tras validación de firma y límites |
| `VerificationStatus` | `Untrusted / Authenticated / Correlated / Applied / Rejected`; no promover por respuesta frontend |
| `AttemptNumber` | Entero acotado para retry técnico sin cargo automático |
| `OutcomeCode` | Código seguro/redactado, nunca cuerpo HMAC ni credenciales |

Los tipos son ideas para un futuro contrato, no nombres de miembros de clases existentes.

## Invariantes de procesamiento futuro

1. **EV01 — Deny-by-default:** cualquier evento de proveedor sin firma HMAC validada, timestamp permitido, tamaño seguro, esquema admitido y anti-replay no produce efecto financiero.
2. **EV02 — Tenant:** resolver empresa contra intento interno autenticado; no confiar en subdominio, payload o propiedad de objeto del proveedor.
3. **EV03 — Autorización separada:** API para emitir, consultar, cancelar, conciliar o reembolsar usa permisos RBAC específicos y membresía vigente.
4. **EV04 — Durabilidad:** la futura deduplicación debe persistirse con índice único/transactional outbox o mecanismo equivalente; un `HashSet` in-memory no certifica seguridad de cobros.
5. **EV05 — Canonicalidad:** misma clave y digest coincidente devuelve estado existente; digest distinto causa conflicto auditable.
6. **EV06 — Concurrencia:** procesamiento simultáneo del mismo `ProviderEventId` no crea dos movimientos.
7. **EV07 — Pago no por redirect:** respuesta de navegador `success` no marca `Pagado`; exigir verificación y conciliación de proveedor autorizado.
8. **EV08 — Orden:** evento tardío o repetido no debe degradar `Aplicado` a `Pendiente` o repetir compensación.
9. **EV09 — Firma/timestamp:** comparar HMAC en tiempo constante en implementación real, con raw body limitado y ventana anti-replay explícita.
10. **EV10 — Fallo de red:** si estado es desconocido, recuperar por consulta segura y evidencia persistida; no repetir cargo a ciegas.
11. **EV11 — Retry acotado:** backoff, cuota, límite por empresa y poison-message aislado; no congestionar otras empresas.
12. **EV12 — Identidad de consumidor:** workers futuros no heredan autorización por ejecución, requieren identidad y ámbito de servidor verificables.
13. **EV13 — Doble gasto:** reconciliación/asiento y deduplicación se harán en transacción real, con pruebas concurrentes sobre MySQL efímero.
14. **EV14 — Compensación:** reversión conserva historia e IDs originales, usa permiso de refund, nunca borrado físico de ledger.
15. **EV15 — PII y secretos:** logs minimizados sin datos de tarjeta, URL firmada, token, llave HMAC o body completo de webhook.
16. **EV16 — Esquema/versionado:** versión desconocida = rechazo y alerta, sin migración silenciosa del estado económico.
17. **EV17 — Observabilidad:** métricas por decisión segura, razones redactadas, latencia y reintento, sin identificar a otros tenants.
18. **EV18 — Infraestructura:** no existe cola, webhook ni processor instalado por este documento, no desplegar ni contratar proveedores.
19. **EV19 — Puertas técnicas:** implementación futura .NET10/MySQL8.4/JWT/RBAC exige CI real, tenant A/B, pruebas HMAC y Scope Lock exact-SHA.
20. **EV20 — A5:** revisión independiente documental no autoriza activar API, PSP o producción.

## Matriz de escenarios FUTUROS, NO EJECUTADOS

| Esperado positivo | Negativo a bloquear |
| --- | --- |
| P01 Callback autenticado crea una observación correlacionada | N01 Firma HMAC inválida |
| P02 Reenvío idéntico conserva una aplicación | N02 Replay produce segundo asiento |
| P03 Empresa A observa sólo intento A | N03 Callback B actualiza intento A |
| P04 Digest coincide con payload aceptado | N04 Misma clave/digest diferente se acepta |
| P05 Estado incierto se reconcilia sin nuevo cargo | N05 Timeout causa cargo duplicado |
| P06 Evento tardío conserva estado financiero correcto | N06 Evento fuera de orden revierte liquidación |
| P07 Refund autorizado produce compensación trazable | N07 Usuario lector ejecuta devolución |
| P08 EventId/correlation permite auditoría redactada | N08 Log contiene clave PSP o datos de tarjeta |
| P09 Mensaje inválido agota cuota sin tocar otras empresas | N09 Retry sin límite satura sistema |
| P10 Replay durante concurrencia tiene resultado único | N10 Procesamiento paralelo registra dos asientos |

Los diez pares son **oráculos de implementación futura**. Ninguna prueba positiva, negativa, HTTP, HMAC, MySQL, benchmark o integración PSP se ejecutó en esta microtarea.

## A0–A5, dependencias y rollback

**A0:** diff sólo este Markdown y sección CHANGELOG autocontenida; no secretos ni cambios técnicos. **A1:** verificar 16 campos, 20 invariantes y diez pares P/N como diseño futuro, sin atribuir comportamientos observados. **A2:** verificar barreras JWT/RBAC, tenant A/B, deduplicación duradera, HMAC, cuotas y antifuga como requisitos, no runtime acreditado. **A3:** validar compatibilidad conceptual con M01.01–06 LISTO+A5 y M02.01/M02.04 documentales sin depender de su aprobación A5 propia; `.NET/MySQL/Playwright/PSP` = NO_APLICA a este diff de documentación. **A4:** commit Git fast-forward CAS exact-HEAD, Scope Lock y procedencia V5 `completed/success` sobre SHA de publicación, readback PLAN/COLA/DAG/BITACORA. **A5:** otra Task Javier, independiente del autor, deberá revisar SHA/blob/diff y no inferir permiso de despliegue.

**Rollback:** `git revert` normal del changeset Markdown. No crear webhooks, dispatcher de eventos, colas, cargos, migraciones, secretos, usuarios, cuentas, schedules ni cambios de QA/main/PROD.
