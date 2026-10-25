# P0-006.B — Verificación negativa de rutas privadas y precedencia (DEV)

**Proyecto y gobierno:** SOLQARYN; `PROJECT_SCOPE_LOCK=STRICT`; `ACTIVE_DEV`. **Microtarea:** P0-006.B, prioridad P0, Plan Comercial Inicial Unificado. **Ejecutor:** `PRIMARY_DEV_01` / `JAVIER_CHATGPT` / Task `6adbb9fff1548191a1bd5b30d9e9984a` / GitHub `jmejia31`. **Base causal:** `dev@bafcde53ce2562f70d5b4115184df51e5c48483d` (P0-006.A aprobado en PR #3751). **Rama:** `work/solqaryn-p0-006-b`.

## Objetivo y alcance

Certificar la **regresión estática negativa** frente a un matcher público genérico `/:slug` que intercepte rutas administrativas. El control es conservador: verifica declaraciones de router, orden de proveedores, guardas de autenticación y capacidades en código, y que mutaciones peligrosas sean rechazadas. **No presenta una prueba estática como una certificación E2E, aislamiento backend, ni despliegue READY.**

Dependencias verificadas en `COLA`: `P0-006.A=LISTO`, `P0-012.B=LISTO` y `DEPENDENCIAS_OK=SI`. Esta acta reutiliza el inventario de 92 rutas y 39 enlaces de P0-006.A, pero agrega un validador regresivo autónomo en `scripts/verify-p0-006-b-route-guards.mjs`.

## Evidencia dirigida y caso negativo

Fuentes leídas **en el SHA base**, sin tocar el router ni credenciales:

- `frontend/src/app/app.routes.ts`: 92 declaraciones `path`, 90 `loadComponent`, dos `redirectTo`; la entrada pública de tienda es la ruta prefijada `tiendas/:storeKey`, no un `:slug` en raíz. Los slugs de productos/categorías permanecen bajo el prefijo `tienda/`.
- `frontend/src/app/app.config.ts`: `provideRouter` expande grupos declarados antes de `...routes` y conserva la prioridad expresa del router. Los grupos dinámicos pueden tener rutas duplicadas; la existencia de 18 registros por dos expansiones es observación pendiente, **no prueba de bypass**.
- `frontend/src/app/shared/navigation/app-navigation-menu.component.ts`: 39 etiquetas `<a routerLink="...">` de navegación.
- `frontend/src/app/core/guards/auth.guard.ts`: valida sesión y contexto tenant, vuelve al login cuando falta verificación. `permiso.guard.ts`: requiere capacidad y ruta completa habilitada tras consultar permisos; `plataforma.guard.ts` se ocupa del acceso institucional.
- Los registros raíz `dashboard` y `usuarios` tienen `authGuard + permisoGuard`; `plataforma` usa `plataformaGuard`; `centro-reportes` tiene `authGuard` padre y nueve hijos con `permisoGuard`; `**` redirige en lugar de cargar directamente una pantalla privada.

**Prueba dirigida sobre el texto del repositorio (lectura GitHub del SHA base):** los siete invariantes del router resultaron verdaderos; los conteos 92/90/2 y 39 enlaces coincidieron. Las verificaciones fuente del tenant, capacidad y ruta completa también pasaron.

**Cuatro pruebas negativas sin alterar la rama:** (N1) sustituir `tiendas/:storeKey` por matcher raíz `:slug`; (N2) retirar guards de `dashboard`; (N3) retirar `authGuard` de `centro-reportes`; (N4) sustituir redirect de `**` por un `loadComponent`. Todas las mutaciones ficticias se detectaron/rechazaron por el evaluador estático. **N1–N4 modifican únicamente cadenas en memoria, nunca archivos reales.**

## Reproducción verificable y límites

Ejecutar, en un checkout de la **rama del PR**: `node scripts/verify-p0-006-b-route-guards.mjs --self-test`. El validador lee las cinco fuentes y falla (exit code diferente de 0) ante cambio de guard, matcher, conteo o patrón que invalide este baseline. La evaluación equivalente de estos invariantes y mutaciones se realizó directamente sobre fuentes GitHub de `dev@bafcde53`; **la ejecución nativa del nuevo archivo Node dentro de un checkout todavía requiere readback/CI dirigido**, ya que el entorno local no pudo clonar GitHub. No afirmar que el simple CI genérico ejecuta este script si no consta su job.

**No equivale a prueba E2E:** sigue pendiente corroborar en navegador con sesión autorizada que rutas privadas responden con redirect/403 según usuario sin permisos y que la API aplica RBAC/tenant del lado servidor; no reutilizar identidades ni credenciales. Tampoco hay evidencia nueva sobre Render/Vercel. La prueba estática es un gate preventivo, no una declaración de seguridad integral.

## Impacto, reversión y cierre

Impacto: `scripts/verify-p0-006-b-route-guards.mjs`, `.github/workflows/p0-006-b-route-guards.yml`, esta acta y `CHANGELOG_AI.md`; ninguna API, DTO, tabla, migración, endpoint, guard o servicio es modificado. Consumidores: revisores de CI y mantenimiento de navegación. Seguridad: prueba de regresión sobre patrones/guards existentes, sin cambiar JWT/RBAC/PII. Observabilidad: evidencia de SHA, PR, checks y Sheet.

**Rollback:** revert protegido del script/acta/changelog sobre `dev`, sin tocar frontend de producción ni datos. El PR debe someterse a revisión de otra identidad GitHub, Scope Lock, seguridad y backend **3/3 SUCCESS del HEAD exacto**, y merge protegido antes de declarar `LISTO`. El readback de `PLAN_MAESTRO/COLA/BITACORA` es obligatorio. Hasta entonces: `VALIDANDO`.

## Ejecución nativa dirigida en CI del PR

Se incorpora el workflow mínimo `.github/workflows/p0-006-b-route-guards.yml`, limitado a eventos `pull_request` sobre `dev` cuando cambian los guardas, rutas, el validador o el propio workflow. `permissions: contents: read`; utiliza `actions/checkout@v4` y Node 24.21.0, sin instalar dependencias, escribir datos ni desplegar. El comando obligatorio es `node scripts/verify-p0-006-b-route-guards.mjs --self-test` sobre el checkout del SHA del PR. El job es evidencia adicional, **no reemplaza** los tres checks protegidos, aprobación GitHub independiente, merge ni conciliación Sheets. Registrar su resultado real solo cuando GitHub Actions lo confirme; no presumir PASS al publicar YAML. La reversión del changeset debe incluir este workflow.
