# C06-01.M06.04 — Ensayo de rollback reversible (DEV / V23)

**Acción exacta:** PLAN_MAESTRO!D899 «Ensayar rollback o desactivación sin borrar datos legítimos». PLAN!E899 es una meta acumulativa M06: esta microtarea no certifica la fase entera, ni habilita QA, main o PROD. Repositorio exclusivo solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT. **Riesgo R2** por introducir un nuevo workflow de CI, aunque no se cambia el runtime.

## Entrega reproducible

Los artefactos son scripts/quality/c06-01-m06-04-rollback-drill.mjs, .github/workflows/v23-c06-01-m06-04-rollback-drill.yml y este documento. El runner GitHub-hosted usa Node 22, checkout completo y permisos GitHub **contents: read**, sin credenciales persistentes.

El ensayo usa el commit auténtico de la ruta pública C06-01.M04.01, **9b90aa0df48d155545351a35ca825bd096c4a761** (padre **17a18521773539138b884cf115b0b71788225822**). Crea un worktree Git temporal detached en el runner, verifica que el commit original sólo modificó cinco rutas autorizadas y ejecuta **git revert --no-commit** exclusivamente dentro de ese worktree; no hace push, no crea ramas ni commits y no ejecuta reversión en dev live.

Compara el índice del worktree revertido con el contenido exacto anterior de frontend/src/app/app.routes.ts, acredita la ausencia local del componente añadido por M04.01 y exige que sólo se hayan revertido las cinco rutas originales (workflow, documento, spec Playwright, app.routes.ts y componente landing). Un fixture **sintético**, no datos de empresa, conserva SHA-256 idéntico antes y después. El HEAD y los archivos del checkout primario deben mantenerse intactos. El worktree de ensayo se elimina al terminar. Este resultado no simula un rollback real de datos de clientes.

## Pruebas A0–A5 y riesgos

**A0:** diff limitado a un script, un workflow de CI aislado y este documento; ninguna API, DTO, Angular de producción, tabla/migración o política JWT cambia. **A1:** pruebas positivas de una reversión real de Git; cinco negativas de refs hostiles, revisión exacta del diff y datos fuera de alcance. **A2:** worktree detached con permisos de lectura, sin redes de producción, sin secretos ni clientes autenticados; no se certifica JWT/RBAC ni tenant A/B. **A3:** herramienta Git real ejecutada en runner GitHub-hosted; HTTP, MySQL y Chromium **NO_APLICA a este diff de ensayo**, no al alcance global de SOLQARYN. **A4:** el check dirigido y Scope Lock/V5 deben finalizar completed/success en el SHA publicado; SKIPPED/CANCELLED/FAIL/IN_PROGRESS no son PASS. PLAN/COLA/BITACORA requieren readback. **A5:** revisor independiente de las otras cinco Tasks JAVIER, distinto de DEV05 autora/reparadora.

Reproducción en un checkout limpio y completo: ejecutar node scripts/quality/c06-01-m06-04-rollback-drill.mjs --self-test y después node scripts/quality/c06-01-m06-04-rollback-drill.mjs. Fallos de guardas, diff, revert, árbol, permisos o conservación impiden LISTO. Rollback del propio changeset: git revert normal del commit, nunca reset ni force-push.

**Limitación de fase:** UI-03 P1 sigue parcial; el ejercicio no modifica ni certifica la raíz pública, visual premium, Core Web Vitals de campo, JWT firmado, persistencia de leads, backup/restauración MySQL real ni despliegues. Un fixture de datos sintéticos no sustituye a datos legítimos reales. V23: SKREF-03/05/10 no aplican por ausencia de cambios UI/Angular; SKREF-04 no hay motion, SKREF-06 no hay copy público, SKREF-11 no se solicita threat model, SKREF-12 no hay profiling de .NET. Sólo se usa governance canónica y el registro de doce referencias; no se instalan skills.
