# C02-01.M04.04 — Prevención atómica de doble acción del selector empresarial

Repositorio único `solqaryn/Solqaryn@dev`; microtarea `C02-01.M04.04` fila 311; owner histórico DEV05; `PROJECT_SCOPE_LOCK=STRICT`. Riesgo **R2** por flujo de selección de contexto empresarial con JWT. Referencia Angular original **SKREF-10** `angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9`, sin instalar dependencias ni modificar frameworks.

## A0 — alcance y cambio mínimo

El selector existente `TenantSelectorComponent.activarEmpresa()` ya impide una segunda petición mientras `cargando`, pero su condición conjuntaba carga con selección inválida y colocaba erróneamente `Selecciona una empresa...` al intentar un segundo submit durante una validación legítima. Se separa el guard: la segunda acción se ignora sin crear petición, cambiar contexto ni pintar un error falso; el error permanece únicamente para empresa inválida. No se alteran backend, endpoints, JWT, RBAC, tenants, migraciones ni cookies. Se preserva la verificación de membresía por GET real antes de confirmar, el control del servicio de versión/selección y fail-closed.

## A1 y A2 — pruebas negativas y de frontera

Se agrega una prueba Angular TestBed real con `HttpTestingController` que reproduce dos empresas, selección manual válida, dos invocaciones seguidas de `activarEmpresa()` antes del GET de confirmación, **exactamente una solicitud HTTP**, **cero eventos de navegación anticipados**, cero error espurio, contexto no verificado antes de responder, respuesta correcta con una sola emisión de `confirmado` y contexto firmado por el servicio solo tras GET. La suite existente valida 0/1/2 empresas, preferencia obsoleta, 403, errores de lista y datos malformados. Estos stubs del navegador **no demuestran JWT/API/MySQL reales** por sí solos y no se presentan como integración autenticada.

## A3/A4, CI y limitaciones

Pruebas locales dirigidas según la arquitectura de Angular 22 y GitHub Actions para el **SHA causal**, con `npm test`, lint y Angular production build; se requiere `verify-solqaryn-project-scope`, V5 y los checks pertinentes `completed/success`. Se reutiliza el workflow ya existente `v20-c02-01-m04-02-tenant-states.yml` que se activa al modificar el selector o su spec; no se relajan validadores. Probar de forma independiente la navegación completa con servidor auténtico, cambio de permisos/caché y redirecciones si la cobertura es exigida por el criterio V21; nunca inferir PASS solo por TestBed. Conservar `VALIDANDO` mientras falten A0–A4. A5 corresponde a otra de las cinco Tasks Javier con TASK_ID distinto, revisión genuina, bajo candado V17.

Rollback de código: revert normal del commit publicado tras verificar HEAD, sin force-push. No modificar QA/main/PROD, secretos, servicios pagos ni fuentes productivas. PLAN E describe un resultado acumulativo M04, **no se certifica la fase entera** por este incremento.

## V21 — reparación cruzada DEV01: ensayo real JWT/MySQL/Chromium, sin interceptar respuestas

Se agrega `frontend/e2e/c02-01-m04-04-real-double-action.spec.ts` y un workflow dirigido con API .NET 10, MySQL 8.4 aislado, JWT firmado con clave aleatoria CI y navegador Chromium. Tres casos: membresía A+B con doble submit mientras GET real está pendiente (una petición, respuesta 200, transición después de confirmación y sin bucle); elección vacía con formulario denegado y cero GET; JWT de A y empresa B falsificada con denegación 403/404. `route.continue()` únicamente demora la petición, **no crea respuestas simuladas**. El workflow se ejecuta sólo sobre SHA dev y no usa proveedores externos, secrets reales ni entornos productivos.

Al publicar el commit, resultados A1–A4 permanecen **pendientes de lectura causal en GitHub Actions** hasta `completed/success` de las tres pruebas, build .NET/Angular, Scope Lock, V5 y controles pertinentes. No se sobreescribe owner DEV05, no se autocertifica A5. El flujo real de permisos y otras rutas no queda certificado por esta suite, y la meta acumulativa M04 no sustituye los criterios propios del ID. Riesgo R2, rollback normal fast-forward/revert, mantener VALIDANDO hasta prueba y readback.
