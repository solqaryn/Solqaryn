# SOLQARYN V5 FINAL — C02-04.M02.05: eventos, idempotencia, correlación y límites

**Alcance EXCLUSIVAMENTE DOCUMENTAL, INERTE.** Autor: PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Fecha: 2026-11-14. Repositorio solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT.

**NO IMPLEMENTAR EVENTOS TODAVÍA.** Este documento no crea ni ejecuta publicadores, consumidores, websockets, workers, brokers, colas, servicios, outbox, HTTP saliente, webhooks, cron, triggers o Tasks, ni modifica API, JWT/RBAC, MySQL, secretos o entornos QA/main/PROD. Los 6 Tasks Javier permanecen como ejecutores de desarrollo independientes, no forman un dispatcher continuo.

**Autoridades únicas:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill y AGENTS.md vigentes, Scope Lock. Gate individual de M02.05: DAG F=VALIDADO/G=SI y COLA G/O=SI para reclamo documental exclusivo, sin autorización a M03 ejecutable. C02-04.M01.01–06 están LISTO con A5 independiente.

## 1. Código real y contratos documentales consultados

| Origen | Blob SHA exacto | Comportamiento verificado y límite |
| --- | --- | --- |
| backend/src/Domain/Entities/RegistroAuditoria.cs | d669ac397a78ba02a5ba19e2cfb4bc9c7a115462 | Entidad actual con fecha, usuario, empresa, ámbito, resultado y CorrelationId; NO implica entrega de mensajes |
| backend/src/API/Controllers/PlataformaEmpresasController.cs | 87fbf187b7993ba0f05edff393500f24d0856833 | Crea RegistroAuditoria con HttpContext.TraceIdentifier, SaveChangesAsync y controles puntuales; NO implementa outbox ni eventos de concesiones excepcionales |
| backend/src/Infrastructure/Services/PlataformaScopeService.cs | 2a71a56750805ef513c7f312fb6c6c2d53a8c314 | Valida autoridad global actual contra persistencia, no confía en claim JWT aislado |
| docs/v5/c02-04-m02-01-entidades-ownership-invariantes-consola-global.md | 791a1cc82d0cd51c0d3515b88290072395593c7f | Identidad, MFA y grant excepcionales como diseño aún no ejecutable |
| docs/v5/c02-04-m02-02-api-dto-errores-compatibilidad-consola-global.md | 4c1b3444bccae8a6899e82b68a2cbf53141b4f4b | Versiones, DTO, errores y deduplicación como propuestas no instaladas |
| docs/v5/c02-04-m02-04-configuracion-ambitos-flags-seguros-consola-global.md | e002fba7da8a1382994e049e71178e1a3a8d864a | Opciones deny-by-default y kill-switch conceptuales, sin flags activados |

BITACORA del Sheet (26 columnas A:Z) gobierna microtareas; RegistroAuditoria es dato del producto. Ninguna sustituye la otra ni constituye un message broker.

## 2. Esquema conceptual de evento FUTURO (no publicado)

| Campo futuro | Regla tipada de preparación | Invariante de seguridad |
| --- | --- | --- |
| eventId | ID opaco, único, estable, sin datos de usuario | Dedupe identificable, sin token |
| eventType / schemaVersion | allowlist cerrado con versión, rechazo de desconocidos | No ejecutar código ni tipo arbitrario |
| occurredAtUtc | tiempo confiable del servidor | No usar reloj de navegador como verdad |
| correlationId / causationId | identificadores opacos acotados y saneados | No exponer PII, JWT, OTP o DSN |
| actorId / roleVersion | identidad y versión verificadas por backend | Evento no concede autoridad por sí mismo |
| targetTenantId / resourceId / operation | exactamente una empresa, recurso y acción | Prohibido wildcard y acceso A→B |
| decision / auditReferenceId | decisión mínima y auditoría durable confirmada | No anunciar éxito antes de commit |
| idempotencyDigest | digest de actor+tenant+operación+payload normalizado+versión | No reenviar contenido secreto o claves |
| payloadSafe | únicamente identificadores mínimos y resultado permitido | Sin credenciales, nombres privados o PII |

**No hay transporte seleccionado.** Futuro consumidor necesitará identidad de servicio, autenticación, autorización tenant específica, cuota, anti-replay, seguridad y despliegue autorizado; el contenido no crea infraestructura ni eventos.

## 3. Secuencia hipotética, idempotencia y límites

| Etapa conceptual | Precondición para implementación futura | Rechazo seguro |
| --- | --- | --- |
| Validar | Actor, tenant, recurso, operación, motivo, clave y versión normalizados | Clave repetida con contenido distinto causa conflicto |
| Autorizar | Rol global vigente, MFA, reauth reciente, grant acotado, scope y empresa exactos | Revocación, expiración o dato cliente falso causan denegación |
| Confirmar | Mutación sensible y auditoría durable bajo transacción real | Fallo de auditoría o DB causa rollback, no éxito |
| Preparar | Identificador de evento solo después de commit confirmado; outbox futuro solo con otra autorización | No mensaje ni garantía de entrega hoy |
| Entregar | Reintento finito con backoff, TTL, límite por actor/tenant y acuse verificado | No crear Tasks ni reintentar en bucle perpetuo |
| Consumir | Dedupe durable por eventId+tenant+schemaVersion y revalidación de permisos actuales | Duplicado, replay viejo o payload B en A se rechaza |
| Recuperar | Reconstrucción de estado desde verdad de DB/auditoría sin efectos repetidos | No reabrir grants revocados ni emitir éxito retrospectivo |

Semántica eventual de entrega: at-least-once con consumidor idempotente, NUNCA prometer exactly-once universal. Una invocación entre Tasks no es ciclo server-side infinito. Cuotas por ventana y tamaños deben fijarse positivamente con límites finitos, no valores arbitrarios de frontend.

## 4. Invariantes verificables de diseño

| ID | Invariante |
| --- | --- |
| EVT-01 | Un evento no concede rol global, MFA, reauth, grant ni permiso. |
| EVT-02 | Cada mensaje tiene tenant, recurso, operación y versión inequívocos. |
| EVT-03 | JWT/OTP/password/DSN no se copian al sobre, log ni correlationId. |
| EVT-04 | AuditReference requiere commit durable previo a éxito. |
| EVT-05 | Misma key con mismo contenido no genera efecto duplicado. |
| EVT-06 | Misma key con diferente actor, tenant, versión o contenido se rechaza. |
| EVT-07 | Mensajes de A nunca revelan datos a consumidores de B. |
| EVT-08 | Consumidor tardío revalida grant y no revive permisos caducados. |
| EVT-09 | Retry y cuotas son finitos, con backoff y denegación en error. |
| EVT-10 | No activar transporte, worker, outbox ni webhook por documentación o feature flag cliente. |
| EVT-11 | Auditor segregado lee evidencia mínima sin mutar tenants. |
| EVT-12 | A5 del proyecto y CI no son una autorización de deployment. |

## 5. Oráculos positivos y negativos simbólicos (NO tests ejecutados)

| Caso | Tipo | Situación futura | Oráculo esperado |
| --- | --- | --- | --- |
| P01 | POS | Operación A autorizada con auditoría confirmada | Referencia única causal solo tras commit |
| P02 | POS | Reintento idéntico idempotente | Un resultado lógico, sin doble mutación |
| P03 | POS | Revocación antes de consumo diferido | Consumidor niega nuevas acciones |
| P04 | POS | Auditor consulta evento saneado | Evidencia mínima sin permiso de mutación |
| P05 | POS | Schema versionado compatible y autorizado | Consumidor solo procesa contrato permitido |
| N01 | NEG | Idempotency key con payload distinto | Conflicto sin segundo efecto |
| N02 | NEG | Reutilización de key por empresa B | Rechazo y aislamiento interempresa |
| N03 | NEG | Éxito antes de commit de auditoría | Prohibir publicación, rollback |
| N04 | NEG | Rol, grant, MFA o sesión revocados | No reautorizar desde mensaje anterior |
| N05 | NEG | Retry supera cuota o TTL | Cortar, no loop infinito |
| N06 | NEG | Consumidor B recibe mensaje A | Rechazar sin exponer datos |
| N07 | NEG | Duplicado concurrente | Dedupe, único efecto |
| N08 | NEG | CorrelationId con JWT/OTP/PII | Sanitización y rechazo |
| N09 | NEG | Tipo desconocido o tamaño excedido | Denegar antes de procesar |
| N10 | NEG | Mensaje solicita crear Task o invocar webhook | NO_GO, cero invocaciones |
| N11 | NEG | Auditoría falla mientras opera | Rollback, no 2xx |
| N12 | NEG | Activación mediante texto documental o config cliente | No hay transporte habilitado |
| N13 | NEG | Auditor sin permiso intenta mutar | Deny |
| N14 | NEG | Grant caducado antes de consumo | Denegación actual obligatoria |

## 6. Verificación proporcional y rollback

- A0: source blobs reales y seis M01 LISTO/A5, diff de Markdown, ausencia de secretos y encabezados de CHANGELOG sin viñetas.
- A1: 5 criterios positivos y 14 negativos documentales, **no** ejecutados en xUnit/HTTP/MySQL/Playwright.
- A2: límites fail-closed JWT/RBAC, tenant A/B, auditoría, idempotencia, replay y sanitización de PII, sin cambios ejecutables.
- A3: NO_APLICA tests runtime; no worker, dispatcher, webhook, migración, API ni proveedor nuevo. Jobs auxiliares que estén in_progress/skipped no son PASS.
- A4: publicar documental mediante fast-forward/CAS dev, Scope Lock y V5 CHANGELOG exact-SHA completed/success, readback PLAN/COLA/BITACORA de 26 campos.
- A5: Task Javier independiente del autor debe revisar; LISTO_DEV documental no autoriza QA, main ni PROD.

Rollback de este documento: git revert normal sin reescribir historia; BITACORA mantiene auditoría histórica. Un sistema real de eventos necesita autorización y un scope posterior propio; **NO IMPLEMENTAR EVENTOS TODAVÍA**.
