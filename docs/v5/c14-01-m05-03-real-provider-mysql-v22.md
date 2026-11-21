# C14-01.M05.03 — verificación real del proveedor de identidad sobre MySQL 8.4

PLAN D2044: ejecutar una integración real de persistencia/proveedor. Riesgo R2 por JWT, permisos y aislamiento multiempresa. Se añade UNA prueba Playwright en `frontend/e2e/c14-01-m04-03-real-branding.spec.ts` sin cambiar código productivo ni endpoints, DTO, middleware, base de datos desplegada o configuración de tenancy.

## Integración física y evidencia
El workflow existente `.github/workflows/v21-c14-01-m04-03-real-branding.yml` arranca en GitHub-hosted un contenedor **mysql:8.4 efímero**, inicializa API ASP.NET Core 10 y Angular, carga Empresas A/B con logos diferentes mediante SQL y emite JWT firmados de tres inicios de sesión de navegador aislados. El caso agregado consulta por HTTPS no; por HTTP loopback real `127.0.0.1:5005` (solo CI aislada), y compara nombre/logo/EmpresaId persistidos en el proveedor. Dos tokens sin membresía cruzada deben ser denegados, la identidad A persiste en sesión nueva después de leer B, `Cache-Control` exige no-store y la ruta anónima no da 200. No hay mocks ni `route.fulfill` en este nuevo escenario.

## A0–A4
- A0: diff de un test y documentación; la infraestructura de CI ya existente aporta backend, MySQL y Chromium. No se publica cambio runtime.
- A1: lecturas A y B positivas y relectura A tras nuevo JWT; assert de nombre, logo e ID sobre persistencia real, rechazos cruzados A/B y anónimo, frontera de no-store.
- A2: JWT firmados por login real, membresía A/B en servidor, denegación negativa y ausencia de exposición de otra empresa; pruebas de roles completos del ERP no quedan certificadas.
- A3: ASP.NET Core 10/EF/MySQL 8.4 **efímero real** + Chromium sobre el SHA del test; comprobar logs de 6 escenarios y 0 FAIL, no inferir esto de un build verde.
- A4: exigir `completed/success` de `C14 branding JWT real MySQL y Chromium`, exact-SHA Scope Lock, V5 y comprobaciones obligatorias relevantes. `SKIPPED/CANCELLED/QUEUED/IN_PROGRESS` no constituyen PASS requerido.
- A5: revisión genuina por otra Task JAVIER distinta de la autora, después de LISTO.

## Limitaciones y reversión
Prueba lectura del proveedor actual y SQL preparado por CI, **no** crea ni actualiza tenant en el ERP productivo, no prueba toda M05, configuración fiscal, roles externos, SLA, qa/main/PROD ni contratos con proveedores de terceros. Reversión mediante `git revert` FF sin alterar historial o BITACORA.
