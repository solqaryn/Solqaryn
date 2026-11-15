# SOLQARYN V5 — C10-03.M02.05: señales inertes e idempotencia del inventario condicionado

**ID:** C10-03.M02.05 · PLAN fila 1524 · DAG fila 1485 · **Autor:** PRIMARY DEV 03 `6adc2e4bd8a48191ac7d8641ed95a1a3`, `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; fecha 2026-11-15.

## Objeto y exclusiones

Se documentan eventos potenciales, claves de idempotencia, correlación, cuotas, errores y consumidores hipotéticos de almacenamiento futuro. **No se implementa ni ejecuta** bus, outbox, worker, webhook, dispatcher, callback, trigger, flujo de archivos, R2, Cloudinary, MySQL, API, dominio ni migración. El inventario real es **NO_VERIFICADO**; ningún evento equivale a un conteo, inventario cero o éxito de proveedor. Para futuro cero autenticado omitir históricos; para positivo se exigen manifiesto/conciliación/rollback con autorización independiente.

Preflight: `PLAN!H1524=PENDIENTE/I1524` vacío, `COLA!G1524/O1524=SI`, `DAG_TECNICO!F1485=VALIDADO/G1485=SI` y C10-03.M01.01–06 LISTO+A5. M02.04 tiene ID/owner propios; esta unidad es independiente y no usa su estado como prerrequisito no expresado.

## Envelope hipotético, NO PUBLICADO

```text
PrivateStorageSignalV1 {
  eventId: server-generated-opaque-id;
  eventType: allowlisted;
  schemaVersion: explicit;
  occurredAtUtc: verified-clock;
  tenantId: resolved-from-current-auth-backend;
  actorId: verified-and-minimal;
  resourceId: opaque;
  resourceVersion: optimistic;
  causationId: nonauthorizing-reference;
  correlationId: nonauthorizing-reference;
  idempotencyDigest: scoped-server-digest;
  InventarioState: UNKNOWN_OR_VERIFIED;
  redactedPayload: minimal;
}
```

No URL firmadas, JWT, HMAC, secretos, PII, claves reales, paths de proveedor ni contenido de archivo en la señal. Un evento sin owner/tenant verificado se rechaza antes de cualquier envío.

## Doce eventos conceptuales EVT01–EVT12 (INERTES)

| ID | Nombre propuesto | Autoridad futura indispensable |
| --- | --- | --- |
| EVT01 | `FileInventarioRequested` | Operador y fuente autenticados; sin ejecución por este diseño |
| EVT02 | `FileInventarioReconciled` | Conteos de DB/proveedor y timestamp auditables |
| EVT03 | `FileInventarioMismatchDetected` | Discrepancia produce NO_GO migración |
| EVT04 | `HistoricalInventarioZeroVerified` | Conteo completo 0 acreditado, no inferido |
| EVT05 | `HistoricalInventarioPositiveVerified` | Manifiesto y hash por tenant |
| EVT06 | `NewFileValidated` | MIME bytes/tamaño/tenant/owner y política vigente |
| EVT07 | `PrivateObjectVersionRecorded` | CAS de versión sin borrar histórico |
| EVT08 | `SignedReadCapabilityPrepared` | Actor/verbo/objeto/TTL/replay autorizados |
| EVT09 | `FileRetentionHoldChanged` | Policy/autoridad aprobada, nunca bypass hold |
| EVT10 | `StorageProviderReadFailed` | Error saneado, sin secreto y sin fallback permisivo |
| EVT11 | `FutureMigrationManifestCompared` | SHA/hashes de objeto verificados, sin cutover |
| EVT12 | `StorageRollbackProofRecorded` | Fixtures no productivos y restauración validada |

La mera declaración de EVT04 no genera cero ni prueba una consulta histórica. No emitir señal antes de un commit durable ni compartir IDs como autorización.

## Reglas IR01–IR14 de idempotencia, replay y cuotas

1. Namespace de deduplicación por tenant validado, objeto, versión, operación y actor.
2. Misma clave + hash distinto provoca conflicto, no sobrescritura.
3. Identidad/tenant derivan del backend; un cliente no autentica vía envelope/host/path.
4. `correlationId` registra causalidad, nunca concede permiso.
5. Tipo y versión de esquema sólo de allowlist validada por consumidor.
6. Reintentos acotados y backoff; detener circuito ante error persistente.
7. Cuando se requiera outbox, persistencia y evento deben ser atómicos antes de publicarse.
8. Un `UNKNOWN` no se convierte a `ZERO_VERIFIED` por replay, retries ni falta de registros.
9. Firma temporal de capacidad liga objeto/verbo/actor/TTL y comprueba revocación.
10. Retención y hold se imponen aun cuando eventos lleguen fuera de orden.
11. Invalidez de actor/tenant B desde A se rechaza antes de consulta a proveedor.
12. Payload, logs, errores y métricas exponen únicamente IDs opacos y valores saneados.
13. Límites de eventos/bytes por tenant, usuario y consumidor sin superar techos plataforma.
14. Ack 202 o señal de conciliación no significa archivo persistido, migración concluida ni restore probado.

## Consumidores hipotéticos y fronteras
- **Inventario de auditoría:** sólo metadata autorizada y fecha; sin archivo ni secreto.
- **Storage privado:** aplica únicamente operaciones validadas por caso de uso backend, no por mensaje.
- **Control de retención:** preserva hold y referencias comerciales, falla cerrado.
- **Diagnóstico de discrepancias:** retiene manifiesto redactado, no hace cutover.
- **Observabilidad:** agrega correlationId opaco, censura URL/PII/JWT/secretos.

## Doce pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Inventario acreditado cero produce señal tras conciliación | UNKNOWN emite cero |
| P02/N02 | Inventario positivo emite digest de manifiesto | Conteo parcial considera éxito |
| P03/N03 | A emite evento de recurso A | B consume contenido A |
| P04/N04 | Evento con versión nueva válida | Versión antigua sobrescribe |
| P05/N05 | Reintento mismo digest es idempotente | Igual clave/digest distinto se acepta |
| P06/N06 | TTL/replay respetados | Enlace vencido se vuelve válido por retry |
| P07/N07 | MIME válido en bytes se registra | Extensión simulada supera filtro |
| P08/N08 | Hold retiene archivo | Evento de lifecycle lo borra |
| P09/N09 | Cache A/B invalidada por versión | Payload A se publica en host B |
| P10/N10 | Error proveedor se registra saneado | Token o URL firmada aparece |
| P11/N11 | Corte futuro espera manifiesto íntegro | Señal dispara migración sin orden |
| P12/N12 | Rollback futuro tras fixture es reproducible | Acknowledgement se confunde con restore |

**No se ejecutaron** pruebas, migraciones, firmas, cargas, restauraciones o acciones de proveedor. Requerir CI real .NET/xUnit, MySQL efímero, Angular/Playwright si cambia UI, y SHA exacto en futura implementación; checks CANCELLED/SKIPPED/IN_PROGRESS nunca PASS pertinente.

## A0–A5, alcance y reversión
A0: fuente V5/Skill/cuenta/HEAD, DAG/COLA/PLAN y claim exclusivo. A1: 12 eventos inertes, 14 reglas, 12 pares futuros, coherencia con M01. A2: tenant A/B, RBAC/JWT, replay, TTL, retención, redacción y límites. A3: contrato conceptual sin buses, SQL, workers ni endpoints: runtime CI NO_APLICA a sólo docs, no PASS. A4: commit dev fast-forward/CAS con CHANGELOG autocontenido, checks Scope Lock/V5 `completed/success` del SHA y readback institucional. A5: otra Task Javier distinta de autora, pendiente sin liberar M03.

**Rollback:** git revert normal del commit Markdown, preservando BITACORA aditiva, proveedores, secretos, DNS, QA/main/PROD y datos.
