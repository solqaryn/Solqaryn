# C08-04.M05.04 - aceptacion positiva real en Chromium

Autor: PRIMARY DEV 04, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. PLAN fila 1289, V17 lock 2000001289; rama dev con PROJECT_SCOPE_LOCK STRICT.

## A0 — alcance y rollback
Riesgo R2 por CI e interaccion navegador; nueva prueba Playwright/Chromium, workflow con concurrencia SHA, documento y CHANGELOG. No se modifica codigo producto, backend/API/DTO, tenant/JWT/RBAC/EF/MySQL ni informacion productiva. Reversibilidad con git revert normal.

## A1 — positivo, negativo y frontera
Prueba de aceptacion desde una sesion sintética permitida: cuatro capacidades exactamente (Reservas, Membresias, Catalogo, Sitios por rubro), todas etiquetadas No conectado, rotulos que no conceden autorizacion, ausencia de formularios/activaciones y cero HTTP mutaciones o llamadas a API de modulos. El navegador renderiza a 320/1280 sin desbordamiento. Caso negativo con tenant-context 403 sintetico redirige a login, no revela tarjetas ni diagnostico privado.

## A2/A3 — alcance de evidencia
Playwright se ejecuta de verdad sobre Chromium y servidor de Angular lanzado en GitHub-hosted Actions; tenant-context es completamente simulado. No se certifica autenticacion JWT, RBAC ni aislamiento multiempresa A/B reales. No se toca MySQL/EF ni proveedores reales; esas integraciones son NO_APLICA motivado para este diff de prueba UI. LCP/CLS reales NO_MEDIDO.

## A4/A5 — entrega y gates
Para LISTO: checks GitHub Actions exact SHA completados SUCCESS, incluyendo Scope Lock V5, lint/build, Chromium y verificaciones transversales aplicables; readback institucional y sin P0/P1. Mientras CI QUEUED/IN_PROGRESS se conserva VALIDANDO, owner y run/SHA, sin PASS ficticio. Revision A5 por otra Task Javier, nunca el autor. QA/main/PROD permanecen prohibidos.
