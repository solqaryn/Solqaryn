# SOLQARYN V5 — C10-03.M01.01: alcance, no-regresión e inventario real condicionado de archivos

**Unidad:** C10-03.M01.01 · PLAN_MAESTRO fila 1514 · **Autor:** PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a` · **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` · **Rama:** `solqaryn/Solqaryn@dev` · **PROJECT_SCOPE_LOCK:** STRICT · **DAG:** fila 1475 F=VALIDADO/G=SI en el preflight · 2026-11-14.

## Resultado y prohibición de inventar datos

El resultado exigido por el plan es **«Verificar inventario real de medios; si no hay archivos históricos, registrar cero y omitir exportación, copia, checksum o carga de migración. Preparar almacenamiento seguro para archivos futuros»**. Evidencia futura mínima: inventario real (cero permitido), fixture no productivo, MIME/tamaño/tenant y rollback si aplica.

**Estado real de inventario histórico al corte de este documento: NO_VERIFICADO.** No se ha leído un conteo autorizado de objetos en Cloudinary, MySQL ni proveedor alguno durante esta unidad; por tanto **no se declara ni cero ni un número positivo de archivos reales**. Un conteo cero es una conclusión que sólo puede escribirse tras inventario autenticado y contrastado, nunca una hipótesis derivada de que la plataforma está nueva. Del mismo modo, el documento no prueba que el inventario sea mayor que cero.

**Alcance de M01.01:** precisar objetivo, exclusiones, no-regresión, ramas del proceso y evidencia requerida para un futuro inventario seguro y almacenamiento de nuevas cargas. Se trata de **preparación documental**, no de exportación, migración ni implementación. No se acceden servicios de pago, proveedores productivos, credenciales, archivos de clientes o datos reales; no se cambia Cloudinary, R2, DNS, permisos, rutas, tablas, secretos o endpoints.

## Fuentes técnicas y fronteras

- `references/architecture.md` de la Skill vigente: Angular 22.2.1; .NET 10; Domain ← Application ← Infrastructure; MySQL EF; JWT/RBAC y pertenencia tenant en backend; Cloudinary documentado como integración actual, Cloudflare R2/Hostinger como arquitectura objetivo condicionada.
- `docs/v5/c10-02-m01-01-alcance-r2-dominios-archivos.md` (blob `3359ab47c9d6a472d39c64a627da057ac01ddd1f`) establece NR01–NR14, prefijos A/B, acceso privado, TTL, replay y no-cutover.
- `docs/v5/c10-02-m01-02-actores-interfaces-r2-dominios.md` (blob `185471a4de72653b6e3a199ee7a8aaa34ef3ea88`) define actores, I/O y fronteras conceptualizadas, **no proveedores ya activos**.
- `docs/v5/c10-02-m01-03-inventario-fuentes-brechas-r2-dominios.md` (blob `2b3b5de9f410ef2e287e2863bef9ca83e9358400`) inspeccionó interfaces `IImageStorageService`, `ICompraDocumentoStorageService`, `StorageTenantContext`, adaptadores Cloudinary y tests actuales, sin inventario productivo.
- `docs/v5/c10-02-m01-04-riesgos-supuestos-decisiones-r2-dominios.md` (blob `5408dbbc36aa3a45329cd931c01cea2e8ab7b070`) mantiene pendientes las decisiones de proveedor, retención, revocación, caché, costos y cutover.
- `docs/v5/c10-02-m01-05-criterios-aceptacion-r2-dominios.md` (commit `8d271013d22800e243dc002da85018ee2edd0fd7`) establece AC01–AC14 y 14 pares P/N futuros **NO_EJECUTADOS**.

Estos documentos se emplean como especificación y evidencia de fuentes Git, no como prueba de lectura de contenido de proveedores ni como autorización para conectar o exportar archivos. No se expone configuración de secretos.

## Árbol de decisión del inventario futuro

| Estado comprobable | Operación admisible en fase posterior autorizada | Evidencia obligatoria |
| --- | --- | --- |
| **INVENTARIO_DESCONOCIDO** (estado presente) | No exportar, copiar, borrar, cargar ni declarar cero; continuar diseño e identificación de permisos | Fuente, identidad, ambiente, consulta aprobada y criterio de exclusividad pendientes |
| **INVENTARIO_CERO_VERIFICADO** | Omitir por completo exportación, copia y checksums de archivos históricos; probar solo **fixtures nuevos no productivos** | Conteo `0` de todas las fuentes autorizadas, conciliación metadata/objetos y snapshot con fecha, ambiente y permisos |
| **INVENTARIO_POSITIVO_VERIFICADO** | Preparar manifiesto limitado por tenant, tipo, owner, hash/tamaño y retención; **no migrar todavía** | Conteos reconciliados, controles A/B, backup, autorización separada y pruebas de reversión |
| **INVENTARIO_INCONSISTENTE** | NO_GO sólo al scope de migración; conservar proveedor y datos, investigar discrepancia | Divergencia fuente DB/proveedor, error causal, propietario, próximo preflight seguro |

**No hay transición válida** de INVENTARIO_DESCONOCIDO a INVENTARIO_CERO_VERIFICADO únicamente por ausencia de registros visibles en una pantalla, por documentación del roadmap o por una respuesta no autenticada. Evitar enumeraciones cross-tenant y consultas administrativas no autorizadas; no guardar tokens ni URLs firmadas en BITACORA.

## Invariantes NR01–NR14 de no-regresión

| ID | Criterio y NO_GO |
| --- | --- |
| NR01 | El inventario distingue proveedor, ambiente y tenant; no mezcla objetos de A con B |
| NR02 | Un ID/slug/header de frontend nunca se considera prueba de pertenencia; backend valida JWT/RBAC |
| NR03 | El estado `DESCONOCIDO` nunca se codifica o reporta como `0` |
| NR04 | Cero histórico verificado implica cero exportaciones y cero checksums de objetos históricos |
| NR05 | Conteos positivos exigen reconciliar DB/metadatos y storage, sin inferir pérdidas |
| NR06 | Cada objeto migrable tiene owner, clasificación, tamaño, integridad, retención y origen |
| NR07 | No se borran históricos ni se cambia proveedor activo durante M01 |
| NR08 | Un fixture nuevo de prueba no se contabiliza como archivo histórico |
| NR09 | Acceso temporal mantiene recurso/método/TTL y modela explícitamente replay |
| NR10 | MIME se inspecciona por bytes, más tamaño/stream y checksum, no sólo extensión |
| NR11 | Secretos, firmas, keys, PII y URLs privadas nunca aparecen en logs/plan/Sheet |
| NR12 | Caché, respaldo y rollback conservan aislamiento entre tenants y ambientes |
| NR13 | Retención, hold y auditoría prevalecen sobre lifecycle de borrado |
| NR14 | No se habilitan R2, DNS, cutover, workers ni QA/main/PROD sin autorización específica |

## Escenarios P01–P08 / N01–N12 (FUTUROS, NO_EJECUTADOS)

**P01:** inventario acreditado encuentra cero y activa rama sin migración; **P02:** inventario acreditado positivo exige manifiesto y evidencia sin ejecutar copia; **P03:** objeto nuevo A con fixture permitido se procesa sólo en ambiente efímero; **P04:** objeto de B permanece aislado; **P05:** rechazo de MIME/tamaño conserva integridad; **P06:** fallo de proveedor de fixture se revierte sin objeto parcial; **P07:** auditoría registra operación/correlación sin secreto; **P08:** rollback controlado preserva último estado válido.

**N01:** inferir cero desde plan; **N02:** contar sólo registros MySQL pero omitir proveedor; **N03:** exponer archivos A al listar B; **N04:** cliente falsifica owner; **N05:** rol lector exporta/borrado; **N06:** fixture nuevo se confunde con histórico; **N07:** MIME de cliente no coincide con bytes; **N08:** tamaño excedido deja parcial; **N09:** URL firmada/replay no permitido; **N10:** credencial o URL aparece en logs; **N11:** lifecycle borra archivo con hold; **N12:** cutover ejecutado sin orden, reconciliación o rollback.

Los 20 escenarios son **oráculos propuestos, no pruebas ejecutadas**. `P01/P02` se excluyen mutuamente según inventario acreditado y sus resultados requieren evidencia distinta; no se asume la rama aplicable.

## Impacto, A0–A5 y rollback

**Rutas realmente modificadas:** un Markdown nuevo en `docs/v5` y una sección indivisible de `CHANGELOG_AI.md`. **No** cambian contratos API/DTO, Domain/Application/Infrastructure, Angular, entidades, MySQL, migraciones, auth, JWT/RBAC, logs, proveedor o datos. La implementación real futura tendrá controles y tests proporcionales por capa.

- **A0:** autoridad única V5 y DAG individual VALIDADO/SI; owner claim CAS; Scope Lock STRICT; no datos ni permisos de proveedor.
- **A1:** lógica de estados de inventario, catorce invariantes y escenarios observables futuros; ningún `PASS` de conteos ni runtime.
- **A2:** tenant A/B, JWT/RBAC, secreto, URL bearer, retención, integridad y default-deny revisados documentalmente.
- **A3:** cotejo de fuentes de C10-01/C10-02 con arquitectura. CI .NET, Angular, MySQL y Playwright: **NO_APLICA** al diff de dos Markdown, no PASS inventado.
- **A4:** fast-forward CAS a `dev`; verificar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success en SHA exacto, y PLAN/COLA/BITACORA readback.
- **A5:** revisión independiente por otra de las seis Tasks Javier, distinta de la Task autora, pendiente tras LISTO_DEV.

**Reversión:** `git revert` aditivo de este commit, sin force-push. **C10-03.M01.06, M02/M03, cualquier consulta/operación externa y QA/main/PROD requieren preflight y permisos propios**. Ningún valor de inventario real se certificó en M01.01.
