# SOLQARYN V5 — C06-03.M02.01: entidades, ownership e invariantes editoriales y mapas

**Alcance:** contrato M02 DOCUMENTAL, sin esquema SQL, API, UI, geocodificación ni mapa instalado. **Autor:** PRIMARY DEV 05 JAVIER_CHATGPT, TASK_ID 6adc2e55f47c81919f6374e561687076. **Repositorio:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

## 1. Propósito, fuentes y fronteras

Plan único: Standard de Implementación de Automatizaciones V5 FINAL (Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); hoja institucional 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU. Preflight de C06-03.M02.01: PLAN_MAESTRO fila 944 PENDIENTE sin responsable, COLA G/O=SI y DAG F=VALIDADO/G=SI, terminal C06-03.M01.06 LISTO+A5 independiente. La microtarea fue reclamada por cambio exclusivo y readback antes de publicar. Los gates del documento M01.06 no autorizan una implementación automática.

**M01 fuentes auditables:** M01.01 doc blob d16fd73d5211161bdde429a3b705ec9b9d09cdcf (alcance); M01.02 c5f815be7ef044ffb2cd4e4910aa97dff511731a (interfaces); M01.03 c4434ef2fc3cfb351a0ea5d5c819c0b5d2d560b7 (brechas); M01.04 76eda76336c6046d835583ac7080b7ec37f3762b (riesgos); M01.05 3b4ec22673eea45b78772e235280040ce447fd53 (criterios futuros). El terminal M01.06 publicado SHA 0dc07b4509e3f74c9b867fc450b3b5abda63075b, blob e51e254064bcaf53449ea66efeac7b5612966121, fija G01–G16, RB01–RB08, 14 pares P/N FUTUROS_NO_EJECUTADOS y mantiene decisiones D01–D08 abiertas.

El código de storefront/SEO ya existente no constituye CMS editorial, geocodificador, propiedad de dominios, permiso de publicar ni aislamiento validado de contenidos nuevos. El Host y cabeceras forwarded son entradas no confiables; tenant y sucursal requieren resolución backend verificada. Mapas son opcionales; una dirección pública aprobada puede representarse en texto sin proveedor. La ubicación privada del visitante nunca debe inferirse ni registrarse para mostrar contenido editorial.

## 2. Modelo lógico: 15 estructuras futuras NO IMPLEMENTADAS

| ID | Entidad prevista | Relación tenant y regla de integridad |
| --- | --- | --- |
| E01 | EditorialSite | PK lógica (TenantId, SiteId); dueño backend verificado, estado y versión |
| E02 | EditorialSiteRevision | FK tenant+SiteId; revisión versionada editable o aprobada, CAS |
| E03 | EditorialPage | FK tenant+RevisionId; ruta canónica y estado, borrador no público |
| E04 | EditorialSection | FK tenant+PageId; bloque tipado/sanitizado, orden estable y schemaVersion |
| E05 | EditorialContact | FK tenant+SiteId, datos públicos aprobados y política de privacidad |
| E06 | BusinessLocation | TenantId + LocationId/sucursal verificada, dirección pública explícita |
| E07 | OpeningHoursSchedule | FK tenant+LocationId; TZ declarada, horario especial y vigencia |
| E08 | PublicMapReference | FK tenant+LocationId, link/coords públicos aprobados, proveedor opcional |
| E09 | ModerationDecision | TenantId+RevisionId+actor, revisor diferente del editor, estado auditado |
| E10 | PublicationApproval | TenantId+SiteId+Revision, publicador autorizado y versión esperada |
| E11 | ActiveEditorialPublication | (TenantId, SiteId), puntero CAS a revisión aprobada; rollback anterior |
| E12 | EditorialPreviewGrant | TenantId+actor+revisión, expiración/revocación y no-store/noindex |
| E13 | EditorialSeoProjection | Versión publicada, canonical/robots/sitemap, no drafts |
| E14 | EditorialAssetLink | TenantId+AssetId y owner de medio verificado, URL solo pública |
| E15 | EditorialAuditEvidence | TenantId+EventId, actor minimizado, operación, versión y UTC sin JWT/PII completa |

Las relaciones propuestas mantienen FK tenant-bound y unicidad de rutas dentro de revisión; no autorizan utilizar TenantId=null como plataforma global. Actor, rol y pertenencia vienen del backend y membresía vigente, nunca de un slug, host, claim de UI, localStorage ni cabecera arbitraria. Ninguna entidad/índice/propiedad anterior está creada por este archivo; el DDL y la transacción real corresponden a fases de implementación con MySQL 8.4 efímero.

## 3. Invariantes I01–I16 que deben probarse en código futuro

| ID | Requisito de seguridad, experiencia y consistencia |
| --- | --- |
| I01 | Toda lectura y cambio de editorial se filtra por TenantId/owner backend antes de paginación |
| I02 | SiteId, PageId, LocationId y RevisionId opacos nunca conceden permiso por conocimiento |
| I03 | Host/Forwarded solo se resuelve mediante binding aprobado y trusted proxy, sin tenant default |
| I04 | Editor, Moderador y Publicador son permisos distintos, revalidados con membresía vigente |
| I05 | Un borrador no sale en sitemap, canonical, cache pública o proyección SEO |
| I06 | Preview es efímero, noindex/no-store, revocable y solo para actor autorizado |
| I07 | Publicación requiere aprobación separada, revisión CAS y auditoría confirmada |
| I08 | Bloques usan esquema cerrado, sin HTML/JS arbitrario ni URLs privadas |
| I09 | Dirección/horarios mostrados pertenecen a sucursal pública explícitamente aprobada |
| I10 | Ubicación privada del visitante no se solicita/infiera como requisito de editorial |
| I11 | Mapas y scripts de terceros son opt-in, con consentimiento, cuotas y permiso comercial |
| I12 | Ausencia de mapa/proveedor usa dirección textual neutra, no falla abierta o ubicación de B |
| I13 | Assets e imágenes tienen ownership A/B y permiso de publicación público verificado |
| I14 | Cache/SEO se particionan por tenant, host y versión publicada, nunca por URL sola |
| I15 | Logs y auditoría no filtran API key, JWT, token preview, datos privados o error interno |
| I16 | Migración, host binding, DNS, QA/main/PROD y servicios pagos requieren autorización nueva |

G01–G16 de M01.06 continúan abiertos, con RB01–RB08 de reversión no ejecutados. Los valores numéricos de cuotas, TTL y presupuestos de mapas se definirán con evidencia futura, sin inventar cifras aprobadas.

## 4. Casos positivos/negativos futuros, NO EJECUTADOS

| Par | Positivo obligatorio futuro | Negativo obligatorio futuro |
| --- | --- | --- |
| PN01 | Editor A crea draft A con membresía vigente | Editor B o rol revocado no modifica A |
| PN02 | Host A aprobado muestra sitio A publicado | Host desconocido o forwarded spoofed no filtra A |
| PN03 | Publicador distinto aprueba revisión CAS | Editor autoaprueba o versión vieja no cambia producción |
| PN04 | Preview A con grant vigente noindex | Anónimo, token revocado o expirado no ve draft |
| PN05 | Sección tipada se representa saneada | Script/HTML activo no se ejecuta |
| PN06 | Contacto A aprobado aparece solo para A | Contacto privado/B no se expone por ID |
| PN07 | Horario y TZ verificados se publican correctamente | Sucursal B o horario desactualizado no se mezclan |
| PN08 | Ubicación aprobada se muestra en texto público | No inferir ubicación privada de visitante |
| PN09 | Mapa opcional usa proveedor autorizado | Sin consentimiento/clave/cuota no cargar terceros |
| PN10 | CDN y assets públicos A servidos con owner verificado | Asset B/privado y cache cruzada rechazados |
| PN11 | Canonical/robots/sitemap solo publicados | Draft, preview o host no aprobado nunca se indexa |
| PN12 | Rollback CAS restaura versión previa auditada | Falta de snapshot o permiso implica NO_GO |

Estos son escenarios de diseño, **no resultados de pruebas** xUnit, Vitest, HTTP, MySQL, Playwright, red de mapas ni autorización de pago. La futura aceptación de aislamiento A/B exige ensayos positivos/negativos reproducibles sobre código real en CI.

## 5. Contratos posteriores, seguridad y rollback

**M02.02 API/DTO:** definir consulta pública, edición, preview, moderación, publicación, contacto y horarios, sin endpoints instalados. **M02.03 persistencia:** proponer índices tenant-bound, migraciones aditivas y fixtures sintéticos con reversión MySQL efímera, no tocar Aiven real. **M02.04 flags:** configuración tipada con defaults restrictivos y mapa desactivado hasta consentimiento y cuotas. **M02.05 eventos:** solo conceptualizar idempotencia/correlación, NO IMPLEMENTAR EVENTOS, webhooks, dispatcher ni Task. **M02.06 terminal:** exigir M02.01–05 LISTO+A5 y prerequisitos DAG vigentes, además de evidencia y rollback; no habilitar M03 si no corresponde.

**Posibles RB:** retirar binding incorrecto, revocar preview, revertir publicación por versión, invalidar caché por tenant, degradar a dirección textual si mapa cae, rollback EF/SQL probado en CI efímera y conservar audit trail. No se ejecuta ninguna acción RB por esta documentación. Futura implementación: Domain→Application→Infrastructure/MySQL con API ASP.NET Core 10, frontend Angular 22.2.1 y pruebas Chromium/Playwright accesibles; exigir seguridad JWT/RBAC/tenant A/B, CI exact-SHA y revisión A5 separada.

## 6. Auditorías A0–A5 para ESTE documento

**A0:** verificar PLAN H/I, COLA G/O, DAG F/G, M01.01–06 + A5, cinco blobs, Skill canónica y ausencia de secretos/cambios fuera de alcance. **A1 documental:** cotejar 15 entidades, 16 invariantes y 12 pares PN con propósito/resultado V5 y las 16 puertas del terminal, sin llamar PASS a runtime futuro. **A2:** revalidar diseño fail-closed en tenant A/B, JWT/RBAC, host/forwarded, terceros opcionales, ubicación/PII y datos de contacto. **A3 documental:** trazabilidad Domain/Application/Infrastructure/API/UI y compatibilidad de stores existentes, sin despliegue ni migración. **A4:** commit fast-forward contra SHA esperado de dev; diff solo este Markdown y CHANGELOG autocontenido, Scope Lock/V5 completed/success exact-SHA y readback PLAN/COLA/DAG/BITACORA. **A5:** Task Javier diferente de la autora, evidencia independiente de SHA, blobs, diff y seguridad, antes de desbloquear siguientes requisitos técnicos.

**NO_APLICA causal:** builds frontend/backend, MySQL, browser, geocoding y pruebas de proveedores no evalúan un diff solo Markdown y sus estados queued/cancelled/skipped no son PASS. Prohibidos QA/main/PROD, CDN/DNS real, R2, API keys, servicios pagos, migrations, OAuth alternativo, webhooks y eventos runtime. Reversión del documento: git revert normal preservando HEAD concurrente y BITACORA histórica.
