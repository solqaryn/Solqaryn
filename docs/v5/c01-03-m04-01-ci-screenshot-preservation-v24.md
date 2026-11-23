# C01-03.M04.01 — evidencia visual de CI V24
22 de noviembre de 2026. Riesgo R2 CI/UX. Scope Lock STRICT, rama dev, Task autora DEV03.

## Causa y reproducción
El artefacto real de GitHub-hosted Actions 14776156811 del run 45077510405 no contenía PNG; solo backend/c1504-api.log, frontend/c1504-angular.log y frontend/test-results/c1504-real-jwt.xml. La primera suite Playwright crea 2 tenants × 5 anchos × 2 zooms; la siguiente invocación Playwright limpia test-results antes del upload-artifact.

## Solución concreta
Entre ambas suites, exigir que existan exactamente los 20 PNG de Chromium (identidades CI sintéticas A/B, 320/390/768/1280/1440, zoom100 y zoom200-pinch), comprobar que cada PNG no está vacío y que su MIME es image/png, y copiarlo a frontend/v24-c0103-m0401-screenshots. Generar SHA256SUMS y agregar dicha carpeta al upload-artifact. El fallo ante archivo ausente/corrupto es deliberado: no inventar capturas ni degradar el control.

## Alcance de seguridad y calidad
A0: el cambio modifica solo workflow CI, doc y changelog; no toca Angular, JWT, RBAC, tenants, backend ni MySQL. A1: positivo exige los 20 PNG; negativo falta/corrupción debe producir FAIL. A2: capturas únicamente de usuarios efímeros de la CI, no se incluyen secretos ni datos productivos. A3: ejecución física de .NET 10/MySQL 8.4/Angular/Chromium se corrobora por GHA y artefactos reales, no por mocks. A4: pendiente SUCCESS exact-SHA Scope Lock/V5/frontend/backend/security y revisión de 20 PNG descargados. A5: independiente de autor DEV03 y reparadores.

La captura CDP pinch 200 es zoom visual, NO zoom de página de escritorio 200% ni evidencia histórica antes/después. Esas brechas siguen abiertas y se deben registrar sin atribuirles PASS. Mantener H=VALIDANDO, I=DEV03 y no modificar DAG.
SKREF-03 Impeccable: pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199. SKREF-10 Angular: angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9. Solo leídos para criterio UI desde orígenes y pins; 04/05/06/11/12 NO_APLICA al empaquetado de CI. No instalar scripts externos.

Rollback: git revert del SHA nuevo, con V17, HEAD y readback; conservar BITACORA. Nunca QA/main/PROD.
