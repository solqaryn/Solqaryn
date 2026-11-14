# SOLQARYN V5 — C06-03.M01.03: lectura e inventario causal de fuentes

**ID:** C06-03.M01.03, PLAN_MAESTRO fila 940. **Autor:** PRIMARY DEV 06 JAVIER, Task `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repo:** `solqaryn/Solqaryn`, `dev`, PROJECT_SCOPE_LOCK=STRICT. Este artefacto **lee y documenta** evidencia; no realiza implementación ni certificación funcional.

## Método y límites

El plan C06-03 exige páginas y bloques editoriales con ubicación, horarios, contacto y mapas, más moderación, sanitización, accesibilidad y SEO. Se cotejaron fuentes en el HEAD de `dev` mediante GitHub y los contratos M01.01/M01.02 de este mismo scope. Los hashes identifican el contenido real de la fuente observada; **no prueban ejecución funcional, exhaustividad de backend ni ausencia global de módulos**. No se autoriza proveedor cartográfico, geolocalización, un constructor, publicación, cambio de dominio o despliegue.

## S01–S12: inventario acotado de código y contratos

| Fuente | Ruta | SHA blob comprobado | Evidencia y límite |
| --- | --- | --- | --- |
| S01 | `docs/v5/c06-03-m01-01-alcance-sitios-editoriales-mapas.md` | `d16fd73d5211161bdde429a3b705ec9b9d09cdcf` | NR01–NR12, exclusiones, oráculos futuros, no runtime |
| S02 | `docs/v5/c06-03-m01-02-actores-interfaces-editoriales.md` | `c5f815be7ef044ffb2cd4e4910aa97dff511731a` | A01–A12/I01–I12/B01–B08 futuros, sin autorización concedida |
| S03 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas tienda/ERP vigentes; no página editorial arbitraria certificada |
| S04 | `frontend/src/app/features/storefront/storefront-identidad.service.ts` | `c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f` | Identidad comercial de tienda y fallback neutro; no CMS |
| S05 | `frontend/src/app/features/storefront/storefront.component.ts` | `1490eb5950eb3a9619046972ccf78bb80cab4bd9` | Superficie storefront; no demuestra mapas editoriales |
| S06 | `frontend/src/app/core/interceptors/auth.interceptor.ts` | `4bdf19135ed1932a99fd09ace1f900bcccc5b25b` | Cabecera X-Empresa-Id expresa solicitud, no autoridad |
| S07 | `frontend/server/seo-utils.js` | `b0886e30cc85efa6f800e558d5284412d8c573fc` | Lee x-forwarded-host/host en lógica SEO existente; no evidencia exploit ni autorización para host→tenant |
| S08 | `frontend/api/robots.js` | `7ae514cfa9d5d645caba899dc10d2ccf0a9b34f9` | Reglas robots de tienda; borradores editoriales sin certificar |
| S09 | `frontend/api/sitemap.js` | `ee50880520c906178a7ebd31553f3ab6d28f6b2d` | Sitemap existente; no índice de versiones editoriales publicadas |
| S10 | `docs/v5/c06-02-m01-01-objetivo-no-alcance-landing-tenant.md` | `d749c986ad03bf20bded35a3afc87d2b05d4e9fd` | Contrato de host/landing futuro; no liga DNS/TLS |
| S11 | `docs/v5/c06-02-m01-02-actores-interfaces-landing-tenant.md` | `050ef36f55d80e367e097f4d4699718663c91d9d` | I01–I12 de landing y límites editoriales conceptuales, no endpoints creados |
| S12 | `docs/v5/c06-02-m01-03-inventario-fuentes-landing-tenant.md` | `3fd8977ff3a001e01178e81e08b206e8a46511e1` | Inventario de catorce blobs de landing/SEO; no certifica infraestructura futura |

Los blobs de código S03–S09 se leyeron directamente; S10–S12 son contratos documentales del mismo repositorio. La selección y el análisis son reproducibles mediante blobs Git, sin deducir funcionamiento de funciones ausentes en esta muestra. El `hostFromRequest` existente de SEO exige analizar proxies confiables antes de un futuro resolver de host, pero **no se reporta vulnerabilidad probada**.

## G01–G08: brechas de evidencia, NO fallas demostradas

| ID | Condición pendiente de acreditar | Evidencia futura requerida |
| --- | --- | --- |
| G01 | Ownership server-side de página/bloque por empresa | Consulta A/B y denegación cross-tenant en API |
| G02 | Borrador, revisión y publicación versionada | Contratos DTO/RBAC y concurrencia CAS reales |
| G03 | Sanitización y allowlist de bloques rich-text | Casos positivos y XSS negativos en render/API |
| G04 | Ubicación aprobada y protección de PII | Control de precisión y datos personales |
| G05 | Horarios/sucursales con zona y excepciones | Pruebas de horarios de empresas/sucursales distintas |
| G06 | Mapas externos con fallback accesible | Proveedor aprobado, privacidad, cuotas y alternativa textual |
| G07 | Canonical/robots/sitemap por versión pública | Pruebas SEO de host/preview/noindex de draft |
| G08 | Aislamiento de caché, medias y rollback | Ensayos entre empresas, fallo CDN, reversión y logs |

**No se afirma que las brechas equivalgan a defectos existentes.** Las decisiones comerciales y de proveedor deberán tener autoridad y evidencias propias.

## Oráculos futuros NO_EJECUTADOS

**P01–P06 positivos:** P01 página publicada de A pertenece a A; P02 editor guarda borrador solo en A; P03 moderador y publicador actúan según RBAC; P04 horario/zona/contacto aprobado se presenta correctamente; P05 mapa opcional ofrece alternativa accesible; P06 SEO solo enumera versiones publicadas con rollback reversible.

**N01–N12 negativos:** N01 host B no accede a A; N02 editor sin permiso no publica; N03 preview/draft no aparece en sitemap; N04 script/URL malicioso no ejecuta; N05 domicilio privado no se publica; N06 horario de sucursal ajena no se mezcla; N07 permiso por frontend no concede backend; N08 mapa no obliga geolocalización; N09 proveedor caído no expone secreto ni deja sin contacto textual; N10 caché A no se reutiliza por B; N11 carrera CAS no pierde versión; N12 error/log no filtra JWT/PII.

Son especificaciones para pruebas posteriores, **no PASS de Node, Angular, HTTP, MySQL, JWT ni Playwright**.

## Impacto, auditorías y reversión

M01.03 añade este Markdown y un encabezado con sus viñetas contiguas en `CHANGELOG_AI.md`. Cero cambios Domain/Application/Infrastructure/API, UI, modelos, datos, migraciones, roles, jobs, servicios, SEO runtime, mapa, costes, dominios o secretos. El DAG individual y la cola habilitan la lectura documental, no M01.06/M02/M03/QA/main/PROD.

**A0** verificar S01–S12 y alcance. **A1–A3** contrastar G01–G08, positivos y negativos y controles tenant como criterios FUTUROS sin runtime (NO_APLICA). **A4** commit fast-forward CAS a `dev`, diff de dos Markdown, Scope Lock y control V5 CHANGELOG exact-SHA y readback institucional. **A5** siempre otra Task Javier; no autoaprobación. Rollback con `git revert` convencional; BITACORA es aditiva e inmutable.
