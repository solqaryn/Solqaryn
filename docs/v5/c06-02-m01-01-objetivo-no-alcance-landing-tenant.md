# SOLQARYN V5 — C06-02.M01.01: objetivo, exclusiones y no-regresión de landing por tenant

**Unidad:** C06-02.M01.01, PLAN_MAESTRO fila 902. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Repos/branch:** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Objetivo comprobable y línea base

C06-02 busca **página pública administrable por empresa** con dominio/slug, branding, preview y publicación controlada. El resultado futuro mínimo exige pruebas de **host binding, preview, noindex para borradores y aislamiento multiempresa**. Esta M01.01 solo define alcance, exclusiones e invariantes: **no hay nueva landing, binding de Host, publicación, DNS/TLS, dominio, endpoint, control de preview, ni cambio de configuración**.

La arquitectura vigente exige Angular 22.2.1 standalone/Signals con Zone.js 0.16.3, ASP.NET Core net10.0, MySQL/EF Core, JWT/RBAC y tenant validado en backend. La topología futura Hostinger/Cloudflare y la habilitación de sitios/hosts solo son objetivos sujetos a autorización. La presencia de storefront tenant-aware **no** acredita un builder, binding de dominio o workflow de publicación.

**Fuentes reales inspeccionadas:**
- `frontend/src/app/app.routes.ts` blob `2f560c14693455b79ac4546e9333fb74586ef234`: raíz a login; storefront `tienda/*`; `dashboard` protegido; ninguna evidencia de host binding en este router.
- `frontend/src/app/app.config.ts` blob `cd77384e713014015d83b348955b3d83573348b3`: router, interceptor y preload; no prueba resolución server-side por dominio.
- `frontend/src/app/features/storefront/storefront-identidad.service.ts` blob `c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f`: configuración pública de tienda con identidad neutra si falla backend; no sustituye autorización de host/tenant.
- `frontend/src/app/features/storefront/storefront.component.ts` blob `1490eb5950eb3a9619046972ccf78bb80cab4bd9`: catálogo/experiencia de tienda actual, no edición de landing por tenant.
- `frontend/src/app/core/guards/auth.guard.ts` blob `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6` y `permiso.guard.ts` blob `1052bd38041055cc81e84a14d5fe0364e169b1fe`: guardas privadas y revalidación tenant por backend.
- `frontend/src/app/core/interceptors/auth.interceptor.ts` blob `4bdf19135ed1932a99fd09ace1f900bcccc5b25b`: cabecera tenant solicitada por cliente, no autoridad.
- `docs/v5/c06-01-m01-01-objetivo-no-alcance-sitio-publico-saas.md` blob `96d8280c96125af30b8f3f580c092460cb397b84`: distingue landing institucional futura de ERP/tienda, no autoriza hosting por empresa.
- `docs/v5/c05-03-m01-01-objetivo-no-alcance-builder-gobernado.md` (referencia conceptual del builder, leer versión actual antes de M02/M03): la preparación nunca concede permisos de publicación.

Los hashes citados son de archivos ya comprobados; no son resultado de un barrido exhaustivo de servicios HTTP ni DNS.

## Doce invariantes NR01–NR12

| ID | Límite obligatorio | NO_GO observable |
| --- | --- | --- |
| NR01 | El dominio/host solicitado no determina por sí solo autoridad tenant | Host arbitrario no puede acceder a otro tenant ni resolver slug de empresa B |
| NR02 | La asignación host→tenant debe verificarse en backend desde ownership aprobado | Nunca confiar en cabecera Host reenviada sin normalizar ni allowlist |
| NR03 | La página pública de empresa no comparte control-plane ni ERP privado | No eliminar guardas auth/permiso ni abrir endpoints administrativos |
| NR04 | Los borradores nunca son indexables ni públicamente enumerables | Preview requiere autorización y noindex; tokens opacos expiran |
| NR05 | El branding pertenece al tenant del host comprobado | Sin nombre, logo o contacto de otra empresa como fallback |
| NR06 | Slug, ruta y dominio deben normalizarse, ser únicos y sujetos a control | Bloquear path traversal, homógrafos no normalizados y alias no autorizados |
| NR07 | El estado publicado se deriva de versión aprobada, no de contenido en edición | Publicar únicamente snapshot inmutable autorizado y auditado |
| NR08 | La edición de páginas usa RBAC backend y controles CSRF/XSS | Frontend no convierte rol de editor en autoridad por solo mostrar CTA |
| NR09 | SEO y metadatos no revelan PII ni rutas con autenticación | Canonical/robots/sitemap permiten solo hosts y rutas publicados |
| NR10 | Cache/CDN y recursos estáticos preservan partición entre tenants | Tenant A no recibe headers/HTML/caché de B al cambiar Host |
| NR11 | Accesibilidad, responsividad y desempeño no se sacrifican por builder | Foco, teclado, aria-live, reduced-motion, 320px y assets medibles |
| NR12 | Dominio, TLS, DNS y publicación productiva requieren autorización distinta | Ningún cambio de DNS, SSL, secrets, R2, cuentas ni deploy por esta preparación |

## Exclusiones de esta microtarea

La preparación no implementa tablas de dominios, migraciones, cookies, JWT nuevo, autorización de editor, recursos públicos, provider, CDN, sitemap, API, UI, SSR, builder, preview ni deployment. No toca cuentas, secretos, dominios, Cloudflare/Hostinger/R2, producción o datos reales. El dominio y slug pertenecen al *scope futuro* y no se autorizan por una celda DAG con G=SI.

## Oráculos del lote FUTUROS NO_EJECUTADOS

**P01–P06 positivos futuros:** P01 host aprobado resuelve tenant correcto; P02 slug publicado ofrece branding correcto; P03 preview autorizado sin indexación; P04 borrador separado de versión publicada; P05 caché/recursos segregados tenant A/B; P06 foco, teclado, 320px y fallback seguros.

**N01–N12 negativos futuros:** N01 Host arbitrario no concede tenant; N02 alias de B no accede A; N03 path/slug manipulado rechazado; N04 preview anónimo/expirado denegado; N05 borrador noindex y ausente de sitemap; N06 HTML/script malicioso sanitizado; N07 rol sin permiso no publica; N08 contenido no aprobado no aparece como publicado; N09 caché de A no se sirve a B; N10 dominio sin TLS/aprobación no activa release; N11 error de storage/CDN no expone datos privados; N12 rollback conserva versión anterior sin alterar ERP. **No son pruebas ejecutadas**.

## Validación proporcional y reversión

**A0–A4 de este cambio documental:** comprobar fuentes, 12 invariantes, exclusiones y escenarios futuros; diffs de dos Markdown, Scope Lock y procedencia CHANGELOG SUCCESS en SHA exacto y readback PLAN/COLA/BITACORA. Las builds de runtime, tests xUnit/Angular/E2E y pruebas de DB no se atribuyen a este cambio; si corren se clasifican NO_APLICA para esta unidad, no PASS. **A5:** revisor independiente TASK_ID de otro Javier, no autor. **Rollback:** revert fast-forward normal del changeset si fuera necesario; BITACORA no se reescribe. No QA/main/PROD ni permisos de ejecución comercial.
