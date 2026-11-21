# C04-01.M05.01 — reglas unitarias del catálogo de planes (V22)

**PLAN D602** ordena una prueba unitaria o de regla con resultado reproducible. Este cambio sólo crea seis casos Angular TestBed/Vitest, workflow GitHub-hosted SHA específico y esta evidencia; NO implementa tarifario, precios, límites comerciales server-side, pagos, proveedor, contratos API, MySQL ni cambios de runtime. PLAN E/M05 es meta **acumulativa**, no criterio para afirmar que una prueba de la unidad .01 completó toda la fase.

## A0 y riesgo

R2 conservador: SaaS billing aunque el diff sea sólo tests. Fuente existente `C0401PlanesReadonlyComponent` es una referencia privada no comercial: tres secciones congeladas, `NO_CERTIFICADO`, sin formularios de cobro. Conserva la ruta lazy guardada con `plataformaGuard` y la autorización global del servicio de sesión. No exporta contratos comerciales ni transforma atributos inyectados en cobros.

## A1 / A2

Seis unitarios reales contra el componente Angular: (1) lista y objetos `Object.freeze` con intentos de adulteración que deben fallar; (2) autoridad global permite **sólo** vista informativa, sin formularios/cobros ni llamadas al proveedor; (3) rechazos anónimo, ámbito Empresa y falso administrador aun con campos falsos de precio/upgrade; (4) revocación reactiva posterior de la sesión; (5) sesión hostil con texto HTML y precio falsos sin XSS ni activación comercial; (6) guard de ruta lazy exacto sin grants arbitrarios. Son asserts positivos, negativos, de frontera y error para detectar regresiones reales.

## A3 / A4 y límites

A3 corresponde a Angular real + TestBed/Vitest en GitHub-hosted, no a transporte HTTP ni a base de datos. `PlataformaSesionService` se sustituye **sólo en el test** por una sesión señalizada; no valida JWT firmado, RBAC backend, tenant A/B persistente, MySQL 8.4, PSP, créditos ni políticas de límites server-side. Cada integración faltante deberá probarse en sus scopes M05.02–06. Esto queda explícitamente como `NO_APLICA` para el diff test-only y como `LIMITACION` de la fase comercial.

A4 exige seis casos reales ejecutados con cero fallos/skip, tipado Angular, lint, build, Scope Lock/V5 y checks pertinentes `completed/success` del SHA exacto; `in_progress`, `skipped`, `cancelled` y `failure` no son PASS. Pasar de EN_PROGRESO a VALIDANDO con CI externa corriendo no equivale a LISTO; A5 será otra Task Javier distinta de DEV01. Registro CAS V17, BITACORA append-only y readback obligatorios.

Rollback: revert Git normal sobre `dev`; sin force-push. `CHANGELOG_AI.md` no se reemplaza al superar límites de lectura del conector para no truncar historia; este documento y BITACORA conservan la procedencia. Prohibidos QA/main/PROD, pagos reales y secretos.

## Reparación causal de CI del 21 de noviembre de 2026

El job GitHub-hosted `136798821917` del SHA `f8a10ddabdb9577be972e27766959af7752c725e` falló en el compilador **TS2322**, antes de ejecutar Vitest: el contrato TypeScript `PlataformaSesion` restringe `ambito` a `Plataforma` y `esAdministrador` a `true`, mientras las pruebas intentan alimentar valores hostiles `Empresa`/`false`. Se corrigen **sólo las dos asignaciones adversariales** con casteo explícito `as unknown as PlataformaSesion` en la frontera del test; no se relaja el modelo de tipos de producción ni se elimina una sola aserción. Es necesario simular información desautorizada recibida en runtime aunque el tipo nominal sea restrictivo. Nueva CI exact-SHA debe demostrar seis casos ejecutados, sin fallo ni omisiones, Angular, Scope Lock y V5; un build verde sin tests no acredita A1. Histórico fallo conservado y A5 independiente pendiente.
