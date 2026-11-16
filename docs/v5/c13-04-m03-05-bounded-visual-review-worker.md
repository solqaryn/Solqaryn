# SOLQARYN V18 — C13-04.M03.05 worker acotado para revisión visual aislada

**Identidad y autoridad:** `solqaryn/Solqaryn`, rama `dev`, PROJECT_SCOPE_LOCK=STRICT, seis Tasks Javier. Autor PRIMARY DEV 03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. PLAN_MAESTRO fila 1998; lock atómico V17 `SQV17_C13_04_M03_05` / `2000001998`. No hay autorización para QA, main, PROD ni proveedores live.

## A0 — alcance, arquitectura y criterio

M03.05 exige implementar worker, proveedor, cola o integración **cuando aplique**. La implementación auténtica elegida es un **orquestador asíncrono acotado y secuencial en proceso**, no un servicio alojado ni una cola externa. Consume la interfaz `IC1304VisualReviewSnapshotReadPort`, cuyo adaptador real delega en `C1304VisualReviewSnapshotAdapter.ReadAsync` ya existente (M03.04). Cada lectura vuelve a comprobar autoridad con `IC1304VisualReviewAuthoritySource` de M03.03. Cero cambios a DI, HTTP, JWT, EF, MySQL, migraciones, tablas, secretos, dominios o tenants productivos.

Los tickets se materializan una única vez y antes del primer `await`, en lote de 1–8; el preflight valida forma exacta, clave, SHA de 40 hex minúsculos, tenant idéntico, ausencia de duplicados y rechazo explícito de publicación. Una enumeración mutable, inexacta o que lanza error se rechaza sin leer la fuente. Se leen snapshots secuencialmente, se valida cada resultado contra tenant/clave/SHA y se devuelve **sólo código y contador**, nunca superficies privadas. Un fallo parcial, dato forjado, fuente caída o revocación no certifica ningún elemento. Cancelación propagada sin reintento.

No hay API externa que pueda asumirse conectada ni exportación/telemetría de servidor habilitada. `VISUAL_REVIEW_STAGED_BROWSER_A5_REQUIRED` expresa exclusivamente revisión preliminar sintética; nunca significa permiso, navegador aprobado, WCAG certificada o despliegue.

## A1–A3 — pruebas adversariales causales

xUnit dirigidas: integración real dentro de proceso con adapter de snapshots y fuente sintética de autoridad vigente/revocada; éxito sin concesiones; entradas mixtas tenant A/B, SHA distinto, clave inválida, publicación, duplicados, lote vacío y excesivo; enumerador con `Count` mentiroso; cambio de colección durante lectura async; fuente que lanza excepción con cadena secreta sanitizada; resultado aparentemente positivo con tenant o SHA alterado; cancelación previa y durante lectura.

**A2:** aislamiento lógico A/B, rechazo fail-closed y revocación verificados con autoridad sintética. **A3:** ejercicio de integración in-process con las clases existentes. MySQL 8.4, EF, endpoints HTTP, JWT auténtico, proveedor live y Chromium **NO_APLICA a este diff**, no atribuir pruebas de aquellos sistemas. Ante falla xUnit/CI, conservar VALIDANDO y reparar en nuevo SHA.

## A4–A5, pruebas y rollback

Publicar un solo commit fast-forward en `dev` sobre HEAD revalidado y verificar `verify-solqaryn-project-scope`, procedencia V5, backend Release/xUnit y seguridad/autorización pertinentes `completed/success` en el SHA publicado, o descendiente con equivalencia de blobs y test config demostrada. `QUEUED`, `IN_PROGRESS`, `SKIPPED`, `CANCELLED` o `FAILURE` no son PASS. Registrar evento idempotente de CLAIM/VALIDANDO/LISTO con readback de PLAN, COLA, DAG y BITACORA. Si CI externa está en curso, permanecer VALIDANDO con SHA/RUN/check/owner y liberar lock V17 tras readback. A5 es examen independiente de otra de las cinco Tasks Javier; la Task autora no se autoaprueba.

Rollback: `git revert` normal del commit en dev, preservando historia, BITACORA y evidencia. No force push ni datos productivos.
