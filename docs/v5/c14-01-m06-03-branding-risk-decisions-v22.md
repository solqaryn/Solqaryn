# C14-01.M06.03 — Registro de riesgos, limitaciones y decisiones de identidad V22

**Criterio vigente:** `PLAN_MAESTRO!D2050`: «Documentar limitaciones, riesgos abiertos y decisiones tomadas». Fecha local 2026-11-20, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, autor de esta unidad DEV06 `6af16bbad26881918c283abe06e1f32a`. **R0 para este diff documental**, conservando **R2** en pruebas de JWT/RBAC/tenant/integraciones observadas. PLAN E2049–2053 describe una meta acumulativa de fase, **no** obliga a esta microtarea a certificar toda M06 ni altera permisos de producción.

## A0. Fuentes verificables y superficie del cambio

Cambios de esta unidad: este Markdown y una nueva sección independiente en `CHANGELOG_AI.md` preservando íntegro el historial. No se modifican runtime Angular 22, ASP.NET Core 10, RBAC, JWT, MySQL, DI, assets de empresas, rutas, pagos, migraciones, cuentas, secretos, QA/main/PROD ni branching. Consumidores del registro: próximos controles C14-01.M06.04–.06, revisores A5 y DAG. C14-01.M05.01–06 ya constaban LISTO bajo sus criterios; la prueba de M05.06 reparada `fc611e676aa1faa93101419f951baf701262956d` obtuvo GitHub-hosted Chromium **4/4 PASS**, 0 FAIL/0 SKIP, job `136735799511` run `44867961201`, Signed JWT de API ASP.NET10, MySQL 8.4.13 **efímero** y Angular22. Un tercer revisor DEV06 (distinto del autor DEV03 y reparador DEV05) dejó A5_PASS **de M05.06 únicamente**, con evento `SQV22-J50-F51E9CD1-C1401M0506-A5PASS-FC611E67-20261121-R11`. El fallo original 2/4 de SHA `8f809ead0a65ce17d5d31a52554ab6ccfffc2fa4` permanece histórico y no es PASS.

Las pruebas de M05.01 corresponden a reglas Angular (16/16), M05.02 contrato HTTP **simulado** de Angular (6/6), M05.03 navegador/API/JWT/MySQL real efímero (6/6), M05.04 aceptación positiva real (8/8), M05.05 rechazo real cross-tenant (7/7), M05.06 regresión y teclado real (4/4). La tabla de SHAs, ruta y jobs reproducibles está en `docs/v5/c14-01-m06-02-branding-evidence-manifest-v22.md`, autor DEV06; la auditoría D2048 independiente debe mantener su propio gate A5. **No extrapolar un contrato mock de M05.02 a autorización HTTP real.**

## Matriz de limitaciones (observaciones, NO defectos automáticamente confirmados)

| Superficie | Evidencia positiva disponible | Límite / prueba futura exigible |
| --- | --- | --- |
| Branding tenant privado | JWT firmado y SQL real en runner, encabezado de marca persistido A/B y 403/404 cross-tenant | No se ha demostrado que **toda** vista/archivo/job/exportación y toda sucursal respeten identidad y autorización; BOLA/IDOR requieren prueba por endpoint real |
| Plataforma global | Navegación global neutra y JWT separado de tenant A mediante BrowserContext distinto | No implica que cualquier rol empresarial pueda leer un catálogo privado, ni que todos los menús y rutas administrativas dispongan de guard vivo |
| JWT, rol y revocación | Rechazos anónimo, firma alterada, cabecera tenant A→B, ID sin pertenencia y ruta pública sin PII en flujos dirigidos | Expiración/refresh/stale claims, cambio de rol, reemisión y revocación concurrente para **todas las rutas** no se certifican por 4 casos de identidad visual; hipótesis R2 a probar, no hallazgo P0/P1 fabricado |
| Persistencia | MySQL 8.4.13 real **solo en GitHub-hosted efímero**, sin store, recarga A/B | No hay prueba con datos de clientes, migración en vivo, PSP o recuperación operacional; nunca usar producción para suplir un test |
| Usabilidad/performance | Playwright Chromium 320/375/768/1280, Tab/Enter natural y foco perceptible; regresión de 4 pruebas, tiempos CI 226/594/269/212ms | **No** Core Web Vitals medidos en campo, móvil físico, WCAG completo, SLA ni latencia global; métricas de campo **NO_MEDIDO** |
| Proveniencia GitHub CI | ScopeLock, V5, frontend/backend/security pertinentes SUCCESS sobre el SHA fuente reparado; 4/4 Chromium | La evidencia no se traslada automáticamente a nuevos SHAs cuando cambian blobs, fixtures o workflows; `SKIPPED/CANCELLED/FAIL/IN_PROGRESS` requeridos no son PASS |
| Rollback y continuidad | Revert normal de cambios Git en `dev` con CAS, documentación de gates y no alterar datos | El ensayo propio de **C14-01.M06.04** aún requiere evidencia real y separada; jamás interpretar este documento como rollback de base productiva |
| A5 y liberación | A5 independiente de M05.06 por DEV06, y C14-01.M06.01/.02 con sus entregables de scope | M06.03/.04/.05/.06 tienen A5 y DAG **propios**; A5 de una microtarea no certifica fase M06 entera ni libera QA/main/PROD |

## Decisiones y medidas preventivas trazables

1. Mantener **denegación por defecto** y presentación global neutra en clientes que carezcan de tenant autorizado; un branding visual mostrado no concede privilegio de leer recursos.
2. Para cualquier evolución R2, exigir pruebas adversariales con HTTP real de ASP.NET10 + JWT firmado y MySQL8.4 efímero por empresa A/B, con roles caducados/revocados, IDOR y respuesta segura. Playwright Chromium cuando se cambie UI; no sustituir por `HttpTestingController` de Angular o EF InMemory si el requisito requiere integración física.
3. Resolver errores de CI con hipótesis nuevas y corrección real, preservando `8f809ead` 2/4 FAIL histórico; el SHA `fc611e67` 4/4 PASS demuestra solo las rutas causales ejecutadas.
4. Mantener requisitos de regresión e identidad reactivos para M06.04 rollback, M06.05 solicitud de revisor y M06.06 gate técnico. Usar `PLAN H/I`, `COLA G/O`, `DAG F/G` **releídos** después de cada transición. No forzar fórmulas, ni alterar I del autor histórico o borrar BITACORA.
5. Las decisiones que implican QA, main, producción, pagos, datos reales, secrets, grants o una acción R3 siguen **NO_GO** sin orden específica; un CI verde de `dev` no habilita esas operaciones.

## A1/A2/A3 — Auditoría positiva, negativa, frontera y fallo

**A1 positivo:** reconocer 4/4 reales en SHA fuente como evidencia de login A/B y Tab/Enter y sus negativas, verificar que M05.02 6/6 usa mock, que los tiempos CI son locales y que 2/4 FAIL se conserva. **Negativo:** afirmar «M05.02 mock = real HTTP/JWT» o «M05.06 = todas las rutas/permisos y CWV del público» debe considerarse **error del informe**. **Frontera:** no elevar A5 de un caso M05 a M06.06. **Fallo:** required check CI distinto de `completed/success` deja la unidad VALIDANDO si resulta aplicable, sin inventar PASS. A2/A3 de **este cambio documental**: **NO_APLICA** porque no modifica runtime, roles, JWT, DB ni UI. Para cambios R2 futuros sí exigir pruebas pertinentes.

## A4 y A5 — Publicación y aprobación independientes

Publicar solo documento más encabezado autocontenido de `CHANGELOG_AI.md` sin truncar historia, en `dev`, commit FF/CAS sobre HEAD vivo. Exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` **SUCCESS exact-SHA** antes de LISTO documental; si siguen en progreso, conservar VALIDANDO/owner y evidencias. A4 incluye PLAN/COLA/BITACORA readback y lock V17 H:I exclusivo, ningún cambio artificial de DAG. **A5 por otra Task Javier distinta del autor DEV06**, auditando fuentes y límites. Rollback mediante `git revert` ordinario, sin borrar datos legítimos o eventos históricos. Fuentes: STANDARD V5 institucional, `PLAN!D2042:D2053`, GitHub `fc611e...` y https://github.com/solqaryn/Solqaryn/actions/runs/44867961201/job/136735799511.
