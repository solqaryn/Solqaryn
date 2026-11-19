# C03-04.M06.04 — Ensayo de rollback transaccional MySQL 8.4 en sandbox efímero (V22)

PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
MICROTAREA_ID=C03-04.M06.04
AUTHOR_TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09
RISK=R2_INTEGRACION_MYSQL
A5=REVISION_INDEPENDIENTE_PENDIENTE

## Requisito exacto, fronteras y dependencias

PLAN_MAESTRO!D575 exige **ensayar rollback o desactivación sin borrar datos legítimos**; PLAN!E575 describe la meta acumulativa M06 (runbooks, RPO/RTO, restauración C10/C12), no permite afirmar una restauración global sólo por esta prueba atómica. Esta entrega crea exclusivamente una suite bash y un workflow GitHub-hosted, además de este documento y una sección aditiva de CHANGELOG. No modifica Domain, Application, Infrastructure, API, Angular, EF, migraciones, configuraciones de proveedor, contratos públicos, secretos, datos vivos ni el scheduler de las seis Tasks.

Base causal: las pruebas de C03-04.M05.03 y M05.06 utilizaron MySQL 8.4 aislado, pero la [matriz de riesgos anterior](./c03-04-m06-03-limitaciones-riesgos-decisiones.md) declara que ninguna realizó una restauración íntegra de respaldo. Se preserva ese hecho. Esta unidad aporta **rollback de transacción de SQL real**, no un restore de copia de seguridad real, no un worker de incidentes ni una revocación JWT.

## Experimento GitHub-hosted y oráculos falsables

Workflow: `.github/workflows/v22-c03-04-m06-04-mysql-rollback.yml`. Script: `scripts/quality/v22-c03-04-m06-04-mysql-rollback.sh`. Se dispara por push de ambas rutas a `dev`, en runner Ubuntu con Docker `mysql:8.4` sin red de producción ni credenciales reales. El contenedor y la contraseña son exclusivos del ensayo; la limpieza elimina ese contenedor descartable.

1. Confirmar MySQL **8.4** real, crear la base exclusivamente sintética `sqv22_continuity_rehearsal` y dos filas legítimas tenant 10/20 con payloads de prueba.
2. Capturar el snapshot ordenado y su SHA-256. Mutar la fila A dentro de una transacción; el `ROLLBACK` debe devolver el mismo contenido.
3. Ejecutar una actualización con predicado de tenant incorrecto. El número de filas afectadas debe ser **cero**, no sólo un estado final correcto tras deshacer.
4. Simular fallo parcial con `SAVEPOINT`, mutar la fila B y `ROLLBACK TO SAVEPOINT` antes del `COMMIT`; la firma debe permanecer intacta.
5. Reiniciar el contenedor y volver a consultar ambas filas; tanto contenido como SHA-256 deben seguir siendo idénticos, sin eliminación ni corrupción.
6. Fallar el job ante versión inesperada, fila omitida, actualización no nula de otro tenant, rollback incorrecto, reinicio no durable o hash diferente.

El predicado SQL verifica el guard de esa consulta sintética; **no** acredita JWT firmado, RBAC de servidor, aislamiento del sistema completo, MySQL de Aiven, backup cifrado, restauración de archivos/Cloudinary, RPO/RTO o recuperación productiva. No se tocó ningún registro de clientes. Es deliberadamente un incremento acotado, con límites trazados a M06.03 y consumidores C10/C12.

## Gates A0–A5 y recuperación

- **A0:** acción D575; diff esperado de exactamente cuatro rutas (workflow, script, documento y CHANGELOG), sin cambios de producción. Riesgo R2 por ejercicio real de SQL en contenedor efímero.
- **A1:** transacción, negativa de tenant con `ROW_COUNT=0`, rollback parcial, estabilidad antes/después de reinicio; el test puede fallar.
- **A2:** no se modifica auth ni runtime, pero el predicado adversarial es obligatorio para este scope; JWT/RBAC HTTP = NO_APLICA **a este diff** y no equivale a certificación de seguridad de fase.
- **A3:** GitHub Actions debe demostrar MySQL 8.4 real, cero fallos de todas las verificaciones, no mocks. Ni `queued`, ni `in_progress`, `skipped`, `cancelled` ni `failure` requeridos cuentan PASS.
- **A4:** commit serializado FF/CAS sobre `dev` tras lock V17, checks `verify-solqaryn-project-scope`, `V5 - Sin encabezados vacios ni atribucion cruzada` y workflow dirigido exact-SHA SUCCESS, inspección de logs y readback PLAN/COLA/BITACORA. Mantener `VALIDANDO` mientras se ejecuta CI.
- **A5:** revisión causal posterior por otra Task Javier distinta de DEV04, con informe PASS o FAIL genuino, nunca autocertificación. M06.05/M06.06 y el DAG conservan sus propios gates.

**Rollback del changeset:** `git revert` normal en `dev` tras revalidación de HEAD. No confundir revertir estas pruebas con restauración de datos; no force-push, QA/main/PROD ni operaciones destructivas.
