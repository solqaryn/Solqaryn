# SOLQARYN V5 — C06-01.M01.03 inventario causal de fuentes para sitio público SaaS

**Autor** PRIMARY DEV 03 JAVIER — `6adc2e4bd8a48191ac7d8641ed95a1a3`. **PLAN_MAESTRO** fila 868, microtarea C06-01.M01.03. **Autoridad** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Ámbito** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha** 2026-11-14.

## Resultado y límites de inferencia

Inventario comprobable de código/contratos existentes antes de diseñar la futura landing institucional: propuesta de valor, planes, sectores, demo, contacto, contenido legal y SEO. Esta M01.03 es **solo lectura y documentación**: no crea páginas, rutas, robots/sitemap, formulario, texto comercial aprobado, API, SMTP, datos, migraciones, proveedores ni despliegues. La evidencia mínima de la fase (rutas públicas, metadatos, sitemap y formulario seguro) **no queda satisfecha por este inventario**. Angular 22.2.1/Zone.js 0.16.3, ASP.NET net10.0, MySQL, JWT y RBAC continúan siendo autoridad arquitectónica según Skill canónica.

## Doce fuentes originales inspeccionadas en dev

| N.º | Archivo exacto | SHA del blob Git | Hallazgo acotado a ese archivo |
| --- | --- | --- | --- |
| 1 | `docs/v5/c06-01-m01-01-objetivo-no-alcance-sitio-publico-saas.md` | `96d8280c96125af30b8f3f580c092460cb397b84` | NR01-NR12 y delimitación de landing futura; ningún release |
| 2 | `docs/v5/c06-01-m01-02-actores-interfaces-sitio-publico-saas.md` | `abc1084b024d32391559f02c365cf4111c5f739a` | A01-A12, I01-I12 y fronteras futuras, no endpoints |
| 3 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Raíz redirige a login; tienda pública distinta de ERP privado y plataforma guardada |
| 4 | `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | Router, preload selectivo e interceptor; sin prueba SEO |
| 5 | `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Build Angular application y budget 650/750 kB declarado, no medido |
| 6 | `frontend/src/app/core/guards/auth.guard.ts` | `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6` | Contexto tenant backend verificado o retorno a login |
| 7 | `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Permisos runtime y revalidación de tenant fail-closed |
| 8 | `frontend/src/app/core/guards/plataforma.guard.ts` | `9ba2af19988da93a755e3753dc8cbc93563e916a` | Control-plane solo con identidad y validación backend |
| 9 | `frontend/src/app/core/interceptors/auth.interceptor.ts` | `4bdf19135ed1932a99fd09ace1f900bcccc5b25b` | Bearer y tenant solicitado, sin elevar su autoridad |
| 10 | `frontend/src/app/features/storefront/storefront-identidad.service.ts` | `c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f` | Branding de tienda con fallback neutro, no landing SaaS |
| 11 | `frontend/src/app/features/storefront/storefront.component.ts` | `1490eb5950eb3a9619046972ccf78bb80cab4bd9` | Catálogo/carrito de tienda no es catálogo de planes SaaS |
| 12 | `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | ARIA role/live y cinco estados reutilizables, sin integración contacto |

Estos blobs son fuentes, no resultados de test HTTP/browser ni prueba de que no existan implementaciones en otros repositorios o rutas no inspeccionadas. M01.01 NR01-NR12 y M01.02 A01-A12/I01-I12 siguen siendo contratos futuros. El sitio de tienda `tienda/*` existente no autoriza mezclar identidad, PII o catálogo de otro tenant con el portal institucional.

## Ocho brechas que requieren evidencia futura (B01–B08)

- **B01** Ruta pública institucional separada de `/tienda`, `/login`, `/dashboard` y control-plane, probada en Angular/HTTP.
- **B02** Ofertas, sectores, planes, precios y promesas comerciales aprobados y versionados; sin inventar capacidades o fechas.
- **B03** Metadatos por URL: título, descripción, canonical y política de indexación realmente inspeccionada por crawler/HTTP.
- **B04** Robots y sitemap allowlisted; excluir rutas autenticadas, filtros de tenant, identificadores personales y tokens.
- **B05** Contacto con consentimiento, payload mínimo, antispam/rate limit backend, validación, auditoría segura y rechazo negativo.
- **B06** Demo ficticia aislada con TTL y no uso de datos reales, MySQL productivo o tenant de terceros.
- **B07** Términos y privacidad con revisión legal/comercial, versión, fecha, aprobación y reversión.
- **B08** Playwright/HTTP/seguridad/SEO/accesibilidad 320px, foco/teclado/ARIA, rendimiento y rollback medidos sobre SHA.

Son brechas de **evidencia**, no defectos ya diagnosticados. No se presume falta universal de características no inspeccionadas.

## Oráculos positivos y negativos futuros: NO_EJECUTADOS

**P01–P06:** P01 acceso anónimo únicamente a landing permitida; P02 catálogo aprobado; P03 canonical/sitemap correctos; P04 contacto consentido con límites backend; P05 demo efímera aislada; P06 teclado, foco, 320px y aria-live comprobados en navegador.

**N01–N12:** N01 anónimo denegado en ERP; N02 sin fuga tenant A/B; N03 JWT caducado denegado; N04 precios ficticios no publicados; N05 legal no aprobado no publicado; N06 XSS/sanitización; N07 spam/rate-limit; N08 rechazo sin consentimiento; N09 canonical externo inseguro; N10 sitemap no revela privado/PII/token; N11 demo no accede a clientes/PROD; N12 fallback y rollback seguro ante error.

Ninguna de estas pruebas futuras se declara PASS en esta unidad. La validación propia se limita a inventario estático, coherencia documental y controles de repositorio del SHA de publicación.

## Impacto, seguridad, pruebas y rollback

**Cambios:** únicamente este Markdown y sección autocontenida de CHANGELOG_AI.md. **Sin modificación** Angular/HTTP/DB/auth/tenant, datos, UI, recursos, dominios, secretos, trabajadores o eventos. **Seguridad:** frontend no sustituye autorización backend, tenant solicitado no otorga autoridad y las rutas privadas conservan guards. **A0–A4:** inspección de fuentes y límites, casos documentales P/N etiquetados NO_EJECUTADOS, comprobar diff/publicación fast-forward, SHA exacto, Scope Lock y procedencia V5; CI runtime no pertinente al diff se clasifica NO_APLICA, jamás PASS implícito. **Rollback:** git revert no destructivo en dev y revalidación de SHA. **A5:** revisión independiente de otra de cinco Tasks Javier autorizadas antes de promociones; nunca autoevaluación. QA/main/PROD, migraciones, pagos, SMTP real o lanzamiento no autorizados.
