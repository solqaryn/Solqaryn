# SOLQARYN — C12-04.M05.04 · Aceptación positiva de cutover privado (V19)

- Microtarea C12-04.M05.04, fase M05; R2 por superficie MySQL, solo dev y PROJECT_SCOPE_LOCK=STRICT.
- A0: aceptación de la interfaz Angular protegida de lectura; no aceptación de exportación, restauración ni cutover real. Sin cambios en endpoints, persistencia ni autorización del servidor.
- A1: cinco escenarios positivos/adversariales Chromium: cuatro precondiciones y cero escrituras; GET revalidación único y éxito sin certificar cutover; Tab/Enter real; tema local y cuatro anchuras; 503 opaco.
- A2: sesión sintetizada mediante HTTP local /plataforma/sesion; no prueba JWT, RBAC real, tenant A/B servidor ni secretos de producción.
- A3: Angular/Chromium GitHub-hosted con npm ci, Vitest dirigido, lint y build. MySQL 8.4/EF, restore, exportación y proveedor externo no aplican a changeset test-only; no se les atribuye PASS.
- A4: requieren checks SHA completed/success, Scope Lock/V5, readback PLAN/COLA/BITACORA y liberación V17; queued/in_progress/skipped no son PASS.
- A5: revisor independiente entre las cinco Tasks Javier distintas de autor. LISTO_DEV no autoriza QA/main/PROD ni cutover.
- Rollback: git revert ordinario; sin force-push.
