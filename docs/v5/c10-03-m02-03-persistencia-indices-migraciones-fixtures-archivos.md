# SOLQARYN V5 — C10-03.M02.03: persistencia conceptual, índices, migraciones y fixtures para archivos

**Unidad:** C10-03.M02.03 · PLAN_MAESTRO fila 1522 · DAG_TECNICO fila 1483 · **Autor:** PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Repositorio:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK STRICT; 2026-11-15.

## Objetivo y precondiciones

Diseñar **únicamente en Markdown** la persistencia futura de un inventario histórico condicionado y archivos nuevos. El inventario histórico sigue `NO_VERIFICADO`, que no equivale a cero. Sólo recuentos autenticados y conciliados de todas las fuentes autorizadas podrán cambiarlo a `CERO_VERIFICADO` o `POSITIVO_VERIFICADO`. Con cero genuino no corresponde exportar, copiar ni calcular checksums de objetos históricos; con positivos debe existir manifiesto conciliado antes de considerar una migración por autorización separada. Un origen inconsistente produce NO_GO en el alcance afectado, preservando el proveedor actual.

**No materializar ninguna tabla, entidad EF, migración, índice, fixture real, consulta MySQL, endpoint, credencial, token, bucket, proveedor, R2, DNS, host, archivo ni operación comercial**. Compatibilidad con .NET 10, EF Core 10.0.12/Oracle MySql.EntityFrameworkCore 10.0.9, MySQL 8.4, JWT/RBAC y Cloudinary existente; no reemplazar servicios vigentes.

**Preflight:** antecedente `C10-03.M01.01–06` LISTO con A5 independiente; `DAG_TECNICO!F1483=VALIDADO/G1483=SI`, `COLA!G1522/O1522=SI` y `PLAN_MAESTRO!H1522=PENDIENTE/I1522` vacío antes de CAS puntual (una coincidencia). Esta unidad es independiente de M02.01/M02.02/M02.04/M02.05 por el DAG; no se apropia de sus autorías ni pruebas.

## Doce proyecciones FUTURAS, NO TABLAS ACTUALES

| ID | Proyección propuesta | Clave/relación conceptual | Invariante de propiedad |
| --- | --- | --- | --- |
| P01 | `InventarioFuenteAutorizada` | sourceRef opaca, environment, policyVersion | Sin permitir accesos por simple texto de fuente |
| P02 | `InventarioCorrida` | runRef, startedUtc, sourceSetDigest, state | Corrida autenticada, no reutilizar resultado parcial |
| P03 | `InventarioConteoFuente` | runRef, sourceRef, itemCount?, verifiedUtc | NULL/UNKNOWN jamás equivale a cero |
| P04 | `InventarioDiscrepancia` | runRef, opaqueRef, reasonCode, status | Divergencia bloquea liberación, sin PII en error |
| P05 | `ManifiestoArchivo` | manifestRef, digest, version, tenantScope | Sólo emitir después de conciliación autorizada |
| P06 | `ManifiestoArchivoEntrada` | manifestRef, opaqueObjectRef, size, digest | 1:N entradas versionadas; no key privada en DTO |
| P07 | `ArchivoPrivado` | resourceRef, EmpresaId efectivo, classification | Tenant definido por backend, no payload cliente |
| P08 | `VersionArchivo` | resourceRef, version, checksum, byteLength | Versiones append-only y sin pérdida de originales |
| P09 | `LocalizacionProveedor` | resourceRef, version, providerRef, sealedLocator | Proveedor y localizador no constituyen ACL |
| P10 | `IngresoPendiente` | intentRef, resourceRef, operationDigest, expiry | No publicar objeto parcial ni reintento diferente |
| P11 | `RetencionArchivo` | resourceRef, policyRef, legalHold, dueUtc | Hold domina sobre borrado y lifecycle |
| P12 | `AuditoriaArchivoSaneada` | tenantScope, opaqueRef, correlationRef, outcome | Sin firma, URL temporal, secreto, JWT ni PII |

**Relaciones hipotéticas:** Tenant 1:N archivos; archivo 1:N versiones, retenciones y auditoría; corrida 1:N conteos/discrepancias; manifiesto 1:N entradas; versión 0:N localizadores; un recurso puede pasar por varios estados sin borrado de historia. No duplicar modelos EF actuales ni establecer nuevos FKs sin inspección del esquema real, datos legados y compatibilidad transaccional.

## Doce índices/constraints candidatos, NO CREADOS

| ID | Índice o restricción conceptual | Razón y precaución |
| --- | --- | --- |
| IX01 | `(EmpresaId,Estado,Clasificacion,Id)` | Paginación tenant-scoped; no usar índice como autorización |
| IX02 | `UNIQUE(EmpresaId,ResourceRef,Version)` | Evitar colisión de versiones sin eliminar historia |
| IX03 | `(RunRef,SourceRef)` | Una fuente versionada por corrida; idempotencia |
| IX04 | `(RunRef,State,VerifiedUtc)` | Distinguir pending, error y cero acreditado |
| IX05 | `(ManifestRef,OpaqueObjectRef,Version)` | Manifiesto reproducible sin locator público |
| IX06 | `(ResourceRef,Version,Digest)` | Búsqueda de evidencia de integridad, no autenticidad por sí sola |
| IX07 | `(EmpresaId,IdempotencyKey,PayloadDigest)` | Key repetida con digest distinto rechazada |
| IX08 | `(EmpresaId,ResourceRef,RetentionPolicyRef)` | Inspección de hold antes de operación destructiva |
| IX09 | `(ResourceRef,ExpectedVersion,PendingState)` | CAS y compensación de cargas parciales |
| IX10 | `(EmpresaId,OccurredUtc,CorrelationRef)` | Auditoría por tenant, sin contenido sensible |
| IX11 | `(ProviderRef,OpaqueLocatorDigest)` | Conciliación referencial, nunca permiso de acceso |
| IX12 | `(RunRef,ReasonCode,ResolutionState)` | Tratar huecos/huerfanos antes de corte futuro |

Para EF/MySQL futuros revisar colación, índices compuestos, longitudes, FK/unique, nulos heredados, soft-delete, claves naturales, zonas horarias, coste de escritura, locks, tamaño de strings y migración reversible. No se presume que estas claves existan en DB ni que una transacción SQL coordine atómicamente el proveedor externo.

## Invariantes DB01–DB14 fail-closed

- **DB01–DB02:** tenant efectivo proviene de identidad/membresía servidor; slug, EmpresaId libre, host y localizador no autorizan.
- **DB03–DB04:** cero histórico sólo si todas las fuentes comprobadas concilian; UNKNOWN, fallo o fuente omitida conserva NO_VERIFICADO/INCONSISTENTE.
- **DB05–DB06:** un objeto sin propietario verificable está en cuarentena; referencias opacas y versión nunca filtran recurso de B hacia A.
- **DB07–DB08:** digest se valida sobre bytes auténticos, MIME por firma/bytes, cuota y tamaño streaming; no confiar en extensión o metadatos declarados.
- **DB09–DB10:** legal hold y retención bloquean eliminación; versiones previas y origen permanecen durante rollback.
- **DB11–DB12:** reintento idempotente no duplica, key con payload distinto es conflicto; timeout parcial se compensa sin publicar fragmento.
- **DB13–DB14:** firmas/TTL/nonce y JWT/RBAC no se almacenan en evidencia en claro; auditoría/telemetría saneará URL privada, token, PII y secretos.

## Siete etapas de migración futura (DISEÑO, NO EJECUCIÓN)

1. **MIG01 — Inventario real:** permiso explícito para consultar fuentes, fijar fecha UTC y universo; resultado UNKNOWN no es cero.
2. **MIG02 — Conciliación:** contar y verificar referencias de metadatos/objetos, tenants y versiones; bloquear ante discrepancias.
3. **MIG03 — Estrategia:** cero verificado salta export/copia/checksum históricos; positivo produce manifiesto y presupuesto reversible.
4. **MIG04 — Modelo aditivo:** ensayo de schema EF/MySQL 8.4 efímero con up/down, FK, índices y nulos antes de cualquier dato real.
5. **MIG05 — Fixture nuevo:** probar carga privada y MIME/size/cuota A/B sin mover objetos históricos ni alterar Cloudinary.
6. **MIG06 — Ensayo de transferencia:** sólo con proveedor, costo, datos y permisos por orden separada; diff de digest, ACL, versiones y recuperación.
7. **MIG07 — Decisión cutover:** resultados integrales, restauración probada y autorización expresada; si falla, volver a origen sin borrarlo.

Cada etapa exige gate y evidencia propia, rollback, logs redactados, revisión independiente y sin tratar esta documentación como permiso para migrar.

## Doce pares de fixtures sintéticos FUTUROS_NO_EJECUTADOS

| Par | Resultado positivo futuro | Rechazo negativo obligatorio |
| --- | --- | --- |
| F01/N01 | Todas las fuentes confirman cero y no se copian históricos | Una sola fuente vacía determina cero global |
| F02/N02 | Histórico positivo tiene manifiesto y conteo íntegro | Manifiesto parcial se declara completo |
| F03/N03 | Corrida inconsistente bloquea traslado | Huecos se ignoran por retry |
| F04/N04 | Archivo A sólo existe en ámbito A | Tenant B obtiene A con locator externo |
| F05/N05 | Version 2 preserva version 1 | Update elimina/reescribe versión anterior |
| F06/N06 | Bytes, hash y MIME de fixture coinciden | Archivo truncado o MIME falso se publica |
| F07/N07 | Mismo payload+key retorna resultado idempotente | Misma key con digest nuevo crea duplicado |
| F08/N08 | Hold conserva recurso | Purga elimina recurso bajo hold |
| F09/N09 | Timeout de carga se recupera sin público parcial | Error parcial da estado COMPLETADO |
| F10/N10 | URL futura limita actor/verbo/TTL | TTL vencido o método distinto concede lectura |
| F11/N11 | Auditoría de A está saneada | PII, JWT o URL privada aparecen en log |
| F12/N12 | Restauración en efímero conserva ACL/hash | Cutover fallido borra origen o cruza tenants |

**Oráculos únicamente especificados.** Ningún test SQL, dotnet/xUnit, Category=Integration, EF migration, MySQL 8.4, Playwright, HTTP, proveedor ni restauración se ejecuta por esta unidad. Cuando se implemente DB, exigir job CI que ejecute de verdad Category=Integration sobre MySQL 8.4 efímero y 0 FAIL; un build verde aislado no certifica integración.

## Alcance de A0–A5, publicación y rollback

- **A0:** identidad/Javier/Skill canónica, HEAD, Scope Lock exacto, fuente V5, CAS sobre H1522 y owner I1522, COLA G/O, DAG F/G, diff sólo documento + CHANGELOG.
- **A1:** revisión estática de 12 proyecciones, 12 índices, 14 invariantes, siete etapas y doce pares P/N **futuros**, no pruebas positivas ejecutadas.
- **A2:** JWT/RBAC/tenant A/B, contención de MIME/hash, cuarentena, integridad, idempotencia, hold, secretos y privacidad sin tocar runtime.
- **A3:** relación conceptual con contratos `IImageStorageService`, `StorageTenantContext`, Cloudinary y C10-03.M01; compilación, MySQL, EF migrations y pruebas HTTP `NO_APLICA` para un diff puramente documental, nunca PASS ficticio.
- **A4:** publicar un único commit dev fast-forward/CAS con doc y CHANGELOG autocontenido; verificar blobs y `verify-solqaryn-project-scope` + `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success en SHA exacto, leer PLAN/COLA/DAG/BITACORA.
- **A5:** `A5_PENDIENTE`, revisión independiente por otra Task Javier distinta de DEV01 antes de cualquier sucesora técnica; LISTO_DEV documental no implica autorización QA/main/PROD.

**Rollback de esta unidad:** `git revert` ordinario del commit documental o nueva corrección aditiva; no force-push ni cambios a datos o infraestructura. **Rollback futuro** de objetos/base de datos es otro scope, condicionado por aprobación y prueba real.
