# C02-01.M04.05 — Verificación responsive, teclado, foco, labels y contraste (V20)

- Rama/alcance: solqaryn/Solqaryn@dev; sin QA/main/PROD. Riesgo R2 por límites de acceso y selección empresarial.
- A0: incremento limitado a cuatro pruebas Playwright de la superficie de acceso, archivo e2e nuevo, workflow dirigido y CHANGELOG. No modifica API, permisos, datos, cookies ni lógica de producción.
- A1: navegador real a 320/375/768/1280 comprueba anchura sin overflow, campos etiquetados; Tab natural (sin .focus) y Enter para mostrar contraseña; contraste del encabezado WCAG AA >= 4.5; prueba negativa de empresaId=999 en localStorage que no evita redirección a login.
- A2: la prueba negativa de navegador NO demuestra firma JWT, RBAC, tenant A/B, membresía revocada ni HTTP 401/403 real contra backend. Deben probarse por separado según alcance antes de certificar A0-A4 de toda la microtarea.
- A3: GitHub-hosted Chromium/Angular local efímero al ejecutar el workflow V20 dirigido. No se ejecuta MySQL ni integración con proveedor en este incremento y no se afirma validación de la autoridad server-side.
- A4: revisar check-runs del SHA causal, Scope Lock exact-SHA, V5 y workflow dirigido; queued/in_progress/skipped/cancelled/failure no son PASS. VALIDANDO hasta pruebas pertinentes completas.
- A5: otra Task Javier distinta de DEV06 debe revisar realmente el código, el alcance y las pruebas; sin autoaprobación.
- Rollback: revert normal del commit; no se borran registros ni se usa force-push.

## Reparación cruzada V21 — contraste efectivo, sin PASS sintético

El test original muestreaba únicamente el fondo CSS inmediato del encabezado, que puede ser transparente. La regresión ahora compone opacidad de todos los ancestros hasta el fondo del documento, calcula luminancia sRGB efectiva y exige contraste WCAG AA 4.5:1 tanto en el encabezado como en las etiquetas reales de usuario/contraseña, en 320 y 1280 px. La ejecución debe hacerse en Chromium real de GitHub Actions, sin mocks del DOM ni .focus() artificial. No altera runtime, API, seguridad ni permisos; por tanto no certifica host falso/JWT firmado/revocación/tenant A/B. Mantener VALIDANDO y exigir A0–A4 completos y revisión A5 independiente tras PASS causal. Rollback: revert normal del changeset.
