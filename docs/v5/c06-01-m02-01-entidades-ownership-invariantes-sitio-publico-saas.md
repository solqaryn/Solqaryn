# SOLQARYN V16 — C06-01.M02.01: entidades, ownership, relaciones e invariantes de sitio público SaaS

**Microtarea:** C06-01.M02.01 — PLAN_MAESTRO fila 872. **Autor/owner:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Única autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo V15:** R0 documental, sin runtime. **Fecha:** 2026-11-15.

## 1. Alcance real y exclusiones

Fija **entidades conceptuales, propiedad, referencias y reglas de invariantes** para la futura landing institucional de SOLQARYN: propuesta de valor, planes, sectores, demo, contacto, legales y SEO. M02.01 es un contrato técnico, **no es un diseño de tabla EF ejecutado** ni una migración; no crea clases .NET, routes Angular, controllers, DTO, formulario, SMTP, indexación, scripts, cookies, APIs, pagos, planes comerciales, tablas, host, SEO publicado o tenants. Ninguna referencia conceptual indica que la superficie web exista ya. El sitio actual mantiene raíz que redirige a login, `tienda/*` y ERP con guards sin intervención de esta unidad.

**Preflight:** PLAN H872=PENDIENTE/I872 vacío antes del CLAIM individual de esta Task, COLA G872/O872=SI y DAG A833/F833=VALIDADO/G833=SI; se realizó CAS dirigido H872 a EN_PROGRESO y readback I872. Las dependencias M01.01–.06 con sus A5 actuales permiten **documentar M02** exclusivamente; no autorizan publicar landing ni contratos comerciales pendientes. No reclamar otra EN_PROGRESO mientras esta unidad permanezca abierta.

## 2. Fuentes originales cotejadas en dev

Los blobs se compararon en el HEAD previo a esta entrega. Sus referencias son evidencia de diseño documental, no pruebas E2E ni aprobación comercial.

| Unidad fuente | Ruta | Git blob verificado |
| --- | --- | --- |
| M01.01 objetivo/NR | docs/v5/c06-01-m01-01-objetivo-no-alcance-sitio-publico-saas.md | 96d8280c96125af30b8f3f580c092460cb397b84 |
| M01.02 actores/interfaces | docs/v5/c06-01-m01-02-actores-interfaces-sitio-publico-saas.md | abc1084b024d32391559f02c365cf4111c5f739a |
| M01.03 inventario | docs/v5/c06-01-m01-03-inventario-codigo-evidencia-sitio-publico-saas.md | e3d90f0f7f49da2360b11120139ff4003d4ae50e |
| M01.04 riesgos/decisiones | docs/v5/c06-01-m01-04-riesgos-supuestos-decisiones-sitio-publico-saas.md | c2b970feee1b2ab1d62349f21af394655070a77a |
| M01.05 aceptación | docs/v5/c06-01-m01-05-criterios-aceptacion-sitio-publico-saas.md | c6e254f6728a00a220216875341f8dd61065e936 |
| M01.06 salida/rollback | docs/v5/c06-01-m01-06-bloqueo-rollback-liberacion-sitio-publico-saas.md | 5f13fec349767c5b6deccd8857eafd33109ae49d |

M01.04 deja pendientes las decisiones D01 (claims y planes), D02 (legal/consentimiento), D03 (rutas/SEO), D04 (contacto seguro), D05 (demo A/B), D06 (accesibilidad/rendimiento), D07 (medios/terceros) y D08 (releases/rollback). Ninguna se asume resuelta.

## 3. Modelo conceptual E01–E14

**No existen tablas ni contratos ejecutables creados aquí.** Los nombres son términos de diseño, sujetos a revisión de API, datos y seguridad en sus propias microtareas futuras.

| ID | Entidad propuesta | Propósito | Autoridad/propiedad | Relación o invariante |
| --- | --- | --- | --- | --- |
| E01 | PublicSiteRevision | Versión de sitio institucional aprobable | Plataforma, no tenant visitante | Identificador inmutable, revisión, estado borrador/publicación |
| E02 | PublicPageDraft | Composición tipada de una página futura | Plataforma editorial | Referencia a revisión, sin HTML ejecutable ni publicabilidad implícita |
| E03 | PublicPagePublication | Proyección pública aprobada | Plataforma editorial | Enlace a revisión autorizada, aprobación, ventana y rollback |
| E04 | PublicPageRoute | Ruta pública y alias autorizados | Plataforma/operaciones | Slug único en ámbito de host, no colisión con ERP/storefront |
| E05 | PublicContentBlock | Bloque versionado del catálogo precompilado | Plataforma editorial | Clave allowlisted, propiedades tipadas, orden acotado |
| E06 | ApprovedCommercialClaim | Atributo comercial verificable | Responsable comercial | Fuente certificada, versión, vigencia, aprobación explícita |
| E07 | CommercialPlanReference | Referencia a plan y prestaciones aprobadas | Catálogo comercial autorizado | Solo ID/versión, nunca fabricar moneda, precio ni contrato |
| E08 | SectorUseCase | Texto/sector de muestra editorial | Plataforma editorial | Datos sintéticos; no testimonios de clientes sin permiso |
| E09 | DemoRequest | Intención de demo mínima | Solicitante público; custodia backend | Identificador no enumerables, TTL, consentimiento, deduplicación |
| E10 | DemoFixtureScope | Aislamiento de demo eventual | Plataforma segregada | Solo fixtures sintéticos y caducidad; sin datos A/B reales |
| E11 | ContactLead | Contacto minimizado, no sesión ERP | Visitante con consentimiento | Campos mínimos, retención acotada, rate limit y secreto fuera de repo |
| E12 | ContactConsent | Evidencia de consentimiento y versión legal | Persona contactante | Propósito, fecha, versión; nunca consentimiento implícito |
| E13 | LegalContentRevision | Versión de términos, privacidad y avisos | Responsable jurídico/privacidad | Jurisdicción, aprobación y vigencia antes de publicar |
| E14 | SeoManifestRevision | Metadata, canonical, robots/sitemap propuestos | Plataforma/editorial | Allowlist de URLs públicas; jamás incluir rutas ERP/PII |

### Relaciones y ámbitos propuestos (futuros)

`PublicSiteRevision (E01) 1:N PublicPageDraft (E02)`; cada E02 usa N bloques E05, referidos por catálogo tipado/versionado. `PublicPagePublication (E03)` referencia exactamente una revisión aprobada E01 y un conjunto explícito E04 de rutas públicas. Cada E06/E07/E08 que alimente páginas públicas exige aprobación de producto/comercial y versión de fuente; una referencia no se convierte automáticamente en precio contractual. E14 enlaza solo E04 publicadas/allowlisted. E09/E10 son ámbitos de demo sintética **diferentes del tenant empresarial**. E11/E12 vinculan contacto y consentimiento con propósito explícito y retención mínima; E13 identifica versión legal aplicada.

**Frontera de ownership:** contenido de marketing público autorizado pertenece a la entidad administradora de la plataforma, no a cualquier `empresaId` enviado por cliente. No inferir que un usuario anónimo o cuenta de un tenant puede editar/publicar contenido global. Las referencias de demo/lead no transfieren permisos de ERP, ni un host/slug se convierte en un tenant confiable. El control de acceso final se aplica server-side con JWT/RBAC, membresía, propiedad y auditoría.

## 4. Invariantes INV01–INV18 (verificables en implementación futura)

| ID | Regla que debe bloquear defectos futuros |
| --- | --- |
| INV01 | Solo una revisión publicada activa por ruta/host aprobado; versiones históricas inmutables |
| INV02 | La ruta raíz privada/ERP y los guards actuales no se alteran por contratos de landing |
| INV03 | Lectura anónima exclusivamente a proyección pública aprobada, no a borradores ni API privada |
| INV04 | La UI Angular no concede JWT/RBAC ni ownership; permisos se verifican en backend |
| INV05 | Tokens, empresas y recursos de tenant A nunca se leen ni filtran desde tenant B, incluso por slug/cache |
| INV06 | Planes/precios/monedas son referencias a catálogo vigente aprobado, nunca valores inventados |
| INV07 | Claims, referencias legales y testimonios requieren procedencia, aprobación y vigencia explícitas |
| INV08 | Contacto solicita mínimos y consentimiento específico; no actúa con permiso ausente |
| INV09 | Contacto y demo requieren rate limit, idempotencia, TTL y rechazo de abuso antes de cualquier backend real |
| INV10 | Demo usa fixtures sintéticos segregados; acceso a datos empresariales o productivos siempre NO_GO |
| INV11 | HTML/JS/CSS libre, handlers, SQL ejecutable y URL peligrosa no se derivan de campos editoriales |
| INV12 | Host, canonical, redirect y sitemap están allowlisted y no enumeran rutas protegidas |
| INV13 | Errores y logs no revelan tokens, PII, stack traces, queries SQL ni IDs de otra empresa |
| INV14 | Publicación, edición y rollback exigen permiso de operación/versión, auditoría y CAS exacto |
| INV15 | Rollback agrega una revisión trazable o vuelve a versión segura sin perder historia ni pisar escritura concurrente |
| INV16 | No instalar trackers/cookies/SMTP ni conectar proveedores sin controles y autorización específica |
| INV17 | Accesibilidad teclado, 320px, foco/ARIA y reduced-motion son gates medidos de futura UI, no claims de este documento |
| INV18 | QA/main/PROD, DNS, secretos, precios, decisiones D01-D08 y permisos quedan fuera del alcance y bloqueados |

Los invariantes no implican que haya tests ejecutados. Requieren matrices de validación API, frontend, privacidad/consentimiento, acceso A/B y pruebas de concurrencia cuando exista código.

## 5. Diez pares de aceptación P/N FUTUROS_NO_EJECUTADOS

| Par | Positiva FUTURA | Negativa FUTURA (debe fallar) |
| --- | --- | --- |
| PN01 | Revisión aprobada referencia claim firmado vigente | Claim sin fuente, precio supuesto o vigencia expirada se rechaza |
| PN02 | Ruta pública autorizada sirve publicación mínima | Ruta ERP privada o slug no permitido no se expone |
| PN03 | Bloque tipado conocido mantiene texto como datos | Script/handler/HTML libre no se convierte en ejecución |
| PN04 | Visitante solicita contacto con consentimiento válido | Sin consentimiento o exceso de frecuencia no persiste ni envía |
| PN05 | Demo solo lee fixtures sintéticos con TTL | Demo alcanza datos reales/tenant B o TTL vencido: denegar |
| PN06 | Publicación usa permiso y revisión vigente | Sin rol o CAS desactualizado: rechazar sin overwrite |
| PN07 | Robots/sitemap contiene únicamente rutas públicas | Dashboard o URL con token se excluye/rechaza |
| PN08 | Legal aprobado con jurisdicción se presenta | Texto provisional/fecha vencida no se publica |
| PN09 | Logs de contacto minimizan datos sensibles | Token, PII y recurso B no aparecen en respuesta ni logs |
| PN10 | Pruebas a11y/320px/reduced-motion pasan con build futuro | Regresión foco, overflow o motion forzado bloquea release |

**Resultados de runtime de esta unidad: NO_MEDIDO / NO_EJECUTADO.** Hay 10 parejas redactadas, no 10 PASS de API/MySQL/Angular/Playwright. La existencia de una especificación no sustituye un test de aceptación.

## 6. Decisiones abiertas y gates de próximas microtareas

- **M02.02 API/DTO** deberá definir esquema y errores sin resolver D01–D08 por inferencia. Ningún endpoint o autorización debe añadirse sin pruebas y alcance.
- **M02.03 persistencia** será conceptual hasta reclamar y aprobar una microtarea de implementación; si se modifica EF/MySQL, requerirá MySQL 8.4 efímero con `Category=Integration`, migración y rollback reales.
- **M02.04 configuración** no habilitará flags, SMTP, host ni terceros por escribir tipos.
- **M02.05 eventos** no activará webhooks ni colas; correlación e idempotencia son contratos futuros.
- **M02.06 terminal** requerirá A5 y prerequisitos reales antes de liberar M03; evitar usar LISTO documental como prueba de despliegue.
- La futura implementación R2 deberá demostrar JWT/RBAC, tenant A/B, PII, rate limits, idempotencia, 401/403 y CAS/rollback en pruebas negativas; frontend con Playwright/Chromium si cambia UI y métricas reales si promete rendimiento.

## 7. Control A0–A5 por riesgo y rollback de la entrega

**A0:** ID exacto, seis fuentes con hashes conservados, alcance de diff limitado a este archivo y una sección autocontenida de `CHANGELOG_AI.md`; sin cambios en framework, API, DB, IAM, UI, secretos o producción. **A1:** 14 entidades, 18 invariantes y diez casos P/N FUTUROS_NO_EJECUTADOS coherentes con M01. **A2:** fronteras de autoridad server-side y privacidad fail-closed conservadas, pruebas reales A/B **NO_APLICA** al diff Markdown R0. **A3:** coherencia con M01 y Standard V5; API, MySQL, SMTP, Angular, Playwright y HTTP **NO_APLICA** a contrato documental. **A4:** exigir commit fast-forward/CAS en `dev`, Scope Lock y procedencia V5 `completed/success` sobre SHA publicado, readback del ID y evento BITACORA idempotente. **A5:** posterior y exclusivamente independiente de otro TASK_ID Javier, con facultad de A5_FAIL; sin QA/main/PROD.

**Rollback del cambio documental:** `git revert` normal de su commit y confirmación de HEAD, sin borrar historia, forzar ramas o alterar datos reales.
