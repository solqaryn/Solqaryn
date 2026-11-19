# C03-04.M04.06 — Verificación browser de densidad y progreso (V20)

SOLQARYN@dev, PLAN_MAESTRO fila 565; candado V17 2000000565; autor DEV06. Riesgo R2 por guía privada de incidentes. Este changeset incorpora exclusivamente una suite Playwright, workflow de CI y documentación, sin modificar la aplicación, API, datos, JWT, permisos ni proveedores.

## A0, A1, A2, A3
Se prueban CSS real de densidad compacta, ausencia de desbordamiento a 320/768/1280, ocultación de decisiones durante GET de sesión retenido, error HTTP 503 adversarial sin diagnóstico interno ni mutaciones y métricas DOMContentLoaded/FCP locales 320/1280. Los cuatro escenarios usan Chromium real con respuestas HTTP simuladas expresamente: NO acreditan JWT firmado, tenant A/B, MySQL, recuperación de backup, RTO/RPO, finanzas ni seguridad de producción. Integración EF/MySQL NO_APLICA al diff test-only; la revisión R2 global puede exigir otras pruebas cuando corresponda.

## A4 y A5
GitHub-hosted ejecuta npm ci, lint, Angular build, Chromium, Scope Lock y V5 por SHA; QUEUED/IN_PROGRESS/FAILED no significa PASS. Conservar VALIDANDO hasta A0–A4 pertinentes, pruebas causales SUCCESS y readback. A5 independiente por Task Javier distinta de DEV06. Rollback mediante git revert normal; ninguna rama extra, sin QA/main/PROD.


## V20 — reparación tras A5_FAIL P2 independiente (19 noviembre 2026)

El revisor DEV03 detectó que la prueba 503 del SHA 44733378 solo escuchaba métodos no-G​​ET en /plataforma/sesion. Un POST/PUT/PATCH/DELETE a otro endpoint no incrementaría el contador y permitiría un falso PASS de la afirmación "sin escrituras". Se conserva el A5_FAIL P2 original y su bitácora.

La corrección usa un observador compartido de métodos POST/PUT/PATCH/DELETE sobre CUALQUIER URL del navegador. El test HTTP503 lo usa para rechazar toda solicitud mutante. Un quinto test adversarial ejecuta cuatro mutaciones simuladas en la ruta local /__v20_mutation_oracle_probe__ interceptada con route.fulfill, y exige que el MISMO observador registre todas; ninguna sale al backend. El código Angular, permisos, JWT, MySQL y API siguen intactos.

Se requiere nuevo commit fast-forward dev, CI GitHub-hosted exact-SHA con cinco Chromium SUCCESS, lint/build, Scope Lock y V5. No reutilizar A5_PASS previo ni autoaprobar: nueva A5 por una Task Javier distinta del autor DEV06 y reparador DEV03. No certificar pentest, tenant A/B, RTO/RPO ni producción.
