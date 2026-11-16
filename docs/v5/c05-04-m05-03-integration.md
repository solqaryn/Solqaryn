# C05-04.M05.03 — integración real del build de ligereza y reporte de bundle

Autor: PRIMARY DEV 02, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. Exclusivo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO fila 856; COLA G/O=SI y DAG F=VALIDADO/G=SI verificados antes del claim V17. Riesgo R2 por nueva CI y validación de artefactos, no por cambios en producción.

## A0: contrato, capas, dependencia y reversión

Se integran artefactos de un build Angular productivo real con el analizador ya autorizado frontend/scripts/performance-bundle-baseline.mjs. Prueba causal nueva compara referencias JS/CSS de index.html con todos los archivos iniciales medidos en disco, sus bytes y tamaños gzip/brotli. No modifica código runtime, routing, componentes, API/DTO, JWT, RBAC, tenant, EF, MySQL, migraciones, datos, secretos ni servicios externos. El bundle lazy nunca se interpreta como inicial. El presupuesto existente de 1 MiB advertencia/2 MiB error es informativo; no equivale a certificación automática. Core Web Vitals de campo: NO_MEDIDO.

## A1: aserciones adversariales falsables

Cuatro tests Node sobre build real: positivo comprueba coincidencia de referencias, bytes y compresión; negativo muta rawBytes y debe rechazarse; error de proveedor simulado añade script inexistente y debe rechazarse; frontera de index sin referencias impide fingir éxito. Se rechazan rutas relativas inseguras y duplicados. Ninguno falsifica una medición del usuario o un proveedor vivo.

## A2: seguridad y control empresarial

No se crea acceso a datos de empresa, API o tenant, ni se cambian autenticación, permisos, CSS dinámico, scripts del runtime o PII. Seguridad JWT/RBAC, aislamiento tenant A/B y revocación HTTP real NO_APLICA al diff de pruebas de artefactos; no se declara PASS de servidor ni se desactivan validadores. La evidencia no otorga autorización de producción.

## A3: integración ejecutable

GitHub-hosted con Node 24.21.0: npm ci, npm run lint, npm run build:prod, analizador existente y node --test del archivo nuevo sobre index/JS/CSS generados. Este flujo acredita integración real de bundler+analizador+reporte en filesystem del runner, no integración de MySQL, Cloudinary, SMTP o API externa. Playwright NO_APLICA porque no hay cambio runtime ni interacción nueva. Si faltan assets, o el reporte diverge, falla el job.

## A4, A5 y handoff

Exigir checks completed/success del SHA exacto (Scope Lock, procedencia V5, workflow dirigido, frontend y seguridad pertinente), readback PLAN/COLA/DAG/BITACORA, sin tratar skipped/queued/in_progress como PASS. Si está CI externa activa, mantener VALIDANDO con SHA, RUN y owner. A5 queda para otra Task Javier con revisión independiente de código, tests y evidencia. Rollback: git revert normal, jamás force-push; no MAIN/QA/PROD ni secretos.
