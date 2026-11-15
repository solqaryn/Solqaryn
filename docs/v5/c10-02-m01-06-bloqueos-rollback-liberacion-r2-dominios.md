# SOLQARYN V5 — C10-02.M01.06: bloqueos, rollback y liberación documental de R2/dominios

**Microtarea:** `C10-02.M01.06`, PLAN_MAESTRO fila 1483. **Resultado exigido:** definir bloqueo, reversión y condición de liberación del lote M01. **Autor:** PRIMARY DEV 06 JAVIER, Task `6af16bbad26881918c283abe06e1f32a`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`; **fecha:** 2026-11-15.

## Decisión de alcance: M01 sólo prepara M02

El objetivo de la unidad C10-02 es definir criterios de buckets privados, namespaces/prefijos por tenant, URLs firmadas con expiración y replay, retención/lifecycle y dominios de archivos para una **posible** integración futura. **El resultado de esta M01.06 es exclusivamente un contrato documental de gates y reversión**. No instala Cloudflare R2, cambia DNS/CDN/CORS, adquiere servicios, activa credenciales, migra Cloudinary, cambia JWT/RBAC/tenancy, ejecuta migraciones MySQL, modifica UI/API ni despliega en QA/main/PROD. Cloudinary sigue siendo la integración documentada vigente, sin inferir configuración real por ambiente.

**Liberación de M02 documental** significa únicamente que se puede reclamar una M02.* cuando el **DAG individual F=VALIDADO/G=SI, COLA G/O=SI, PLAN PENDIENTE sin owner y CAS exclusivo** lo permitan; no libera automática ni colectivamente las seis filas M02. El cierre de esta M01.06 en LISTO con A0–A4 **no** equivale a A5_PASS; para las sucesoras que requieren A5 debe existir revisión independiente de otra Task Javier, distinta del autor.

## Antecedentes y criterio exacto de la dependencia

El DAG `C10-02.M01.06` exige los M01.01–05 de los bloques `C02-01/02/03/04`, `C03-01/02/03/04` y `C10-01`, más `C10-02.M01.01–05+A5`, `C02-01.M01.06+A5` y `C10-01.M01.06+A5`. Revalidar estos IDs exactos antes del cierre y de cualquier sucesora. El preflight encontró `DAG F=VALIDADO/G=SI` y `COLA G/O=SI`; esta fotografía no reemplaza la relectura poscambio.

Documentos propios originales contrastados por blob Git:
- `c10-02-m01-01-alcance-r2-dominios-archivos.md`, blob `3359ab47c9d6a472d39c64a627da057ac01ddd1f`: objetivo, NR01–NR14 y 6P/12N **futuros**.
- `c10-02-m01-02-actores-interfaces-r2-dominios.md`, blob `185471a4de72653b6e3a199ee7a8aaa34ef3ea88`: 14 actores, 12 pares I/O y 8 fronteras de confianza.
- `c10-02-m01-03-inventario-fuentes-brechas-r2-dominios.md`, blob `2b3b5de9f410ef2e287e2863bef9ca83e9358400`: 10 fuentes de código/documento, brechas G01–G12, sin certificación de proveedor.
- `c10-02-m01-04-riesgos-supuestos-decisiones-r2-dominios.md`, blob `5408dbbc36aa3a45329cd931c01cea2e8ab7b070`: 12 riesgos potenciales, 8 supuestos y 8 decisiones pendientes.
- `c10-02-m01-05-criterios-aceptacion-r2-dominios.md`, blob `d68729dfbb96072870cbaf7252f390c2d2febf07`: 14 criterios AC con 14P/14N, seis fixtures y ocho NFR, **NO_EJECUTADOS**.

Los cinco documentos ya estaban LISTO y poseían A5 independiente registrado al reclamo de M01.06. La propia M01.06 no puede autoaprobar A5.

## Matriz de bloqueo B01–B12 — fail-closed

| Gate | Condición negativa que BLOQUEA | Evidencia para liberar *esa operación*, nunca de forma implícita |
| --- | --- | --- |
| B01 | Predecesor sin LISTO o A5 requerido pendiente/FAIL | PLAN H/K, BITACORA 26 columnas, DAG F/G y COLA G/O por ID exacto |
| B02 | Otro owner, lease o escritor activo | CAS único y readback H/I/K; no transferir por antigüedad |
| B03 | Scope Lock, Skill, HEAD o check SHA no acreditados | Skill canónica, Git SHA exacto y check `verify-solqaryn-project-scope` SUCCESS |
| B04 | Emisor de objeto confía en `EmpresaId`, slug, header o key del navegador | Autorización JWT/RBAC y owner server-side con pruebas negativas tenant A/B |
| B05 | URL firmada sin objeto/verbo/TTL, revocación o replay aclarado | Modelo bearer/one-time explícito, reloj controlado y pruebas de expiración/reutilización |
| B06 | CDN/cache permite lectura cruzada, enumeración o URLs permanentes | Cache partition/no-store apropiado, negativos A→B, 403/404 saneados |
| B07 | Prefijo/key admite traversal, colisión, credencial en cliente o permisos excesivos | Normalización, test de aislamiento y política privada con menor privilegio |
| B08 | MIME, streaming, tamaño, checksum o cuota no validados por bytes reales | CI con archivo benigno y malicioso, truncamiento, fault injection y rollback parcial |
| B09 | Retención/hold no documentada o lifecycle borra datos protegidos | Matriz de clasificación, restore efímero y autorización de retención competente |
| B10 | Inventario/backups/hashes o reversión de migración no probados | Reconciliación antes/después, prueba restauración y plan cutover reversibles |
| B11 | Logs/tokens/URL firmada/PII en errores, código o auditoría | Secret scan, pruebas negativas de redacción, correlación sin contenido privado |
| B12 | R2, DNS, dominio, costos, datos reales o QA/main/PROD sin orden expresa | Autorización adicional diferenciada; ningún PASS documental la sustituye |

**B04–B12 son puertas para futuras implementaciones/activaciones, no afirmaciones de que existan defectos en la versión actual**. Para la unidad M01.06 la prueba causal es consistencia documental + gobernanza + checks Git del cambio Markdown.

## Condiciones de salida y reversión por capa

**Nivel 1 — M01.06 LISTO_DEV documental:** (a) A0 inspección real de autoridad, DAG, alcance, diff, secretos; (b) A1 coherencia observable de B01–B12 y mapa de entregables/casos; (c) A2 límites JWT/RBAC/tenant A/B, replay, privacidad y denegación segura documentados; (d) A3 interfaces/capas existentes no modificadas, no se contabilizan como PASS suites runtime no causales a Markdown; (e) A4 publicación fast-forward/CAS de un documento más sección indivisible de CHANGELOG y checks Scope Lock/V5 completos SUCCESS **SHA exacto** y readbacks PLAN/COLA/BITACORA. Si CI obligatoria está QUEUED/IN_PROGRESS/CANCELLED/FAIL, mantener VALIDANDO, no declarar LISTO.

**Nivel 2 — A5 independiente:** otra Task entre las seis Javier, distinta del autor, inspecciona diff, blob SHA, evidencia, seguridad y BITACORA; actualiza PLAN K y evento idempotente A:Z y relee DAG/COLA. Un resultado FAIL mantiene el gate; no fabricar PASS.

**Nivel 3 — M02 permitido:** sólo ante LISTO+A5 en los predecesores requeridos y DAG individual VALIDADO/SI, COLA G/O=SI, owner libre y CAS exclusivo. M02 define modelos/API/DTO/configuración **conceptuales**; no ejecuta implementación R2. Ningún cierre en M01 autoriza M03, terminal M06, IAM/DB, migración, producción ni pagos.

**Rollback R1 documental:** si se detecta inconsistencia en esta entrega, mantener scope VALIDANDO o A5_FAIL y publicar `git revert` **aditivo** del SHA afectado (sin force-push, sin borrar BITACORA) y actualizar estados/evidencia mediante controles CAS. Releer gates de sucesoras y suspender sólo las afectadas.

**Rollback R2 futuro (NO_EJECUTADO):** antes de una migración autorizada, preparar manifiesto de objetos y hash, backups probados y cutover de doble lectura o congelación coordinada. Ante fallo de firmas/ACL/tenancy/caché/corruptela, detener escrituras al destino, revocar capacidades si hay mecanismo, volver al proveedor fuente verificado y reconciliar escrituras; no borrar datos del origen hasta auditoría y restauración comprobadas. No se asume que una URL expirada permita revocación inmediata; un token bearer puede reproducirse durante su TTL.

## Pruebas positivas y negativas requeridas a futuro, no ejecutadas

**P01–P06 (futuras):** autorización A→A/B→B; URL de lectura con método/objeto/TTL correcto; upload validado por bytes+checksum; cambio tenant invalida caché; lifecycle respeta hold; reversión de migración autorizada conserva hashes.

**N01–N12 (futuras):** A lee B; header de empresa falsificado; lector borra; firma alterada o expirada; replay dentro de TTL; traversal/prefix injection; MIME falso; stream parcial/sobre cuota; caché entre tenants; fuga de firma/token en logs; retención violada; cutover R2/DNS sin permiso.

Ninguno de P01–P06 o N01–N12 es PASS de prueba ejecutada en M01.06. Pruebas .NET 10/xUnit/Angular 22.2.1/Playwright/MySQL 8.4 efímero se exigen según el diff **futuro**, nunca con datos productivos sin autorización.

## Impacto y autoridad

**Impacto ejecutado previsto:** únicamente este `docs/v5` y `CHANGELOG_AI.md`; cero cambios en Domain/Application/Infrastructure/API, Angular, SQL, redes, secretos, JWT/RBAC, dominios, proveedores o cuentas. Observabilidad futura sanitizada es un requisito, no instrumentación activa.

**Control de salida:** registrar cambio de estado/eventos A:Z/readbacks antes y después; reconsultar sucesoras exactamente en DAG/PLAN/COLA. Conservar Task autora y revisión A5 futura independiente. **No crear webhook, dispatcher, PR, rama auxiliar, Task nueva ni ningún despliegue**.
