# C01-03.M04.05 — responsive, teclado, foco, labels y contraste (DEV)

## A0, ámbito, dependencias y riesgo
PLAN_MAESTRO!D240 manda verificar responsive, teclado, foco, labels y contraste. Se prueba la ruta real privada /configuracion/dinamica-v5 entregada en M04.01, con guards y componente tenant existente. Riesgo R2 por pruebas real JWT/RBAC/A-B/MySQL/Chromium; ningún cambio de backend, frontend runtime ni base de datos. PLAN E es meta acumulativa, no certificación de toda M04. Rollback normal git revert FF/CAS.

## A1–A3 y evidencia
Nueva suite frontend/e2e/c01-03-m04-05-responsive-accessibility.spec.ts para: redirección sin sesión, dos sesiones firmadas A y B, labels reales, Tab real sin .focus(), cinco anchos 320/390/768/1280/1440, ausencia de body overflow o recorte de región principal, capturas posteriores, denegación cruzada mediante header/localStorage, JWT alterado 401. CSS zoom=2 en 768/1280/1440 es PROXY de estrés de reflow, NO prueba zoom NATIVO 200% del navegador. Contraste WCAG numérico y zoom nativo 200%, scroll local de modales inexistentes y captura antes de una ruta que aún no existía quedan LIMITACIONES, NO_PASS. Los tests reportan sus verdaderos errores, sin preaprobar accesibilidad.

Workflow GitHub-hosted aislado usa MySQL 8.4 efímero, API ASP.NET Core 10 real, JWT firmados de prueba, usuarios A/B y Angular 22.2.1 con Playwright Chromium; sin datos, secretos ni cobros productivos. CI SKIPPED, CANCELLED, FAIL, QUEUED, IN_PROGRESS no son SUCCESS.

## A4, A5 y exclusividad
Exigir GHA dirigida exact SHA, Scope Lock, V5, frontend/backend, JWT seguridad, y evidencias/capturas examinadas antes de LISTO. H=VALIDANDO hasta pruebas pertinentes realmente completas. A5 requiere otra Task Javier independiente del autor y reparador bajo candado V17. No QA/main/PROD; no cambios de fórmulas ni del owner histórico.

## Referencias V23 verificadas
Gobierno local comparado con canónica dev. SKREF-03 Impeccable pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199 y SKREF-10 Angular angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9, SKILL.md originales leídos en sus pins. Operate, reutilización, semántica/foco y responsive pertinentes. SKREF-04/05/06/11/12 NO_APLICA: sin motion nuevo, landing, nueva copy, modelo formal de amenaza ni perf .NET medida. Sin instalar/copiar scripts externos.
