# C03-04.M04.06 — Verificación browser de densidad y progreso (V20)

SOLQARYN@dev, PLAN_MAESTRO fila 565; candado V17 2000000565; autor DEV06. Riesgo R2 por guía privada de incidentes. Este changeset incorpora exclusivamente una suite Playwright, workflow de CI y documentación, sin modificar la aplicación, API, datos, JWT, permisos ni proveedores.

## A0, A1, A2, A3
Se prueban CSS real de densidad compacta, ausencia de desbordamiento a 320/768/1280, ocultación de decisiones durante GET de sesión retenido, error HTTP 503 adversarial sin diagnóstico interno ni mutaciones y métricas DOMContentLoaded/FCP locales 320/1280. Los cuatro escenarios usan Chromium real con respuestas HTTP simuladas expresamente: NO acreditan JWT firmado, tenant A/B, MySQL, recuperación de backup, RTO/RPO, finanzas ni seguridad de producción. Integración EF/MySQL NO_APLICA al diff test-only; la revisión R2 global puede exigir otras pruebas cuando corresponda.

## A4 y A5
GitHub-hosted ejecuta npm ci, lint, Angular build, Chromium, Scope Lock y V5 por SHA; QUEUED/IN_PROGRESS/FAILED no significa PASS. Conservar VALIDANDO hasta A0–A4 pertinentes, pruebas causales SUCCESS y readback. A5 independiente por Task Javier distinta de DEV06. Rollback mediante git revert normal; ninguna rama extra, sin QA/main/PROD.
