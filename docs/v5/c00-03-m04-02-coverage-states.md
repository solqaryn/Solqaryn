# C00-03.M04.02 — Estados de cobertura y permisos de plataforma

**Alcance:** ruta existente `/plataforma/cobertura-v5`, protegida por `plataformaGuard`. La autoridad sólo procede de `GET /plataforma/sesion` verificado por el servidor; una respuesta sintética de E2E no acredita JWT/RBAC/MySQL real.

## Contrato de experiencia
- **Carga:** al seleccionar «Revalidar sesión», indicador y mensaje de estado accesible con `role=status`, acción deshabilitada para evitar peticiones repetidas. Durante la espera no se presenta una denegación prematura.
- **Éxito:** autorización server-side válida para consultar; mensaje explícito de **solo lectura**, sin representar que exista un repositorio live de cobertura.
- **Vacío:** una fila semántica con `colspan=5` declara que no existen datos institucionales de cobertura verificados en esta vista. No se fabrican ID, evidencias, SHA, estados ni cuotas.
- **Error:** respuesta 403, corte de red o sesión inválida muestra texto genérico sin detalles del servidor ni datos privados; la identidad verificada desaparece.
- **Permisos:** ausencia de token y rechazo 403 bloquean desde el guard ya existente; el frontend no puede conceder grants ni hacer cambios sobre PLAN_MAESTRO, COLA o BITACORA.

## Pruebas y evidencia
`frontend/e2e/v5-plataforma-browser-guards.spec.ts` añade casos Chromium de vacío honesto y estado de carga / rechazo 403 sin fuga. El workflow `v5-plataforma-browser-guards.yml` en push a `dev` instala Node 24, npm 11, compila Angular 22.2.1, ejecuta Playwright y se vincula al SHA exacto. Controles A0 estáticos y revisión de secretos antes del commit; A1 funcional y A2 negativas de interfaz, A3 build/browser, A4 exact-SHA/readback. A5 por otra Task Javier.

**No aplica:** .NET, MySQL, Docker y E2E con base productiva, pues no se cambian API, datos, grants o persistencia. **Riesgo residual:** acceso real multitenant debe verificarse en un scope backend separado y nunca se infiere de mocks.

**Reversión:** commit inverso no destructivo sobre `dev`; no force-push ni QA/main/PROD.
