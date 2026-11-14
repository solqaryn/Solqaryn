# SOLQARYN V5 — C10-01.M01.05: aceptación funcional y técnica observable de storage provider-neutral

**Unidad:** `C10-01.M01.05`, PLAN_MAESTRO fila 1446; **autor:** PRIMARY DEV 02 JAVIER / Task `6af16bb9492c8191b8e8c7d606f11507`; **autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Doc Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fecha:** 2026-11-14.

## Objeto y delimitación de evidencia

Especificar criterios de aceptación y rechazo **medibles para etapas futuras** de un almacenamiento provider-neutral que cubra imágenes, documentos, videos y exportaciones, preservando la autorización real en backend. **Esta unidad es exclusivamente documental y NO ejecuta los escenarios de runtime descritos.** La configuración actual menciona Cloudinary, QuestPDF y MySQL; Cloudflare R2/Hostinger son objetivos condicionales que precisan permisos y pruebas separados. No se crean API, SDK, interfaz, bucket, cuenta, llave, worker, webhook, migración, datos, cargos ni despliegue.

Las referencias causales leídas del árbol dev son: `docs/v5/c10-01-m01-01-objetivo-no-alcance-storage-neutral.md` (NR01–NR12, blob `622884a0a307e01c41ff1bec6797a1f36e67e21e`), `docs/v5/c10-01-m01-02-actores-superficies-storage-neutral.md` (I01–I12, blob `6d18023f7bb42685cce150b6bbe29f1fa20c30c8`), `docs/v5/c10-01-m01-03-inventario-codigo-evidencia-storage-neutral.md` (diez fuentes/invariantes, blob `1d807b2b9cba1f8b87a0e4e4e571f8e832ff0203`) y `docs/v5/c10-01-m01-04-riesgos-supuestos-decisiones-storage-neutral.md` (R01–R12, S01–S08 y D01–D08; commit `5156b946e288d96605f4dbfc42cc767d31fea190`). Estas fuentes no sustituyen una prueba de integración.

## Matriz AC01–AC16 — aceptación FUTURA, NO_EJECUTADA

| ID | Condición de aceptación verificable | Oráculo y evidencia mínima futura |
| --- | --- | --- |
| AC01 | Una interfaz de Application no depende de un SDK específico | Compilar tests de contrato y comparar implementaciones sin referencia cloud en DTO |
| AC02 | Owner/tenant resueltos server-side, no desde parámetro cliente | HTTP/xUnit con usuarios de empresas A/B, same ID cross-tenant denegado |
| AC03 | Roles de lectura/escritura/borrado/export separados por operación | Matriz de 403/404 positiva/negativa por rol y membresía |
| AC04 | Archivo permitido pasa allowlist MIME y firma de bytes | Fixture de bytes, MIME real y declarado, resultado tipado y trazable |
| AC05 | El límite de tamaño se impone antes y durante el stream | Prueba con payload justo bajo/igual/sobre máximo; consumo acotado |
| AC06 | Checksum se calcula sobre bytes reales y se compara tras descarga | Reproducir hash de entrada/salida con fixture binario y mismatch |
| AC07 | IDs expuestos son opacos y no filtran ruta o bucket | Snapshot DTO y logs sin host/path interno ni token |
| AC08 | URL de acceso privado expira y limita recurso/tenant | Prueba de expiración, revocación y replay A/B |
| AC09 | Fallos de proveedor no publican bytes incompletos | Injectar timeout/cancelación y comprobar limpieza/compensación |
| AC10 | Retries de upload no producen duplicados visibles no gobernados | Reintento con clave idempotente y conciliación de hash/versiones |
| AC11 | Export PDF/documento solo incluye datos del tenant autorizado | Integration/QuestPDF con dos empresas y payload sin datos ajenos |
| AC12 | Auditoría registra acción, correlación, actor y clasificación | Inspección de traza sanitizada sin contenido/PII/secreto ni URL firmada |
| AC13 | Conmutación A→B invalida estado de descarga/cache de A | Browser + API con cambio de tenant y caché, no recurso A en B |
| AC14 | UI Angular 22.2.1 expresa carga, error, éxito, permisos y teclado | Playwright Chromium 320px, foco, ARIA y reduced-motion |
| AC15 | El proveedor efectivo se configura por ambiente autorizado | Test de selección de adaptador y NO_GO de fallback no autorizado |
| AC16 | Una migración futura demuestra inventario/hashes/restore/rollback | Plan de migración aprobado, servicio efímero, reversión y conteos iguales |

Para cada AC se conservarán **SHA del changeset**, ambiente efímero, dataset sin PII productiva, logs redactados, resultado esperado/real, y comando repetible. El mero cumplimiento documental **no cierra** ninguna AC en runtime.

## Matriz de rechazos N01–N16 — casos futuros, NO_EJECUTADOS

| ID | Entrada negativa | Rechazo observable exigido |
| --- | --- | --- |
| N01 | Usuario tenant A lee objeto B | 403/404 seguro, sin bytes ni metadatos B |
| N02 | Petición sin JWT válido a objeto privado | 401/403 según contrato, no recurso |
| N03 | Rol lectura intenta upload/delete/export sensible | Denegación por permiso real de operación |
| N04 | Cliente altera `EmpresaId`, slug o header de empresa | Ignorar dato no confiable, denegar cross-tenant |
| N05 | MIME de cabecera correcto pero bytes prohibidos | Rechazo por contenido, no por extensión |
| N06 | Payload rebasa tamaño/cuota durante streaming | Interrupción, sin escritura parcial visible |
| N07 | Checksum declarado/recuperado no coincide con bytes | Error de integridad, no marcar válido |
| N08 | Nombre con traversal, NUL, backslash o ruta absoluta | Identificador seguro, rechazo de path |
| N09 | URL privada expirada, revocada o usada por tenant B | Denegación y no exposición en logs |
| N10 | Timeout proveedor después de escribir algunos bytes | Compensación, no objeto parcial público |
| N11 | Reintento con idempotency key pero contenido diferente | Conflicto seguro, no reemplazo silencioso |
| N12 | Solicitud de archivo B durante cambio de tema/tenant A→B | Invalidar caché/estado, nunca mostrar B/A ajenos |
| N13 | Exportación incluye fila de otro tenant | Denegar/sanitizar y verificar no filtración |
| N14 | Tracing contiene token, PII, URL firmada o bytes | Redacción, control negativo de no-log |
| N15 | Fallback automático a proveedor no autorizado en incidente | NO_GO explícito, sin salida por cuenta legada |
| N16 | Pedido de migración R2 sin respaldo, alcance y permiso | NO_GO; no crear bucket, no mover/borrar objeto |

## Dependencias, criterios de bloqueo y no-regresión

**Gate documental de M01.05:** `PLAN_MAESTRO!A1446=C10-01.M01.05`, previo PENDIENTE sin owner; `COLA!G1446/O1446=SI`, `DAG_TECNICO!A1407=C10-01.M01.05/F1407=VALIDADO/G1407=SI`. Los antecedentes C01-03/C02/C03 M01.01–05 son preparatorios, nunca aprobación de infraestructura. Las decisiones D01–D08 de M01.04 continúan **PENDIENTES**. M01.06 exige su propio gate y A5 independientes, sin habilitación automática.

**Bloqueos por etapa:** no pasar a persistencia, JWT/RBAC, tenant runtime, M01.06 terminal, M06.06, workers, R2/Hostinger, migraciones, secretos, despliegue o producción con solo estos AC. Si cualquier control de seguridad falla, aplicar fail-closed a ese scope. Si falta prueba real de la tecnología tocada, no declarar PASS.

**Impacto real de esta unidad:** solo este Markdown y sección de CHANGELOG; sin cambios en Domain/Application/Infrastructure/API, frontend, DTOs, pruebas ejecutables, MySQL/EF, Cloudinary, contratos live, CORS, DI, secretos o archivos reales. **A0:** diff de dos Markdown, fuente y ausencia de encabezados vacíos/secretos; **A1:** contar AC01–AC16 y N01–N16 y verificar oráculos reproducibles NO_EJECUTADOS; **A2:** preserve aislamiento tenant A/B, JWT/RBAC, MIME/tamaño/checksum, tokens y PII solo a nivel documental; **A3:** conciliar NR/I/R/S/D anteriores, DAG y stack; **A4:** commit fast-forward CAS a dev, checks exact-SHA `verify-solqaryn-project-scope` y V5 CHANGELOG completed/success, readback PLAN/COLA/BITACORA; .NET/Angular/MySQL/Playwright runtime **NO_APLICA a dos Markdown**, jamás PASS inventado.

**A5_PENDIENTE:** otro TASK_ID Javier autorizado distinto del autor debe efectuar revisión independiente sobre SHA original, blobs y evidencia; no autoaprobar ni ascender QA/main/PROD. **Rollback:** `git revert` normal del commit solo documental; BITACORA histórica permanece inmutable. No eventos, webhooks, servicios de pago ni despliegue real.
