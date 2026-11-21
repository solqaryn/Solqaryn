# C14-01.M05.05 — negaciones JWT/RBAC tenant A/B en proveedor privado V22

PLAN D2046: caso negativo de seguridad, aislamiento o abuso. Se añade UNA prueba Playwright de seguridad de aceptación bajo riesgo R2 en el flujo de branding privado existente, sin cambios de código productivo, permisos, migraciones ni datos reales.

## Prueba de integración real
El workflow V21 de Chromium ya ejecuta un backend ASP.NET Core 10 y una instancia MySQL 8.4 efímera con empresas A y B, JWT firmado y datos SQL preparados. La prueba hace `loginReal` de A, exige lectura permitida de A (control positivo) y verifica cinco denegaciones **contra HTTP backend real**, sin mocks: solicitud anónima, firma JWT modificada, JWT A sobre tenant B, header `X-Empresa-Id` B con ruta A, y enumeration de ID de tenant no miembro. Todos deben responder 401/403/404/503 sin filtrar marcas ni logos privados. Además, proveedor `/empresa-configuracion/publica` conserva cuarentena P0 HTTP 503 aun con JWT empresarial.

## Calidad
- A0: un único cambio de test en `frontend/e2e/c14-01-m04-03-real-branding.spec.ts`, documento y CHANGELOG aditivo. No modificar tenants, runtime ni endpoints.
- A1: control positivo A y seis caminos de denegación con asserts adversariales, no solo status verde por build.
- A2: firma JWT real, rechazo de A->B, selector cruzado, ID enumerado, anonimato y no exposición en cuerpos de error.
- A3: backend real + MySQL 8.4 efímero + Chromium GitHub-hosted sobre SHA exacto; esto no cubre todos roles o endpoints del ERP, SSL externo ni datos productivos.
- A4: `C14 branding JWT real MySQL y Chromium` exige casos 7/7 0FAIL (incluidos M04.03/M04.06/M05.03), check Scope Lock, V5, backend/frontend/seguridad pertinentes SUCCESS en el mismo SHA. `QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAIL` requeridos no son PASS.
- A5: revisión formal independiente por otra Task JAVIER distinta del autor y reparador después de LISTO.

Rollback por git revert normal, preservar FAIL histórico y BITACORA. No enviar cambios a QA/main/PROD ni usar secretos ajenos, cuentas reales o pagos.
