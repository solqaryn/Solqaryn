# C15-01.M06.02 — manifiesto reproducible de versiones, diff, artefactos y resultados

SOLQARYN / solqaryn/Solqaryn@dev / PROJECT_SCOPE_LOCK=STRICT. Autor: DEV01 (6adbb9fff1548191a1bd5b30d9e9984a). **PLAN D2193:** Registrar versión, diff, artefactos y resultado reproducible. **Riesgo R0** para este manifiesto: documental, sin cambios runtime; las pruebas e integraciones referidas mantienen su riesgo R1/R2. PLAN E «Completa M06» es meta acumulativa, no se certifica por este ID.

## A0 — matriz de procedencia de siete unidades históricas

SHA de GitHub exacto por unidad, ruta del artefacto causal y hash Git blob. Un blob históricamente registrado no equivale por sí solo a CI verde, ni permite afirmar que la ruta no cambió en un HEAD posterior.

| Unidad | Commit fuente | Artefacto con blob Git exacto | Resultado pertinente y límite |
| --- | --- | --- | --- |
| C15-01.M05.01 | 7ce694ac0375415ac2695b6aa3084adfcdd82226 | scripts/quality/c15-01-m05-01-plan-rule.mjs — ef84366bef9b3265198486bc238065ab197f24a9 | Check dirigido cobertura 2340/negativos: success. Prueba regla/instantánea, NO equivalencia semántica de todos los módulos |
| C15-01.M05.02 | 531717a8dca49ab0787d611c77652d5eab081ab9 | backend/tests/Solqaryn.Tests/Application/C1501M0502CoverageDtoContractTests.cs — 7cc2f86354263e32e85e9b1de9f642e684adebe9; servicio Application aebb1734c3634e8e348e937f37844b63b25b3ea9 | xUnit .NET contrato/errores PASS dirigido y Scope Lock/V5 success, pero check transversal de módulos **completed/failure** |
| C15-01.M05.03 | 71fae45dcd26a0fab79b081ac0d3806f39dad4d3 | scripts/quality/c15-01-m05-03-mysql.py — e89c7a442bfa5073bb38c5687c09e943c34b6026 | MySQL 8.4 efímero, cobertura 2340 IDs con rollback/negativos: success dirigido. No Aiven, lectura Sheet viva ni mapeo semántico |
| C15-01.M05.04 | 1bd41c39492799eb77f99cc9cd9ca802edde5a23 | scripts/quality/verify-c15-01-m05-04-coverage.mjs — 74b0812b5143961b842450f9dc6be5620c713a1e | Node audit estática positiva/adversarial: success; no navegación real de todas las pantallas |
| C15-01.M05.05 | 1bee9d8b92a83331895527892c808aecac036be3 | scripts/security/verify-c15-01-m05-05-guards.mjs — 183a2176a5e8fe190d98686c6f63c5dfdd533126 | Ocho mutaciones/guardas sintácticas: success; no IDOR HTTP JWT de todos los tenants |
| C15-01.M05.06 | c1f717d6dd705d350a4e77dc3c5fb780adb78abd | frontend/e2e/c15-01-m05-06-regression.spec.ts — 8df7619b087b6c3e2809c9a2a4665d4e1d795bbb | Chromium real 3/3 con 320/768/1280, Tab 44 px y 403/503 **de sesión sintética**, PASS dirigido; check transversal módulos **completed/failure**; fallo inicial 2/3 en SHA anterior preservado |
| C15-01.M06.01 | c6f2e5306bf35875f51ba9f30a74a888d91d3723 | docs/v5/c15-01-m06-01-real-vs-mocks-evidencia.md — d39c88aeaf1a409da116a2ae363e981482c13f9e | Auditoría R0 de seis checks fuente, distingue pruebas reales y mocks; Scope Lock/V5 success exact-SHA. A5 independiente sigue su propio gate |

**Versiones de stack de referencia en la rama dev:** Node 24; Angular 22.2.1; ASP.NET Core net10.0; EF Core 10.0.12; MySql.EntityFrameworkCore de Oracle 10.0.9. Releer package.json/csproj del SHA si las herramientas cambian. MySQL 8.4 del runner es temporal, no una instancia productiva. GitHub Actions dirigido sí ejecuta comandos reales para los IDs de integración, pero esto no significa que se hayan ejecutado integraciones de todos los sistemas.

## A1 — procedimiento reproducible, positivo y negativo

1. Consultar GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100 para cada SHA anterior; exigir head_sha idéntico, status=completed y conclusion=success de los checks pertinentes. Usar los logs de ejecución enlazados, filtros y número de tests, no sólo el nombre de un job.
2. Con git show <SHA> --name-status y git diff <SHA>^ <SHA> -- <ruta> verificar que el artefacto fue parte del changeset. Contrastar hashes de blobs indicados con git rev-parse <SHA>:<ruta> (formato Git blob). Si HEAD cambió, revalidar los blobs reales, configuración y dependencias o reejecutar tests; no reutilizar PASS por parentesco sin evidencia.
3. Prueba positiva documental: los siete SHA y ocho blobs anteriores tienen fuentes concretas y comprueban un ámbito delimitado. Prueba negativa: rechazar que un success dirigido borre failures de M05.02 y M05.06 o que mocks/session page.route equivalgan a JWT firmado, MySQL real de negocio, WCAG global o dispositivos físicos.
4. Registrar con PLAN H/I/J/K/L/M, BITACORA append-only, COLA G/O y DAG F/G. El manifiesto no cambia fórmulas, no toma ownership de tareas históricas, ni convierte M06.06 a elegible.

## A2/A3 — NO_APLICA sólo al changeset documental

Este manifiesto no toca runtime/frontend/backend, JWT, RBAC, MySQL, migraciones o usuarios: por ello NO_APLICA ejecutar nuevas pruebas HTTP, MySQL, Browser para este diff R0. Ello NO elimina el check transversal completed/failure de M05.02/M05.06, riesgos R2 del alcance y pruebas aún no ejecutadas por otros IDs. El M05.02 revisado sí modificó código Application en su fuente; debe conservarse ese riesgo real al inferir capacidades. Los archivos de test y la operación real de GitHub/MySQL/Chromium se citan únicamente con su alcance.

## A4/A5, continuidad y reversión

Publicar en dev mediante commit FF/CAS y CHANGELOG de sección nueva con encabezado+viñetas contiguas, preservando 100 % de historia previa. Verificar verify-solqaryn-project-scope y V5 provenance completed/success exact-SHA para este manifiesto; dejar VALIDANDO si CI todavía corre. Transicionar sólo con lock V17 y CAS puntual, evento BITACORA idempotente y readback PLAN/COLA. A5 requiere otra Task Javier distinta de DEV01, independiente de autor y reparador del changeset.

Sin autorización QA/main/PROD, pagos, dominios, restauración productiva, borrado de datos, forzado de DAG ni certificación general M06. Rollback de este nuevo documento mediante git revert normal tras revalidar HEAD y escrituras concurrentes, nunca reset/force-push.
