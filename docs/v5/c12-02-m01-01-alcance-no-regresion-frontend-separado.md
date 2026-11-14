# SOLQARYN V5 — C12-02.M01.01: alcance y no-regresión de frontend separado

**Unidad:** `C12-02.M01.01` (PLAN_MAESTRO fila 1766), C12-02 / Frontend separado / M01 Alcance y preparación. **Autor:** PRIMARY DEV 06 Javier (`6af16bbad26881918c283abe06e1f32a`). **Autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). **Corte fuente:** `solqaryn/Solqaryn@dev` `4882153668928d1e036a5c9fb91fd2d7457d6385`, 2026-11-14; `PROJECT_SCOPE_LOCK=STRICT`.

## Decisión documental y límites

Definir el objetivo de un **frontend eventualmente separado**, conservando Angular 22.2.1, Zone.js, guards, rewrites SEO/API, headers, caché de assets, despliegue reproducible, CDN y rollback. El frontend actual está declarado en Vercel, backend en Render, datos en Aiven y Cloudinary como proveedor de medios. La futura topología objetivo Hostinger/Cloudflare **no está implementada ni contratada por esta entrega**. El alcance de M01 es definir contratos, no efectuar build de migración, smoke público, cambio de dominio, hosting, certificados, DNS, configuración de backend, secretos ni compras.

El resultado exigido de fase incluye build/CDN/dominio/headers/cache/deploy independiente con smoke y rollback, pero **sus pruebas reales pertenecen a fases posteriores y requieren autorización**. Ninguna NR se marca ejecutada por quedar redactada; no se afirma proveedor/SLI/SLO/costo real sin evidencia.

## Diez referencias de código y manifiestos auditados por blob

| # | Ruta en el repositorio | Blob Git exacto en corte | Contrato observable y límite |
| --- | --- | --- | --- |
| F01 | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Scripts build/lint y dependencias Angular; no prueba build futuro |
| F02 | `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | @angular/build:application, polyfills zone.js, output browser |
| F03 | `frontend/vercel.json` | `5f12bcf667a21afcfc4314b66787ff66627ae666` | Rewrites API/SEO, fallback, headers y cache-control |
| F04 | `frontend/scripts/vercel-ignore-build.mjs` | `81d9c7fe6639da0fee1a23dac34e337f3c994507` | Bindings dev/qa/main y decisión skip/build por rama |
| F05 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas Angular y guards de seguridad/permiso |
| F06 | `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | provideZoneChangeDetection, router e interceptores |
| F07 | `frontend/src/environments/environment.ts` | `fcc3144df39328605e7d7b561d4024b930fdfd59` | Desarrollo local apunta localhost; no es endpoint desplegado |
| F08 | `render.yaml` | `f64d291425e2bbfdd9b6466d268753f8f8d86b3d` | Contratos backend actual por ambiente, no cambiar |
| F09 | `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Arquitectura Angular 22.2.1, Zone.js, backend auth |
| F10 | `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Scope lock dev strict y autorizaciones |

Nota causal: `frontend/scripts/vercel-ignore-build.mjs` documenta bindings de proyectos por rama y utiliza `process.exit(0)` para omitir builds cuando corresponde; jamás interpretar un **skip** como PASS de verificación funcional. `frontend/src/environments/environment.ts` apunta a localhost en desarrollo y no acredita un endpoint de producción. El proxy/rewrites de Vercel no elevan derechos del navegador sobre tenant/JWT/RBAC. `render.yaml` no es evidencia de despliegue ni cambios autorizados en el host.

## NR01–NR16 con criterio positivo y rechazo negativo

Las pruebas de esta matriz son **FUTUROS_NO_EJECUTADOS**. Una futura prueba deberá registrar SHA, ambiente, fixture, resultado y rollback con logs sensibles redactados. No existe ninguna invocación a Playwright, CI nueva, live CDN o proveedor por la mera creación de este documento.

| Invariante | Condición preservada | Positivo futuro observable | Negativo que debe ser rechazado |
| --- | --- | --- | --- |
| NR01 | Angular 22.2.1 y Angular Material/CDK 22.2.1 conservan versiones y configuración de build declaradas. | P01: El build objetivo reproduce el artefacto browser con el mismo source SHA. | N01: No admitir un build que omita assets o ruta de salida declarada. |
| NR02 | Zone.js 0.16.3 y provideZoneChangeDetection siguen activos hasta cambio arquitectónico separado. | P02: Hydration/arranque y eventos UI siguen funcionando con Zone.js. | N02: No transformar a zoneless por el solo movimiento de hosting. |
| NR03 | Routing SPA preserva fallback y deep links Angular. | P03: Una ruta privada enlazada directo conserva guard y render correcto. | N03: Acceso directo no devuelve datos privados ni se transforma en 404 accidental. |
| NR04 | La autenticación y permiso efectivos permanecen en backend, no en Vercel ni en el navegador. | P04: Guard de ruta aplica UX y API verifica JWT/RBAC de forma independiente. | N04: Manipular localStorage, header o tenant_id no autoriza API. |
| NR05 | Los rewrites SEO de /tienda y /tienda/producto/:slug mantienen contratos y user-agents definidos. | P05: Bots/rutas SEO sirven contenido esperado sin romper SPA. | N05: Un slug manipulado no exfiltra tenant ni genera rewrite abierto. |
| NR06 | /api/:path* conserva backend-proxy, respuesta HTTP, headers y errores. | P06: Errores 401/403/404/429 se preservan tras el proxy. | N06: No transformar 401/403 en 200, redirigir a host arbitrario o crear loop. |
| NR07 | CSP, HSTS, nosniff, X-Frame-Options y Referrer-Policy conservan protección efectiva. | P07: Cabeceras auditadas en respuestas de ensayo y assets. | N07: No admitir CSP permisiva por comodidad o downgrade de HTTPS. |
| NR08 | Assets versionados conservan Cache-Control immutable y contenido reproducible. | P08: Assets con hash cargan y conservan digest entre despliegues. | N08: No cachear HTML/config mutable como immutable ni servir JS obsoleto. |
| NR09 | Variables de ambiente no filtran secretos ni endpoint privado. | P09: Build expone sólo configuración pública mínima. | N09: No publicar JWT secret, credencial Aiven o token de proveedor en bundle. |
| NR10 | Los entornos dev, qa y main permanecen aislados por project-binding. | P10: Cada proyecto construye sólo su rama canónica. | N10: No interpretar ignorar build como aprobar cross-environment ni cambiar bindings sin autorización. |
| NR11 | Backend Render y Aiven vigentes quedan intactos durante preparación. | P11: El frontend preparado conserva endpoint efectivo autorizado del entorno. | N11: No cambiar DNS, conexión MySQL ni desactivar guards de Aiven/Render. |
| NR12 | Accesibilidad y ligereza visual se preservan durante cualquier traslado. | P12: Interacciones por teclado y foco, responsive y tiempos bajo presupuesto aprobado. | N12: No sacrificar navegación, contrastes o feedback por cambio de CDN. |
| NR13 | Rutas públicas y privadas mantienen CORS explícito y políticas de cookies/tokens. | P13: Origen autorizado opera sin pérdida de sesión. | N13: Origen no autorizado no consigue sesión ni credenciales cruzadas. |
| NR14 | Rollback conserva release previo, hashes y URL canónica. | P14: Simulación puede volver a versión aprobada con pruebas smoke. | N14: No proceder a cutover si falla reversión o se pierden referencias. |
| NR15 | Build/CI preserva evidencia por SHA exacto, no por workflow ajeno. | P15: Build Node/Angular y E2E causales reportan success de SHA publicado. | N15: SKIPPED/CANCELLED/IN_PROGRESS nunca se cuentan como PASS obligatorio. |
| NR16 | No contratar CDN/hosting, mover DNS ni crear servicios sin aprobación separada. | P16: Plan de cutover identifica autoridad y ventana de cambio. | N16: Un DAG SI documental no es permiso de migración ni compra. |

## Verificación transversal para futuro cutover y rollback

- **Identidad:** pruebas de API sin confiar en guard de Angular; JWT válido solo para rol y empresa A; JWT ajeno, vencido o EmpresaId/slug alterado devuelven denegación. El backend decide la autorización real.
- **Rutas públicas:** conservar `/robots.txt`, `/sitemap.xml`, SEO de tienda, `/api/:path*` y fallback SPA; pruebas de HTTP/robots, bots, querystring, errores y cache.
- **Assets y caché:** JS/CSS versionados con hash se cachean con `immutable`; HTML/config de entrada debe actualizarse. Comparar checksum y headers en CDN real **solo después** de un permiso explícito.
- **Segregación:** DEV/QA/main no comparten secretos, proyecto, branch, DNS ni base. No tocar hosts/productos vigentes ni exigir escalado a plan de pago.
- **Rollback:** mantener artefactos versionados, salida de build y configuración previa; si smoke, SSL, auth, caché o reversión fallan, permanecer en despliegue anterior. No declarar objetivos temporales inventados.

## Controles A0–A5 y naturaleza de la evidencia

**A0:** Skill local/canónica coincidentes, Scope Lock, HEAD dev, estado PLAN H1766/I1766, COLA G/O=SI, DAG F=VALIDADO/G=SI, claim CAS exclusivo, diez fuentes Git.

**A1 documental:** 16 invariantes NR, 16 criterios positivos y 16 negativos consistentes; rechazar como falso PASS toda prueba futura no ejecutada.

**A2 seguridad documental:** conservar JWT/RBAC/tenant A/B, origen CORS, CSP/HSTS, delimitación de envvars y no acceso a secretos ni a otros entornos; no confiar en frontend como control autoritativo.

**A3 integración documental:** cotejo de `package.json`, Angular build, Vercel rewrites/headers, script skip, routing/guards y contrato Render. Sin prueba HTTP/browser/CDN, sin nuevos endpoints: clasificar runtime **NO_APLICA a este changeset Markdown**, no PASS.

**A4 publicación:** solo `docs/v5` y sección autocontenida de `CHANGELOG_AI.md` en dev, commit fast-forward CAS, verificar checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact SHA y Git/Sheet/BITACORA readback. Los checks obligatorios que estén en progreso **nunca** son PASS.

**A5:** otro TASK_ID Javier independiente, posterior al cierre DEV; no autopremiar ni promover QA/main/PROD. No liberar M01.06 o M03 sólo por esta preparación.

**Rollback:** `git revert <sha>` con nuevo commit gobernado, no force-push, no eliminar BITACORA pasada.

**Conclusión permitida:** contrato documental de no-regresión preparado para revisión, **sin frontend separado desplegado**, smoke real ni CDN nuevo.
