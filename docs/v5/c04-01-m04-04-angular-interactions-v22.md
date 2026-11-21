# C04-01.M04.04 — formularios, navegación, errores y doble acción (V22)

PLAN_MAESTRO D599 exige verificar formularios, navegación, mensajes y prevención de doble acción. Este incremento R2 es exclusivamente una suite E2E de Angular 22.2.1 bajo Chromium real y una CI GitHub-hosted dirigida; no cambia implementación comercial, autorización backend, API, esquema de MySQL ni procesadores de pago.

La suite contiene cuatro comprobaciones adversariales: (1) filtro GET, resultado vacío y permanencia de ruta; (2) 503 de consulta y reintento sin exposición de plan antes de la respuesta; (3) rechazo por tenant/rol y ausencia de datos privados; (4) onboarding con backend sintético retardado, botón deshabilitado durante espera, única escritura simulada y encabezado Idempotency-Key. Se usa sesión sintética *no firmada* y respuestas de API interceptadas; no afirmar JWT real, SQL ni dinero real. El navegador sí es Chromium auténtico operando Angular servido por localhost en GitHub-hosted.

A0 diff test-only y ausencia de cambios de runtime. A1–A3 esperar PASS causal de Chromium 4/4 y TypeScript/build, registrar negativas y condiciones frontera. A4 exigir Scope Lock, V5/procedencia y controles pertinentes completos SUCCESS del SHA publicado; nunca contar SKIPPED/CANCELLED/IN_PROGRESS. A5 corresponde a otra Task Javier. Mantener alcance dev exclusivamente, rollback normal mediante revert y no liberar QA/main/PROD por esta prueba.

## Reparación causal CI V22 (SHA 01b8600c)

El job GitHub-hosted 136778107811 ejecutó 4 Chromium: 2 PASS, 2 FAIL, sin declarar éxito. F1: la primera respuesta 503 se recuperó por un segundo GET concurrente en inicialización, haciendo desaparecer la alerta antes de la aserción; el mock ahora mantiene 503 mientras el test no habilite explícitamente el reintento. F2: con permiso Configuracion:Ver revocado el guard navegó a /perfil, una ruta privada distinta pero segura, y no a /login; el test exige ambas salidas permitidas fuera de /configuracion y cero revelación del plan. Ninguna aserción sobre rechazo tenant/rol se elimina. El error anterior permanece en GitHub Actions; prueba nueva exact-SHA obligatoria. No se modifica seguridad, backend ni runtime.
