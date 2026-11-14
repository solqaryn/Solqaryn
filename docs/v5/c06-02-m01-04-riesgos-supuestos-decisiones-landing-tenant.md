# SOLQARYN V5 — C06-02.M01.04: riesgos, supuestos y decisiones de landing tenant

**Microtarea:** C06-02.M01.04, PLAN_MAESTRO fila 905. **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Fecha:** 2026-11-14. **Alcance:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT; contrato documental, no implementación.

## Alcance y evidencia causal

Esta M01.04 registra riesgos **potenciales, no vulnerabilidades observadas**, supuestos **a comprobar** y decisiones **PENDIENTES** sobre landing pública administrable por empresa, host/slug, branding, preview, noindex, publicación y rollback. No cambia API .NET, Angular, MySQL/EF, tenancy/JWT/RBAC, DNS/TLS, CDN, Cloudinary, R2 ni producción. El plan V5 objetivo de Hostinger/Cloudflare no autoriza despliegue alguno.

**Contraste verificado con blobs Git de `dev`:** `C06-02.M01.01` (`d749c986ad03bf20bded35a3afc87d2b05d4e9fd`, NR01–NR12), `M01.02` (`050ef36f55d80e367e097f4d4699718663c91d9d`, A01–A12/I01–I12/B01–B08) y `M01.03` (`3fd8977ff3a001e01178e81e08b206e8a46511e1`, S01–S14/G01–G10). Router `app.routes.ts` (`2f560c14693455b79ac4546e9333fb74586ef234`), `auth.interceptor.ts` (`4bdf19135ed1932a99fd09ace1f900bcccc5b25b`), `storefront-identidad.service.ts` (`c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f`) y `seo-utils.js` (`b0886e30cc85efa6f800e558d5284412d8c573fc`) evidencian piezas existentes de tienda/seguridad/SEO; no un motor de landing certificado.

La función `hostFromRequest` en `seo-utils.js` lee el encabezado reenviado o Host como dato. Sin inspeccionar confianza del proxy, despliegue y controles upstream **no puede afirmarse explotación**, pero la futura resolución de tenant por host debe someterse a pruebas explícitas de spoofing.

## R01–R12 — matriz de riesgos hipotéticos y mitigación propuesta

| ID | Riesgo (no incidente confirmado) | Criticidad prevista | Gate técnico y oráculo futuro |
| --- | --- | --- | --- |
| R01 | Host/alias asignado a empresa equivocada | alto | normalización, ownership servidor y rechazo de host spoofed antes de habilitar; P01/N01–N02 |
| R02 | Cross-tenant en caché/SSR o recurso gráfico | crítico | particionar cache por host+tenant+versión y prueba A/B/N09 |
| R03 | Borrador/preview expuesto a crawler | alto | token efímero, noindex/no-store, exclusión sitemap y N04–N06 |
| R04 | Editor sin publish ejecuta release | crítico | RBAC server-side por acción y version CAS; N07–N08 |
| R05 | XSS o HTML activo en builder futuro | alto | schema de bloques tipado, sanitización servidor/CSP; N06 |
| R06 | Alias/dominio homógrafo o colisión de slug | alto | canonicalización, unicidad acotada a tenant y N03/N10 |
| R07 | Confundir storefront actual con landing administrable | medio | inspección diferencial de app.routes, storefront y contrato M01; no declarar runtime certificado |
| R08 | SEO revela datos privados o preview | alto | canonical/robots/sitemap por host publicado, noindex y N05/N10 |
| R09 | Fallback visual toma identidad de empresa ajena | alto | fallback neutro validado por datos públicos, pruebas tenant A/B/N09 |
| R10 | Dependencia de encabezado reenviado sin proxy confiable | alto | revisar hostFromRequest, normalización y proxy allowlist; N02; no asumir explotación actual |
| R11 | Pérdida de versión o rollback destructivo | alto | estado inmutable, CAS, restore sin borrar drafts/ventas; P06/N12 |
| R12 | Cambio DNS/TLS o R2/Cloudflare sin permiso | crítico | separar gate de infraestructura del DAG documental; N10 y autorización explícita |

Todos los riesgos son **abiertos hasta contar con evidencia de implementación y pruebas**; esta matriz no los cierra.

## S01–S08 — supuestos falsables

| ID | Supuesto | Estado de conocimiento | Cómo verificar sin inferir PASS |
| --- | --- | --- | --- |
| S01 | Existe una política de dominio verificable por tenant | NO_VERIFICADA | Exigir ownership probatorio y autorización antes de M03 |
| S02 | El backend puede derivar contexto tenant sin confiar en la UI | PARCIAL_CODE | Inspeccionar endpoint concreto y probar A/B autenticación futura |
| S03 | La configuración de marca pública tiene ownership inequívoco | PARCIAL_CODE | Confirmar filtros server-side y fallback en falla |
| S04 | Los hosts CDN/proxy tienen un trust boundary explícito | NO_VERIFICADA | Revisar headers reales y allowlist de intermediarios |
| S05 | Existe modelo de versionado/draft compatible con rollback | NO_VERIFICADA | Buscar entidades/migraciones y diseñar CAS |
| S06 | Se cuenta con robots/sitemap reutilizable por tenant | PARCIAL_CODE | Validar indexación por host publicado, no por tienda solamente |
| S07 | El plan comercial autoriza crear DNS/Hostinger/Cloudflare hoy | FALSA_PARA_ESTA_TAREA | No alterar infraestructura y requerir permiso separado |
| S08 | Existe suite runtime host/preview A/B lista | NO_VERIFICADA | Crear pruebas dirigidas solo en etapa autorizada |

`PARCIAL_CODE` solo acredita la presencia de un patrón en el código inspeccionado, no una prueba HTTP, proveedor, tenant o migración. `NO_VERIFICADA` y `FALSA_PARA_ESTA_TAREA` bloquean cualquier autorización derivada de esos supuestos.

## D01–D08 — decisiones pendientes, no aprobaciones

| ID | Decisión requerida | Estado | Evidencia o autorización necesaria |
| --- | --- | --- | --- |
| D01 | Resolver host y canonicalización | PENDIENTE | Diseñar trust proxy, allowlist y tabla ownership; no registrar alias reales |
| D02 | Forma del contenido editable | PENDIENTE | Schema tipado y sanitizado, sin JS/HTML libre |
| D03 | Autorización editor vs publicador | PENDIENTE | Permisos separados server-side; revisar control-plane |
| D04 | Persistencia/índices de versiones y alias | PENDIENTE | Diseñar EF/MySQL con migraciones/rollback probados en CI |
| D05 | Semántica token preview | PENDIENTE | Opaque/TTL/revocación/no-store/noindex y auditoría |
| D06 | Cache/SEO por tenant | PENDIENTE | Definir host+tenant+versión, robots, canonical y sitemap |
| D07 | Política de assets y proveedor | PENDIENTE | Conservar Cloudinary vigente; R2 solo decisión futura |
| D08 | Autorización de DNS/TLS y lanzamiento | PENDIENTE | Separar producción, dominio, roles, aprobación humana y rollback |

Ninguna decisión se marca APROBADA. Resolver D01–D06 implica un diseño e implementación posterior con preflight propio, límites JWT/RBAC/tenant A/B y pruebas pertinentes; D07–D08 requieren además autorización diferenciada para cambios reales de proveedor, cuentas, DNS/TLS y producción.

## Escenarios de aceptación FUTUROS NO_EJECUTADOS

**Positivos P01–P06:** P01 host aprobado resuelve A; P02 branding de A nunca hereda B; P03 preview autorizado es noindex/no-store; P04 publicador válido registra snapshot auditable; P05 sitemap/robots solo indexan publicación; P06 rollback restaura sin perder borradores.

**Negativos N01–N12:** N01 Host arbitrario no resuelve tenant; N02 encabezado reenviado manipulado no confiere autoridad; N03 slug inválido o colisivo rechazado; N04 preview anónimo/expirado denegado; N05 borrador fuera de sitemap; N06 XSS no se ejecuta; N07 editor sin publish denegado; N08 CAS de publicación concurrente falla seguro; N09 caché A/B aislada; N10 dominio/TLS no aprobado no se publica; N11 error de proveedor no filtra archivos privados; N12 rollback no afecta ERP ni historial.

Estas 18 comprobaciones son **propuestas NO_EJECUTADAS**: no reportar xUnit, Angular/Playwright, MySQL, DNS, security live ni A/B productivas como aprobadas por la redacción.

## Impacto, controles y reversión

Cambian **solo** `docs/v5/c06-02-m01-04-riesgos-supuestos-decisiones-landing-tenant.md` y `CHANGELOG_AI.md`. No crea endpoint, migración, ruta, cookie, permiso, storage, política de seguridad, evento ni worker. A0: confirmar fuentes y 12 riesgos/8 supuestos/8 decisiones; A1–A3: revisión de coherencia documental y negativa explícita de runtime; A4: diff de dos Markdown, Scope Lock y check V5 de procedencia completados exitosamente en SHA exacto + readback PLAN/COLA/BITACORA. La CI general compilación/seguridad para código no modificado es `NO_APLICA` a esta auditoría, jamás PASS inventado. A5 independiente por otra Task Javier distinta de la autora; QA/main/PROD prohibidos sin autorización nueva.

**Rollback:** `git revert` normal del commit, con lectura de HEAD y checks, sin force-push ni editar historial BITACORA. Riesgos y decisiones permanecen pendientes para las fases técnicas siguientes.
