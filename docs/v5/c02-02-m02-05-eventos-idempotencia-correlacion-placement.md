# SOLQARYN V5 FINAL — C02-02.M02.05: eventos conceptuales, idempotencia y correlación

**Autor:** PRIMARY DEV 04 JAVIER; TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Ámbito:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT. **Fecha:** 2026-11-15.

**Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG por ID, Skill canónica, M01.06 y contratos C02-02.M02.01–04. Se preservan `AppDbContext` y `StorageTenantContext` actuales; no se activa shared/dedicated routing.

**Natureza del cambio:** contrato de diseño futuro para notificaciones de estados de placement, no emite eventos ni instala dispatcher, webhook, cola, worker, invocación de otra Task, transporte, tabla, SQL o llamada entre entornos. Las señales no otorgan autoridad. Es obligatorio separar el evento hipotético de toda ejecución real.

## 1. Señales futuras SIG01–SIG08 (NO EMITIDAS)

| ID | Señal propuesta | Precondiciones y límite |
| --- | --- | --- |
| SIG01 `PlacementReviewRequested` | Actor backend autorizado, tenant resuelto, intento idempotente |
| SIG02 `PlacementReviewRejected` | Motivo saneado, sin revelar target de otro tenant |
| SIG03 `MigrationPlanPrepared` | Plan y revisión registrados, sin ejecutar copia |
| SIG04 `SnapshotRestoreVerified` | Solo tras prueba de restauración real futura, no por texto |
| SIG05 `ConsistencyCheckFailed` | Hash/FK/filas divergentes: obliga NO_GO |
| SIG06 `CutoverDenied` | Fencing o permiso ausente; no mutación de router |
| SIG07 `PlacementChanged` | Únicamente tras commit transaccional autorizado y readback |
| SIG08 `RollbackConfirmed` | Restore/cambio validado contra versión previa, no `git revert` |

NINGUNA SIG01–SIG08 es llamada desde código actual. No se crean hooks, Task programada, servicio de pago, evento de nube, mensaje de Angular ni nueva integración.

## 2. Sobre conceptual de evento EVT01–EVT15

| Campo hipotético | Tipo / regla |
| --- | --- |
| EVT01 `eventId` | UUID/ID único generado servidor, no cliente |
| EVT02 `eventType` | Enum SIG01–SIG08 |
| EVT03 `schemaVersion` | Entero positivo de versión compatible |
| EVT04 `tenantRef` | Identificador opaco obtenido de backend |
| EVT05 `actorRef` | Actor redactado, permiso backend verificado |
| EVT06 `sourceSha` | Commit 40 hex exactos y comprobados |
| EVT07 `sourceRevision` | Revisión del mapping de procedencia |
| EVT08 `idempotencyKey` | Tenant + operación + intento + versión |
| EVT09 `correlationId` | Referencia trazable con datos saneados |
| EVT10 `causationEventId` | Evento anterior validado, sin cadenas cíclicas |
| EVT11 `occurredAtUtc` | UTC confiable, no timestamp de navegador |
| EVT12 `environment` | Dev, sin autorización implícita QA/PROD |
| EVT13 `assignmentRevision` | Versión validada de placement |
| EVT14 `resultCode` | Resultado genérico redactado |
| EVT15 `payloadDigest` | Hash de campos permitidos sin PII/DSN |

El payload NO incluye DSN, host de MySQL, token, password, fila de otra empresa, datos personales, backup, secretos o enlaces privilegiados. No se acepta `eventType` recibido desde el navegador como mandato de infraestructura.

## 3. Reglas de idempotencia y seguridad REG01–REG14

1. REG01 Enviar o procesar evento futuro exige actor backend autenticado, JWT validado, membresía vigente y RBAC.
2. REG02 Se obtiene tenant de servidor y se comprueba propiedad A/B en cada transición.
3. REG03 IdempotencyKey se vincula a tenant, operación, intento, versión y SHA; el cliente no la usa como permiso.
4. REG04 Una clave repetida con mismos argumentos devuelve resultado único y no duplica writes/jobs.
5. REG05 Una clave repetida con payload o tenant diferente se rechaza sin exponer recursos.
6. REG06 Un evento causal inexistente o circular no modifica estado.
7. REG07 `sourceSha` distinto al contrato/artefacto ejecutado falla cerrado.
8. REG08 Orden y versión se revisan con fence monotónico antes de cualquier cutover.
9. REG09 `SnapshotRestoreVerified` requiere evidencia de restauración efectivamente comprobada, no documento.
10. REG10 `PlacementChanged` nunca equivale a permiso de producción, migración o A5 independiente.
11. REG11 Eventos no pueden modificar secreto, DSN, ruta DB, permisos de usuario ni destinatario de otro tenant.
12. REG12 Reintento con proveedor caído conserva estado no terminal y audita NO_GO con límites estrictos.
13. REG13 Retención, redacción, cuotas y consentimiento deben definirse antes de instalar transporte futuro.
14. REG14 A5 y check exact-SHA requerido `completed/success` no se sustituyen por telemetría ni eventos.

## 4. Orden y correlación sin servidor de eventos

Relación conceptual: `MigrationIntent` 1:N `PlacementAuditEvent`, con `PlacementRevision` y `FenceEpoch` como autoridad causal. La ordenación no depende de entrega FIFO del proveedor; si llega fuera de orden, validar versión y rechazar mutación. Un `correlationId` puede agrupar intentos, pero jamás revelar empresa o rol. Los estados futuros solo pueden avanzar por la misma máquina de estados del modelo M02.01 y con el mismo contrato idempotente. No se asume bus, topic, worker, transacción outbox ni at-least-once configurados aquí.

**Cuotas de diseño:** máximo conceptual de 100 eventos por intento y 500 observaciones de diagnóstico por ventana acotada, sujeto a evaluación de capacidad real futura; un exceso devuelve NO_GO. Estos números son propuestas, no límites aplicados en producción ni cambios a las seis Tasks Javier.

## 5. Oráculos positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativa obligatoria |
| --- | --- | --- |
| P01/N01 | Evento de empresa A atribuido a actor A autorizado | Empresa B observa A o cambia destino A |
| P02/N02 | IdempotencyKey repetida conserva un único resultado | Retry genera doble cutover |
| P03/N03 | Causalidad válida entre revisión y evento | Cadena circular avanza estado |
| P04/N04 | SHA exacto coincide con artefacto futuro | SHA anterior toma CI de otra versión |
| P05/N05 | Restore real previo a señal verificada | Documento se toma como restore ejecutado |
| P06/N06 | Rechazo redactado con ID de correlación | Evento revela token/DSN o PII |
| P07/N07 | Error de proveedor deja NO_GO reanudable | Falla parcial produce éxito silencioso |
| P08/N08 | Fence nuevo autoriza cambio futuro | Intento obsoleto sobreescribe revisión |
| P09/N09 | Candidato documental solo habilita análisis | Evento ficticio activa worker/cuenta externa |
| P10/N10 | A5 independiente con exact SHA y autorización de entorno | Autoría propia/CI cancelada dispara release |

Estos diez pares **no son tests ejecutados** ni evidencia de producción; no hay HTTP, MySQL, eventos, Playwright ni CI causal de runtime atribuibles al documento.

## 6. A0–A5 y rollback

**A0:** diff exclusivo Markdown y CHANGELOG con viñetas contiguas, sin credenciales, código, despliegue ni worker. **A1:** verificar estructura de SIG01–08, EVT01–15, REG01–14 y P/N futuros. **A2:** JWT/RBAC, tenant A/B, idempotencia, error redacted y denegación por defecto. **A3:** coherencia con M01.06 y M02.01–04, sin asumir existencia de servicio de eventos. **A4:** publicación fast-forward en dev con HEAD esperado y controles Scope Lock y V5 `completed/success` en SHA exacto; PLAN/COLA/DAG/BITACORA readback. Compilaciones .NET/Angular/MySQL no causales al diff Markdown se clasifican NO_APLICA, jamás PASS. **A5:** otra Task Javier revisa SHA, blobs, contrato, seguridad y evidencia; sin autoaprobación.

**Rollback aplicable:** `git revert` normal de este changeset documental; ningún evento/worker/tabla/operación real requiere reversión. Sin QA, main, PROD, DNS, secretos, datos reales, cuentas ni servicios de pago.
