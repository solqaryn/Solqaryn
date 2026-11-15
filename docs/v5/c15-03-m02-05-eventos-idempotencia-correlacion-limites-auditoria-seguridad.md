# SOLQARYN V5 FINAL — C15-03.M02.05: eventos, idempotencia, correlación y límites de auditoría de seguridad

**Autor:** PRIMARY DEV 04 — JAVIER_CHATGPT (`6aeacb7820ac8191b7d768a6f33dfa09`). **Repositorio exclusivo:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Resultado DOCUMENTAL M02.05**: contrato conceptual, sin publicar eventos, dispatcher, webhook, cron, worker, scheduler, cola ni API. No se reprograman las seis Tasks Javier.

## 1. Autoridad y preflight

Fuentes: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.06](c15-03-m01-06-bloqueos-rollback-liberacion-auditoria-seguridad-tenancy.md), [M02.01](c15-03-m02-01-modelo-entidades-ownership-auditoria-seguridad-tenancy.md), [M02.02](c15-03-m02-02-contratos-api-dto-errores-validaciones-seguridad-tenancy.md) y [M02.04](c15-03-m02-04-configuracion-tipada-ambitos-flags-seguridad-tenancy.md). Todas son especificaciones documentales sin permiso para instalar un bus.

Preflight por ID exacto: PLAN H2244=PENDIENTE/I sin owner, COLA G2244/O2244=SI, DAG F2205=VALIDADO/G2205=SI, con M01.06 A5 revisado independientemente. No inferir que el estado de otra fila o un self-report equivale a lease transaccional. M02.03 pertenece a otra Task durante esta preparación: no tocar sus archivos ni propiedad.

Objetivo futuro: disponer de evidencia mínima de controles tenant/JWT/RBAC, hallazgos, retest y A5 sin divulgar PII o tokens. La existencia de evento conceptual no autoriza consumo automático ni un ciclo perpetuo entre Tasks; la operación física sigue siendo la de seis Tasks Javier con sus agendas existentes.

## 2. Catálogo de señales hipotéticas no emitidas

| Señal | Condición de emisión posterior a autorizar | Regla de seguridad |
| --- | --- | --- |
| EV01 `AuditCutObserved` | Fuente y SHA verificados | No presentar corte incompleto como cobertura total |
| EV02 `SecurityFindingOpened` | Hallazgo con evidencia mínima saneada | No registrar payload de explotación ni PII |
| EV03 `SecurityFindingRechecked` | Retest con nuevo SHA | No borrar hallazgo original |
| EV04 `TenantBoundaryDenied` | Prueba negativa de acceso A/B | No incluir identificadores sensibles |
| EV05 `PermissionRevoked` | Revocación verificada del backend | No confiar en UI ni cabeceras |
| EV06 `EvidenceCheckCompleted` | Check exact-SHA completado | CANCELLED/SKIPPED/FAIL nunca PASS |
| EV07 `WorkClaimRecorded` | Claim exclusivo acreditado | No presentar Sheets como CAS atómico |
| EV08 `IndependentA5Reviewed` | Revisor distinto, Javier allowlist | No aceptar A5 propia |
| EV09 `DependencyGateChanged` | DAG recalculado por precedentes | No forzar G/O |
| EV10 `AuditDegraded` | CI/Drive/GitHub no disponible | INDETERMINADO, no éxito |
| EV11 `RollbackRequested` | Incidente y permiso de revert | No force-push ni restaurar secretos |
| EV12 `AuditCutClosed` | Evidencia completa, sin bloqueos | Sin scan real no afirmar cero P0/P1 |

**EV01–EV12 = DISEÑO FUTURO_NO_IMPLEMENTADO.** No hay infraestructura de eventos o productores nuevos. Los registros reales de cambios de estado continúan en BITACORA institucional con 26 campos A:Z y EVENT_ID/IDEMPOTENCY_KEY.

## 3. Envelope propuesto y validaciones

| Campo | Tipo conceptual | Requisito |
| --- | --- | --- |
| `eventId` | cadena única | No se reutiliza para payload distinto |
| `idempotencyKey` | hash/clave estable por alcance + operación | Dedupe consistente por emisor y versión |
| `eventType` | enum allowlist EV01–EV12 | Tipo desconocido se rechaza |
| `schemaVersion` | semver aprobada | Incompatibilidad se rechaza |
| `occurredAtUtc` | timestamp | No puede suplantar hora nativa de Task |
| `sourceSha` | SHA Git 40 caracteres | Cut/CI al mismo árbol |
| `microtaskId` | ID exacto del plan, nullable según señal | No crear filas para un evento |
| `tenantScope` | ámbito autorizado/backend | No asumir autoridad por valor cliente |
| `correlationId` | cadena opaca acotada | No revela token ni sesión |
| `payloadDigest` | hash de contenido saneado | Payload distinto con key igual = conflicto |
| `actorTaskId` | ID allowlist cuando aplica | No crear Task ni aceptar retiradas |
| `evidenceRef` | referencia de solo lectura | No URL arbitraria ni secretos |

Ningún campo se crea en DB, API o archivo de configuración real. La única autoridad presente para estados es PLAN H/I, A5 en K, COLA G/O y DAG F/G. Los futuros consumidores deben revalidar seguridad con servidor y permisos antes de emitir/leer, no derivar privilegios de la envoltura.

## 4. Idempotencia, orden, cuotas y recuperación

| Regla | Comportamiento exigido futuramente | Contrafallo que no se permite |
| --- | --- | --- |
| ID01 | Mismo key y digest devuelve mismo resultado lógico | Duplicar BITACORA o notificación |
| ID02 | Mismo key con digest distinto rechaza conflicto | Sobrescribir evento anterior |
| ID03 | Evento de SHA viejo se conserva pero no libera gate actual | Reutilizar PASS anterior |
| ID04 | Entrega fuera de orden se concilia por corte/estado | A5 llega antes de LISTO y desbloquea |
| ID05 | Lease dueño verdadero se valida al aceptar transición | Dos escritores |
| ID06 | Retest enlaza finding/patch/new SHA | Cerrar P0 sin reparación |
| ID07 | Error externo preserva estado anterior y contexto | Error se convierte en success |
| ID08 | Reintento eventual con presupuesto finito y nueva hipótesis | Bucle indefinido entre Tasks |
| ID09 | Cuota por cuenta/tenant/actor del backend | Evento permite abuso entre tenants |
| ID10 | Descarte o DLQ conservaría referencia a hallazgo | Silenciar incidente sin auditoría |
| ID11 | Rollback por commit normal y control exact SHA | Borrar historia para simular progreso |
| ID12 | Logs minimizados, correlación sin secretos | Payload de excepción expone token |

La cuota, retención, error budget, reintentos y DLQ son políticas para eventual aprobación separada. **No** implementarlas ahora. No diseñar un evento que autoeleve `PENDIENTE` a `LISTO`; sólo la gobernanza A0–A5 y una Task autorizada con readback puede registrar esa transición.

## 5. Diez pares de pruebas positivas/negativas futuras

| Caso | Positivo verificable eventualmente | Negativo |
| --- | --- | --- |
| T01 | Evento repetido produce una sola evidencia | Key idéntica con digest distinto sobrescribe |
| T02 | Tenant A consume sólo A por autorización backend | Payload B visible desde A |
| T03 | Check exact-SHA completed/success se vincula correctamente | CANCELLED/SKIPPED como PASS |
| T04 | A5 de Task distinta y autorizada | Autor se autoaprueba |
| T05 | Falta Drive/GitHub conserva INDETERMINADO | Falta evidencia se cuenta cero hallazgos |
| T06 | Retest negativo mantiene hallazgo abierto | Cierre sin patch/proof |
| T07 | CorrelationId permite rastreo sin datos personales | Token/PII aparece en log |
| T08 | Orden fuera de secuencia se detecta | Gate se abre sin precedentes |
| T09 | Presupuesto de reintentos es finito | Notificación recursiva interminable |
| T10 | Reversión conserva evidencia histórica de otra Task | Reset destruye BITACORA |

**T01–T10 = FUTUROS_NO_EJECUTADOS**. No hay test runtime del bus, MySQL, A/B bajo carga, tolerancia a fallos, SLA, penetración o incidentes reales en este documento.

## 6. A0–A5, impacto y rollback

- **A0:** comprobar fuentes y precedencias, 12 señales EV, 12 reglas ID, 10 pares T, dos Markdown, ausencia de secretos y encabezados vacíos.
- **A1:** revisión de escenarios positivos/negativos documentados, no afirmar ejecución de idempotencia.
- **A2:** preservación JWT/RBAC/tenant A/B, no exponer PII, no instalar event dispatcher.
- **A3:** concordancia con M01.06 y modelos M02.01/02/04, PLAN/COLA/DAG/BITACORA, sin cambiar código.
- **A4:** commit fast-forward/CAS en dev con documento + CHANGELOG, Scope Lock y V5 `completed/success` exact SHA, readback PLAN/COLA/BITACORA.
- **A5:** pendiente de revisión independiente de otra Task Javier, distinta de DEV04; ninguna autoaprobación.

Consumidores previstos: M02.06 gate terminal, M03 solo al habilitarse por DAG y autorización de código. No tocar QA/main/PROD, DB, mensajería, plataformas externas, llaves ni schedules. **Rollback:** revert Git normal y nuevo evento institucional compensatorio append-only, sin reescritura de historia.
