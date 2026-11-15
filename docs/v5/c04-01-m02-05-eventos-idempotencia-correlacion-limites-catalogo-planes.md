# SOLQARYN V5 — C04-01.M02.05: señales conceptuales, idempotencia y correlación de planes

**Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO, AGENTS.md y Skill canónica.

**Únicamente documentación de diseño M02.05:** ningún evento es emitido, persistido, programado o consumido. No se crea dispatcher, webhook, outbox, worker, broker, scheduler, endpoint, tabla, migración, tarifa, transacción, cargo, permiso o integración nueva. Todo caso P/N escrito es `FUTUROS_NO_EJECUTADOS`.

## 1. Entrada, procedencia y dependencia

- `C04-01.M02.05` cumplió PLAN H588=PENDIENTE/I588 vacío antes de CLAIM, COLA G588/O588=SI, DAG F=VALIDADO/G=SI y predecesor explícito `C04-01.M01.01–06+A5` en LISTO con revisión independiente.
- `C04-01.M01.06`, source SHA `960443f9eabe54645059597c34e3c6ea4eeb0064`, doc blob `54ac2a2a5f3855837039d1e5e80f0ce15faf1379`, protege tenant, precios, impuestos, idempotencia, historial y consentimiento comercial.
- Código existente `backend/src/Domain/Entities/SuscripcionesSaaS.cs`, blob `d9b5dbe19fb728293b00b748f10ac10336debd2d`: define `Plan`, `PlanLimite`, `Suscripcion` y sus estados. No debe confundirse con evento ya instalado ni con permiso de cobro.
- Los contratos M02.01–04 son complementarios y tienen gates/A5 independientes; ningún cierre de este texto autoriza M02.06 o M03 antes de dependencias reales.
- Stack ASP.NET Core net10.0, MySQL 8.4 con EF Core 10.0.12 y proveedor Oracle MySql.EntityFrameworkCore 10.0.9, Angular 22.2.1. Los futuros handlers deberán respetar Domain → Application → Infrastructure y API autoritativa.

## 2. Ocho tipos de señales NO EMITIDAS

| Código | Significado futuro | Fuente exclusiva y frontera |
| --- | --- | --- |
| EV01 PlanRevisionProposed | Propuesta de revisión de plan | Autoridad interna con política comercial; no activa tarifa |
| EV02 PlanRevisionApproved | Revisión aprobada | Doble evidencia comercial y auditoría; no crea RolPermiso |
| EV03 PlanLimitDefinitionChanged | Cambio propuesto de límite | Dominio + tenant definido; no altera uso por mensaje |
| EV04 TenantSubscriptionChanged | Cambio de asignación | Transacción backend validada por tenant y versión |
| EV05 SubscriptionSuspended | Suspensión acreditada | Estado persistido primero; ningún replay reabre |
| EV06 SubscriptionCancelled | Cancelación acreditada | Estado persistido irrevocable según contrato |
| EV07 PlanChangeRejected | Denegación de cambio | Razón saneada y correlación, sin existencia de B |
| EV08 CapabilityUsageObserved | Medición autorizada futura | Lectura agregada tenant-scoped; jamás grant |

**Las ocho señales son nombres hipotéticos, no mensajes actuales.** Su mera presencia no causa cargos, cambios de plan ni desbloqueos. Una decisión de precio, impuestos, consentimiento o contratación sigue pendiente de autoridad explícita y no surge por un evento.

## 3. Quince campos posibles de envoltorio, sin datos confidenciales

| ID | Campo hipotético y validación |
| --- | --- |
| F01 | SchemaVersion: enum de backend, desconocido = DENY |
| F02 | EventId: UUID opaco único, no autoridad del cliente |
| F03 | EventType: allowlist EV01–EV08 |
| F04 | OccurredAtUtc: fecha UTC de servidor |
| F05 | TenantDigest: hash contextual de empresa, no ID público |
| F06 | ActorDigest: referencia opaca de identidad |
| F07 | PlanRevisionRef: revisión comprobada y vigente |
| F08 | SubscriptionRefDigest: referencia hasheada por tenant |
| F09 | OperationCode: acción tipada allowlisted |
| F10 | PolicyVersion: versión autorizada, ausente = DENY |
| F11 | CorrelationId: identificador opaco, no token |
| F12 | CausationId: solicitud backend persistida y verificable |
| F13 | IdempotencyDigest: digest tenant/actor/action/version |
| F14 | PayloadDigest: hash de campos no sensibles permitidos |
| F15 | DecisionCode: OBSERVED/PROPOSED/REJECTED/APPROVED tras prueba previa |

Ningún campo transporta JWT, roles o permisos de cliente, contraseñas, PII, datos de tarjeta, URL firmada, impuestos privados, precio de cliente ni cadenas de conexión. Los registros deben redactarse y protegerse por tenant antes de cualquier publicación futura.

## 4. Catorce invariantes de idempotencia, seguridad y fallos

1. Solo una transacción backend acreditada puede ser fuente de evento; recibir un mensaje no constituye aprobación.
2. Un plan/entitlement no transforma una denegación JWT/RBAC/ABAC en permiso.
3. La identidad empresarial procede de membresía y grants backend, no header, token sin verificar o UI.
4. Idempotencia por tenant/actor/operación/versión/payload; no compartir replay de empresas A y B.
5. Mismo EventId o key con payload distinto = conflicto y cero mutación.
6. Reintento del mismo evento permitido no duplica suscripciones, períodos ni cobros.
7. Política de precios desconocida o decisión no aprobada = NO_GO económico.
8. Cancelación o revocación vigente prevalece sobre mensaje retrasado o caché antiguo.
9. CorrelationId/CausationId no equivale a autorización y nunca despierta otra Task ChatGPT.
10. Un evento propuesto no modifica `PlanLimite`, `Suscripcion` ni saldo contable.
11. Error de MySQL, auditoría, timeout o mensajería deniega sin estado parcial.
12. Auditoría y logs minimizan PII, recursos externos, secretos y tokens.
13. La aplicación futura exige controles de orden, concurrencia y eventual compensación probados en CI efímera.
14. Estados PLAN/COLA/DAG, BITACORA y CI exact SHA no cambian por un mensaje o webhook: solo eventos institucionales reales.

## 5. Doce pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Resultado positivo futuro | Negativo exigido |
| --- | --- | --- |
| P01/N01 | Revisión aprobada A queda auditada en A | Mensaje A cambia plan o revisión de B |
| P02/N02 | Mismo key/digest conserva un resultado lógico | Id repetido con payload nuevo realiza otra mutación |
| P03/N03 | Cancelación A confirmada prevalece en solicitudes futuras | Evento anterior reactiva cancelada |
| P04/N04 | Límite de plan válido se registra para revisión | Evento cliente eleva cuota o concede RolPermiso |
| P05/N05 | Política vigente permite registrar propuesta aprobable | Tarifa/moneda no aprobada activa cobro |
| P06/N06 | CausationId señala transacción backend validada | Texto arbitrario del cliente se acepta como prueba |
| P07/N07 | A y B reciben claves de correlación separadas | Hash/log permite asociar identidades de B a A |
| P08/N08 | Publicación fallida no cambia suscripción | Queue caída concede plan o genera cargo |
| P09/N09 | Soporte GET allowlisted observa dato permitido | Mensaje de soporte habilita escritura |
| P10/N10 | Reintento tras fallo usa idempotencia por tenant | Retry cross-tenant duplica inscripción |
| P11/N11 | CI causal completed/success en SHA exacto acredita prueba requerida | queued/skipped/cancelled/in_progress es PASS |
| P12/N12 | Revisión A5 independiente usa autor y SHA reales | Evento autoasigna A5 o hace promoción QA/PROD |

Los doce pares NO son tests de HTTP, JWT, MySQL, xUnit ni navegador ejecutados. Su implementación necesita otra microtarea autorizada, evidencias reales de integración A/B, CI en SHA y rollback; no se instalan triggers ni mecanismos automáticos en esta M02.

## 6. A0–A5 y rollback

- **A0:** diff limitado a documento y sección contigua de CHANGELOG, sin código, configuración, secretos o pagos.
- **A1:** revisar ocho tipos EV, quince campos F, catorce reglas y doce pares P/N FUTUROS_NO_EJECUTADOS; no afirmar que se emitieron.
- **A2:** comprobar JWT/RBAC/ABAC, tenant A/B, idempotencia, revocación, PII, aprobación económica y política fail-closed.
- **A3:** M01.01–06 LISTO+A5 y compatibilidad con `Plan`, `PlanLimite` y `Suscripcion`; builds .NET/Angular/SQL/MySQL/E2E = NO_APLICA para diff Markdown.
- **A4:** publicar fast-forward/CAS en dev, validar blob y SHA exacto; Scope Lock y procedencia V5 completed/success, registrar BITACORA y leer PLAN/COLA/DAG.
- **A5:** revisión auténtica por otra Task Javier distinta de la autora antes de habilitar sucesoras con gate A5.
- **Rollback:** git revert no destructivo de Markdown, evento compensatorio nuevo sin reescribir BITACORA; no hay worker, webhook, base, precio o dinero real que revertir.
- **Prohibición:** ninguna señal activa QA, main, PROD, secretos, infraestructura externa, cobros o Tasks externas.
