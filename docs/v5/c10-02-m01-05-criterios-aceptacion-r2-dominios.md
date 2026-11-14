# SOLQARYN V5 — C10-02.M01.05: criterios de aceptación observables para objetos privados, R2 y dominios

**Unidad y fuente:** C10-02.M01.05, PLAN_MAESTRO fila 1482; STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Autor:** PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14. **DAG:** C10-02.M01.05 fila 1443, F=VALIDADO y G=SI en preflight.

## Alcance y condición de verdad

El objetivo de fase es preparar contratos de acceso privado, namespaces tenant, URLs firmadas, expiración, replay y lifecycle sin realizar aún el cutover a otro proveedor. Esta microtarea **sólo define aceptación funcional y técnica documentada**: no habilita Cloudflare R2, buckets, DNS, URLs, claves, secretos, endpoints, workers, migraciones, compras, QA/main/PROD ni cambios de seguridad. Cloudinary sigue siendo integración documentada del producto actual; la selección efectiva por ambiente no se infiere de este documento.

**Todos los casos P01–P14 y N01–N14 son escenarios FUTUROS, NO_EJECUTADOS, no PASS ni garantías de comportamiento actual.** Una URL firmada es un bearer reutilizable durante su TTL salvo estado adicional específico para uso único. Su expiración no demuestra revocación instantánea por sí misma.

## Fixtures y precondiciones del futuro ensayo

- **F01:** tenants sintéticos A/B y usuarios lector, gestor, administrador, usuario suspendido y anónimo; JWT/RBAC y pertenencia confirmados exclusivamente por backend, nunca mediante header de EmpresaId o slug de frontend.
- **F02:** almacenamiento efímero, privado y aislado; objetos con identificadores opacos, datos no productivos, namespaces A/B no superpuestos y versiones de archivo para reversión. Un stub sirve sólo para unidad, nunca certifica al proveedor real.
- **F03:** reloj UTC determinista, controles antes/después de TTL, método/recurso de firma inmutables, clock skew explícito y configuración de caché diferenciada por empresa.
- **F04:** archivos válidos e inválidos con firmas de bytes y MIME, longitud exacta/límite/exceso, checksum y streams truncados; cuotas y fallos de proveedor inyectados.
- **F05:** política sintética de retención/hold, inventario de hashes, eventos de auditoría sin PII/tokens/URLs privadas, y ensayo reversible sólo cuando exista autorización independiente.
- **F06:** tests causales de xUnit y API en .NET 10; MySQL 8.4 efímero si se modifica EF; Angular 22.2.1/Playwright si se altera UI; CI verificada contra SHA exacto. `SKIPPED`, `CANCELLED`, `FAIL` e `IN_PROGRESS` nunca son PASS obligatorio.

## Criterios AC01–AC14 (pares positivo/negativo NO_EJECUTADOS)

| ID | Positivo futuro | Negativo futuro | Evidencia exigida |
| --- | --- | --- | --- |
| AC01 | P01: usuario A con permiso consulta objeto A | N01: usuario A consulta objeto B | Prueba A/B de API, sin filtración de existencia ajena |
| AC02 | P02: tenant B crea key interna opaca y namespace B | N02: cliente altera EmpresaId, slug o header para usar A | Resolución de owner server-side y DTO sin key/bucket |
| AC03 | P03: lector autorizado sólo obtiene lectura | N03: lector solicita delete, upload o export sin rol | Matriz JWT/RBAC y pertenencia efectiva por operación |
| AC04 | P04: objeto válido normaliza key interna sin colisión | N04: entrada con traversal o Unicode ambiguo altera prefijo | Test de normalización, path injection y aislamiento |
| AC05 | P05: enlace firmado restringe objeto y método HTTP | N05: método/objeto/query mutados mantienen acceso | Firma y alcance verificables, sin mostrar secretos |
| AC06 | P06: enlace sirve antes de TTL y vence después | N06: enlace vencido o fuera de clock skew sirve contenido | Fake clock y caché expirada no reutilizada |
| AC07 | P07: política define explícitamente bearer reusable/one-time | N07: repetición dentro del TTL se presume bloqueada sin estado | Ensayo de replay; nonce/registro si se exige uso único |
| AC08 | P08: cambiar A→B limpia la caché del cliente | N08: CDN/proxy devuelve objeto A a sesión B | Prueba de partición e invalidación A/B |
| AC09 | P09: bytes/MIME/extensión admitidos y verificados | N09: extensión MIME benigna esconde bytes prohibidos | Detección de bytes y respuesta segura |
| AC10 | P10: upload respeta cuota, longitud y checksum | N10: excedente/stream truncado deja objeto visible parcial | Tests límites, integridad, rollback de objeto huérfano |
| AC11 | P11: fallo de proveedor produce error tipado saneado | N11: timeout/reintento crea duplicado o muestra firma | Inyección de fallos, idempotencia y logs redactados |
| AC12 | P12: lifecycle respeta retención/hold sintético | N12: política elimina objeto bajo retención | Prueba de hold, respaldo y restauración segura |
| AC13 | P13: auditor conserva correlación y tenant autorizado | N13: trace/log/API revela token, firma, ruta o datos B | Aserciones negativas sobre observabilidad y DTO |
| AC14 | P14: migración autorizada verifica manifiesto, hash y rollback | N14: activar R2/DNS/cutover sin autorización o reversión | Gate de permiso separado; ensayo efímero y readback |

**Regla de evaluación:** un futuro `PASS` requiere fixture, pasos, resultado esperado/real, evidencia, ambiente, autor, SHA de código y checks causales. Una fuente documental no sustituye pruebas runtime ni A5 independiente. `NO_APLICA` sólo con justificación por diff y tecnología.

## NFR01–NFR08 y gates

| ID | Restricción observable para implementación futura |
| --- | --- |
| NFR01 | JWT/RBAC/tenant aplicados en backend con denegación por defecto |
| NFR02 | Firma mínima en recurso, método, TTL y alcance; mecanismo de replay explícito |
| NFR03 | Ausencia de caché compartida, leaks A/B y enumeración de objetos |
| NFR04 | MIME por bytes, checksum, tamaños/cuotas y streaming sin objetos parciales |
| NFR05 | Auditoría/correlación sin secretos, firmas, key interna ni PII |
| NFR06 | Lifecycle sujeto a retención, integridad, backup y recuperación |
| NFR07 | Métricas de latencia/tamaño/errores/costo en ambiente efímero, sin inventar umbrales alcanzados |
| NFR08 | Cutover Cloudinary→R2 y DNS sólo tras orden expresa, doble lectura, reconciliación y rollback |

**Trazabilidad:** `c10-02-m01-01-alcance-r2-dominios-archivos.md` NR01–NR14 (blob `3359ab47c9d6a472d39c64a627da057ac01ddd1f`); `c10-02-m01-02-actores-interfaces-r2-dominios.md` A01–A14, F01–F08 (blob `185471a4de72653b6e3a199ee7a8aaa34ef3ea88`); `c10-02-m01-03-inventario-fuentes-brechas-r2-dominios.md` G01–G12 (blob `2b3b5de9f410ef2e287e2863bef9ca83e9358400`); `c10-02-m01-04-riesgos-supuestos-decisiones-r2-dominios.md` R01–R12/S01–S08/D01–D08 (blob `5408dbbc36aa3a45329cd931c01cea2e8ab7b070`). El inventario de código vigente contiene `StorageTenantContext`, interfaces de almacenamiento y adaptadores Cloudinary; ninguno demuestra integración R2.

## Impacto, A0–A5 y reversión

**Impacto real:** únicamente este documento y una nueva sección contigua de `CHANGELOG_AI.md`. No cambia Domain/Application/Infrastructure/API, DTO, frontend, proveedor, SQL, tenant/JWT/RBAC, datos, secretos, workers, infraestructura ni cuentas. El futuro diseño deberá respetar la separación de capas y denegación server-side.

- **A0:** fuente, equipo autorizado, DAG VALIDADO/SI, PLAN estado/owner por claim único, Scope Lock STRICT y archivo fuera de scopes de otras Tasks.
- **A1:** AC01–AC14, 14 positivos/14 negativos y ocho NFR definidos y contrastables, todos NO_EJECUTADOS.
- **A2:** riesgos A/B, JWT/RBAC, expiración, replay, caché, logs, retención y migración considerados como contratos, no mitigaciones ya probadas.
- **A3:** compatibilidad documental con C10-01 y C10-02.M01.01–04. Tests runtime de .NET/MySQL/Angular/Playwright: NO_APLICA para un diff sólo Markdown; no marcarlos PASS.
- **A4:** publicación fast-forward y CAS sobre HEAD vivo; `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact-SHA; PLAN/COLA/BITACORA readback.
- **A5:** PENDIENTE hasta revisión por otra Task Javier distinta del autor; nunca autoprobación ni promoción por documento.

**Rollback:** `git revert` aditivo del commit documental y verificación posterior, sin reescritura del historial. M01.06, M02, M03, R2 real, migraciones y QA/main/PROD necesitan sus propios controles y autorizaciones.
