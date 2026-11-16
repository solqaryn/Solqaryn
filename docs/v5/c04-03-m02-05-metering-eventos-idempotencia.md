# SOLQARYN V17 — C04-03.M02.05: eventos, idempotencia y correlación de metering

**Autor:** PRIMARY DEV 05 Javier, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Origen autorizado:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO fila 660, COLA G/O=SI, DAG_TECNICO fila 621 VALIDADO/SI; predecesor C04-03.M01.01–06 + A5. **Candado:** `SQV17_C04_03_M02_05` / `2000000660`. **Entorno:** exclusivamente `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## A0 — alcance, arquitectura y límites

Entrega **R0 exclusivamente documental** del contrato futuro de eventos para medición multiempresa de consumo (usuarios, operaciones, archivos, almacenamiento). No implementa API, DTO en código, workers, colas, Kafka, webhooks, suscripciones, proveedor de pagos, tablas, EF, migraciones, facturas, cálculos de precios, cuotas activas ni cobros. No certifica interoperabilidad HTTP/DB ni operación en producción. Respeta el modelo conceptual de `C04-03.M02.01` y el contrato `C04-03.M02.02`; `C04-03.M02.03/.04/.06` tienen entregas y gates propios. Las decisiones comerciales D01–D08 aún requieren autorización explícita; ningún evento produce una operación financiera.

**Fronteras futuras:** Application valida origen y derechos; Domain modela invariantes; Infrastructure únicamente persistirá y publicará tras implementación, migración, pruebas y autorización específicas; API nunca debe aceptar `EmpresaId`, cantidad, unidad o idempotency identity no confiables. Los consumidores deberán confirmar los contratos y leer la política vigente antes de activar cualquier efecto.

## Catálogo propuesto de eventos (12, no implementados)

| Evento conceptual | Condición de emisión futura | Efecto permitido por este documento |
| --- | --- | --- |
| `metering.source.observed.v1` | Operación backend auténtica recibida | Describir evidencia, sin sumar consumo |
| `metering.usage.accepted.v1` | Origen, tenant y ledger validados | Propuesta de asiento aditivo, no cobro |
| `metering.usage.rejected.v1` | Permiso, unidad u origen inválidos | Denegación auditable, sin asiento |
| `metering.usage.duplicate.v1` | Replay de misma clave y digest | Señalar resultado anterior sin duplicar |
| `metering.usage.conflict.v1` | Clave previa con digest distinto | Rechazar, nunca actualizar el original |
| `metering.window.closed.v1` | Ventana UTC certificada | Referencia a cierre inmutable de periodo |
| `metering.projection.rebuilt.v1` | Conciliación desde ledger autoridad | Recalcular proyección sin editar ledger |
| `metering.entitlement.checked.v1` | Política y permiso vigentes leídos | Evidencia de decisión, no habilitación autónoma |
| `metering.adjustment.requested.v1` | Ajuste vinculado a evento previo | Solicitud sin alterar historia |
| `metering.adjustment.approved.v1` | Aprobador/RBAC autorizados y auditados | Futuro asiento compensatorio separado |
| `metering.reconciliation.opened.v1` | Diferencia entre fuentes/proyección | Abrir discrepancia, prohibir factura automática |
| `metering.audit.anomaly.v1` | Replays, corrupción o PII sospechosa | Alertar sin exponer payload sensible |

## Sobre conceptual mínimo y versiones

Campos futuros: `EventId` UUID único, `EmpresaId` de sesión backend autenticada, `EventType` allowlist, `SchemaVersion`, `SourceSystem` allowlist, `TrustedOperationId`, `IdempotencyKey`, `PayloadDigest` SHA-256 de representación canónica sin secretos, `CorrelationId`, `CausationId`, `MetricCode`, `MetricCatalogVersion`, `Unit`, `Quantity` no negativa y de escala declarada, `WindowKey`, `PolicyRevision`, `OccurredAtUtc`, `RecordedAtUtc`, `ActorEvidenceRef`, `SourceEvidenceRef` y `OutcomeCode`. La API futura omitirá campos sensibles al presentar proyecciones. Identificadores de actor/recurso deben ser referencias mínimas, no PII ni tokens.

Versionado: v1 requiere validación estricta de campos y tipos. Desconocidos críticos, versiones no soportadas, eventos fuera de orden sin causa o referencias incompatibles son rechazados, no reinterpretados silenciosamente. No se autoriza aceptar nombres de eventos controlados por cliente para dirigir routing. `CorrelationId` agrupa diagnósticos, **no** reemplaza `IdempotencyKey` ni autoriza tenants.

## Idempotencia, seguridad y límites: 16 controles que se implementarán y probarán posteriormente

1. **I01:** clave única futura `(EmpresaId, SourceSystem, IdempotencyKey)`; el digest NO forma parte de la unicidad: un mismo key con distinto digest implica conflicto, no segunda operación.
2. **I02:** el digest canónico se compara con el registrado antes de devolver replay; nunca sustituir `PayloadDigest` del original.
3. **I03:** dos requests concurrentes con misma identidad tienen a lo sumo un efecto ledger, acreditado por transacción/constraint real en futura implementación MySQL.
4. **I04:** la identidad `EmpresaId` procede del contexto servidor reautenticado; no confiar en header, query ni cuerpo.
5. **I05:** JWT, membresía, RBAC y revocación se validarán en cada operación sensible; falta de autorización deniega.
6. **I06:** `Tenant A` no puede leer ni deduplicar eventos de `Tenant B`, incluso con claves idénticas.
7. **I07:** `SourceSystem` y `TrustedOperationId` deben estar vinculados a evidencia backend verificable y a su tenant.
8. **I08:** `MetricCode/Unit/Scale` están tipados contra catálogo versionado; sin conversión arbitraria entre bytes, operaciones o usuarios.
9. **I09:** las ventanas usan UTC confiable, fronteras no solapadas e inclusividad explícita; reloj del cliente no define cierre.
10. **I10:** cantidades, contadores, estados desconocidos, overflow, `NaN` y unidades inválidas generan rechazo, nunca cobro.
11. **I11:** `ledger + idempotency identity + outbox` se confirman atómicamente o quedan pendientes de reconciliación sin éxito anunciado.
12. **I12:** reintentos de entrega tienen backoff acotado, identidad estable y dead-letter bajo RBAC; `at least once` no implica doble efecto.
13. **I13:** orden de versiones/causación se comprueba al aplicar cambios; eventos tardíos van a conciliación, no reescriben periodos cerrados.
14. **I14:** borrado/corrección lógica se expresa como evento compensatorio aditivo y enlace al original; historia no se borra.
15. **I15:** logs y mensajes de error omiten secretos, emails, tokens, datos bancarios y payload bruto; correlación usa identificador seguro.
16. **I16:** D01–D08, A5 independiente, migrations/CI MySQL 8.4 y autorizaciones de producción son puertas futuras separadas, nunca concedidas aquí.

## A1/A2 — escenarios de aceptación FUTUROS (no ejecutados)

| Positivo futuro | Negativo que deberá fallar |
| --- | --- |
| P01 replay idéntico devuelve resultado previo | N01 mismo key y digest diferente genera doble ledger |
| P02 Tenant A emite A y B emite B con su propio scope | N02 lectura/dedupe cruzada con claves iguales A/B |
| P03 operación con JWT/RBAC actuales se registra | N03 JWT, membresía o permiso revocados aceptados |
| P04 source autenticada determina cantidad/unidad | N04 cliente altera cantidad, unidad o SourceSystem |
| P05 concurrencia tiene un único resultado efectivo | N05 race genera dos asientos confirmados |
| P06 timestamp servidor ubica ventana UTC correcta | N06 reloj cliente cierra una ventana distinta |
| P07 evento desconocido/versionado se rechaza | N07 downgrade o campo crítico ignorado |
| P08 timeout deja estado recuperable con replay | N08 error parcial devuelve éxito de consumo |
| P09 ajuste firmado vincula el asiento original | N09 corrección destructiva elimina historia |
| P10 conciliación detecta divergencia proyección | N10 divergencia provoca factura automática |
| P11 logs usan referencias sin PII | N11 logs incluyen email, JWT o payload |
| P12 consentimiento y entitlement vigentes se revalidan | N12 política ausente habilita quota/cobro |

Estos 12 pares son **criterios para tests posteriores**, no asserts ejecutados; los tests MySQL/HTTP/roles auténticos deben construirse en sus microtareas de código, sin reutilizar mocks para acreditar integración.

## A3/A4/A5 y rollback

**A3:** MySQL 8.4/EF, API/HTTP, navegador, proveedores y despliegue son `NO_APLICA` para esta entrega documental: no se alteran componentes ejecutables. **A4:** exigir documento + CHANGELOG autocontenido, commit fast-forward exclusivo en dev, Scope Lock y check V5 de procedencia `completed/success` sobre SHA causal. Registrar PLAN/COLA/DAG y BITACORA con candado V17, sin editar fórmulas; A0–A4 completos únicamente cuando esa evidencia final exista. **A5:** pendiente de revisor con TASK_ID diferente entre las seis Javier; nunca autopase.

**Rollback:** `git revert` normal del changeset documental tras revalidar HEAD y escritores activos. No borrar BITACORA ni eventos ya auditados, no reescribir historia y no cambiar QA/main/PROD. M02.06 sigue prohibida hasta completar todas las predecesoras LISTO y A5 exigidas.
