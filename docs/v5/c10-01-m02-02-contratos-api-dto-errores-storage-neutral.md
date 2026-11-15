# SOLQARYN V5 — C10-01.M02.02: contratos API, DTO, errores y compatibilidad de storage provider-neutral

**Responsable:** PRIMARY DEV 06 JAVIER, Task `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Ámbito:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK STRICT.

**Naturaleza:** diseño de contratos M02, sin endpoint nuevo, DTO compilado, integración Cloudflare R2, migración, pago, cambio de credenciales, DNS o despliegue. No ejecutar uploads/downloads contra datos ni proveedores externos. A5 pendiente de revisor de otra Task Javier.

## 1. Contratos reales que deben preservarse

`C10-01.M01.03`, blob `1d807b2b9cba1f8b87a0e4e4e571f8e832ff0203`, inventarió: `backend/src/Application/Interfaces/IImageStorageService.cs` blob `f74f496957dfa7cc1d1ec00a26444ec4befa24fa`; `ICompraDocumentoStorageService.cs` blob `454d588851ecc7db810259e63705852360172928`; `StorageTenantContext.cs` blob `716e8e205502d6300f555947c79edc2852fd63ba`; `CloudinaryImageStorageService.cs` blob `2413e0b9a05859f7978a6c1708b05fbc2648d5bb`; `CloudinaryCompraDocumentoStorageService.cs` blob `3bcc063a6396c41ed03d26f6f6c69e38acb4351f`; `ImagenUploadSecurity.cs` blob `456a7875f5f9b9ac475ed7ce83af3215beb2b408`. Las dos interfaces tienen contratos actuales con overload tenant-aware y legacy, pero no certifican abstracción universal para videos/exportaciones.

`C10-01.M01.06` blob `b82c2f7734f6b5b9fd582090c5110b4c64d9591e` fija BL01–BL14, rollback RB01–RB10 y M01 LISTO+A5. Ninguna aceptación futura de MIME, checksum, proveedor R2, URL firmada ni backup está acreditada por la lectura de código.

## 2. Operaciones provider-neutral propuestas (NO IMPLEMENTADAS)

| ID | Caso de uso | Solicitud mínima futura | Resultado y autorización requeridos |
| --- | --- | --- | --- |
| OP01 | Preparar subida | Tipo lógico imagen/documento/video/export, size declarado, scope | Backend autentica usuario, tenant, permiso escritura y cuota |
| OP02 | Recibir bytes | Stream, media type declarado, objeto opaco, operación idempotente | Detectar magic bytes, limitar tamaño, checksum SHA-256 real |
| OP03 | Confirmar objeto | ID provisional, hash real, versión esperada | No publicar objeto truncado ni cruzar A/B |
| OP04 | Obtener metadatos | ID opaco, acción de lectura, contexto autenticado | Sanitizar nombre, tamaño, tipo real, hash y propiedad |
| OP05 | Descargar | ID opaco, permiso específico y scope servidor | Stream autorizado o URL temporal estrictamente limitada |
| OP06 | Revocar/eliminar lógicamente | ID, versión y motivo | No borrar historia o datos ajenos; auditoría mínima |
| OP07 | Listar con paginación | Tipo, cursor opaco, recurso titular | Filtro tenant server-side, no enumeración cruzada |
| OP08 | Validar integridad/restauración | Manifest SHA, objeto y copia autorizados | Operación separada con permiso, sin migración automática |

OP01–OP08 son intenciones contractuales y **no nuevas rutas HTTP**. La futura implementación debe decidir verbos, rutas, convenciones `ApiResponse`, excepciones y esquema sin romper los consumidores Cloudinary existentes. `StorageTenantContext` debe provenir de identidad efectiva comprobada, jamás del nombre de carpeta, `EmpresaId` cliente, cookie o header.

## 3. DTO y compatibilidad futura

| DTO conceptual | Campos mínimos propuestos | Control obligatorio |
| --- | --- | --- |
| `StorageObjectRefV1` | opaqueId, resourceType, scopeVersion, contentVersion | Sin credenciales ni path físico expuesto |
| `StorageUploadIntentV1` | expectedMime, expectedBytes, requestId, resourceRef | Valores estrictos; no trust en extensión |
| `StorageContentEvidenceV1` | actualMime, byteLength, sha256, sourceSha | Calcular hash de bytes, no aceptar claim como evidencia |
| `StorageDownloadGrantV1` | opaqueRef, issuedAtUtc, expiresAtUtc, actionScope | TTL/permiso efectivo, no URL permanente pública por defecto |
| `StorageProviderResultV1` | objectRef, state, safeProviderCode?, correlationRef | No filtrar bucket, token ni endpoint privado |
| `StorageAuditTrailV1` | actorRef, tenantScopeRef, operationRef, outcome, evidenceSha | Append-only, sin PII/tokens |
| `StorageMigrationManifestV1` | sourceRef, destRef, byteCount, checksum, rollbackRef | Contrato para alcance futuro, no export ni migración ahora |

DTO anteriores no existen como clases en esta tarea. Usuarios y consumidores actuales deben conservar payloads existentes; cualquier campo nuevo deberá ser aditivo, versionado, autorizado y probado contra clientes concretos antes de crear endpoint.

## 4. Denegaciones E01–E12 y oráculos futuros

| Error | Causa | Comportamiento futuro |
| --- | --- | --- |
| E01 | No autenticado/JWT inválido | 401 seguro, sin recurso privado |
| E02 | RBAC sin permiso leer/escribir/borrar | 403 sin URL o metadatos |
| E03 | Objeto de tenant B con actor A | Denegar y no revelar existencia |
| E04 | Path `../`, backslash o prefijo forjado | Rechazar antes del proveedor |
| E05 | MIME declarado no coincide con firma real | Rechazar y no publicar |
| E06 | Tamaño/quota excedidos o imagen bomba | Rechazo y borrado de provisional si aplica |
| E07 | SHA-256 declarado diferente del real | Error de integridad, ninguna confirmación |
| E08 | URL temporal vencida o revocada | Denegar nueva lectura |
| E09 | Carga parcial o servicio externo caído | Objeto no visible, retry acotado |
| E10 | idempotencyKey duplicada con bytes distintos | Conflicto, sin sobreescritura |
| E11 | Tenant/responsable revocado o cambió sesión | Invalidar enlace y respuesta cacheada |
| E12 | Log/DTO con JWT, firma URL o PII | Rechazo/saneamiento y alerta sin secreto |

**P01/N01**: imagen válida y tenant A autorizado / MIME falso con extensión permitida. **P02/N02**: documento A privado leído por autorizado / lectura de B por A. **P03/N03**: hash byte a byte coincide / hash declarado falso. **P04/N04**: carga parcial queda invisible / objeto truncado se publica. **P05/N05**: URL temporal vigente y permiso actual / caducada o rol revocado. **P06/N06**: idempotencia de envío repetido / body diferente con misma key. **P07/N07**: paginación A sólo lista A / IDs de B o NULL legacy aparecen. **P08/N08**: proveedor indisponible produce error seguro / fallback externo no autorizado.

Todos estos P/N son **FUTUROS_NO_EJECUTADOS**. El límite de 10 MB documentado en `ImagenUploadSecurity` es específico de imágenes actuales; no inventar el mismo valor como cuota de videos/exportaciones. La política futura de MIME, tamaño/retención de cada clase requiere diseño y pruebas independientes.

## 5. Auditoría, reversibilidad y dependencia

- A0: verificar dos Markdown y origen M01 sin secretos o cambios runtime.
- A1: revisar OP01–OP08, siete DTO y E01–E12 más ocho parejas P/N; ninguna prueba HTTP hecha.
- A2: tenant A/B y JWT/RBAC del backend; no delegar autorización al adaptador o al frontend.
- A3: compatibilidad conceptual con `IImageStorageService`, `ICompraDocumentoStorageService` y Cloudinary; no se instala adaptador ni se toca R2/MySQL.
- A4: commit fast-forward/CAS, diff/blob SHA y controles `verify-solqaryn-project-scope` y V5 CHANGELOG `completed/success` en SHA exacto, PLAN/COLA/BITACORA leídos de vuelta.
- A5: revisión independiente de otro TASK_ID Javier antes de dependencias que lo exijan.

Rollback de esta especificación: `git revert` normal del changeset Markdown, sin sobrescribir otros autores. M02.06, M03 y proveedores operativos se rigen por DAG y permisos propios; **sin migraciones, R2 real, QA/main/PROD ni eventos**.
