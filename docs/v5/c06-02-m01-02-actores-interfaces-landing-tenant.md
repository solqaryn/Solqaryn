# SOLQARYN V5 — C06-02.M01.02: actores, entradas, salidas y superficies de landing por tenant

**Proyecto:** solqaryn/Solqaryn, rama dev; PROJECT_SCOPE_LOCK=STRICT. **Microtarea:** `C06-02.M01.02`, PLAN_MAESTRO fila 903. **Autoría:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Predecesora documental:** `C06-02.M01.01`; no hay gate de runtime ni autorización de despliegue.

## Alcance y estado comprobado

El producto futuro requiere una landing administrable por empresa con dominio/slug, marca, previsualización autorizada, versión publicada, `noindex` de borradores y aislamiento de tenants. Esta entrega **solo documenta las interfaces futuras y las fronteras de confianza**. No crea una landing, rutas nuevas, APIs, persistencia, dominio, DNS, TLS, reglas de CDN, worker, webhook, JWT, rol, archivo R2 ni despliegue.

Línea base real contrastada en `dev`:
- `frontend/src/app/app.routes.ts` (blob `2f560c14693455b79ac4546e9333fb74586ef234`): raíz hacia `login`, rutas `tiendas/:storeKey` y `tienda/*`, y superficie privada con guardas. Estas rutas **no** demuestran un resolver de hostname ni el editor futuro.
- `frontend/src/app/features/storefront/storefront-identidad.service.ts` (blob `c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f`): nombre/marca pública de tienda y fallback neutro. No acredita tenant por host ni autorización de landing.
- `frontend/src/app/core/interceptors/auth.interceptor.ts` (blob `4bdf19135ed1932a99fd09ace1f900bcccc5b25b`): `X-Empresa-Id` es selección solicitada, nunca autoridad; la revalidación ocurre en backend.
- `frontend/src/app/core/guards/auth.guard.ts` (blob `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6`) y `permiso.guard.ts` (blob `1052bd38041055cc81e84a14d5fe0364e169b1fe`): requieren contexto tenant/permiso verificado por servidor en navegación privada.
- `docs/v5/c06-02-m01-01-objetivo-no-alcance-landing-tenant.md` (blob `d749c986ad03bf20bded35a3afc87d2b05d4e9fd`): NR01–NR12 y P01–P06/N01–N12 son contratos de fase anterior, **no** pruebas runtime ejecutadas.

## A01–A12 — actores y derechos esperados (ninguno se concede por este documento)

| ID | Actor | Entrada permitida futura | Salida/denegación esperada |
| --- | --- | --- | --- |
| A01 | Visitante anónimo | Host y ruta pública publicada | HTML aprobado sin datos privados; host no reconocido: 404 seguro |
| A02 | Cliente de la empresa | Navegación pública, enlaces publicados | Contenido correspondiente al tenant resuelto; no leer borradores |
| A03 | Editor de tenant | Sesión JWT, contexto tenant y permiso de edición | Borrador en scope propio; nunca publica por ser editor |
| A04 | Publicador autorizado | Permiso RBAC específico y versión revisada | Publicación auditada y atómica, no versión de otro tenant |
| A05 | Administrador de empresa | Gestión de su marca/configuración | Cambios tipados permitidos; no administra DNS ni cuenta global |
| A06 | Operador de plataforma | Gestión interna con rol global auditado | Sin bypass de autenticación ni exposición de PII en páginas |
| A07 | Propietario técnico de dominio | Prueba de posesión y aprobación externa | Solicitud de binding, no cambio automático de DNS/TLS |
| A08 | Buscador/crawler | Solo rutas canónicas publicadas | Borradores y previews con noindex y exclusión de sitemap |
| A09 | CDN/proxy | Host normalizado y cache-key segura | Caché particionada; no mezclar HTML tenant A/B |
| A10 | Proveedor de medios | Recurso publicado y ownership | Asset público autorizado, no objeto privado ni fuga de URL firmada |
| A11 | Auditor/observabilidad | Eventos mínimos correlacionados | Cambios trazables sin token, PII ni secretos en logs |
| A12 | Operador de rollback | Versión anterior aprobada y autorización | Restauración de publicación anterior sin borrar ediciones vigentes |

## I01–I12 — interfaces, entradas, salidas y fronteras

| ID | Superficie futura | Entrada y validación obligatoria | Salida o rechazo |
| --- | --- | --- | --- |
| I01 | Resolver host→tenant (API) | Host normalizado/allowlist + ownership servidor | Tenant aprobado o rechazo genérico; jamás confiar en header de cliente |
| I02 | Resolver slug/ruta | Slug canonizado, tenant de I01, estado publicado | Página de ese tenant o 404; rechazar path traversal |
| I03 | Consulta pública de versión | Referencia inmutable publicada, tenant de I01 | Solo bloques y metadatos aprobados, sin campos administrativos |
| I04 | Preview de borrador | Token opaco efímero + JWT/RBAC cuando aplique | Respuesta no-cache/noindex, prohibida a anónimos sin autorización |
| I05 | Consola de edición | JWT + rol/permiso + contexto backend | Borrador del tenant, sin publicar ni interpretar HTML arbitrario |
| I06 | Comando de publicación | Validación server-side, revisión y CAS de versión | Resultado auditable/idempotente; rechazar edición concurrente |
| I07 | Configuración de branding | Schema/allowlist y ownership | Marca del tenant; fallback neutro ante errores, nunca otra empresa |
| I08 | Gestión de alias/dominio | Prueba de posesión, aprobación y estado TLS | Propuesta pendiente, no activación por esta fase |
| I09 | SEO/robots/sitemap | Host publicado y URL canónica segura | Indexar solo versión pública; excluir preview, draft y PII |
| I10 | Assets/cache/CDN | Prefijo tenant, policy pública, cache key host+versión | Asset autorizado, errores sin fuga de buckets/keys |
| I11 | Consentimiento/contacto | Consentimiento específico, tamaño y rate-limit | Respuesta mínima; anti-spam, privacidad y trazas sin datos sensibles |
| I12 | Rollback/auditoría | Rol y versión de destino con CAS auditado | Revertir publicación sin eliminar historial ni modificar ERP |

**Dirección del flujo propuesto, no implementado:** Browser público → resolver server-side de host y slug → lectura de versión aprobada → HTML/SEO público. Editor privado → autenticación/selección tenant verificada → RBAC backend → borrador → revisión → publicación/versionado/auditoría. El frontend nunca decide la autoridad del tenant.

## B01–B08 — fronteras que deben mantenerse cerradas

1. B01: Host/slug/controlado por cliente frente al tenant **autorizado y resuelto en backend**.
2. B02: Páginas públicas frente a rutas de ERP/dashboard con JWT y permiso.
3. B03: Borrador/preview frente a versión publicada, token, expiración, noindex y cache.
4. B04: Configuración visual tipada frente a scripts/HTML/SQL arbitrarios y XSS.
5. B05: Tenant A frente a tenant B: identidad, rutas, assets, CDN, métricas y SEO.
6. B06: Publicación interna frente a DNS/TLS/domains/productivo, que exige nueva autorización.
7. B07: Datos de contacto y consentimiento frente a analítica/SMTP y trazas de PII.
8. B08: Concurrencia editorial frente a versiones y rollback; nunca lost update ni cambio unilateral.

## Escenarios de aceptación FUTUROS NO_EJECUTADOS

**Positivos:** P01 host verificado de A resuelve página A; P02 slug de A usa branding A; P03 editor autorizado recibe preview temporal noindex; P04 publicador con permiso versiona sin perder draft; P05 crawler ve solo publicación; P06 rollback recupera snapshot aprobado.

**Negativos:** N01 Host B no lee A; N02 `X-Empresa-Id` arbitraria no concede A; N03 slug traversal denegado; N04 preview anónimo/expirado denegado; N05 draft jamás aparece en sitemap; N06 script malicioso rechazado; N07 editor sin permiso no publica; N08 publish concurrente pierde CAS y no pisa versión; N09 cache A jamás sirve B; N10 alias no verificado no activa TLS/DNS; N11 falla asset/CDN sin mostrar objeto privado; N12 token/PII nunca sale en logs.

**No se ejecutaron estos casos:** se mantienen como oráculos del futuro M03/M05. La lectura de fuentes no certifica host binding, aislamiento HTTP real, infraestructura ni seguridad productiva.

## Impacto, verificación documental y rollback

Capas afectadas por esta microtarea: **solo `docs/v5` y `CHANGELOG_AI.md`**. Cero nuevas APIs/DTO, persistencia, cambios EF/MySQL, secretos, jobs, integraciones o políticas JWT/RBAC. Para un futuro M03 sí se exigirán xUnit/.NET, Angular/Playwright, test tenant A/B, denegación de host/slug y controles exact-SHA; aquí no corresponden como PASS.

A0: revisar correspondencia con Plan V5/M01.01 y los cinco blobs anteriores; A1–A3: inspección de consistencia interna de las 12 entradas/salidas, 12 interfaces y 8 fronteras, sin simulación runtime; A4: revisar diff solo Markdown, GitHub Scope Lock y procedencia de CHANGELOG del SHA publicado, más readback PLAN/COLA/BITACORA. CI backend/frontend/security no relacionadas = **NO_APLICA**, aunque su status no sea success. A5 queda **PENDIENTE** para una Task Javier distinta a la autora. Rollback futuro de este documento: `git revert` normal, sin reescribir historia ni BITACORA. Sin promoción QA/main/PROD.
