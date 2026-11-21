# C04-01.M04.01 — Superficie privada de referencia del catálogo de planes
Repositorio: solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT.
Autor y escritor: SOLQARYN PRIMARY DEV 03 JAVIER, 6adc2e4bd8a48191ac7d8641ed95a1a3.
Origen institucional: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES y PLAN_MAESTRO!D596.
Riesgo: R2 por ruta privada y protección de contexto de plataforma; no cambia autoridad backend.

## A0 — alcance, integración, dependencias y rollback
Salida atómica de M04.01: registrar ruta lazy privada `/plataforma/catalogo-planes-v22` y componente Angular 22 standalone, informativo, accessible y sin operaciones comerciales. La meta acumulativa M04 de PLAN E no autoriza inventar precios, monedas, versiones oficiales, vigencias, impuestos ni límites activos. Las definiciones M01/M02 siguen siendo contractuales: no existe autorización para cobro, upgrade, downgrade, suscripción productiva, migración ni PSP. La ruta usa `plataformaGuard` existente: sesión JWT local y revalidación HTTP backend `GET /plataforma/sesion`; el componente oculta contenido ante señal revocada, alcance Empresa o admin falso. El backend mantiene la autoridad real y la resolución tenant/RBAC. No se modifican API/DTO, Domain, Application, MySQL ni migraciones; datos empresariales no se consultan. Rollback: git revert del changeset bajo dev y validación de rutas, manteniendo histórico institucional.

## A1 — pruebas capaces de fallar
Vitest dirigido verifica ruta única con guard y lazy-load, render autorizado de tres contratos informativos, revocación reactiva, anonimato, Empresa/admin falso, HTML hostil y ausencia de controles de escritura/precios. Playwright en Chromium prueba anonimato, 403, 200 sintético, ausencia de métodos POST/PUT/PATCH/DELETE, responsive 320/375/768/1280 y foco real por Tab. GitHub-hosted workflow dedicado verifica exact HEAD, Scope Lock, npm 11, typecheck/lint, Angular build, Vitest y Chromium. No declarar PASS hasta ver jobs `completed/success` exact-SHA.

## A2 — seguridad y limitaciones
Los resultados de interfaz y una sesión sintética **NO** acreditan JWT firmado, revocación en base de datos, RBAC server-side ni aislamiento MySQL A/B. Esas pruebas y sus gates corresponden a implementación backend/seguridad conforme a las microtareas de M03/M05 pertinentes. La ruta carece de capacidad para conceder permisos; la autorización global se decide en API. Nunca tratar guards Angular como fuente de autoridad de negocio.

## A3/A4 — evidencia y cierre
Para esta modificación frontend-only, API HTTP real, MySQL efímero y transacción comercial son NO_APLICA motivado como pruebas de **este diff**; se conserva explícita la limitación de cobertura integral de fase. Chromium sí es pertinente y se exige. Evidencia final: SHA de commit, check-runs GitHub-hosted de frontend y Scope Lock, procedencia CHANGELOG, PLAN H/I, COLA/DAG y BITACORA con readback. Si CI está en cola/progreso/falla, conservar VALIDANDO sin LISTO ni A5 aprobada. A5 requiere otra Task Javier revisora independiente del autor/reparador. QA/main/PROD quedan fuera.
