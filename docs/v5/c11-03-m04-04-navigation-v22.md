# C11-03.M04.04 - pruebas reales de navegacion, mensajes y doble accion (V22)

## A0 y riesgo

Accion PLAN_MAESTRO!D1679: verificar formularios, navegacion, mensajes y prevencion de doble accion de la vista privada de observabilidad outbox. Test-only, R2 por permisos, aunque no cambia codigo de runtime. La vista es estatica y **no contiene formularios**, por lo que el envio y validacion de formularios son NO_APLICA a esta unidad; se certifica que no aparecen formularios/botones mutadores. No equivale a completar la fase M04 o a integrar el relay.

## A1-A4 causales

- En GitHub-hosted Ubuntu/Node24/npm11, usar Playwright Chromium real, Angular 22.2.1, Vitest4 de guard/roles, npm ci, lint/build:prod, Scope Lock antes de dependencias.
- Negativo: ruta anonima, HTTP 403 sintético y respuesta de traza no exponen panel ni mensajes internos.
- Positivo limitado al navegador: usuario de plataforma sintético ve aviso SIN_FUENTE_CONECTADA y enlace accesible de retorno. Dos ciclos independientes de acceso/salida requieren navegacion sin ninguna peticion mutadora, sin controles de envio. La ausencia de accion evita doble escritura, pero **no prueba** idempotencia del backend.
- Negativo: sesion con ambito Empresa y admin falso nunca ve panel ni nombre sensible. El frontend no confiere permiso del servidor.
- El token es falso, no JWT firmado. HTTP, MySQL, tenant B/A real, outbox persistente y proveedores **NO_APLICA** para este diff exclusivamente de pruebas; quedan **NO_CERTIFICADOS** para fase/producto. No afirmar mocks como datos reales.
- A4 requiere GitHub Actions directed 3 Chromium/4 Vitest, Scope Lock, V5, P0 Catalog, Angular build y checks pertinentes exact-SHA COMPLETED SUCCESS, no queued/cancelled/skipped/failed.
- A5 independiente por otra Task Javier distinta de DEV01 y de cualquier reparador, con revision efectiva de pruebas, rutas, riesgo y log.

## Rollback

Git revert regular en dev del changeset de pruebas y workflow; preservar BITACORA. No QA, main, PROD, secretos ni payments.
