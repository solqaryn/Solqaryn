# SOLQARYN V5 — C06-03.M01.01: objetivos y no-regresión de sitios editoriales y mapas

**ID:** C06-03.M01.01, PLAN_MAESTRO fila 938. **Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio/entorno:** `solqaryn/Solqaryn`, rama `dev`, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Objetivo, exclusiones y frontera real

El objetivo futuro de C06-03 es ofrecer páginas y bloques editoriales por tenant con ubicación, mapas opcionales, horarios, contacto y contenido administrado. El resultado exige moderación, sanitización, accesibilidad y SEO verificables. **Esta M01.01 es exclusivamente preparación de alcance**: no se implementan editores, mapas, geocodificación, proveedor externo, permisos, endpoints, tablas, migraciones, dominio, SSR, publicación, workers, CDN ni UI nueva. Las rutas de tienda existentes no certifican que exista el sitio editorial previsto.

A diferencia de una ficha de tienda, una página editorial necesita distinguir contenido público aprobado de borradores, atribuir ownership tenant/sucursal y registrar edición, moderación y publicación por separado. La identidad del tenant se resuelve en backend, nunca por `Host`, ruta, parámetro, `X-Empresa-Id` ni un estado de Angular. Un mapa nunca activa geolocalización del visitante por defecto ni envía claves privadas a clientes.

## Ocho fuentes reales del árbol dev contrastadas

| Ruta | Blob Git verificado | Lo que acredita y su límite |
| --- | --- | --- |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas `tienda/*` y ERP; no acredita router de páginas editoriales publicadas |
| `frontend/src/app/features/storefront/storefront-identidad.service.ts` | `c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f` | Identidad de tienda y fallback neutro; no moderación |
| `frontend/src/app/features/storefront/storefront.component.ts` | `1490eb5950eb3a9619046972ccf78bb80cab4bd9` | Storefront real; no CMS ni módulo de mapas certificados |
| `frontend/src/app/core/interceptors/auth.interceptor.ts` | `4bdf19135ed1932a99fd09ace1f900bcccc5b25b` | `X-Empresa-Id` expresa tenant solicitado, no autorización |
| `frontend/server/seo-utils.js` | `b0886e30cc85efa6f800e558d5284412d8c573fc` | SEO y contexto de proxy de tienda, no autoridad host→tenant universal |
| `frontend/api/robots.js` | `7ae514cfa9d5d645caba899dc10d2ccf0a9b34f9` | Robots para tienda; no noindex de borradores editoriales |
| `frontend/api/sitemap.js` | `ee50880520c906178a7ebd31553f3ab6d28f6b2d` | Sitemap actual; no sitemap de sitios editoriales por empresa |
| `docs/v5/c06-02-m01-01-objetivo-no-alcance-landing-tenant.md` | `d749c986ad03bf20bded35a3afc87d2b05d4e9fd` | Límites documentales de landing/host/preview, sin publicación operativa |

La muestra es intencionalmente acotada. No demuestra ausencia de otros módulos ni certifica backend, MySQL, sanitización runtime, HTTP, navegador, SEO vivo o DNS.

## Invariantes NR01–NR12 para pruebas futuras

| ID | Invariante | Bloqueo / caso de no-regresión |
| --- | --- | --- |
| NR01 | Propiedad tenant validada en backend para cada página/bloque | Slug o Host de A no concede acceso a B |
| NR02 | Borradores privados separados de versión publicada | Draft aparece en respuesta pública, sitemap o caché |
| NR03 | Editor, moderador y publicador tienen RBAC distinto | Editor sin permiso publica o aprueba |
| NR04 | HTML, texto enriquecido y URL se sanitizan antes de persistir/renderizar | Script, evento inline, URL javascript: o iframe inseguro se ejecutan |
| NR05 | Ubicación y contacto requieren consentimiento y datos mínimos | Domicilio/PII privada se publica sin aprobación |
| NR06 | Horarios y excepciones se atribuyen a sucursal/zona horaria | Horarios de sucursal A o zona incorrecta se asignan a B |
| NR07 | Teléfono, correo, dirección y branding pertenecen al tenant autorizado | Contacto de otra empresa aparece por fallback |
| NR08 | Mapas futuros usan proveedores aprobados, secretos privados y minimización | Geolocalización forzada, coste o clave expuesta |
| NR09 | Publicación versionada con auditoría, CAS y rollback | Edición concurrente pisa versión aprobada |
| NR10 | Canonical, robots y sitemap solo usan hosts y rutas publicados | Borrador/URL privada o host sin verificar se indexa |
| NR11 | Mapa/bloques conservan texto alternativo, teclado, foco, 320px y carga progresiva | Sin JS o mapa caído, el usuario no puede leer horarios/contacto |
| NR12 | Caché, medios, errores y fallback segregados por empresa | Sesión o caché A revela información de B |

## Oráculos POSITIVOS y NEGATIVOS futuros: NO_EJECUTADOS

**P01–P06 futuros:** P01 página publicada de A devuelve contenido A; P02 permisos distinguen editor/moderador/publicador; P03 horario con zona y excepción se presenta correctamente; P04 mapa autorizado ofrece alternativa textual accesible; P05 sitemap/canonical solo incluyen URL publicada y aprobada; P06 rollback conserva historial y versión anterior.

**N01–N12 futuros:** N01 tenant A/B cruzado rechazado; N02 editor sin publish no publica; N03 borrador noindex y no sitemap; N04 script HTML bloqueado; N05 enlace malicioso rechazado; N06 domicilio privado no expuesto; N07 horario de otra sucursal rechazado; N08 fallo proveedor mapa no filtra secretos ni bloquea alternativa textual; N09 caché de A no sirve B; N10 carrera de versiones no pisa publicación; N11 geolocalización no se activa por defecto; N12 errores SEO no filtran PII ni rutas privadas.

**Ninguno de esos casos se ha ejecutado como test runtime en esta microtarea.** Son condiciones de aceptación y negativos exigibles en etapas posteriores.

## Dependencias, impacto y salida de esta M01.01

Se validó `DAG_TECNICO!F899/G899=VALIDADO/SI` y PLAN/COLA H/I y G/O al reclamar. Los contratos preparatorios de C02, C05 y C06-02 no liberan por sí mismos M01.06, M02/M03, IAM/JWT/RBAC, migraciones, datos productivos o publicación. Angular 22.2.1, .NET 10, MySQL 8.4 y EF Core vigentes quedan intactos.

**Impacto real:** se crea un archivo `docs/v5` y una sección autocontenida de `CHANGELOG_AI.md`. No cambia API/DTO, Domain/Application/Infrastructure, rutas Angular, datos/DB, permisos, servicios de mapas, métricas, infraestructura ni secretos. A0 contrasta autoridad, DAG, fuentes y alcance; A1–A3 son revisiones de coherencia documental positivas/negativas y aislamiento, runtime **NO_APLICA**; A4 exige commit fast-forward, diff de dos Markdown, Scope Lock/procedencia V5 SUCCESS exact-SHA y readback PLAN/COLA/BITACORA. A5 permanece pendiente de otra Task Javier; jamás autoaprobada. Rollback por `git revert` normal, eventos de BITACORA solo aditivos, y ninguna intervención en QA/main/PROD.
