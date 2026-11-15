# SOLQARYN V5 — C10-02.M02.05: contratos inertes de eventos e idempotencia de archivos y dominios

**ID:** C10-02.M02.05, PLAN_MAESTRO fila 1488/DAG fila 1449 · **Plan:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` · **Autor:** PRIMARY DEV 02 Task `6af16bb9492c8191b8e8c7d606f11507` · **Scope:** `solqaryn/Solqaryn@dev` STRICT · **Fecha:** 2026-11-15.

## Alcance negativo y compatibilidad

Este documento define posibles eventos de archivo/objeto, enlace privado, host y lifecycle en una futura arquitectura provider-neutral. **No crea bus, event dispatcher, webhook, worker, outbox, endpoint, R2, bucket, dominio, DNS, HMAC, trigger, almacenamiento ni credenciales.** Cloudinary y las integraciones actuales no se cambian; el objetivo Hostinger/Cloudflare necesita autorización independiente y gates de cutover con rollback real. Los nombres de evento siguientes son **propuestas inertes**, no clases ni tópicos existentes.

El backend resuelve tenant, objeto y permiso por membresía actual y RBAC; un `tenantId`, `path`, host o token dentro del evento **no concede autoridad**. Un objeto sin owner probado se bloquea en cuarentena, nunca se interpreta como público; tenant A no adquiere acceso a B mediante clave/URL/cache.

## Envelope futuro mínimo (no publicado)

```text
StorageEventEnvelopeV1 {
  eventId: unique-server-id;
  eventType: allowlisted-string;
  schemaVersion: explicit-version;
  occurredAtUtc: server-timestamp;
  tenantScope: server-verified-tenant;
  resourceId: opaque-id;
  resourceVersion: optimistic-version;
  actorScope: verified-subject-or-trusted-system;
  correlationId: opaque-nonauthorizing-id;
  causationId: prior-verified-operation-id;
  idempotencyHash: scoped-server-digest;
  body: minimal-private-or-public-projection;
}
```

El envelope jamás lleva credenciales, firmas HMAC, URL privada persistente, tokens JWT, datos sensibles ni secretos de proveedor. El productor futuro sólo emite tras operación de negocio autorizada y confirmada. Si algún flujo necesita outbox/transacción, diseñar atomicidad MySQL y deduplicación de consumidor **antes** de habilitarlo.

## Eventos futuros (no existen todavía)

| ID | Evento conceptual | Productor futuro hipotético | Requisito previo |
| --- | --- | --- | --- |
| EVT01 | `FileOwnershipClassified` | Servicio de gobierno de objetos | Owner probado, nunca NULL como global |
| EVT02 | `PrivateFileAccepted` | Aplicación tras validación de contenido | MIME, tamaño, tenant, checksum, auth |
| EVT03 | `PrivateFileVersioned` | Repositorio autorizado | CAS y retención |
| EVT04 | `PrivateLinkPrepared` | Servicio de firmas | Audiencia, operación, expiración/nonce |
| EVT05 | `PrivateLinkRevoked` | Servicio de acceso | Revocación verificable en servidor |
| EVT06 | `PublicMediaProjectionApproved` | Aplicación catálogo/portal | Host y DTO público mínimo |
| EVT07 | `PublicMediaProjectionWithdrawn` | Aplicación | Invalidación scoped de cache |
| EVT08 | `FileLifecyclePolicyChanged` | Operador permitido | Política de retención inmutable para históricos |
| EVT09 | `FileQuarantineRequested` | Validador de seguridad | No exposición pública |
| EVT10 | `FileProviderReadFailed` | Adaptador | Error saneado y correlationId |
| EVT11 | `FileManifestCompared` | Ensayo de migración autorizado | Manifiestos y checksum sin escritura productiva |
| EVT12 | `FileLifecycleTombstoneRecorded` | Servicio de archivo lógico | Preservar documentos referenciados |

No se considera éxito de migración ni publicación por la mera existencia de estos nombres.

## Reglas de idempotencia, replay y límites futuros

1. Namespace de deduplicación por tenant validado + recurso + versión + operación + actor; misma clave en A y B nunca comparte respuesta.
2. Mismo ID con payload diferente se rechaza como conflicto, no se considera retry válido.
3. Reintentos con máximo acotado, backoff y jitter y estado recuperable; no temporizador infinito.
4. Consumidor comprueba tipo allowlisted, schemaVersion, tenant y versión de objeto antes de cualquier efecto.
5. `correlationId` sólo traza, nunca autentica; un host externo no designa propietario por sí solo.
6. Un evento que implique cambio de estado requiere confirmación transaccional; no publicar antes del commit correspondiente.
7. Cada enlace temporal futuro liga permiso, actor, recurso, scope, audiencia, TTL y revocación; replay/nonce inválido deniega.
8. Caches público/privado se invalidan por tenant, host, recurso y versión, sin purgas cross-tenant.
9. Payload y logs exponen mínimo imprescindible; ningún JWT, HMAC, secreto, firma ni PII.
10. Retención, auditoría, documentos comerciales y snapshots tienen precedencia sobre eliminación por lifecycle.
11. Limitar tráfico y bytes por tenant, actor, endpoint futuro y consumidor; cuota tenant no supera techo global.
12. Fallo de proveedor o llave no acreditada produce bloqueo seguro y alerta, no emisión permisiva.
13. Migración futura sólo con manifiesto de objetos, reconciliación de checksums, ensayos de rollback y autorización separada.
14. Una respuesta `202/accepted` futura no constituye operación efectivamente completada sin comprobación causal.

## Consumidores futuros y separación

| Consumidor | Datos permitidos | Datos denegados |
| --- | --- | --- |
| ERP privado | Recurso propio, versión, estado y auditoría mínima | URL de B, archivo sin clasificación, tokens privados |
| Web pública | Slug y media aprobada por host verificado | Datos personales, archivos privados, firma secreta, objeto de B |
| Auditoría | IDs opacos, transición, UTC y correlación | PII y secretos de proveedor |
| Ensayo de migración | Hashes y manifiesto sintético/permitido | Escritura en R2 real o borrado origen sin gate |
| Herramienta de soporte | Errores saneados y tenant propio acreditado | Acceso transversal o ejecución fuera de dev |

## Matriz FUTURA_NO_EJECUTADA de aceptación

| Caso | Esperado positivo | Esperado negativo |
| --- | --- | --- |
| PN01 | A emite evento scoped A | Consumidor B no obtiene datos A |
| PN02 | ID y payload iguales deduplican | Clave igual con payload distinto produce conflicto |
| PN03 | Objeto/actor autorizados se trazan | Autoridad del cliente no concede permisos |
| PN04 | Link con TTL correcto caduca | Link expirado/revocado no descarga |
| PN05 | Nonce válido tiene un solo uso | Replay se rechaza |
| PN06 | Host aprobado muestra media pública | Host B no ve objeto privado A |
| PN07 | Archivo retenido conserva historia | Lifecycle no borra factura o archivo legal |
| PN08 | Hashes iguales permiten ensayo | Divergencia detiene migración/cutover |
| PN09 | Nuevo schemaVersion se valida | Versión desconocida se rechaza |
| PN10 | Correlación segura trazable | Log no expone JWT/HMAC/PII |
| PN11 | Cuota normal deja pasar evento | Burst excedido se limita con error saneado |
| PN12 | Fallo transitorio se recupera con límite | Fallo no genera duplicados ni publicación sin commit |

Los 24 escenarios son criterios de pruebas posteriores, **no ejecutados**. Futuro código exige CI .NET/seguridad por SHA, MySQL efímero cuando exista outbox, pruebas de replay/firma y aislamiento A/B; UI exige Playwright cuando aplique.

## Evidencia, riesgos, A0–A5 y rollback

A0: plan, archivos y dependencias, owner/CAS, Scope Lock. A1: revisar consistencia de 12 tipos de eventos, 14 reglas y 12 parejas; ejecución de eventos `NO_APLICA` a este documento. A2: amenazas JWT/RBAC/tenant A/B, TTL/replay, privacidad y retención examinadas documentalmente, no prueba ejecutada. A3: revisar compatibilidad conceptual con Domain/Application/Infrastructure/API/Cloudinary; CI de MySQL/bus/worker `NO_APLICA` para diff Markdown. A4: commit de doc y sección íntegra CHANGELOG en dev por fast-forward, Scope Lock/V5 exact SHA `completed/success`, PLAN/COLA/DAG/BITACORA posteriores. A5: revisión auténtica por otra Task Javier distinta de esta autora.

Rollback documental aditivo (`git revert`) sin afectar integraciones. Rollback de proveedor, enlaces o eventos runtime NO REALIZADO: requerirá pruebas de doble entrega, reconciliación de manifiestos, reversión de configuración y datos de ensayo. No tocar QA/main/PROD, DNS/R2/Hostinger reales, secretos, tareas ni cuentas de pago.
