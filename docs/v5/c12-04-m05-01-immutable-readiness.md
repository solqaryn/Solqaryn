# SOLQARYN V19 — C12-04.M05.01: congelación profunda de evidencias de cutover (UI)

**Autor** DEV04 JAVIER. Sólo `solqaryn/Solqaryn@dev`, PLAN fila 1862, `PROJECT_SCOPE_LOCK=STRICT`, riesgo **R2** por referencia a restauración/cutover y prevención de falsificación de UI.

## A0 — Hallazgo y cambio concreto

La referencia privada de cutover `C1204CutoverReadonlyComponent` congelaba sólo el array CONTROLES. Un objeto interno aún era mutable por referencia, permitiendo falsificar localmente `evidencia: 'LISTO'` sin MySQL, checksum, restauración ni autorización. Cambio mínimo: congelar con `Object.freeze` cada uno de los cuatro objetos **y** su colección. No cambia IDs, textos, rutas, backend, Base de Datos, permisos ni proveedor.

## A1 — Pruebas causales

Cinco Angular TestBed/Vitest: colección y entradas inmutables ante `Object.assign` malicioso; secuencia exacta de cuatro controles contractuales sin pruebas de restore falsas; cambio de tema sin alteración de evidencia ni solicitud; revocación de sesión oculta checklist; rechazo de insertar un quinto control o modificar un ID. Positivo, negativo, frontera y manipulación adversarial. Si se revierte el `Object.freeze` de cada objeto, el test de mutación falla.

## A2–A4 — Seguridad, integración y limitaciones

El cambio afecta sólo la inmutabilidad de metadatos observacionales. Autorización backend JWT/RBAC/tenant, MySQL 8.4 de ensayo, migraciones, respaldo, checksum, cutover real, proveedor, WCAG integral y producción **NO_CERTIFICADOS/NO_APLICA** para este diff. Angular TestBed real y build/lint en GitHub-hosted Node24/npm11 son las integraciones pertinentes; no se requiere DB EF efímera porque no se modifican esquemas ni consultas. Exigir Vitest dirigido, Scope Lock, V5, frontend y seguridad pertinentes SUCCESS del SHA exacto y readback PLAN/COLA/DAG/BITACORA. `SKIPPED/QUEUED/IN_PROGRESS` nunca PASS.

## A5 y rollback

A5 independiente por otra Task Javier distinta de DEV04, revisando código, test y CI. Reversión con `git revert` normal en dev, sin QA/main/PROD, secretos o datos productivos, conservando histórico BITACORA.

