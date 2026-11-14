# SOLQARYN V5 — C06-02.M01.03: inventario causal de contratos y código de landing por tenant

**Unidad:** C06-02.M01.03 (PLAN_MAESTRO fila 904). **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** solqaryn/Solqaryn, `dev`, PROJECT_SCOPE_LOCK=STRICT. **Tipo:** evidencia documental; no implementación.

## Alcance y método

C06-02 busca una landing administrable por tenant con host/slug, branding, preview, publicación y noindex de borradores. Esta M01.03 **lee fuentes actuales y fija brechas verificables**; no desarrolla un resolver de dominio, publicación, JWT/RBAC, MySQL, API, UI, worker, Cloudflare, R2, DNS ni infraestructura. Se respetan los límites de M01.01 (NR01–NR12) y las interfaces FUTURAS de M01.02 (A01–A12/I01–I12/B01–B08), sin convertir un documento en permiso.

Se consultó `dev` mediante GitHub autenticado y se comparó por SHA de blob cada archivo listado. Los hashes son evidencia de contenido exacto en el árbol consultado, **no** evidencia de funcionamiento HTTP, pruebas de aislamiento A/B reales ni de entorno productivo.

## Inventario S01–S14 de fuentes verificadas

| Fuente | Ruta exacta del árbol `dev` | SHA de blob | Qué demuestra / límite |
| --- | --- | --- | --- |
| S01 | `docs/v5/c06-02-m01-01-objetivo-no-alcance-landing-tenant.md` | `d749c986ad03bf20bded35a3afc87d2b05d4e9fd` | NR01–NR12, estado futuro, no runtime |
| S02 | `docs/v5/c06-02-m01-02-actores-interfaces-landing-tenant.md` | `050ef36f55d80e367e097f4d4699718663c91d9d` | A01–A12, I01–I12, B01–B08, pruebas futuras |
| S03 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | raíz/login, tiendas/storefront y navegación protegida; no binding host |
| S04 | `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | router, interceptor y Zone.js; no autoridad tenant por host |
| S05 | `frontend/src/app/features/storefront/storefront-identidad.service.ts` | `c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f` | marca de tienda, fallback neutro; no editor de landing |
| S06 | `frontend/src/app/features/storefront/storefront.component.ts` | `1490eb5950eb3a9619046972ccf78bb80cab4bd9` | componentes de tienda, no publicación versionada |
| S07 | `frontend/src/app/features/storefront/storefront.service.ts` | `ebf0d5d9287180fb3e6aae4f4190ee93afae11cc` | peticiones de storefront; no prueba de API de landing |
| S08 | `frontend/src/app/core/guards/auth.guard.ts` | `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6` | contexto tenant verificado para ERP; no control preview futuro |
| S09 | `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | permiso de ruta privada; no rol/publicador nuevo |
| S10 | `frontend/src/app/core/interceptors/auth.interceptor.ts` | `4bdf19135ed1932a99fd09ace1f900bcccc5b25b` | X-Empresa-Id solo intención; servidor conserva autoridad |
| S11 | `frontend/server/seo-utils.js` | `b0886e30cc85efa6f800e558d5284412d8c573fc` | hostFromRequest mira x-forwarded-host/host; requiere modelo de proxy confiable antes de binding por tenant |
| S12 | `frontend/api/robots.js` | `7ae514cfa9d5d645caba899dc10d2ccf0a9b34f9` | robots con isIndexableHost; no certifica noindex de drafts |
| S13 | `frontend/api/sitemap.js` | `ee50880520c906178a7ebd31553f3ab6d28f6b2d` | sitemap público existente, no sitemap de landing multiempresa |
| S14 | `docs/v5/c06-01-m01-01-objetivo-no-alcance-sitio-publico-saas.md` | `96d8280c96125af30b8f3f580c092460cb397b84` | scope de marketing institucional distinto de landing tenant |

## Contraste técnico por capa

**Frontend:** el router Angular actual tiene rutas públicas de tienda y navegación interna protegida; la landing universal por host requiere un contrato separado. El storefront dispone de identidad pública y fallback neutro, pero no demuestra constructor ni publicación versionada. Las guardas privadas y el interceptor no convierten `Host`, slug, `X-Empresa-Id` ni `localStorage` en autorización.

**Backend/tenant:** ningún archivo de esta muestra acredita un endpoint de publicación tenant ni una tabla de alias de dominios. La implementación posterior deberá localizar el tenant en servidor, validar ownership y RBAC contra datos reales, y denegar por defecto. No se infiere ausencia absoluta de componentes fuera de esta selección; hará falta un inventario backend puntual antes de M03.

**SEO/borde:** `seo-utils.js` contiene `hostFromRequest()` que lee `x-forwarded-host` o `host`. Esto **no constituye por sí solo vulnerabilidad demostrada**, pero la futura resolución de dominio exigirá configuración explícita de proxies confiables, validación de host autorizado y pruebas contra encabezados manipulados. `robots.js` y `sitemap.js` prueban existencia de rutas SEO públicas actuales, no exclusión certificada de previews ni publicación multiempresa.

**Datos e integraciones:** ninguna modificación en tablas MySQL, migraciones EF, Cloudinary, storage, SMTP, CDN, DNS/TLS, cookies, planes de pago, secretos o tenants reales. La topología Hostinger/Cloudflare es objetivo de plan, no infraestructura concedida para esta tarea.

## G01–G10 — brechas a cerrar con evidencia causal en etapas futuras

| ID | Brecha aún no acreditada | Validación exigida antes de M03/M05 |
| --- | --- | --- |
| G01 | Host→tenant con ownership server-side | Prueba A/B de host aprobado, no aprobado y spoofed |
| G02 | Alias y slug canónicos y únicos | Rechazos de homógrafos, traversal y colisión por tenant |
| G03 | Versionado de publicación | CAS de versión, auditoría y rollback sin pérdida de draft |
| G04 | Autorización de editor/publicador | JWT/RBAC backend; denegar lector y editor sin publish |
| G05 | Preview no indexable | Token efímero, no-store/noindex y exclusión sitemap |
| G06 | Branding segregado | Logo/copy/moneda/asset tenant A nunca en B |
| G07 | SEO/sitemap por host | Canonical/robots acotados a host y publicación autorizados |
| G08 | Cache/CDN confiable | Partición por tenant/host/versión y origen trusted proxy |
| G09 | Accesibilidad y ligereza | Focus/teclado/320px/reduced motion y métricas de bundle |
| G10 | Despliegue/dominio reversible | Aprobación separada DNS/TLS, smoke, backup y rollback |

Estas brechas están **PENDIENTES**, no fallas runtime probadas. Las decisiones comerciales de marca/dominio también permanecen pendientes de la autoridad apropiada.

## Pruebas de aceptación FUTURAS NO_EJECUTADAS

**Positivas P01–P06:** P01 host verificado resuelve tenant; P02 slug publicado muestra branding correcto; P03 preview autorizado lleva noindex/no-store; P04 versión aprobada se publica con auditoría; P05 robots/sitemap solo listan rutas publicadas; P06 rollback recupera snapshot sin destruir datos.

**Negativas N01–N12:** N01 host inventado rechazado; N02 `x-forwarded-host` falsificado no otorga tenant; N03 `X-Empresa-Id` manipulado no autoriza; N04 slug traversal no resuelve; N05 preview vencido/ajeno falla cerrado; N06 draft no aparece en sitemap; N07 editor sin publish no publica; N08 script inyectado no llega a HTML; N09 cache tenant A nunca sirve B; N10 SEO no filtra PII; N11 alias no verificado no activa DNS/TLS; N12 error de CDN o rollback no expone datos privados.

Son **oráculos de diseño sin ejecución**. No deben registrarse como PASS funcional de Playwright, .NET, HTTP, MySQL ni de seguridad tenant.

## Impacto y controles de cierre

**Archivos modificados:** solo este Markdown y sección autocontenida de `CHANGELOG_AI.md`. No cambian capas Domain/Application/Infrastructure/API, frontend, interfaces/DTO, datos, autorizaciones, infraestructura ni servicios. A0: exactitud de los 14 blobs e inventario sin afirmaciones de runtime; A1–A3: consistencia documental de 10 brechas y 18 casos futuros, con runtime `NO_APLICA`; A4: diff solo Markdown, checks GitHub Scope Lock y procedencia V5 del SHA exacto, PLAN/COLA/BITACORA readback. A5 requiere revisión independiente de otra Task Javier.

**Rollback:** revert Git normal del changeset; jamás fuerza de rama ni edición de BITACORA histórica. No se entrega a QA, main o PROD; ningún webhook o dispatcher implementado.
