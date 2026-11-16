# C13-04.M03.04 — adaptador aislado de snapshots de auditoría visual

Identidad: SOLQARYN / solqaryn/Solqaryn / dev / PROJECT_SCOPE_LOCK=STRICT.
Microtarea: C13-04.M03.04, PLAN_MAESTRO fila 1997. Autor: DEV06 JAVIER,
TASK_ID 6af16bbad26881918c283abe06e1f32a.
Candado V17: SQV17_C13_04_M03_04, 2000001997. Riesgo V15 R2.

## A0 — alcance y dependencias

El contrato actual solicita "persistencia, migración reversible o adaptador aislado".
Esta entrega implementa **únicamente la tercera opción**: una clase .NET 10 en
Infrastructure con almacenamiento de snapshots en memoria, independiente de DI,
bases de datos, migraciones, HTTP, JWT y autorizaciones productivas.

Depende exclusivamente de IC1304VisualReviewAuthoritySource del alcance
C13-04.M03.03. Cada operación valida de nuevo identidad, tenant solicitado,
membresía vigente, permiso explícito, revocación, autenticidad de fuente,
SHA exacto, ventana de vigencia y lista de superficies autorizadas.
El almacenamiento usa claves compuestas (tenant, revisión) y revisión
optimista dentro de una sección crítica; la actualización concurrente de la
misma revisión tiene un único ganador. Las entradas son acotadas a 64 por
tenant y las superficies a 1–64 claves seguras. Los resultados exponen una
copia de solo lectura para impedir corrupción por referencias mutables.

La entrega **no persiste nada durablemente**: al destruir la instancia se
pierden los snapshots. No es un repositorio EF ni sustituto de MySQL; tampoco
se registra lector de autoridad real, endpoint, worker, proveedor externo,
revisión WCAG certificada ni pase a QA/PROD. Requiere conectar un proveedor
autorizado y migración separada en un scope futuro bajo riesgo R2/R3.

## A1–A3 — pruebas exigidas

Los xUnit dirigidos verifican: guardar/leer, revisión CAS y stale replay,
aislamiento tenant A/B, fuente revocada y permisos, expiración/SHA ordinal,
allowlist mutable y rechazo de claves inseguras, copia defensiva del resultado,
solicitudes malformadas sin lectura de fuente, error opaco de proveedor,
cancelación, carrera con una sola escritura ganadora y capacidad por tenant.

A1: pruebas positivas, negativas, frontera y error deben ejecutarse realmente
en GitHub Actions (.NET 10 Release) sobre SHA publicado.
A2: autorización/tenant A/B/revocación son simuladas mediante fuente confiable
de pruebas; **no** acreditan JWT HTTP, RBAC real ni acceso a MySQL.
A3: integración en proceso REAL del adaptador Infrastructure con puerto
IC1304VisualReviewAuthoritySource falso, ejercicio concurrente en xUnit.
MySQL 8.4/EF/Docker y navegador: NO_APLICA porque ninguno es utilizado
o modificado; no confundir con certificación de persistencia real.

## A4–A5 y recuperación

Publicación en un solo commit fast-forward a dev: fuente, pruebas, este
documento y CHANGELOG_AI.md. Antes de LISTO se exigen Scope Lock y V5
completed/success, CI backend Release/seguridad/autorización aplicables sobre
SHA exacto; CANCELLED/SKIPPED/QUEUED/IN_PROGRESS no son PASS.
Si la CI sigue externa, mantener VALIDANDO con SOURCE_SHA/RUN/owner,
liberar lock tras readback y retomar REVIEW_FIRST; si falla, reparar con
regresión y nuevo SHA. A5 es revisión independiente de otra de las cinco
Tasks Javier y nunca autoaprobación.

Rollback normal: git revert del commit publicado; no forzar ramas, borrar
historial BITACORA ni modificar main, QA, PROD, secretos o datos productivos.

## A5_FAIL P2 — recuperación de TOCTOU sobre entradas mutables (2026-11-16)

Revisión independiente de DEV01 sobre el SHA ff5a0a3d detectó que WriteAsync
validaba varias veces la IReadOnlyList del llamador, esperaba la autoridad y
almacenaba otra lectura posterior. Un actor con una colección mutable podía
cambiar las superficies entre la validación de allowlist y el almacenamiento.
Se conserva la evidencia A5_FAIL hasta nueva revisión independiente; no hay
prueba de explotación real ni de acceso a MySQL/JWT/producción.

Corrección en dev: materializar UNA sola copia acotada (1–64) antes del primer
await, rechazar conteos falsos/enumeración inconsistente, validar sintaxis,
no duplicados y la lista autorizada de servidor sobre esa misma copia, y
almacenar exactamente los mismos strings validados. La copia sigue siendo
in-memory, aislada y sin permisos. La regresión xUnit usa una colección
adversarial que cambia el contenido entre enumeraciones y dos actores con
mutación de la lista durante la espera de autoridad. La revisión A5 requiere
una Task Javier distinta del autor del cambio, tras pruebas backend Release,
seguridad, autorización, Scope Lock y V5 del SHA publicado. Hasta completar
CI y A5 no se levantan gates DAG ni se autoriza QA/main/PROD.
