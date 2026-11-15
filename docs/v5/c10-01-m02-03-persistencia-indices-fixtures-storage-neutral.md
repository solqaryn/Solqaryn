# SOLQARYN V5 — C10-01.M02.03: persistencia, índices, migraciones y fixtures de almacenamiento neutral

**Autor:** PRIMARY DEV 06 JAVIER, Task `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuentes institucionales:** STANDARD V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet PLAN/COLA/DAG `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

**Alcance:** diseño DOCUMENTAL de M02.03; no crea tabla MySQL, entidad EF, migración, índice, operación Cloudinary/R2, archivo ni consulta productiva. Los fixtures son propuestas NO_EJECUTADAS. No copiar objetos reales ni consultar secretos. A5 independiente de otra Task Javier.

## 1. Contexto técnico y precedencia

El inventario C10-01.M01.03 (blob `1d807b2b9cba1f8b87a0e4e4e571f8e832ff0203`) documenta interfaces `IImageStorageService`, `ICompraDocumentoStorageService`, `StorageTenantContext`, adaptadores Cloudinary y pruebas xUnit existentes, sin ejecutar esas pruebas en esta microtarea. M01.06 (blob `b82c2f7734f6b5b9fd582090c5110b4c64d9591e`) identifica BL01–BL14 y RB01–RB10. DAG M02.03 exige M01.01–06 LISTO+A5; no presume que el destino Cloudflare R2, videos o exportaciones esté configurado.

Diseñar la persistencia de **metadatos** separada del contenido en un proveedor, con referencias opacas, checksum de bytes, tenant efectivo server-side, versión, operación idempotente y estado durable. No duplicar objetos de usuario ni alterar tablas legacy hasta pruebas de migración futura en CI efímera y consentimiento/permiso correspondiente.

## 2. Modelo relacional conceptual (NO IMPLEMENTADO)

| Contrato propuesto | Propiedad / clave lógica | Riesgo y política |
| --- | --- | --- |
| `StoredObject` | objectRef opaco, EmpresaId verificada, resourceKind, resourceRef, version | Un objeto B no visible para A, incluso con ID conocido |
| `StoredObjectVersion` | objectRef + version, providerRef privado, bytes, SHA-256, actualMime | Inmutabilidad y checksum real, sin URL firmada persistida |
| `StorageUploadSession` | operationId + tenantScope + actorRef, expectedVersion, state | TTL/retención definida después; pendiente nunca significa publicado |
| `StorageObjectGrant` | objectRef + actor/permission-scope + expiresAtUtc | Grants no sustituyen JWT/RBAC ni crean rutas públicas |
| `StorageOperationAudit` | requestId, tenantScope, outcome, sourceSha, safeCause | Auditoría append-only saneada y trazabilidad sin PII |
| `StorageMigrationBatch` | sourceRef + destRef + manifestSha + rolloutState | Contrato de traslado futuro, no fuente de permiso |
| `StorageIntegrityEvidence` | objectRef + providerVersion + sha256 + verifiedAtUtc | Distinción entre hash declarado, observado y certificado |

Estos nombres son entidades **sugeridas**, no existentes en `Domain/` ni en MySQL. Requieren comprobar cardinalidad, estado del esquema, consumidores, claves foráneas, exclusiones multiempresa e impacto de migración en microtarea M03. No usar `EmpresaId=NULL` legado como acceso global ni almacenar objeto completo en SQL por defecto.

## 3. Consultas e índices potenciales (sin DDL)

| Consulta futura | Índice lógico candidato | Garantía exigida |
| --- | --- | --- |
| Buscar recursos por propietario | (EmpresaId,resourceKind,resourceRef,state) | Tenant verificado en backend antes de query |
| Localizar versión | (objectRef,version) unique | No sobreescribir bytes de otra versión |
| Deduplicación de carga | (EmpresaId,operationId,requestHash) unique | Same key + different hash produce conflicto |
| Auditoría interna | (EmpresaId,occurredAtUtc,operationRef) | Sin PII ni búsquedas cross-tenant |
| Integridad de lote | (migrationBatchRef,objectRef,sha256) | No considerar lote listo con hashes faltantes |
| Expiración de grants | (expiresAtUtc,state) | Revocación comprobable, no URL permanente |

No ejecutar `CREATE INDEX`, EF migration, rollback SQL ni alteraciones de datos. Todo índice futuro requiere EXPLAIN, cardinalidad, retención, locking, backup y pruebas de concurrencia con MySQL 8.4 de CI; los índices aquí sólo ilustran acceso y límites.

## 4. Flujo futuro con estados y compensación

1. `REQUESTED`: backend verifica JWT/RBAC, Empresa efectiva, recurso y quota permitida.
2. `QUARANTINED`: recibe stream de tamaño limitado; verifica MIME de bytes, límites y digest SHA-256 real.
3. `VERIFIED`: compara byteCount, hash y tipo real; falla cerrado en discrepancia.
4. `PUBLISHED`: confirma metadatos y objeto sólo si operación idempotente gana con versión esperada.
5. `REVOKED`: desactiva nuevas lecturas y grants, conserva trazabilidad; eliminación real requiere retención aprobada.
6. `FAILED/COMPENSATING`: proveedor, transacción o timeout fallidos no muestran objeto parcial, ni intentan proveedor nuevo sin permiso.

Este flujo es diseño futuro; no existe garantía de atomicidad entre MySQL y un proveedor remoto sin protocolo de compensación implementado y probado. No llamar una escritura Cloudinary desde este documento. No inferir que Cloudflare R2 esté provisionado.

## 5. Diez fixtures y negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo esperado en CI posterior | Negativo obligatorio |
| --- | --- | --- |
| F01 | Imagen legítima y tipo real permitido produce checksum estable | JPEG renombrado o magic bytes inválidos se rechazan |
| F02 | A escribe/lee objeto A con RBAC | A adivina objectRef de B y obtiene acceso |
| F03 | Documento privado sólo expuesto por permiso específico | URL firmada caducada o token revocado sirve contenido |
| F04 | Versión v2 preserva v1 según retención | Escritura con expectedVersion obsoleta sobrescribe |
| F05 | Reintento misma operación conserva un solo objeto | Mismo key con cuerpo distinto crea duplicado |
| F06 | Falla del proveedor deja `FAILED` sin objeto visible | Transacción parcial queda publicada |
| F07 | Hash coincide tras lectura con bytes reales | Hash corrupto se considera PASS |
| F08 | Batch de migración sintético compara cuenta/bytes/hash | Cutover con diferencias o sin backup |
| F09 | Objeto expirado sigue política aprobada sin perder auditoría | Borrado sin autorización ni historial |
| F10 | Exportación privada se entrega con scope y cuota correctos | Exportación B, PII o locator real en logs de A |

No se usaron objetos de clientes, pruebas MySQL, cargas HTTP ni restauraciones. Las políticas definitivas de tamaño para PDF/video/export, retención, cuota, token de URL, backoff y proveedor elegido están pendientes y **no se inventan**.

## 6. Migración futura y rollback, sin acción real

MIG01: inventario validado de consumidores, permisos, proveedores y objetos; MIG02: clasificar PII, MIME, tamaños, ownership y retención; MIG03: respaldo/manifest de origen autorizado y checksum por objeto; MIG04: cambio aditivo de EF con MySQL efímero y rollback; MIG05: ensayar lectura/escritura aislada A/B, corrupción, duplicados, endpoint e integridad; MIG06: canary de un conjunto sintético, comparación de conteos/bytes/hashes y plan de conmutación aprobado; MIG07: revert de aplicación/catálogo, conservar origen íntegro, auditoría y restauración demostrada antes de cualquier cutover.

**Ningún MIG se ejecutó**. Sólo se permitirían en una tarea M03/M05 distinta con autorización de infraestructura, proveedor y datos según su gate; la aprobación documental M02 no cubre compras, R2, Hostinger, secretos ni producción.

## 7. Evidencia A0–A5 y salida

A0: archivo Markdown + entrada de CHANGELOG sin código, secretos ni datos. A1: siete entidades conceptuales, seis consultas e índices candidatos, diez P/N, siete pasos MIG; todas son pruebas futuras, no resultados. A2: análisis de amenazas JWT/RBAC, tenant A/B, path, MIME real, tamaño, URL temporal y hash real; NO certifica integración. A3: relación causal con interfaces y adaptadores actuales, sin crear clases/DDL; CI de EF/MySQL no aplicable a este diff exclusivamente documental. A4: publicación fast-forward/CAS, Scope Lock y V5 CHANGELOG `completed/success` del SHA exacto, readback PLAN/COLA/BITACORA. A5: revisión independiente por otra Task Javier, no autoaprobación.

Rollback actual: `git revert` normal del documento y CHANGELOG, respetando otros autores. No modificar QA/main/PROD ni instalar colas, webhooks, migraciones, flags, Cloudflare R2 o servicios de pago. M02.06 sigue sujeto a LISTO+A5 de M02.01–05, DAG F=VALIDADO/G=SI y owner libre; aquí no se libera.
