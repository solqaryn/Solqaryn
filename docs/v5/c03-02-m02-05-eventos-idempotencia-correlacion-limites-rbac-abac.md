# SOLQARYN V5 — C03-02.M02.05: contratos de eventos, idempotencia, correlación y límites RBAC/ABAC

**Owner:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fecha:** 2026-11-15. **Autoridad:** STANDARD V5 FINAL, PLAN/COLA/DAG institucional, AGENTS.md y Skill canónica.

**Contrato únicamente documental de eventos hipotéticos.** No se implementa, registra ni ejecuta event dispatcher, webhook, broker, worker, scheduler, middleware, endpoint, base de datos, outbox ni integración real. Las señales descritas no representan cambios de estado de una Task ni permisos de otro entorno. Ningún mensaje puede producir LISTO, A5_PASS, grant, cobro, QA, main o PROD. Los escenarios se etiquetan FUTUROS_NO_EJECUTADOS.

## 1. Entrada institucional y predecesores

Se leyó `C03-02.M02.05` con PLAN H480=PENDIENTE/I480 vacío, COLA G480=SI/O480=SI y DAG F451=VALIDADO/G451=SI, con `C03-02.M01.01–06+A5` acreditados. El claim exclusivo se materializó por cambio condicional de H y lectura posterior de owner. Las unidades M02.01–04 son contratos complementarios independientes según el DAG; sus revisiones A5 ajenas no se asumen ni se sustituyen por este diseño.

Fuentes leídas de diseño: `docs/v5/c03-02-m01-06-bloqueo-rollback-liberacion-rbac-abac.md`, `docs/v5/c03-02-m02-01-modelo-entidades-ownership-invariantes-rbac-abac.md`, `docs/v5/c03-02-m02-02-contratos-api-dto-errores-compatibilidad-rbac-abac.md`, AGENTS.md, Scope Lock, contratos V5. `TenantPermissionContext`, `RequierePermisoAttribute` y `VerifiedCommercialTenant` son interfaces existentes de referencia; no prueban que este catálogo de mensajes exista.

## 2. Ocho tipos de mensajes conceptuales, NO EMITIDOS

| Código | Intención eventual | Origen confiable futuro | Destino y prohibición |
| --- | --- | --- | --- |
| EV01 MembershipChanged | Cambio verificado de membresía en un tenant | Comando backend autorizado y confirmado en transacción | Auditoría / invalidación; jamás grant por mensaje cliente |
| EV02 RoleGrantChanged | Versión de RolPermiso actualizada | Servicio RBAC después de validar actor y alcance | Invalidar caches del tenant exacto, no inferir un permiso |
| EV03 MembershipRevoked | Revocación acreditada | Autoridad backend con estado persistido | Priorizar DENY de solicitudes siguientes |
| EV04 ResourceAccessDenied | Acceso BOLA/IDOR o sucursal ajena bloqueado | Motor de autorización server-side | Auditoría saneada sin existencia de recurso ajeno |
| EV05 DataExportRequested | Exportación permitida pendiente de validación de filas | Caso de uso autenticado, grant específico | No generar archivo automáticamente por recibir el mensaje |
| EV06 MediaAccessRequested | Solicitud legítima de enlace de medio | Backend revalida tenant/owner antes de firmar | No incluir URL firmada en evento ni emitirla por defecto |
| EV07 SupportReadObserved | GET soporte allowlisted consumido | Servicio de soporte restringido y auditado | Prohibir escritura y lecturas fuera de la allowlist |
| EV08 AuthorizationPolicyVersionChanged | Reglas de autorización versionadas | Propietario autorizado, comprobación de política | Cambiar versión no convierte DENY en ALLOW |

Estos nombres no constituyen eventos actuales ni instrucción de implementación. Todo trabajo futuro que los cree deberá obtener DAG y permisos propios, idempotencia persistente, test de integración y A5 independiente. Una notificación sin readback no certifica la acción causal que describe.

## 3. Envoltorio hipotético de quince campos, sin datos sensibles

| Campo | Tipo / origen futuro | Límite fail-closed |
| --- | --- | --- |
| F01 SchemaVersion | string enum backend | Rechazar versión ausente o desconocida |
| F02 EventId | UUID/ID opaco único | Deduplicación, nunca usar ID provisto por UI como autoridad |
| F03 EventType | enum EV01–EV08 | No ejecutar comandos arbitrarios |
| F04 OccurredAtUtc | fecha real backend | No inferir orden perfecto por reloj |
| F05 TenantRefHash | referencia hash por empresa | No revelar empresa ni cruzar tenants |
| F06 ActorRefHash | identidad opaca backend | No incluir nombre ni email |
| F07 ResourceRefHash | referencia a recurso con salting/ámbito | No exponer ID ajeno, URL o archivo |
| F08 ModuleCode | enum allowlist | Denegar módulo desconocido |
| F09 ActionCode | enum allowlist | Solo acción legal del esquema |
| F10 DecisionCode | DENY/OBSERVED/CHANGED | Nunca crear permiso por valor del evento |
| F11 PolicyVersion | versión comprobada | Un evento desactualizado no revalida policy |
| F12 CorrelationId | opaco, generado por servidor | No introducir JWT o PII |
| F13 CausationId | referencia a operación originaria | Exigir prueba persistida antes de interpretar |
| F14 DedupeKeyDigest | SHA-256 de clave canónica con ámbito | Mismo digest con distinto payload => conflicto |
| F15 PayloadDigest | hash de contenido permitido | No guardar payload sensible, enlaces firmados ni tokens |

**Prohibido** agregar JWT, permisos delegados del cliente, contraseñas, tokens, PII, contenido de archivos, URL firmadas, headers de sesión, connection strings, pagos o datos productivos. El documento es un catálogo de campos, no una tabla ni una API.

## 4. Catorce reglas de idempotencia, correlación y seguridad

1. El origen de autoridad es siempre una transacción backend verificada, nunca la recepción de un mensaje.
2. Dedupe por tenant, actor, recurso, operación, versión y carga, sin compartir claves entre A/B.
3. Un EventId reutilizado con contenido diferente produce conflicto y NO_GO.
4. Una señal duplicada nunca concede grants ni ejecuta otra transición de recursos.
5. Las revocaciones prevalecen frente a mensajes de permisos anteriores o retrasados.
6. Una política desconocida o versión futura no degrada DENY a ALLOW.
7. CorrelationId y CausationId son opacos y no equivalen a autorización.
8. Las operaciones de exportación y medios revalidan permisos antes de bytes/URL, no al recibir mensaje.
9. Tenant y sucursal se resuelven en servidor, jamás desde URL/header/JSON.
10. El contrato de soporte se limita a GET allowlisted, expiración y auditoría.
11. Los reintentos y carreras se prueban con persistencia efímera, locks y compensación explícita.
12. Los logs no exponen JWT, hashes reversibles de identificadores sensibles ni PII.
13. Un evento hipotético no despierta una Task ni crea un bucle server-side.
14. PLAN/COLA/DAG, BITACORA y GitHub exact-SHA siguen como autoridades para estados y A5, nunca un webhook.

## 5. Doce pares de aceptación FUTUROS_NO_EJECUTADOS

| Par | Futuro positivo exigido | Futuro negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | Cambio de membresía A confirmado produce auditoría A | Mensaje de A pretende afectar membresía B |
| P02/N02 | Revocación A invalida acceso en request posterior | Evento de grant antiguo restaura privilegios revocados |
| P03/N03 | Retry mismo digest deja un único resultado | Payload distinto con mismo EventId muta recurso |
| P04/N04 | Tenant A obtiene correlation IDs solo de A | Log o evento permite correlacionar recursos B |
| P05/N05 | Mensaje con versión conocida se valida sin efectos implícitos | Tipo de evento desconocido ejecuta un comando |
| P06/N06 | Error de publicación no altera permisos | Queue caída produce allow o grant parcial |
| P07/N07 | Export A autorizado revalida filas antes de emitir archivo | Evento de export crea CSV con filas B |
| P08/N08 | Media A firmado solo tras permiso actual | Evento contiene URL firmada de B |
| P09/N09 | Support GET registrado dentro de allowlist | Señal de soporte habilita PUT/DELETE |
| P10/N10 | CausationId refiere a una operación backend comprobada | ID arbitrario del cliente se acepta como origen probado |
| P11/N11 | CI causal exact SHA concluida con success acredita la unidad aplicable | skipped/cancelled/in_progress se marca PASS |
| P12/N12 | A5 independiente exige Task distinta de autora y SHA real | Mensaje `A5_PASS` o webhook autodeclara aprobación |

No se emitieron ni consumieron estas señales. Ningún par se ejecutó en código, colas, HTTP, base de datos o navegador. La etapa posterior M03 exigirá implementación real, pruebas xUnit/HTTP, integración con MySQL 8.4 efímero donde proceda y aislamiento tenant A/B; la presente tarea no ofrece esa autorización.

## 6. Contratos posteriores, A0–A5 y rollback

La arquitectura Domain ← Application ← Infrastructure con API ASP.NET Core 10, frontend Angular 22.2.1, EF Core 10.0.12/MySql.EntityFrameworkCore 10.0.9 y MySQL 8.4 se mantiene sin modificaciones. Los eventos conceptuales deben ser aditivos, versionados, con handlers idempotentes y permisos backend; su eventual publicación exige nueva microtarea M03 específica y evidencias de pruebas causalmente necesarias. No se decide aquí R2, IAM, precios, QA/main/PROD ni despliegue.

**A0:** diff limitado a este Markdown y encabezado contiguo + viñetas de `CHANGELOG_AI.md`; no secrets, cambios ejecutables, ramas o PR nuevos. **A1:** revisar 8 señales, 15 campos, 14 reglas y 12 pares P/N como diseño FUTUROS_NO_EJECUTADOS. **A2:** JWT/RBAC/ABAC, tenant A/B, sucursal, revocación, idempotencia, PII y soporte fail-closed. **A3:** M01.01–06 LISTO+A5 y contratos M02 relacionados; backend/Node/MySQL/browser = NO_APLICA a Markdown, nunca PASS por omisión. **A4:** commit Git fast-forward/CAS en dev, Scope Lock y control V5 procedencia `completed/success` en SHA publicado, diff/blobs y readbacks PLAN/COLA/DAG/BITACORA. **A5:** revisión independiente de otra Task Javier distinta de DEV06, documentada por SHA y causalidad; no autoaprobación.

**Rollback:** `git revert` normal de Markdown sobre HEAD vigente, dejando historial BITACORA y autores originales; no existe un dispatcher, tabla, mensajero, worker, webhook, endpoint, SQL, QA/main/PROD o efecto externo que revertir en esta entrega.
