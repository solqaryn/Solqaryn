# C14-01.M05.04 — aceptación positiva funcional automatizada V22

PLAN D2045 exige un caso positivo de aceptación funcional y la orden adicional `SOLQARYN_OWNER_GLOBAL_QA_V1` exige automatización real por Task y A5 independiente, sin firma humana. Entrega atómica de **un caso** que cubre un recorrido positivo público→privado→consola global; NO equivale a certificación completa M05 ni a QA/PROD.

## Recorrido real del navegador
La CI ya disponible `v21-c14-01-m04-03-real-branding.yml` inicia API ASP.NET Core 10, MySQL 8.4 efímero con empresas A/B, Angular 22.2.1 y Playwright Chromium. El nuevo escenario comprueba página pública `/login`, autenticación JWT del admin de A, membresía A verificada por backend, `/perfil` privado con marca y logo persistidos A, refresh que vuelve a presentar marca A sin filtrar B, y **otra sesión de navegador independiente** con JWT de autoridad global válido que carga `/plataforma/auditoria-ux-v5` y muestra marca neutra SOLQARYN sin A/B. No `route.fulfill` ni respuesta falsa en esta prueba.

## A0–A5
- A0: diff de test+documentación+CHANGELOG aditivo, no runtime/SQL externo, ninguna compra ni secreto.
- A1: caso positivo con asserts de login, ruta/heading, logo/nombre correcto, recarga funcional y global console; negativos de no contaminación B y neutralidad global.
- A2: autoridad de servidor real para tenant A y global, JWT firmado, sin salto A/B o contaminación de marcas.
- A3: MySQL 8.4 físico efímero, HTTP ASP.NET Core 10, Angular y Chromium GitHub-hosted, evidencias del job exact-SHA; esto NO significa aprobación de todos los módulos, sectores C07–C09, UX móvil físico, QA/main/PROD.
- A4: exigir resultado `completed/success` en `C14 branding JWT real MySQL y Chromium` (8 escenarios tras este cambio, 0FAIL), Scope Lock exact-SHA, V5, backend/frontend/seguridad relevantes SUCCESS. SKIPPED/CANCELLED/IN_PROGRESS requeridos no cuentan.
- A5: revisión genuina independiente por otra Task JAVIER distinta de autor/reparador, después de cierre A0–A4.

Rollback mediante git revert FF preservando SHA e historial BITACORA. Prohibido cambiar PRODUCCION, QA/main o permisos.
