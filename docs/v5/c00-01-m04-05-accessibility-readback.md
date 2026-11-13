# V5 FINAL C00-01.M04.05 — Accesibilidad real del acceso público

Autor: PRIMARY DEV 05 JAVIER; TASK_ID 6adc2e55f47c81919f6374e561687076.
Origen: Google Doc V5 FINAL, microtarea C00-01.M04.05. Rama exclusiva dev.

## Clasificación y alcance
Tipo 3 (pruebas frontend Angular 22 / Playwright) sin modificación ejecutable del producto.
La superficie cubierta es /login del componente Angular, existente y pública.
No se añaden rutas, API/DTO, permisos, JWT, MySQL schemas, claves, dependencias, migraciones ni producción.
Las comprobaciones no convierten un formulario rellenado en autenticación exitosa.

## Pruebas dirigidas reales
Se extiende el suite existente frontend/e2e/m10-ui-ux-accesibilidad.spec.ts y su workflow
.github/workflows/m10-ui-ux-accesibilidad.yml (push dev, Playwright/Chromium con
frontend Angular, API de .NET 10 y MySQL 8.4 efímeros). El caso:
- inspecciona login anónimo a 320px y el formulario con nombre semántico;
- acredita etiquetas de usuario/contraseña y foco real sucesivo por teclado;
- verifica mostrar/ocultar contraseña mediante Enter y aria-pressed;
- caso negativo: submit deshabilitado sin contraseña; positivo: habilitado con campos válidos;
- valida ausencia de sidebar privado en login anónimo, ancho del documento sin overflow
  y contraste mínimo WCAG AA de variables text/surface;
- nunca envía credenciales ni afirma acceso de backend. Son pruebas DOM/browser, no mocks
  ni verificación de tenancy HTTP A/B.

## Auditores y riesgos
A0: diff acotado, calidad TypeScript, no secretos/PII, sin dependencias nuevas.
A1: positivo/negativo de formulario, DOM y eventos keyboard mediante Playwright real.
A2: sin cambio a RBAC/JWT; ausencia de menú privado sin sesión es señal UI solamente,
     no sustituye suites de seguridad del backend.
A3: frontend npm lint/build y E2E M10 real sobre SHA de commit, no contar SKIPPED.
A4: GitHub SHA exacto, Scope Lock, logs E2E y readback PLAN/COLA/BITACORA.
A5: otro TASK_ID revisor después del cierre DEV; QA/main/PROD sin autorización.
Rollback: git revert normal tras revalidación, nunca reset ni force-push.
