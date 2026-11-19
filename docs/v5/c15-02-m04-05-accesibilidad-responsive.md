# C15-02.M04.05 — accesibilidad y responsive V20

Proyecto SOLQARYN; rama dev; PROJECT_SCOPE_LOCK=STRICT. Actor DEV01, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a.

## A0 Impacto
Regresión de la vista Angular documental privada C15-02.M04.01 existente, sin cambiar backend, API, DTO, JWT/RBAC, datos, migraciones, proveedores ni secretos. Playwright Chromium en GitHub-hosted con Angular efímero loopback. La UI no concede acceso backend.

## Pruebas y puertas
- A1: H1/H2/H3, lista de cinco tarjetas, ausencia de acciones mutantes; teclado Tab y Enter reales sin focus programático, indicador de foco.
- A2: HTTP 403 sintético no expone inventario o mensaje privado; simular sesión global no certifica JWT firmado, roles backend ni tenant A/B real.
- A3: 320/375/768/1280 píxeles sin desborde; objetivo táctil de al menos 44 px; reduced-motion. No significa auditoría WCAG integral.
- A4: exigir GitHub Actions SHA exacto SUCCESS en lint, build, Vitest, Chromium, Scope Lock y V5; SKIPPED, QUEUED, IN_PROGRESS y FAILED no son PASS. Releer PLAN/COLA/DAG/BITACORA.
- A5: revisión independiente por otra Task Javier, no por el autor ni el reparador.

Rollback: git revert normal del commit con verificación del HEAD y sin force-push. No QA, main o PROD.
