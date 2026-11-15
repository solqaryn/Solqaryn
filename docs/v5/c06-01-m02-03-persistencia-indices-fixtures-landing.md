# SOLQARYN V16 — C06-01.M02.03: persistencia, índices y fixtures conceptuales de sitio público SaaS

**Microtarea:** C06-01.M02.03 — PLAN_MAESTRO fila 874. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio/rama:** `solqaryn/Solqaryn@dev`, **PROJECT_SCOPE_LOCK=STRICT**. **Clasificación V15:** R2 por frontera de datos/PII/concurrencia, solo **documental**, 2026-11-15.

## Alcance, autoridad y frontera ejecutable

Se propone un diseño de persistencia **FUTURO** para el sitio institucional: propuesta de valor, planes y sectores aprobados, demo sintética, contacto consentido, legal y SEO. Las familias y claves siguientes no son tablas reales ni decisiones arquitectónicas aprobadas. Esta entrega **NO cambia DbContext, EF, MySQL, DDL, migraciones, tablas, endpoints .NET, UI Angular, rutas, SMTP, datos, secretos ni permisos**. El shell ERP privado, storefront y separación entre empresas permanecen intactos. El GET público no confiere edición o publicación.

**Preflight:** PLAN H874=PENDIENTE e I874 vacío antes de claim, COLA G874/O874=SI y DAG A835 F=VALIDADO/G=SI. CAS dirigido H874 a EN_PROGRESO con I874=TASK_ID propio y readback antes de publicar. M01.06 tenía LISTO+A5 independiente; no se infieren decisiones de precio/legal ni autorización QA/PROD.

Referencias inspeccionadas en `dev`: M01.01 objetivo (blob `96d8280c96125af30b8f3f580c092460cb397b84`), M01.02 actores (`abc1084b024d32391559f02c365cf4111c5f739a`), M01.04 riesgos/decisiones (`c2b970feee1b2ab1d62349f21af394655070a77a`), M01.05 criterios (`c6e254f6728a00a220216875341f8dd61065e936`), M01.06 bloqueo/rollback (`5f13fec349767c5b6deccd8857eafd33109ae49d`), M02.01 entidades/ownership (`8d292a90b165251f4f9a77c43cc9791c02dfa2da`). M01.04 mantiene D01–D08 **PENDIENTES**: precios, legal, routes/SEO, contacto/antiabuso, demo A/B, UX y servicios externos. No duplicar autoridad de catálogo comercial.

## 1. Doce familias de persistencia P01–P12 — FUTURAS

| ID | Familia hipotética | Campos candidatos NO ejecutados | Owner/autoridad | Control |
| --- | --- | --- | --- | --- |
| P01 | SiteRevision | siteId, revision, versionState | Plataforma | Inmutable y aprobable |
| P02 | PageDraft | siteRevision, routeKey, draftVersion | Plataforma | No visible al visitante anónimo |
| P03 | PagePublication | routeKey, approvedRevision, releaseAt | Plataforma | Solo proyección pública autorizada |
| P04 | PublicRoute | hostBinding, locale, slug | Plataforma | Nunca colisionar con ERP/storefront |
| P05 | ContentBlock | draft, componentKey, schemaRevision | Editor autorizado | Allowlist y campos tipados |
| P06 | CommercialClaim | claimId, sourceRevision, validUntil | Comercial | Sin prestaciones/precios inventados |
| P07 | PlanReference | catalogId, catalogRevision | Catálogo aprobado | No duplicar precio como fuente |
| P08 | LegalRevision | legalId, jurisdiction, approvedAt | Jurídico | Estado pendiente no publicable |
| P09 | SeoManifest | siteRevision, canonicalHost, robotVersion | Editorial | Solo URLs públicas allowlisted |
| P10 | DemoRequest | requestId, expiresAt, scope | Solicitante + backend | Fixtures sintéticos y TTL |
| P11 | ContactLead | contactId, purpose, retentionUntil | Consentimiento + backend | PII mínima y rate limit |
| P12 | PublicationAudit | eventId, actor, revision, timestamp | Backend | Append-only e idempotencia |

Las relaciones propuestas son: sitio 1:N revisiones; revisión 1:N borradores y bloques; publicación referencia revisión aprobada inmutable; rutas vinculan únicamente host/locale autorizados; referencias de planes requieren catálogo externo aprobado; contacto vincula propósito+consentimiento; demo solo fixtures sintéticos aislados, jamás tenant ERP; auditoría inmutable. No asumir entidad ni tabla nueva sin etapa de implementación habilitada.

## 2. Doce índices/constraints IDX01–IDX12 — FUTUROS

| ID | Familia | Índice candidato, no creado | Prueba que faltará |
| --- | --- | --- | --- |
| IDX01 | SiteRevision | (siteId, revision) UNIQUE | Evitar doble versión; usar CAS server-side |
| IDX02 | PageDraft | (siteRevision, routeKey, draftVersion) UNIQUE | No sobrescribir borrador obsoleto |
| IDX03 | PagePublication | (routeKey, approvedRevision) UNIQUE | Conservar historial, no garantizar por sí solo publicación activa |
| IDX04 | PublicRoute | (hostBinding, locale, slug) UNIQUE condicionado | Validar colación y host confiable |
| IDX05 | ContentBlock | (draft, ordinal) UNIQUE | Orden determinista y cardinalidad acotada |
| IDX06 | CommercialClaim | (claimId, sourceRevision) UNIQUE | Trazabilidad fuente y vencimiento |
| IDX07 | PlanReference | (catalogId, catalogRevision) | No desnormalizar precio como autoridad |
| IDX08 | LegalRevision | (jurisdiction, legalId) | Aprobación y vigencia por jurisdicción |
| IDX09 | SeoManifest | (siteRevision, canonicalHost) | No indexar ERP/URLs con tokens |
| IDX10 | DemoRequest | (requestId, expiresAt) | Expiración y deduplicación controlada |
| IDX11 | ContactLead | (contactId, retentionUntil) | Retención y purga autorizada |
| IDX12 | PublicationAudit | (eventId) UNIQUE | Idempotencia y auditoría inmutable |

Una clave única por revisión histórica **no** resuelve la restricción de una sola publicación ACTIVA: la implementación deberá comprobar atomicidad mediante transacción, control de versión/CAS y rollback. Índices por expiración no realizan purga ni cumplen consentimiento por sí solos. No establecer colaciones/cotas ni fechas arbitrarias.

## 3. Doce etapas MIG01–MIG12 y rollback futuro — NO EJECUTADAS

| ID | Requisito verificable antes de implementar persistencia EF/MySQL |
| --- | --- |
| MIG01 | Congelar baseline schema, versión provider, plan de rollback y permisos antes de DDL |
| MIG02 | Probar proveedor Oracle EF Core sobre MySQL 8.4 efímero, sin Aiven real |
| MIG03 | Crear exclusivamente estructuras aditivas nuevas; no reescribir datos ERP |
| MIG04 | Asignar host/tenant desde fuente backend confiable, nunca header visitante |
| MIG05 | Instalar constraints tras comprobar cardinalidad y unicidad con fixtures |
| MIG06 | Restringir lectura anónima a proyección aprobada y DTO mínimo |
| MIG07 | Transacción + CAS para cambiar publicación activa y crear evento de auditoría |
| MIG08 | Pruebas de concurrencia con dos editores sin lost update ni efectos parciales |
| MIG09 | Consentimiento, redacción, TTL y fixtures sintéticos demo/contacto |
| MIG10 | Ensayar rollback DDL y restauración de versión sin perder historial |
| MIG11 | Tests reales Category=Integration contra MySQL efímero con 0 FAIL, seguridad A/B |
| MIG12 | Cerrar solo con CI causal exact-SHA, Scope Lock, A5 independiente y permiso de entorno |

No se ha invocado `dotnet ef`, abierto MySQL, creado schemas ni leído datos reales. El proveedor declarado para Infrastructure es Oracle `MySql.EntityFrameworkCore` y EF Core 10; no sustituirlo por proveedor externo de una documentación antigua. Un build Release verde no acredita integración: cuando corresponda exigir job CI real con MySQL 8.4 efímero y filtro `Category=Integration` comprobado en logs, cero FAIL, migración y rollback verificables. Una migración fallida bloquea sólo su scope y preserva evidencia.

## 4. Diez pares FX01–FX10 de fixtures de aceptación FUTUROS

| ID | Caso positivo aún NO ejecutado | Caso negativo aún NO ejecutado |
| --- | --- | --- |
| FX01 | Borrador válido referencia revisión y ruta permitida | Borrador sin versión o sobre ruta ERP falla |
| FX02 | Publicación aprobada genera proyección anónima mínima | Un borrador privado no es visible anónimamente |
| FX03 | Revisión anterior permanece tras nueva publicación | CAS stale no sobreescribe versión reciente |
| FX04 | Claim comercial autorizado se referencia por versión | Precio/beneficio sin aprobación se deniega |
| FX05 | Contacto consentido se almacena con TTL | Sin consentimiento/rate limit no persiste ni envía |
| FX06 | Demo contiene solo fixtures sintéticos segregados | Acceso tenant B/PROD o TTL vencido se deniega |
| FX07 | Sitemap/canonical reflejan páginas públicas | Dashboard o URL con secreto no se indexa |
| FX08 | Publicador autorizado registra auditoría append-only | Actor revocado no publica ni elimina bitácora |
| FX09 | Rollback genera nueva revisión segura auditable | Rollback sin rol no altera estado |
| FX10 | Publicación pública con host confiable permanece aislada | Host/slug de B no proyecta datos del tenant A |

**Ejecución efectiva:** 0/10 casos MySQL/API/Playwright. **Especificación:** 10/10 parejas redactadas. No declarar PASS de pruebas no corridas. Cualquier futuro test debe ser capaz de fallar ante defecto de tenancy/IDOR, consentimiento, CAS, rollback, revocación, host, SEO o fuga de PII.

## 5. Integridad, seguridad y calidad A0–A5

**A0:** impacto limitado al archivo de este contrato y sección autocontenida de `CHANGELOG_AI.md`. No introducir infraestructura/DB, privilegios, cuentas, producción o arquitectura nueva. **A1:** revisar 12 familias, 12 índices, 12 etapas, 10 parejas P/N con negativos relevantes. **A2:** autoridad exclusivamente backend de JWT/RBAC/membresía, aislamiento A/B, logs mínimos y consentimiento; casos adversariales **requisitos futuros** y tests de runtime NO_APLICA al diff Markdown. **A3:** consistencia con M01/M02.01 y fuente institucional; integración MySQL 8.4, HTTP y Angular NO_APLICA hoy, obligatorios al cambiar código/EF/UI. **A4:** commit fast-forward/CAS en `dev`, Scope Lock y procedencia V5 del SHA exacto `completed/success`, verificación de diff y readbacks PLAN/COLA/DAG/BITACORA. SKIPPED/CANCELLED/IN_PROGRESS nunca PASS requerido. **A5:** revisión real independiente por otro TASK_ID Javier, con autoridad para A5_FAIL; no autoprobación.

**Rollback de esta entrega:** `git revert` aditivo del commit documental con comprobación de HEAD, no modificación/restore DB. El rollback futuro de datos requiere ensayo MySQL efímero, backup, autorización y CI aparte. No liberar M02.06/M03 sin dependencia A5 real. QA/main/PROD, secretos, contratos comerciales y precios continúan excluidos.
