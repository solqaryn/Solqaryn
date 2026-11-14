# SOLQARYN V5 — C12-02.M01.02: actores, interfaces y fronteras del frontend separado

**Unidad:** `C12-02.M01.02`, PLAN_MAESTRO fila 1767, C12-02 Frontend separado, M01 preparación. **Autor:** PRIMARY DEV 06 JAVIER, TASK `6af16bbad26881918c283abe06e1f32a`. **Autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). **Corte Git:** `solqaryn/Solqaryn@dev` `a72695c55671937289585071a2abae3106ee3f0e`, fecha 2026-11-14, **PROJECT_SCOPE_LOCK=STRICT**.

## Alcance y estado comprobable

Esta microtarea documenta actores, entradas/salidas y fronteras de confianza para un **frontend futuro desplegable de forma separada** con build Angular 22.2.1, CDN, rutas públicas/privadas, API proxy, headers, caché, dominio, smoke y rollback. **No implementa ni activa** CDN, Hostinger, Cloudflare, DNS, TLS, nuevos servicios, servidores, variables de producción, workers, cuentas o planes de pago. Se preserva el despliegue declarado actual Vercel para frontend, Render para API, Aiven para MySQL y Cloudinary para medios. La arquitectura objetivo no equivale a infraestructura existente; ningún smoke público o E2E nuevo se ha ejecutado aquí.

Predecesor documental `C12-02.M01.01` (blob `3130e21e73ebb3489fd80be726c8bb8d1afb97fd`) define NR01–NR16, positivos y negativos **FUTUROS_NO_EJECUTADOS**, sin licencia para ejecutar una migración. Se mantiene la regla Domain/Application/Infrastructure/API, JWT/RBAC/tenant A/B y no exposición de secretos.

## A01–A12 — actores y límites de autoridad

Los operadores descritos son **roles de diseño/operación futura**, no nuevas cuentas o permisos otorgados por el documento.

| Actor | Función | Interacción prevista | Límite de autorización |
| --- | --- | --- | --- |
| A01 | Visitante público | Solicita páginas públicas y rutas SEO | No posee identidad/permiso de tenant |
| A02 | Usuario autenticado | Navega SPA y solicita recursos protegidos | JWT/RBAC y aislamiento decididos en backend |
| A03 | Administrador de empresa | Administra su sesión y vistas permitidas | No puede actuar como administrador global |
| A04 | Administrador de plataforma | Gestiona capacidades conforme a rol | No obtiene acceso cross-tenant por routing |
| A05 | Desarrollador Angular | Mantiene rutas/components/guards | No puede saltarse API authorization |
| A06 | Operador Vercel DEV | Observa build y headers del proyecto dev | No cambia QA/main por inferencia |
| A07 | Operador QA | Valida preview/QA en proyecto aislado | Sin promocionar por CI documental |
| A08 | Operador PROD | Controla cambio autorizado en main | No participa en esta entrega dev |
| A09 | Operador de DNS/TLS | Propondrá certificados y reversión | No puede alterar DNS real ahora |
| A10 | Desarrollador API | Preserva rutas HTTP y respuestas de auth | No altera MySQL ni JWT por CDN |
| A11 | Revisor seguridad | Revisa CSP/CORS/secrets/tenancy | A5 requiere Task diferente de autor |
| A12 | Agente CI GitHub Actions | Ejecuta builds/tests por SHA | Un queued/skipped no es PASS obligatorio |

## I01/O01–I12/O12 — contratos de entrada y salida

| Interfaz | Entrada | Salida observable | Invariante de seguridad/no regresión |
| --- | --- | --- | --- |
| I01/O01 | Solicitud SPA a ruta pública | URL + ruta no confidencial | HTML/asset/browser app |
| I02/O02 | Solicitud SPA a ruta privada | ruta + sesión del navegador | guard UI + petición API |
| I03/O03 | Carga de assets versionados | URL JS/CSS con hash | asset con Cache-Control adecuado |
| I04/O04 | Solicitud a /api/:path* | HTTP, cabeceras permitidas | backend-proxy y respuesta fiel |
| I05/O05 | SEO /robots.txt, /sitemap.xml | petición bot/usuario | respuesta SEO versionada |
| I06/O06 | SEO de producto/categoría | slug/ruta pública | contenido público permitido |
| I07/O07 | Compilación Angular | package lock + angular config + SHA | artefactos bajo dist/.../browser |
| I08/O08 | Filtro de build Vercel | project ID + ref commit/branch | decisión skip/build por entorno |
| I09/O09 | Configuración ambiente frontend | variables públicas del bundle | URL API y flags públicos |
| I10/O10 | Cabeceras HTTP de frontend | CSP/HSTS/referrer/config Vercel | respuestas protegidas |
| I11/O11 | Smoke futuro CDN | artefacto firmado, dominio previsto | estatus + headers + ruta/asset |
| I12/O12 | Rollback futuro de frontend | SHA previo + manifiesto y runbook | reversión verificable |

El `frontend/vercel.json` inspeccionado declara un rewrite de `/api/:path*` hacia `/api/backend-proxy?path=:path*`, además de rutas SEO por bot, fallback SPA, HSTS, CSP, nosniff y caché de assets con hash. Es un **contrato versionado**, no prueba live de su ejecución. El script `frontend/scripts/vercel-ignore-build.mjs` conserva selección por proyecto/branch y puede omitir una compilación: su salida **no constituye PASS funcional**. `app.routes.ts` utiliza guards de UX pero el backend conserva el control final JWT/RBAC/tenant.

## F01–F12 — fronteras de confianza y condiciones de rechazo

| Frontera | Paso entre dominios de confianza | Recurso o dato | Negativo que debe permanecer prohibido |
| --- | --- | --- | --- |
| F01 | Navegador -> SPA pública | Datos públicos, URLs y assets | No confundir client state con identidad |
| F02 | SPA privada -> auth guard | UX de acceso/ruta | Guard no sustituye backend JWT |
| F03 | Browser -> backend-proxy | Cabeceras y JWT | Nunca confiar en tenant_id arbitrario |
| F04 | Backend-proxy -> API Render | Request/response HTTP | Conservar auth, códigos y logs redactados |
| F05 | API -> servicios/DB | Operación de backend existente | Frontend no obtiene acceso MySQL directo |
| F06 | Vercel DEV -> rama dev | Build automático por proyecto | Proyecto QA/main aislado por binding |
| F07 | CLI/CI -> npm/Angular build | Dependencias y SHA | No ejecutar scripts externos no aprobados |
| F08 | CDN/edge -> cliente | Cache/headers/HTTPS | Evitar servir HTML de otro tenant por cache |
| F09 | Origen público -> SEO | Rutas amigables para crawler | No exponer facturas o datos privados en SEO |
| F10 | Secrets store -> build | Variables de ambiente | No incorporar JWT secret o contraseña al bundle |
| F11 | DNS/TLS -> frontend objetivo futuro | Dominio y certificado planificados | Sin permiso actual de cambio de DNS |
| F12 | QA/PROD -> operación de release | Promoción futura | Sin publicación desde este alcance dev |

**No aplicar un trust implícito a cabeceras, slug, cookie, ruta o querystring**. Empresa/tenant A y B deben continuar aislados desde la API, sin fiarse de la UI ni del edge. El CDN no debe cachear información privada/HTML de sesión cruzada. Los cambios de CSP y CORS requieren revisión de seguridad y pruebas por ambiente.

## Fuentes versionadas que sustentan este contrato

| Fuente | Blob SHA del corte | Cobertura |
| --- | --- | --- |
| `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | npm scripts y Angular |
| `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Builder, assets, polyfills |
| `frontend/vercel.json` | `5f12bcf667a21afcfc4314b66787ff66627ae666` | Rewrites, headers, cache |
| `frontend/scripts/vercel-ignore-build.mjs` | `81d9c7fe6639da0fee1a23dac34e337f3c994507` | Protección por project/branch |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas y guards |
| `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | HTTP y zone change detection |
| `frontend/src/environments/environment.ts` | `fcc3144df39328605e7d7b561d4024b930fdfd59` | Endpoint local dev; no target live |
| `render.yaml` | `f64d291425e2bbfdd9b6466d268753f8f8d86b3d` | API Render por ambiente |
| `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Sólo dev, sin production |
| `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | JWT/RBAC/tenant, frontera de API |

## Escenarios de aceptación de futuras fases, NO_EJECUTADOS

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | Rutas SPA profundas y guards funcionan con build reproducible | 404 espurio o bypass de permiso desde URL/cliente |
| P02/N02 | Proxy conserva 401/403 y cabeceras de API | Cabecera cliente permite tenant B o modifica error a 200 |
| P03/N03 | Fallback SEO y sitemap sirven sólo recursos públicos | Escapar datos de factura/cliente o rutas cross-tenant |
| P04/N04 | CDN respeta hashes y headers de seguridad | Caché compartida revela sesiones de otro tenant |
| P05/N05 | Proyecto dev construye solo dev, QA/main aislados | Confundir `SKIPPED` con compilación certificada |
| P06/N06 | TLS, DNS y rollback se comprueban en ambiente aprobado | Activar dominio/certificado sin orden o perder rollback |
| P07/N07 | UI ligera y accesible funciona bajo degradación simulada | Bloquear teclado/foco o cargar bundle sin control |
| P08/N08 | Secretos y variables por ambiente nunca se empaquetan | JWT secret/DB password aparece en JS, repositorio o logs |

Todos los P/N son **criterios futuros**: no hay nuevas ejecuciones de Playwright, smoke público, migración o CDN durante esta documentación.

## A0–A5, publicación y rollback

**A0:** Skill instalada versus canónica exacta, Scope Lock STRICT, identidad Javier, fila 1767 con PLAN H=PENDIENTE/I vacío antes del CAS, COLA G/O=SI, DAG F=VALIDADO/G=SI, diez fuentes Git preflight.

**A1 documental:** 12 actores, 12 interfaces y 12 fronteras con positivos/negativos futuros; ningún test runtime afirmado.

**A2 seguridad documental:** medidas de JWT/RBAC/tenant A/B, CSP/CORS, headers/cache, secrets y scopes dev/qa/prod conservadas; rechazos de suplantación/credencial en bundle.

**A3 integración documental:** inspección cruzada de `vercel.json`, `vercel-ignore-build.mjs`, `angular.json`, rutas/guards y manifest Angular; no hay consulta ni activación de proveedor externo, QA/main/PROD.

**A4:** solo 2 Markdown de docs/v5 y CHANGELOG en commit fast-forward CAS a dev; checks exact SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success; readback Git/Sheet/BITACORA. Suites runtime no causales se consignan **NO_APLICA**, nunca PASS.

**A5:** un TASK_ID Javier distinto del autor, pendiente; no habilitar M01.06 o migración por revisión de esta unidad. **Rollback:** `git revert` normal del SHA en nuevo commit, sin force-push ni reescritura de BITACORA.

**Dictamen:** superficies y fronteras descritas, no infraestructura provisionada ni backend/caché/SPA certificado en un CDN futuro.
