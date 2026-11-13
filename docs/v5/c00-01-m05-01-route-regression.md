# V5 FINAL — C00-01.M05.01: prueba de regla reproducible para contexto de ruta

## Resultado exigido y alcance
- Implementar y ejecutar una prueba unitaria o de regla con resultado reproducible. El componente afectado es el shell Angular de navegación; los consumidores son branding del storefront, tema CSS y SEO. Sin modificación de API, persistencia, JWT, RBAC, proveedores, secretos o migraciones.
- Riesgo observado: el callback de `StorefrontIdentidadService.cargar()` conserva actividad por `shareReplay({refCount:false})` y, si el usuario abandona la tienda antes de recibir datos, puede reponer tema o SEO del contexto anterior. Reviewer independiente de M04.03 documentó A5_FAIL para SHA fb20c55e; la corrección se requiere antes de reexaminar esa promoción.

## Corrección y aceptación
- `AppComponent.aplicarContextoRuta` incrementa una versión local por cada navegación. Antes de usar el bootstrap comprueba que la versión sea la misma; `ngOnDestroy` invalida respuestas pendientes.
- Prueba 1 negativa: ruta A /tienda → /login antes del bootstrap: no aplicar color ni SEO A; mantener no-index de plataforma.
- Prueba 2 positiva/negativa: ruta A /tienda → ruta B /tienda/productos, B responde primero, luego A: aplicar solamente tema/SEO B.
- Prueba 3 negativa: emisión HTTP después de destruir shell: sin efectos secundarios.
- La prueba usa navegación del `AppComponent` con eventos `NavigationEnd` reales de Angular y fuentes `Subject` RxJS controladas. Es integración dirigida del consumidor, no una afirmación de prueba HTTP/tenancy real en producción.

## Gates
- A0 diff exacto, linter/Typescript Angular, secretos y análisis estático proporcional.
- A1 pruebas dirigidas positivas/negativas en workflow `.github/workflows/priority4-quality.yml`, job de calidad, `npx ng test --watch=false --include=src/app/app.component.spec.ts`.
- A2 aislamiento A/B por ruta con respuestas tardías, no altera autenticación.
- A3 Angular `npm ci`, lint, build; regresión M10 Playwright si la ruta del workflow aplica; MySQL en M10 es únicamente contenedor desechable.
- A4 commit directo fast-forward en dev, Scope Lock SUCCESS y readback PLAN/COLA/BITACORA; A5 independiente posterior, también revisión propia de M04.03 por distinto TASK_ID.
- Rollback: `git revert <SHA>` tras revalidación de HEAD y dependencias, sin force push; prohibido promover a QA/main/PROD.
