# SOLQARYN V5 — C06-02.M02.03: persistencia, índices, migraciones y fixtures de landing por tenant

**Naturaleza:** contrato técnico documental M02.03, NO implementación de tablas, migraciones, seeders, resolutores de host, API o publicación. **Autor:** PRIMARY DEV 05 JAVIER_CHATGPT, TASK_ID 6adc2e55f47c81919f6374e561687076. **Rama:** solqaryn/Solqaryn dev; PROJECT_SCOPE_LOCK=STRICT; 2026-11-15.

## 1. Autoridad, alcance y dependencias

Fuente única: Standard de Implementación de Automatizaciones V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA) y PLAN_MAESTRO fila 910, COLA, DAG_TECNICO. La verificación de esta unidad requiere H=PENDIENTE, owner vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI antes del reclamo. El prerequisito DAG es C06-02.M01.01–06 con A5 independiente; M02.01 define dieciséis entidades/invariantes y M01.06 documenta G01–G16 y RB01–RB08. La M02.02 de otro responsable no es una precondición DAG de esta entrega; no alterar sus archivos ni sus estados.

**Resultado exigido:** especificar persistencia, índices, migraciones reversibles y datos de prueba futuros para landing pública por empresa con host aprobado, contenido versionado, preview no indexable, publicación separada, SEO y caché aislada. El estado LISTO_DEV de este documento no instala una landing ni prueba seguridad de runtime. Backend ASP.NET Core 10, EF Core 10.0.12/MySql.EntityFrameworkCore 10.0.9, MySQL 8.4 y Angular 22.2.1 solo son fronteras futuras.

## 2. Contrato lógico de persistencia propuesto (NO DDL ejecutado)

Todas las referencias a TenantId deben corresponder a identidad de empresa verificada server-side. Las PK de recursos son opacas, sin autoridad propia. Las relaciones con recursos tenant-bound usarán claves compuestas (TenantId, ResourceId) donde resulte posible; un ID de otra empresa jamás debe resolverse con solo ResourceId.

| ID | Estructura lógica prevista | Clave, relación y protección |
| --- | --- | --- |
| P01 | LandingSite | PK (TenantId, SiteId); estado y schemaVersion; no resolver tenant mediante Host o slug |
| P02 | ApprovedHostBinding | PK HostBindingId; host canonicalizado UNIQUE cuando aprobado; TenantId y prueba de propiedad aprobada, revocación y versión CAS |
| P03 | LandingRevision | PK (TenantId, SiteId, RevisionId), FK a P01; draft inmutable una vez publicado |
| P04 | LandingPage | PK (TenantId, RevisionId, PageId); rutas normalizadas por revisión; FK compuesta a P03 |
| P05 | LandingBlock | PK (TenantId, PageId, BlockId); orden y tipo de bloque en esquema cerrado; FK compuesta a P04 |
| P06 | LandingAssetLink | PK (TenantId, AssetLinkId); referencia opaca a medio con ownership comprobado, sin firma privada persistida |
| P07 | PreviewAuthorization | PK (TenantId, PreviewId); actor autorizado, hash de token, expiración UTC, revocación y revisión fija |
| P08 | PublicationApproval | PK (TenantId, ApprovalId); editor y publicador distintos, versión esperada y decisión auditable |
| P09 | ActivePublication | PK (TenantId, SiteId), puntero CAS a revisión aprobada, versión anterior y timestamp |
| P10 | PublishedRoute | PK (TenantId, SiteId, RouteId); canonical/robots/sitemap solo de publicación aprobada |
| P11 | SeoProjection | PK (TenantId, SiteId, RevisionId); canonical, robots, fingerprint de contenido público, sin preview |
| P12 | LandingChangeAudit | PK (TenantId, EventId); actor minimizado, acción, causalidad, hash de versión y UTC, sin JWT ni PII completa |
| P13 | SiteCacheNamespace | PK (TenantId, SiteId, PublicationVersion); clave lógica tenant+host aprobado+versión con TTL/revocación |

El HostBinding es una solicitud de mapeo, no un grant de acceso; X-Forwarded-Host solo se acepta tras proxy confiable validado en una etapa posterior. Las restricciones de integridad impedirán referencias cruzadas A/B incluso ante identificadores opacos correctos de otra empresa. No usar TenantId nulo como bypass de plataforma, ni permitir que la UI establezca ownership.

## 3. Índices y restricciones propuestos, pendientes de medición

| ID | Clave/index conceptual | Motivo y prueba futura |
| --- | --- | --- |
| IX01 | UNIQUE ApprovedHostBinding(CanonicalHost) WHERE ApprovedAndActive | Un host aprobado no se vincula a dos tenants; si MySQL no soporta índice parcial, usar columna generada/alternativa demostrada, no sintaxis imaginaria |
| IX02 | UNIQUE LandingSite(TenantId, SiteId) | Lecturas y FK tenant-bound sin ambigüedad |
| IX03 | UNIQUE LandingRevision(TenantId, SiteId, RevisionNumber) | Conflicto optimista de versión, sin último escritor silencioso |
| IX04 | UNIQUE LandingPage(TenantId, RevisionId, NormalizedRoute) | Colisiones de rutas bloqueadas dentro de versión |
| IX05 | INDEX LandingBlock(TenantId, PageId, Ordinal) | Orden reproducible, sin búsqueda global de bloques |
| IX06 | INDEX PreviewAuthorization(TenantId, PreviewId, ExpiresAtUtc, RevokedAtUtc) | Expiración/revocación verificadas antes de renderizar |
| IX07 | UNIQUE ActivePublication(TenantId, SiteId) + RowVersion lógico | Actualización CAS publica solo revisión autorizada |
| IX08 | INDEX PublishedRoute(TenantId, SiteId, CanonicalRoute, PublicationVersion) | Resolver ruta pública solo tras host aprobado |
| IX09 | INDEX LandingAssetLink(TenantId, AssetId) | No enlazar assets privados ni del tenant B |
| IX10 | INDEX LandingChangeAudit(TenantId, OccurredAtUtc, EventId) | Trazabilidad sin lecturas globales ni token en logs |
| IX11 | INDEX SiteCacheNamespace(TenantId, SiteId, PublicationVersion) | Invalidación aislada por empresa y versión |

Índices son hipótesis de diseño, no rendimiento medido ni SQL aplicado. Validar selección del motor, longitud/collation de hostname, unicidad internacionalizada y efectividad de filtros con EXPLAIN en MySQL 8.4 efímero antes de implementar.

## 4. Migración futura condicionada: seis fases M01–M06

| Fase | Acción prevista y condición de reversión |
| --- | --- |
| M01 | Confirmar DB/provider/DbContext y contrato real, mapear tablas existentes, obtener respaldo verificable de CI efímero |
| M02 | Añadir estructuras de forma aditiva, FK tenant-bound, unicidad y restricciones sin tocar rutas live; verificar schema diff |
| M03 | Importar fixtures sintéticos aislados A/B mediante seeder de test con cleanup idempotente; jamás datos comerciales |
| M04 | Probar lecturas y cambios autorizados/denegados, transacción de publicación CAS, error y rollback íntegro |
| M05 | Validar integridad, EXPLAIN, colisiones de host y ruta, expiración de previews, tamaño/carga y aislamiento de caché |
| M06 | Aprobar reversión practicable, artefacto de evidencia y ventana futura independiente; no ejecutar en Aiven/productivo |

Rollback futuro: antes de un esquema destructivo se exige copia/restauración demostrada, compatibilidad de versiones y decisión explícita. No se propone borrar datos históricos ni lanzar migración sobre bases reales desde M02.03. El rollback del cambio **documental actual** es git revert normal contra HEAD vigente, nunca force-push.

## 5. Fixtures de aceptación para posterior CI MySQL/HTTP (NO_EJECUTADOS)

| Caso | Permiso positivo esperado | Negación o fallo seguro esperado |
| --- | --- | --- |
| FX01 | Tenant A crea draft en Site A con membresía vigente | Tenant B no escribe revision A |
| FX02 | Host A aprobado y normalizado resuelve publicación A | Host no verificado o spoofed forwarded-host no resuelve A |
| FX03 | Editor A escribe bloque tipado permitido | HTML/JS activo y SQL/texto arbitrario como bloque ejecutable rechazado |
| FX04 | Ruta /servicios única de revisión A publicada | Ruta duplicada en misma revisión falla UNIQUE |
| FX05 | PreviewGrant A vigente permite preview noindex/no-store | Anónimo, token expirado o revocado rechaza |
| FX06 | Aprobador diferente al editor activa versión CAS | Editor autoaprueba o versión obsoleta no publica |
| FX07 | Publicación aprobada genera canonical y sitemap públicos | Borrador, preview o URL interna no se indexa |
| FX08 | Asset público A autorizado se referencia en página A | Asset privado o de tenant B no se expone |
| FX09 | Caché A/host/version A sirve HTML A | Petición de B no recibe fragmentos, CSS o SEO de A |
| FX10 | AuditTrail guarda actor minimizado, acción y correlación | JWT, contraseña, URL firmada o PII completa no se guarda |
| FX11 | Error de proveedor devuelve estado neutro estable | No reutiliza host de otro tenant ni filtra excepciones |
| FX12 | Revert en CI restaura esquema/datos sintéticos | Conflicto de FK, colisión o backup ausente bloquea rollback inseguro |

Estos son casos futuros de diseño, no pruebas xUnit/Vitest/Playwright/SQL ejecutadas. Para M03.04, la implementación real requiere migrations EF, provider Oracle MySQL, servicio MySQL efímero, tests de aislamiento A/B y restauración comprobada antes de certificarse.

## 6. Gates de seguridad, integración y evidencia A0–A5

**A0 estática:** comprobar este ID exacto en PLAN/COLA/DAG, M01.01–06 + A5, fuentes V5, inventario de rutas, diferencias Git limitadas al documento y CHANGELOG y sección sin encabezados huérfanos. **A1 documental:** cotejar 13 estructuras, 11 índices, seis fases de migración y doce pares FX con los requisitos de M02.01 y M01.06; no afirmar tests runtime. **A2 estática:** Tenant A/B, JWT/RBAC de editor/publicador, host confiable, preview noindex, PII, cache por tenant, secretos fuera de Git y falla cerrada. **A3 documental:** consistencia conceptual con M02.01, API/DTO M02.02 aún bajo otro autor, storefront público y futura separación Domain/Application/Infrastructure/API; sin conectarse a MySQL ni desplegar. **A4:** commit fast-forward con expected HEAD, diff/blob readback, Scope Lock y V5 provenance completed/success exact SHA, luego PLAN/COLA/BITACORA readback. **A5:** revisión separada de otra de las cinco Tasks Javier, distinta del autor; el autor no registra PASS de su propia entrega.

**NO_APLICA causal:** compilación Angular/.NET, MySQL/EF, navegador y pruebas HTTP no son tests de un diff exclusivamente Markdown; sus estados queued/cancelled/skipped nunca se convierten en PASS. **Bloqueos permanentes:** sin permiso nuevo, no modificar DNS/TLS/CDN/R2/Hostinger, datos productivos, JWT/RBAC, proxies, QA/main/PROD, Tasks adicionales ni ejecutar webhooks/workers. M02 es una definición técnica, no una autorización de implementación o promoción.
