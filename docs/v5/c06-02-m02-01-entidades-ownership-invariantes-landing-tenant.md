# SOLQARYN V5 FINAL — C06-02.M02.01: entidades, ownership e invariantes de landing por tenant

**Entrega:** diseño documental M02, sin API, componente Angular, servicio .NET, base MySQL, resolver host, DNS ni landing instalada. **Autor:** PRIMARY DEV 06 JAVIER_CHATGPT, Task `6af16bbad26881918c283abe06e1f32a`. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuentes:** Standard V5 FINAL en Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, Skill canónica y cinco documentos M01 más M01.06.

## 1. Autoridad, dependencias y propósito

Esta microtarea modela **solo contratos conceptuales** para una landing pública administrable por empresa, manteniendo aislamiento A/B, permisos del editor/publicador, aprobación de host, preview, SEO, contenido tipado, caché y rollback. El catálogo M01.06 `docs/v5/c06-02-m01-06-bloqueos-rollback-liberacion-landing-tenant.md` fija G01–G16, seis condiciones de handoff y RB01–RB08. Su A5 independiente fue acreditada después de un diff únicamente documental; el nuevo preflight verificó `PLAN_MAESTRO!A908=C06-02.M02.01`, H=PENDIENTE, I vacío, `COLA!G/O=SI` y `DAG_TECNICO!F=VALIDADO/G=SI` para el ID exacto. Estos datos autorizan **diseñar el contrato**, no activar funcionalidad.

El storefront público y utilidades como `hostFromRequest` ya existen, pero no equivalen a un binding host→tenant aprobado. El valor `X-Forwarded-Host` requiere lista de proxies confiables, normalización y pruebas de spoofing antes de conectarlo a una empresa. La fuente de autoridad de ownership, JWT y RBAC es backend, nunca Angular, `localStorage`, Host ni un campo enviado por el cliente.

## 2. Modelo lógico propuesto — NO CLASES NI TABLAS IMPLEMENTADAS

| ID y entidad | Clave/owner previsto | Invariante de denegación |
| --- | --- | --- |
| L01 LandingTenant | TenantId validado en servidor y LandingId opaco | Un host o slug no es prueba de pertenencia ni autorización |
| L02 HostBindingPropuesto | Host normalizado, tenant, versión y ownership aprobado | No admitir X-Forwarded-Host desde proxy no confiable |
| L03 LandingRevision | LandingId, RevisionId y versión CAS | No sobrescribir contenido publicado con borrador de otro tenant |
| L04 LandingPage | RevisionId, ruta canónica y estado | No publicar URL borrador en sitemap |
| L05 LandingBlock | PageId, orden tipado y schemaVersion | No aceptar HTML, JS ni SQL arbitrarios |
| L06 TenantBrand | TenantId, logotipo público y tema aprobado | No enlazar imagen o token privado de empresa B |
| L07 AssetReference | TenantId, assetId opaco y política de lectura | No exponer firma de URL ni almacenamiento ajeno |
| L08 PreviewGrant | ActorId, tenant, revisión, expira UTC y revocación | Sin MFA/reauth y permiso no hay vista previa sensible |
| L09 PublicationDecision | Revisión y aprobador distinto del editor | Un flag o rol de frontend nunca publica |
| L10 PublicationVersion | LandingId, versión activa y referencia de rollback | Solo cambiar mediante CAS y verificación backend |
| L11 TenantRouteMap | Host aprobado, ruta, tenant y canonical | Host desconocido no deriva tenant predeterminado |
| L12 LandingSeoProjection | Versión activa, canonical/robots/sitemap | No indexar previews ni PII o URLs internas |
| L13 CachePartition | Clave tenant+host+revisión y política TTL | No reutilizar HTML público de tenant A para B |
| L14 ChangeAuditEvidence | Actor minimizado, acción, SHA/versión, UTC y resultado | No guardar JWT, PII completa ni credenciales |
| L15 PublicationPolicy | TenantId, catálogo de permisos, límites y cambios | No conceder RBAC por tema visual o ruta pública |
| L16 LandingRollbackPoint | Versión anterior aprobada y condición de reversión | Sin respaldo o prueba no efectuar migración irreversible |

**Relaciones propuestas:** Tenant 1:N Landing; Landing 1:N Revision; Revision 1:N Page; Page 1:N Block; Tenant 1:N HostBinding; Revision 1:N PreviewGrant; Landing 1:N PublicationVersion; Tenant 1:N AssetReference y AuditEvidence. Cada FK/lookup se diseñará sobre la DB real en microtarea posterior con índices, filtros, transacciones y rollback **probados en MySQL 8.4 efímero**, no inferidos de este diagrama. No usar `TenantId=null` para acceso de plataforma predeterminado; un ámbito global futuro exige permiso separado.

## 3. Invariantes obligatorias en un futuro runtime

| ID | Regla de seguridad/consistencia |
| --- | --- |
| I01 | El tenant efectivo y propiedad de host se resuelven server-side; no de encabezados no confiables |
| I02 | La identidad del editor y del publicador debe tener membresía vigente y permisos RBAC separados |
| I03 | Las lecturas y cambios se filtran por TenantId efectivo antes de paginación, caché y render |
| I04 | Una ruta o slug colisionante entre empresas no implica alias ni grant |
| I05 | Un preview es no-store/noindex, tiene expiración y se revoca; no aparece en sitemap |
| I06 | Un borrador nunca cambia la versión activa sin CAS y aprobación separada |
| I07 | Los bloques de contenido usan esquema y tipos cerrados; scripts activos no son contenido válido |
| I08 | Los assets tienen owner verificado y contrato público; rutas privadas nunca se filtran |
| I09 | La caché se particiona por tenant+host+versión, no solo URL o nombre de archivo |
| I10 | La política SEO y canonical depende de host validado, revisión aprobada y publicación real |
| I11 | La interfaz no convierte un rol visual, localStorage o claim de ruta en autoridad |
| I12 | Los errores para host desconocido/tenant B son seguros, no enumeran empresas ni despliegan stacks |
| I13 | Cambios de publicación y reversión conservan auditoría minimizada, correlación y versiones |
| I14 | Un fallo de proveedor no activa un binding alternativo a otra empresa; fail-closed |
| I15 | Migrations EF/MySQL, CDN, DNS y TLS no se autorizan por un diseño documental |
| I16 | Sin test A/B, seguridad y CI causal en SHA exacto, M03 no queda certificado |

Las 16 invariantes son **requisitos de implementación futura**, no resultados de pruebas. Sin MFA/reauth, permisos del publicador y propiedad de host validados en el servidor, no se permite una publicación real, incluso si un diseño visual existe o el editor pudo abrir una pantalla.

## 4. Fronteras de integración y dependencias del resto de M02

- **M02.02 API/DTO:** definir contratos de entrada/salida versionados para edición, consulta pública, preview, publicación y reversión; ausencia de un endpoint instalado es la expectativa actual.
- **M02.03 persistencia:** proponer claves/índices tenant-bound, restricciones de unicidad y migración aditiva reversible; no escribir DDL ni consultar Aiven.
- **M02.04 configuración:** feature flags seguros, defaults DENY, ámbitos de host, cache, indexación y límites; un flag no concede acceso.
- **M02.05 eventos:** solo términos de correlación, auditoría e idempotencia conceptual; **NO IMPLEMENTAR EVENTOS TODAVÍA**: no worker, webhook, dispatcher, broker ni Task adicional.
- **M02.06 terminal:** nueva evaluación de todos M02.01–05 LISTO+A5 y sus prerequisitos transversales, sin forzar SI ni omitir checks de código futuros.
- **M03 y M04:** .NET 10/Angular 22.2.1, HTTP, JWT/RBAC, tenancy A/B, MySQL/EF real con servicio efímero, pruebas Playwright si hay UI y Scope Lock exact-SHA; ningún artefacto M02 acredita estas verificaciones.

## 5. Oráculos futuros positivos y negativos — NO_EJECUTADOS

| Caso | Resultado positivo FUTURO | Rechazo negativo FUTURO |
| --- | --- | --- |
| PN01 | Resolver host previamente aprobado de tenant A | Host de B o forwarded spoofed no resuelve A |
| PN02 | Editor A redacta en revisión no pública | Editor de B o rol sin editar no modifica A |
| PN03 | Aprobador ajeno valida publicación CAS correcta | Editor que intenta autoaprobar o versión obsoleta se rechaza |
| PN04 | Preview A con grant vigente sirve contenido noindex | Anónimo/TTL expirado/revocado no obtiene borrador |
| PN05 | Página publicada usa assets públicos de A | Asset privado o de B no se serializa |
| PN06 | Cache A por host y versión devuelve solo contenido A | Solicitud B no recibe HTML/cache A |
| PN07 | Canonical y robots reflejan publicación aprobada | Sitemap no muestra borradores, previews ni hostname no autorizado |
| PN08 | Bloque tipado seguro se representa como contenido | HTML/JS arbitrario, traversal y entrada no saneada se rechazan |
| PN09 | Rollback lógico restaura revisión previa aprobada | Sin snapshot/versión compatible no se cambian datos |
| PN10 | Audit trail minimizado registra cambios permitidos | JWT/contraseña, URLs privadas o texto de error detallado no aparece |

Los diez pares anteriores no son tests Vitest, xUnit, HTTP, SQL ni Chromium ejecutados. Su aceptación en M02 se limita a inspección de la especificación y a la ausencia de contradicciones con la preparación M01. Para afirmar ausencia de BOLA/IDOR, exposición SEO, cruces de caché o fallos de transacción se exigirán experimentos de seguridad reproducibles por SHA sobre el runtime real **en entorno de CI efímero**.

## 6. Controles A0–A5 y rollback

**A0:** verificar dependencias M01.01–06 y A5, PLAN/COLA/DAG, contrato de entidades, falta de secretos y sección de CHANGELOG autocontenida. **A1 documental:** confirmar exactamente 16 entidades, 16 invariantes y diez pares P/N con relación a G01–G16 y RB01–RB08; no llamar tests ejecutados a los escenarios futuros. **A2:** tenant A/B, seguridad de Host/forwarded, editor/publicador RBAC, PII, CSP, caches y denegación. **A3:** compatibilidad conceptual con storefront, SEO y arquitectura Domain/Application/Infrastructure/API/UI sin implementación ni migración; `dotnet`, Angular, MySQL, Docker y Playwright = `NO_APLICA` al diff solo Markdown, **no PASS inventado**. **A4:** commit fast-forward/CAS `dev`, revisar blobs y diff, Scope Lock y procedencia V5 `completed/success` exact SHA y readback PLAN/COLA/DAG/BITACORA A:Z. **A5:** pendiente de una de las otras cinco Tasks Javier, nunca del autor DEV06.

**Prohibiciones:** no DNS/TLS, proxies, R2/Cloudflare, datos productivos, pagos, secretos, migraciones, QA/main/PROD, cambios a autorización ni séptima Task. **Rollback:** `git revert` normal tras verificar HEAD actual; evento compensatorio append-only si cambia el estado institucional, nunca force-push, reset ni edición de BITACORA histórica.
