# SOLQARYN V17 — C14-02.M04.03 presentación local segura

Autor: DEV03 Task 6adc2e4bd8a48191ac7d8641ed95a1a3. Plan fila 2074, candado V17 2000002074. Riesgo R2 por UI de aceptación de seguridad.

## A0 — alcance y dependencias
Se añaden dos presentaciones locales tipadas (institucional y neutra) al componente privado de C14-02.M04.01/M04.02. No cambia rutas, guard, HTTP, JWT, RBAC backend, tenant, credenciales, datos, políticas ni proveedores. No admite URL, HTML, imagen, texto o paleta arbitrarios, ni persistencia que se confunda con configuración comercial. Señal local, valores literales y clases CSS predefinidas. La pantalla continúa siendo guía de consulta sin pentest ni datos en vivo.

## A1 — pruebas funcionales
Vitest dirigido incluye positivo de alternancia sin consultas, negativo de valor arbitrario, frontera de revocación y contexto Empresa; preserva nueve tests anteriores de acceso, estados, 403/503 y single-flight. Playwright/Chromium sintético comprueba controles accesibles, aria-pressed, cambio de clase, token estable y contenido sólo lectura; preserva cinco escenarios anteriores. Requiere CI GitHub-hosted real en SHA publicado; diseño de pruebas no equivale a PASS.

## A2 — controles de seguridad
La elección visual está oculta sin acceso y su setter vuelve a valor institucional para un actor no autorizado; no modifica la autoridad de servidor. Los mocks de sesión no certifican JWT real, aislamiento HTTP tenant A/B, pentest ni proveedores productivos. Denegación 401/403 y revocación se mantienen según M04.02.

## A3 — integración pertinente
Build y test Angular, prueba de navegador Playwright Chromium en GitHub-hosted. MySQL 8.4/EF/migraciones, .NET runtime, proveedores reales: NO_APLICA, pues no son alterados por este diff. Los límites de pruebas sintéticas quedan expresos.

## A4/A5 y rollback
Exigir Scope Lock STRICT exact SHA, V5 provenance, frontend y workflow de 12 Vitest/6 Chromium completed/success, readbacks PLAN/COLA/BITACORA y A5 independiente de otra Task Javier. No aprobar por queued/in_progress/skipped/cancelled. Rollback: git revert fast-forward sobre dev, nunca force push. No tocar QA/main/PROD.
