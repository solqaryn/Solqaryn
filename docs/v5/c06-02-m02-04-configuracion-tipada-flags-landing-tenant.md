# C06-02.M02.04 — Configuración tipada y flags seguros para landing por tenant

**Fecha:** 2026-11-15. **Autor:** PRIMARY DEV 04 / JAVIER_CHATGPT / `6aeacb7820ac8191b7d768a6f33dfa09`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente comercial:** Standard V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## Autoridad y evidencia revisada

Documento de diseño de M02.04, no implementación. El preflight verificó `PLAN_MAESTRO!A911=C06-02.M02.04`, H=PENDIENTE e I vacío antes del claim, `COLA!G/O=SI` y `DAG_TECNICO!F=VALIDADO,G=SI` por ID exacto, con predecesor `C06-02.M01.01–06+A5`. Se hizo claim exclusivo y readback del responsable. M01.06 define G01–G16 y RB01–RB08; M02.01 modela L01–L16 e I01–I16; M02.02 propone operaciones API01–API10, sin rutas instaladas. El documento M02.03 trata la persistencia conceptual por separado.

Fuentes del árbol vivo: `docs/v5/c06-02-m01-06-bloqueos-rollback-liberacion-landing-tenant.md`, `docs/v5/c06-02-m02-01-entidades-ownership-invariantes-landing-tenant.md`, `docs/v5/c06-02-m02-02-contratos-api-dto-errores-landing-tenant.md`, `frontend/src/app/features/storefront/tienda-vinculacion.component.ts` y `frontend/src/app/features/storefront/storefront.service.ts`. Existe vinculación pública de **tienda**, no una landing CMS editable certificada: el componente consulta un binding y una identidad de tienda verificada. Ese flujo no demuestra resolución de dominio arbitrario, editor/publicador, preview, host aprobado ni publicación de landing.

## Precedencia y confianza

Son **opciones lógicas futuras**, no `appsettings`, variables de entorno, flags ni filas MySQL creadas en esta unidad. Resolución propuesta: política rígida SERVER_GLOBAL > gates DEPLOYMENT_GUARD > configuración SERVER_TENANT autorizada > PUBLIC_PROJECTION de versión publicada. El tenant no puede relajar una regla global de aislamiento; el entorno no puede convertir un permiso de UI en permiso backend. La ausencia, formato inválido, expiración o procedencia desconocida de una opción equivale a **DENY**, nunca a habilitación implícita.

`Host`, `X-Forwarded-Host`, `X-Empresa-Id`, slug, ruta, cookie, claim visual y localStorage son entradas no confiables. El backend debe resolver tenant y ownership con registro y proxy confiables; nadie obtiene permiso de publicación por cambiar un flag. El frontend recibirá únicamente una proyección pública saneada, sin secretos, token de preview, tenant ajeno ni ajustes de seguridad editables.

## CFG01–CFG18: catálogo tipado propuesto — NO IMPLEMENTADO

| ID | Opción y tipo previsto | Ámbito y valor inicial restrictivo |
| --- | --- | --- |
| CFG01 | `LandingCapability: disabled/draft/preview/publish` | DEPLOYMENT_GUARD; `disabled`; cada transición requiere gates independientes |
| CFG02 | `RequireHostOwnership: boolean` | SERVER_GLOBAL; true, no delegable |
| CFG03 | `ApprovedCanonicalHosts: HostCanonical[]` | SERVER_TENANT; lista vacía hasta prueba de propiedad |
| CFG04 | `TrustedProxyIdentities: ProxyIdentity[]` | SERVER_GLOBAL; lista vacía, nunca `X-Forwarded-Host` arbitrario |
| CFG05 | `RequireTenantContext: boolean` | SERVER_GLOBAL; true; contexto ausente rechaza |
| CFG06 | `PreviewEnabled: boolean` | SERVER_TENANT; false, sin sustituir grant/RBAC |
| CFG07 | `PreviewTtlSeconds: integer` | SERVER_GLOBAL; propuesta 300, futuro rango validado 30–900 |
| CFG08 | `PreviewResponsePolicy: no-store/noindex` | SERVER_GLOBAL; ambas restricciones obligatorias |
| CFG09 | `DistinctApproverRequired: boolean` | SERVER_GLOBAL; true; editor no se autoaprueba |
| CFG10 | `PublishVersionCas: boolean` | SERVER_GLOBAL; true, no sobrescritura a ciegas |
| CFG11 | `SeoPublicProjectionEnabled: boolean` | SERVER_TENANT; false hasta host y versión publicados |
| CFG12 | `LandingCacheMode: disabled/tenant-versioned` | DEPLOYMENT_GUARD; disabled |
| CFG13 | `CachePartition: tenant+host+revision` | SERVER_GLOBAL; separación obligatoria si se activa |
| CFG14 | `PublicAssetOwnerRequired: boolean` | SERVER_GLOBAL; true para cada recurso |
| CFG15 | `PageBlockSchema: typed-allowlist` | SERVER_GLOBAL; rechaza HTML/JS/SQL arbitrarios |
| CFG16 | `PublicationAuditMode: strict` | SERVER_GLOBAL; fallo de auditoría deniega efecto |
| CFG17 | `MaxPagesPerTenant: integer` | SERVER_TENANT bajo techo global; propuesta 10, no límite activo |
| CFG18 | `MaxBlocksPerPage: integer` | SERVER_TENANT bajo techo global; propuesta 30, no límite activo |

TTL y cuotas son hipótesis sujetas a decisión y pruebas futuras, no defaults del código. La configuración de host, proxy, token, caché y aprobación nunca se deriva de preferencia del navegador. Un valor de CFG01 = publish no autoriza publicación sin sesión vigente, membresía, rol Publicador, host verificado, revisión inmutable, aprobación separada, CAS y auditoría completa.

## Contratos de consumidores y degradación

- **Domain y Application futuros:** validar owner tenant, grant, política, revisión, versión CAS y transiciones. El editor no puede llamar al publicador por un flag habilitado; errores de permiso son fail-closed.
- **Infrastructure futuro:** stores y unicidad tenant-bound, aislamiento A/B, claves de caché particionadas y auditoría strict. M02.03/M03 deberán probar MySQL 8.4 en CI efímera; aquí no hay DDL, índices ni queries ejecutadas.
- **Frontend Angular 22 futuro:** configuración de UI es una proyección solo lectura. El servidor reválida cada operación; no aceptar IDs de tenant, flags o tema visual como permiso.
- **SEO y preview futuros:** solo una versión publicada en host aprobado puede llegar a canonical/robots/sitemap. Preview es no-store/noindex, efímero y revocable. Borradores no se indexan.
- **Assets y proveedores:** autorizar cada referencia por owner. Sin CDN/R2/DNS/TLS ni cambios de dominio; falla de proveedor muestra identidad neutra sin PII ni marca ajena.
- **Cambios de contexto:** invalidar sesión, caché de ajustes, grants y referencias de otro tenant antes de presentar nueva identidad. Evitar confundir URL o alias con autoridad.

## P01–P12 y N01–N12: oráculos futuros NO EJECUTADOS

| Par | Positivo propuesto | Negativo obligatorio |
| --- | --- | --- |
| 01 | Host canónico aprobado resuelve tenant A | Host de B/desconocido no expone A |
| 02 | Proxy confiable normaliza host | Forwarded host spoofed no se acepta |
| 03 | Editor A modifica su draft | Editor B o permiso revocado no modifica A |
| 04 | Preview A con grant vigente es noindex/no-store | Grant caducado/revocado falla |
| 05 | Aprobador distinto publica versión autorizada | Editor sin rol no se autoaprueba |
| 06 | CAS versión correcta publica una sola vez | CAS desactualizado no pisa revisión |
| 07 | Sitemap contiene solo versión publicada | Draft/preview no se indexa |
| 08 | Caché tenant+host+versión devuelve A | Host B nunca recibe bytes A |
| 09 | Asset público de A pasa ownership | Asset privado/B no devuelve URL |
| 10 | Bloque tipado se renderiza escapado | Script, SQL o HTML libre se rechaza |
| 11 | Falta de proveedor da error neutro | No revela marca/token de otro tenant |
| 12 | Rollback restaura versión autorizada | No borra auditoría, pedidos o drafts |

Los 24 casos son propuestas, **no** pruebas .NET, Angular, Playwright o MySQL realizadas en este changeset. A0–A3 documentales validan coherencia de fuentes, límites de confianza y pares positivos/negativos. La CI de runtime ajena al diff se clasifica NO_APLICA con motivo, no PASS.

## Salida, dependencias y rollback

Conservar G01–G16, I01–I16 y RB01–RB08 como gates de implementación futura. M02.05 puede definir **solo** contrato conceptual de eventos e idempotencia, sin webhook/dispatcher. M02.06 debe releer M02.01–05, sus propietarios y A5 según DAG vivo; un M02.04 LISTO no habilita automáticamente M03, QA, main o PROD.

La salida de esta microtarea es **únicamente** este documento y una sección completa en `CHANGELOG_AI.md`, con A0–A4 documentales, SHA exacto, Scope Lock y procedencia V5 `completed/success`, BITACORA/PLAN/COLA readback y A5 independiente aún pendiente. Reversión del changeset por `git revert` normal sobre dev; no alterar historia ni datos productivos.
