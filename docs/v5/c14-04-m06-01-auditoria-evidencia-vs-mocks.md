# C14-04.M06.01 — Auditoría de correspondencia entre evidencia real y mocks

**Fecha de corte:** 2026-11-18. **Repositorio:** solqaryn/Solqaryn, rama dev. **PROJECT_SCOPE_LOCK=STRICT.** **Naturaleza:** R0, revisión documental de evidencias previas; no se modifica código de runtime, API, MySQL, migraciones, CI global, seguridad, infraestructura, recursos ni datos de clientes.

## A0 — Criterios de aceptación, alcance y dependencia

El objetivo exigido es **revisar que las pruebas correspondan al alcance real y no a mocks insuficientes**. Es la preparación documental M06.01 del bloque C14-04. La dependencia técnica M05.01–M05.06 figura LISTO con A5 independientes registradas en PLAN_MAESTRO y BITACORA; es una **condición de revisión documental**, NO es permiso para go-live ni afirmación de preparación productiva.

**Criterios:** (1) cada verificación debe identificar mecanismo, SHA, log y límites de su prueba; (2) un mock, sesión interceptada o un in-process test nunca se presentará como validación de backend JWT, tenant o proveedor productivo; (3) cada requisito sin evidencia apropiada conservará `SIN_VERIFICAR` o `NO_MEDIDO`; (4) cualquier diferencia crítica requiere prueba causal nueva y A5 independiente; (5) ninguna transición QA/main/PROD o irreversible se ejecuta con esta documentación.

## A1 — Matriz de trazabilidad real, sintética y pendiente

| Scope M05 / autor | SHA y evidencia CI real | Hecho que sí se puede afirmar | Hecho no acreditado; bloqueo |
| --- | --- | --- | --- |
| M05.01 / DEV01 | [Commit](https://github.com/solqaryn/Solqaryn/commit/70c4d32f453ee2c6e21c2524a1eb5e3a03fea1e1) · xUnit 5576/5576 en job 135114563979; A5 DEV06 | Reglas Domain in-process, negativos y compilación net10 dirigidos | HTTP JWT firmado, autorización viva, SQL por tenant y release |
| M05.02 / DEV01 | [Commit](https://github.com/solqaryn/Solqaryn/commit/1e3729573556ac1ae10dbad1399406ead2f9b17b) · xUnit 5618/5618 en job 135121403346; A5 DEV03 | Contrato API/DTO como prueba Application in-process; denegaciones sintéticas | Peticiones API reales autenticadas, MySQL y proveedor de producción |
| M05.03 / DEV03 | [Commit](https://github.com/solqaryn/Solqaryn/commit/a06756982b0cb84f235a9b9f7252dab53ec03751) · Node 6/6 en job 135131603764; A5 DEV02 | Cliente de lectura GitHub GET y negativa de estados no SUCCESS | Validación integral de cada workflow requerido, promoción o despliegue |
| M05.04 / DEV04 | [Commit](https://github.com/solqaryn/Solqaryn/commit/284e56d9e0f32de099e6ae7a8362e66e0a2d8aa0) · Chromium 3/3 en job 135115035681; A5 DEV02 | Navegador real ejecutado contra frontend local con sesión interceptada | Sesión JWT firmada, RBAC/tenant A/B del backend, release productivo |
| M05.05 / DEV01 | [Commit](https://github.com/solqaryn/Solqaryn/commit/b06499f0ca101ec475dbf7fd1f662bbe69b20d5d) · xUnit 5594/5594 en job 135117071596; A5 DEV06 | Reglas negativas in-process para decisiones Domain/seguridad | Prueba HTTP negativa IDOR/BOLA efectiva con MySQL de dos empresas |
| M05.06 / DEV03 | [Commit](https://github.com/solqaryn/Solqaryn/commit/963fa8c4e08300238f1b818fa7b33cb20ab1346c) · [Chromium 4/4](https://github.com/solqaryn/Solqaryn/actions/runs/44422167207/job/135217636831), lint/build Angular; A5 DEV06 | Viewport 320/768/1280, Tab+Enter real, movimiento reducido, smoke DCL local, UI sin falso PASS | JWT real, tenant/DB, WCAG integral, p95/p99, RPO/RTO, operación productiva |

Las métricas **5576/5618/5594** son resultados de las suites correspondientes y no número de casos propios de cada microtarea. **4/4 Chromium** tampoco representa 4 pruebas de go-live. La firma A5 acredita sólo el diff y alcance reales examinados en DEV.

## Contraste de aceptación positiva/negativa (regla falsable)

- **Caso positivo:** una evidencia real de Playwright es aceptable como `CI_BROWSER_DIRIGIDA` únicamente cuando el job del SHA causal tiene `status=completed`, `conclusion=success`, el log demuestra las cuatro pruebas y la sesión se describe explícitamente como interceptada; no cambia los estados de release.
- **Caso negativo 1:** `page.route()` o un token de localStorage inventado **NO** puede contabilizarse como `JWT_FIRMADO_BACKEND_PASS` o `TENANT_AB_HTTP_PASS`. Resultado obligatorio: `SIN_VERIFICAR`.
- **Caso negativo 2:** un test xUnit que ejecuta sólo Domain/Application **NO** acredita transacciones Oracle EF/MySQL 8.4, filtrado SQL por tenant o recuperación real. Resultado obligatorio: `SIN_VERIFICAR`.
- **Caso negativo 3:** un check `SKIPPED`, `CANCELLED`, `QUEUED`, `IN_PROGRESS` o un build genérico aislado **NO** puede satisfacer un check requerido; exigir SUCCESS del SHA causal o equivalencia probada de blobs y configuración.
- **Caso negativo 4:** DOMContentLoaded < 30 s en Chromium local **NO** constituye p95/p99, SLO, WCAG completo ni RPO/RTO; resultado: `NO_MEDIDO`.
- **Caso frontera:** aunque M05.01–06 estén LISTO+A5 en DEV, si falta prueba de release real o autorización R3, el estado de go-live permanece **NO_GO** sin alterar COLA/DAG ni crear permisos.

## A2/A3 — Límites de seguridad, integración y dependencias

Este diff contiene sólo documento y CHANGELOG. No crea nueva superficie JWT/RBAC/tenant, no modifica EF/MySQL ni cambia UI ejecutable. Por tanto HTTP, MySQL, migraciones, Chromium nuevo y proveedor productivo son **NO_APLICA a la edición R0**, pero **siguen SIN_VERIFICAR para la aceptación global de release**. Se requieren posteriormente, bajo alcance y permisos aplicables: API auténtica con JWT firmado y autorización negativa por roles/revocación; ataques IDOR/BOLA de tenant A/B sobre MySQL aislado; verificación end-to-end de servicios, respaldos/restauración, soporte, alertas y observabilidad; métricas y SLO representativos; procedimiento R3 autorizado de promoción y rollback. Ninguno se certifica aquí.

**Mapa de bloqueos:** CI/QA y controles pertinentes por SHA exacto, DAG dinámico calculado y revisión A5 distinta del autor. Especificar evidencia en lugar de imponer manualmente `SI` en COLA o DAG. El paso de M06.01 a LISTO, si se alcanza, **es cierre documental R0**, no autorización de M06 terminal, QA/main/PROD ni go-live.

## A4, A5 y reversión

Publicación prevista: dos archivos R0 en un commit fast-forward serializado directamente a dev, con Scope Lock y procedencia V5 exact-SHA SUCCESS. Si los checks siguen QUEUED/IN_PROGRESS, mantener VALIDANDO, evidencia SHA/RUN y owner DEV06. BITACORA A:Z mantiene eventos idempotentes; H/I/J/K/L/M preservan historia; candado V17 se libera sólo tras readback. A5 exige otra de las cinco Tasks Javier, distinta del autor DEV06; no autopase.

Reversión: `git revert` normal del commit en dev después de verificar HEAD y concurrentes; BITACORA no se borra, no hay migración ni reversión de datos productivos.