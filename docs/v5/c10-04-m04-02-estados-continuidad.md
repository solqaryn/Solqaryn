# SOLQARYN V17 C10-04.M04.02 — estados seguros de continuidad privada

**Autor:** PRIMARY DEV 03 JAVIER `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Riesgo V15:** R2 (permiso/UI privada). **Fuente:** Plan comercial V5 FINAL, PLAN_MAESTRO fila 1569, COLA fila 1569, DAG_TECNICO fila 1530. **Exclusividad:** candado V17 `SQV17_C10_04_M04_02` ID `2000001569`. **Repositorios:** únicamente `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## A0 — alcance real
Refuerza en componente Angular 22.2.1 la presentación de estados `vacio/cargando/exito/sin_permiso/error`. El acceso a contenidos estáticos queda condicionado al `PlataformaSesionService.sesion()` reactivo y vigente: revocación externa oculta de inmediato las cuatro guías. La revalidación solo presenta éxito si la emisión y señal local validada contienen `ambito='Plataforma'` y `esAdministrador===true`; una respuesta vacía no certifica acceso. `aria-busy` identifica carga. Conserva guard backend/route y read-only: no hay restauración, jobs, exportaciones, token grant, cambio de roles, MySQL ni cloud real. El código no modifica autorización backend.

## A1/A2 — pruebas negativas y seguridad proporcional
Vitest dirigido incorpora nuevos casos verificables: revocación reactiva, single-flight/carga con botón deshabilitado, emisión inválida, stream completado vacío, HTTP 401 sin filtración y respuesta positiva; preserva baseline 403/503 y ruta protegida. Chromium Playwright con token y API **sintéticos** verifica anónimo, rechazo 403 y flujo móvil permitido->503 con guía oculta y sin secretos. Mock de API no acredita JWT/RBAC/tenant A/B real; la autorización efectiva se ejecuta siempre en el backend existente, que no cambia en este alcance.

## A3 — CI dirigida REAL a exigir
Workflow GitHub-hosted `v17-c10-04-m04-02-estados.yml`: Node 24.21.0, npm 11.19.0, npm ci, Vitest del archivo real, lint, build Angular y Playwright Chromium en servidor localhost con mocks. Exigir logs de paso y números reales; `queued/in_progress/skipped/cancelled/failed` nunca se cuentan como PASS. `MySQL 8.4/EF/HTTP real` NO_APLICA porque no se modifica backend ni integración de datos; no afirmar cobertura backend.

## A4/A5 — publicación, riesgos y rollback
Commit individual fast-forward/expected SHA, diff del componente, pruebas y documento, CHANGELOG autocontenido, checks Scope Lock, V5 procedencia, calidad UI/Chromium dirigidos y readback PLAN/COLA/DAG/BITACORA. Si CI externa no termina, conservar `VALIDANDO` con owner, SHA y RUN. LISTO sólo tras A0–A4 realmente acreditados. A5 por otra de las cinco Tasks Javier, nunca esta autora. Sin QA/main/PROD, secretos, compras ni datos reales. Revertir mediante commit `git revert` normal en dev.
