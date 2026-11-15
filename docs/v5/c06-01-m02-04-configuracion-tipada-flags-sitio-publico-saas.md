# SOLQARYN V16 — C06-01.M02.04: configuracion tipada, ambitos y feature flags seguros

**Microtarea:** C06-01.M02.04; PLAN_MAESTRO fila 875. **Owner:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repo:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Riesgo V15:** R0 documental sin runtime. **Fecha:** 2026-11-15.

## 1. Alcance y requisito atómico

Esta unidad **define contratos futuros de configuracion** para landing institucional SOLQARYN (propuesta de valor, planes, sectores, demo, contacto, legal y SEO). No modifica `frontend/`, `backend/`, rutas, DTO, feature flags reales, variables, base MySQL, JWT, permisos, CDN, DNS, SMTP, API, workers ni deploy. Las opciones OPT y validaciones V son propuestas falsables, **no codigo ni pruebas ejecutadas**.

El frontend Angular 22.2.1 con Signals/Material/CDK y Zone.js permanece separado de ASP.NET Core net10.0 JWT/RBAC y control tenant A/B; EF Core 10.0.12 y MySQL Oracle 8.4 son infraestructura no modificada. Un feature flag de UI no crea un permiso de servidor. La ruta raíz existente a login y el storefront actual no acreditan una landing institucional lista.

**Preflight:** PLAN H875=PENDIENTE/I875 vacío, COLA G875/O875=SI, DAG F836=VALIDADO/G836=SI en la lectura de reclamación; la apropiación individual de H875/I875 se registra mediante evento BITACORA y CAS dirigido. A diferencia de código, la preparacion M02 puede avanzar documentalmente según DAG sin asumir que otras M02 estén LISTO o su A5; M03 y sucesoras técnicas siguen sus propios prerrequisitos.

## 2. Fuentes y contratos existentes

- `docs/v5/c06-01-m02-01-entidades-ownership-invariantes-sitio-publico-saas.md` — blob `8d292a90b165251f4f9a77c43cc9791c02dfa2da`; E01–E14 e INV01–INV18 como modelo conceptual, sin tablas EF.
- `docs/v5/c06-01-m01-05-criterios-aceptacion-sitio-publico-saas.md` — blob `c6e254f6728a00a220216875341f8dd61065e936`; AC01–AC14 y N01–N14 **FUTUROS_NO_EJECUTADOS**.
- `docs/v5/c06-01-m01-04-riesgos-supuestos-decisiones-sitio-publico-saas.md` — blob `c2b970feee1b2ab1d62349f21af394655070a77a`; R01–R12 y decisiones D01–D08 PENDIENTES; los valores comerciales/legales no han sido aprobados aquí.
- `docs/v5/c06-01-m01-06-bloqueo-rollback-liberacion-sitio-publico-saas.md` — blob `5f13fec349767c5b6deccd8857eafd33109ae49d`; BL01–BL16 y REV01–REV08 son puertas futuras, no pruebas de publicación ni consentimiento.

Fuentes de requisitos comerciales y legales exigen aprobación trazable; no inferir valores de catálogos existentes ni fotos de producto. Si cambian artefactos, componentes o permisos, volver a inspeccionar blobs y consumidores antes de una implementación.

## 3. Catorce opciones de configuracion tipada OPT01–OPT14

| ID | Clave conceptual | Ambito de control | Tipo/valor permitido futuro | No-regresion |
| --- | --- | --- | --- | --- |
| OPT01 | SitePublicationState | PLATAFORMA | DRAFT|REVIEWED|APPROVED|ACTIVE|DISABLED | Nunca ACTIVE por flag de cliente; exige gates previos |
| OPT02 | PublicRouteAllowlist | PLATAFORMA | lista de rutas declarativas | No incluir dashboard, /api privadas, token o URL libre |
| OPT03 | ApprovedMarketingCopyRevision | PLATAFORMA | revision/version aprobada | Sin claims comerciales no autorizados |
| OPT04 | PlanCatalogRevision | PLATAFORMA | referencia opaca de catalogo vigente | No crear precio, moneda o impuesto |
| OPT05 | SectorCatalogRevision | PLATAFORMA | version de lista allowlisted | No revelar sector o suscripcion ajena |
| OPT06 | DemoAvailability | PLATAFORMA | DISABLED|ISOLATED_APPROVED | Sin fixtures, TTL y tenant aislado permanece DISABLED |
| OPT07 | DemoExpirationMinutes | PLATAFORMA | entero positivo limitado | No sesiones ilimitadas ni datos reales |
| OPT08 | ContactFormState | PLATAFORMA | DISABLED|APPROVED | No enviar SMTP sin contrato consentido y rate limit |
| OPT09 | ContactRateLimitPolicy | BACKEND | perfil versionado de control antispam | No permitir valor infinito o bypass client |
| OPT10 | ConsentPolicyRevision | PLATAFORMA | version juridica aprobada | Sin aprobacion previa bloquear captura de leads |
| OPT11 | SeoCanonicalOrigin | PLATAFORMA | origen HTTPS allowlisted | Rechazar open redirect y host no autorizado |
| OPT12 | SitemapPublicRouteFilter | PLATAFORMA | allowlist positiva | Nunca indexar ERP o token de acceso |
| OPT13 | PublicMediaPolicy | PLATAFORMA | assets internos allowlisted/tamano maximo | No terceros con cookies, PII o secretos |
| OPT14 | AccessibilityGateVersion | PLATAFORMA | identificador de suite y SHA | No declarar contraste o WCAG sin ejecucion real |

Cada clave debe implementarse en código con esquema versionado, validación del servidor, defaults seguros y tests por cada par. No se crean nombres de variables ejecutables aquí; ni se escriben valores en CONFIG institucional para saltarse DAG.

## 4. Dieciseis reglas de validacion V01–V16

| ID | Clase | Obligacion exigida |
| --- | --- | --- |
| V01 | Origen de autoridad | Solo fuente versionada y aprobacion de backend; no window/localStorage/cookie arbitraria |
| V02 | Privacidad | No almacenar JWT, tokens, PII, datos de A o B en config publica |
| V03 | No-grants | Flags y planes nunca confieren RolPermiso, tenant ni acceso ERP |
| V04 | Anonimo | Rutas publicas no reutilizan guard privado ni exponen estado de sesion |
| V05 | Precedencia | Valores mas restrictivos prevalecen ante conflicto o fuente ausente |
| V06 | Revocacion | Deshabilitar inmediatamente demo/contacto ante perdida de autorizacion |
| V07 | Multiempresa | La configuracion institucional no mezcla branding privado A/B |
| V08 | Versionado | CAS/revision monotona para publicacion y rollback determinista |
| V09 | Idempotencia | Replay mismo payload da resultado estable sin side effects |
| V10 | Tipo | Rechazar bool como string, enteros negativos, null ambiguo y enums desconocidos |
| V11 | SEO | Canonical HTTPS y sitemap solo con origen/rutas allowlisted |
| V12 | Seguridad formulario | Consentimiento, anti-XSS, rate-limit y errores neutros obligatorios |
| V13 | Demo | Fixtures ficticios y TTL aislado de PROD obligatorio antes de ENABLED |
| V14 | A11y | Foco, ARIA, 320px, motion y contraste se miden con browser real |
| V15 | Recuperacion | Error de config reduce funcionalidad a DISABLED sin grants implicitos |
| V16 | Causalidad | No tratar CANCELLED/SKIPPED/QUEUED/IN_PROGRESS como PASS required |

Ante incertidumbre o proveedor no verificado, el resultado seguro es **DISABLED**; no reutilizar un valor obsoleto de sesión, de caché o del tenant anterior. Las reglas V05/V08/V09 requieren pruebas reales de concurrencia cuando sean implementadas; una tabla no demuestra CAS ni transaccion.

## 5. Matriz de ocho pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Positiva futura | Negativa/frontera futura |
| --- | --- | --- |
| P01/N01 | Copy/catálogo aprobados muestran misma revisión | Precio o claim no aprobado bloquea publicación |
| P02/N02 | Ruta pública explícita visible a anónimo | Dashboard/tenant privado rechaza 401/403 |
| P03/N03 | Demo aislada expira por TTL | Fixtures tenant B/PROD o TTL ausente deniegan |
| P04/N04 | Lead válido con consentimiento + rate-limit | XSS/429/payload sin consentimiento no persiste |
| P05/N05 | Canonical y sitemap allowlisted | URL externa o ERP en sitemap rechazada |
| P06/N06 | Flags revocadas deniegan operación | Cliente manipula flag y no concede permiso RBAC |
| P07/N07 | Configuración v2 idempotente con CAS | Replay cruzado o revisión obsoleta da conflicto |
| P08/N08 | UI accesible 320px, foco y motion reducido | Mock/documento no certifica Playwright ni WCAG |

Los 8 pares representan objetivos futuros y **0 ejecuciones** de Vitest, xUnit, Angular build, Playwright, HTTP o MySQL. Los controles de acceso backend y la matriz A/B son obligatorios en la futura implementación R2; no se atribuye PASS a un test que no existe.

## 6. Puertas A0–A5, dependencias y rollback

- **A0:** revisar el requisito C06-01.M02.04 y las cuatro fuentes/blobs, diff de este Markdown + UNA sección CHANGELOG, riesgo R0, no-regresión y cobertura de consumidores.
- **A1:** comprobar manualmente consistencia de 14 OPT, 16 V y ocho pares futuros, todos etiquetados como no ejecutados; no es aprobación funcional del sitio.
- **A2:** seguridad JWT/RBAC, tenant A/B, revocación, secretos, consentimiento y SEO se documentan fail-closed; nuevos tests de seguridad runtime **NO_APLICA** para diff Markdown exclusivamente.
- **A3:** EF/MySQL, HTTP, SMTP, Angular/Playwright y proveedores **NO_APLICA** porque no se ejecutó modificación de integración ni DB; un check auxiliar no demuestra un servicio o una UI.
- **A4:** publicación fast-forward/CAS en dev, comparar blobs/diff, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` **completed/success exact-SHA**, readback PLAN/COLA/DAG/BITACORA. Checks requeridos FAIL/CANCELLED/SKIPPED/in_progress nunca PASS.
- **A5:** A5_PENDIENTE, revisión auténtica de Task Javier distinta de autor, con posibilidad de A5_FAIL; LISTO_DEV documental no habilita QA/main/PROD.

**Salida:** contrato de configuración y flags para futura implementación, sin cambiar settings actuales ni autorizar publicidad, precios, legal, contacto, demo, SEO, DB, secretos o producción. **Rollback real de esta entrega:** git revert normal del commit documental y su sección CHANGELOG; sin reescritura de historia, sin tocar BITACORA histórica. Una M02.05/M02.06 o M03 sigue su propio DAG; no forzar COLA G/O a SI.
