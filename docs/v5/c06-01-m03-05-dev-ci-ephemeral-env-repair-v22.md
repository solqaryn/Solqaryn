# C06-01.M03.05 — recuperación causal de CI MySQL 8.4 (V22 DEV03)

## Causa verificada

En `dev` SHA `0a7abd164a3e781a0e325f73b39b507aef171a1f`, GitHub-hosted Actions [job 136879854541](https://github.com/solqaryn/Solqaryn/actions/runs/44915390027/job/136879854541) terminó **failure** con 141 PASS / 3 FAIL / 0 SKIP sobre 144 pruebas de `Category=Integration`. La causa observada en los logs no fue una migración real: dos casos `C0601M0305LandingReviewQueueMysqlTests` arrojaron `EPHEMERAL_MYSQL84_REQUIRED` porque `dev-ci.yml` no exportaba `C0601_M0305_MYSQL_TEST`. Un caso `C0701M0503CatalogPersistenceAcceptanceTests` arrojó `C0701_M0503_EPHEMERAL_MYSQL84_REQUIRED`; ese test adicional exige la base local exacta `c0701_m0503_bootstrap`.

## Reparación de infraestructura CI exclusiva de dev

- Publicar explícitamente las dos variables de conexión de tests en el trabajo `migrations`, limitadas a `127.0.0.1:3306` y servicio efímero GitHub-hosted `mysql:8.4`; no usar Aiven, QA ni PROD.
- Crear en ese contenedor solamente `c0701_m0503_bootstrap` para el segundo test; las suites no se omiten ni se convierten en mocks.
- Mantener los filtros `Category=Integration` y la barrera fail-closed actual, así como los checks de seguridad, provenance V5 y Scope Lock. Revalidar en el nuevo SHA que los 144 casos ejecutaron de verdad (o nuevos casos explícitamente reportados), **0 FAIL, 0 SKIP**, y que los demás required checks pertinentes terminaron SUCCESS.

## Límite del incremento y rollback

Corrección del gate CI compartido para la entrega aislada C06-01.M03.05; no incorpora proveedor comercial, JWT/HTTP público ni DI de producción. El estado permanece VALIDANDO hasta A0-A4 causal y A5 posterior de Task Javier independiente. Rollback seguro: revert normal del commit si se verifica regresión, sin reset, sin borrar BITACORA y sin modificar otras ramas. CI externa nueva pendiente, no constituye PASS anticipado.
