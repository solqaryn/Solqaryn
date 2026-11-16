# C05-04.M04.06 — densidad, carga progresiva y ligereza verificable

- Autor: PRIMARY DEV 06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a.
- Solo `solqaryn/Solqaryn@dev`, Scope Lock STRICT, PLAN fila 853, candado V17 2000000853.
- Riesgo R1: suite Playwright/Chromium de regresión para la superficie privada `/plataforma`, sin alterar runtime, API, JWT/RBAC, EF/MySQL, permisos, secretos ni producción.

## A0 — trazado del impacto
Se agrega una suite de cinco escenarios y workflow GitHub-hosted con concurrency aislada por SHA. Se reutilizan Angular Signals, componente existente de modo compacto y los estados existentes de directorio; no se crea una segunda UI ni se modifica configuración de tenants. Dependencias: router y guards actuales, servicio de sesión y directorio sustituidos únicamente por fixtures HTTP sintéticas en Chromium. Rollback: git revert normal del changeset, sin force-push.

## A1 — aserciones falsables
Anónimo y sesión 403 deniegan la superficie y no revelan mensajes privados. Con GET retenido se observa estado de carga, alternancia 3→0→3 sin nuevas consultas ni escrituras, finalización a estado vacío. Con 25 registros se inspecciona densidad real de filas, ausencia de overflow del documento a 320/375/768/1280 px y objetivo táctil de 44 px. Con error 503 se exige redacción, ausencia de reintento automático causado por el control de densidad y exactamente un reintento explícito. Estos asserts fallan si el producto rompe esas propiedades.

## A2/A3 — límites de seguridad e integración
Chromium real y compilación Angular en GitHub-hosted prueban UI con HTTP sintético. No se certifica JWT HTTP real, RBAC backend, aislamiento MySQL, acceso a proveedor remoto ni migración. Se comprueban denegaciones y cero métodos de escritura en el escenario progresivo. LCP ≤2,5 s, CLS ≤0,1 y tamaños de bundle son OBJETIVOS; métricas de campo NO_MEDIDO, jamás reportarlas PASS.

## A4/A5 — cierre
Exigir check dirigido de cinco Chromium, build/lint Angular, Scope Lock, procedencia V5, seguridad aplicable y CI obligatoria SUCCESS exact-SHA o descendiente con blobs de scope idénticos. Un job QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAIL nunca prueba PASS. A5 por una Task Javier de ID distinto al autor. Releer PLAN/COLA/DAG y BITACORA en cada transición. No QA, main ni PROD.
