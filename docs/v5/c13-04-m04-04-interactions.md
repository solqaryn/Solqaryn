# C13-04.M04.04 — controles de navegación y doble acción entre vistas

SOLQARYN V5; `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT; autora Task DEV01 `6adbb9fff1548191a1bd5b30d9e9984a`; PLAN fila 2003. Riesgo R2 por pruebas de autenticación sintética/UX, sin cambio runtime. Predecesores y COLA/DAG deben validarse en claim exclusivo V17.

**A0:** trabajo de pruebas para dos vistas protegidas EXISTENTES: consola global y cobertura; no crea formularios ni altera JWT, roles, tenants, aplicación o datos. Cuatro archivos: e2e test, workflow SHA-aislado, documento y CHANGELOG.

**A1:** cuatro tests Playwright reales: (1) anónimo y 403 opaco sin tarjetas/PII; (2) 503 opaco con recuperación solo GET y 2 llamadas; (3) mientras GET en curso aria-busy/disabled impide doble acción y un solo GET; (4) Tab desde documento inicial, Enter en enlace con nombre accesible retorna a plataforma, sin formularios ni POST/PATCH/DELETE. Negativos, error y frontera causal son falsables.

**A2:** sesión local y respuesta HTTP simuladas por `page.route`; no JWT middleware, MFA, RBAC real, tenant A/B, grant, proveedor o datos de empresas. **A3:** navegador Chromium real sobre Angular 22.2.1 compilado y Node 24; CI GitHub-hosted, MySQL/EF/HTTP real NO_APLICA al cambio test-only (y no se certifican). Los fixtures no demuestran seguridad de servidor.

**A4:** exigir Scope Lock STRICT, V5, frontend y CI dirigida real `completed/success` en SHA o descendiente con blobs idénticos; no contar SKIPPED/QUEUED/FAIL como PASS. PLAN/COLA/DAG y BITACORA readback por transición bajo V17. **A5** independiente por una Task Javier distinta de autora; nunca autoaprobación, QA/main/PROD. Rollback `git revert` sin force-push ni borrar historia.
