# C11-03.M04.05 - QA Chromium de accesibilidad outbox (V22)

## Alcance A0 / riesgo R2 preventivo

PLAN_MAESTRO!D1680 exige verificar responsive, teclado, foco, labels y contraste de la superficie privada plataforma/eventos-outbox-v22. No certifica toda M04, el relay outbox, API, JWT firmado ni persistencia real. No toca proveedores ni datos productivos. La UI de M04.01 muestra SIN_FUENTE_CONECTADA.

## Pruebas causales GitHub-hosted

- Playwright/Chromium en GitHub Actions, Angular 22.2.1, Node 24, npm 11.19.0, npm ci, lint, build de producción y Scope Lock antes de npm ci.
- Negativa: navegación anónima y 403 simulado no exponen panel ni mensaje sensible.
- Positiva sintética: cabeceras y nombres accesibles; tres etapas, cero formularios o controles mutadores; sin solicitudes no-GET; ancho 320, 375, 768 y 1280 sin scroll horizontal.
- Teclado desde inicio: Tab real y foco visual medido sin element.focus().
- Contraste medido en navegador, colores computados sobre fondos compuestos y umbral 4.5:1 para cinco superficies textuales; no sólo snapshot.
- Pruebas adversariales deben fallar si desaparece enlace de teclado, foco visible, contraste o privacidad; ninguna prueba omitida ni cancelada cuenta como PASS.

## Límite A2/A3 y cierre

El token de browser es sintético, no JWT válido. El endpoint de sesión se intercepta. JWT/RBAC HTTP auténtico, MySQL/EF, entrega outbox y rollback de eventos son NO_APLICA para este changeset test-only de accesibilidad; NO CERTIFICADOS para el producto o fase. A0-A4 exigen CI Chromium, lint, build, Scope Lock, V5 y checks pertinentes COMPLETED SUCCESS del SHA publicado antes de VALIDANDO a LISTO. A5 por otra Task Javier distinta de DEV01 y del reparador. Si CI sigue ejecutándose, conservar VALIDANDO y SHA/RUN; no fabricar PASS.

## Rollback

Git revert normal del commit de tests y workflow en dev, sin reset, force-push, QA/main/PROD ni alterar BITACORA histórica.
