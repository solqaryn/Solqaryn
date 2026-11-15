# SOLQARYN V5 — C06-03.M02.03: persistencia, índices, migraciones y fixtures editoriales/mapas

**Entrega:** contrato DOCUMENTAL M02, sin DDL, migración, base MySQL, API, geocodificador o proveedor instalado. **Autor:** PRIMARY DEV 05 JAVIER_CHATGPT, Task 6adc2e55f47c81919f6374e561687076; **rama:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT; 2026-11-15.

## 1. Autoridad y no-alcance

Standard V5 FINAL (Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), hoja institucional 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU: C06-03.M02.03, PLAN fila 946 H=PENDIENTE sin owner, COLA G=SI/O=SI y DAG F=VALIDADO/G=SI antes del reclamo CAS. M01.06 (SHA 0dc07b4509e3f74c9b867fc450b3b5abda63075b, blob e51e254064bcaf53449ea66efeac7b5612966121) tiene G01–G16/RB01–RB08 y revisión A5 independiente. M02.01 (SHA 08ee3779cebc0eef388abefb8b9b57e6085982bd, blob 77d4e7a6f9a6327f6a4d3998bcfc56ccc80db013) documenta 15 entidades/invariantes; M02.02 (SHA 7bb13fea68c56c6297136d39c3424d985bf28a46, blob ba46a9f4ad3a54a3db7606d8dcd9e4ef8bd9b0f0) contiene contratos API/DTO. El DAG no exige un falso PASS de runtime para este diseño documental; los gates permanecen.

La futura solución de editoriales debe mantener ownership tenant A/B, host aprobado, Editor/Moderador/Publicador separados, contacto/dirección/horarios públicos verificados, mapa opt-in no obligatorio, borradores noindex y publicación CAS. No usar Aiven real, DNS/TLS, R2/Cloudflare, pagos, datos productivos ni activar geolocalización de visitantes. No se ejecutan migraciones por este documento.

## 2. Doce estructuras lógicas futuras (NO TABLAS CREADAS)

| ID | Relación propuesta | Clave tenant-bound y seguridad |
| --- | --- | --- |
| DB01 | EditorialSiteRecord | PK (TenantId, SiteId); tenant efectivo backend, estado y versionado |
| DB02 | EditorialRevisionRecord | PK (TenantId, SiteId, RevisionId); FK compuesta y estado borrador/aprobado |
| DB03 | EditorialPageRecord | PK (TenantId, RevisionId, PageId); ruta canónica por revisión |
| DB04 | EditorialSectionRecord | PK (TenantId, PageId, SectionId); orden y tipo cerrado, no scripts |
| DB05 | EditorialContactRecord | PK (TenantId, SiteId, ContactId); datos públicos aprobados |
| DB06 | EditorialBusinessLocationRecord | PK (TenantId, LocationId); sucursal verificada, dirección pública con consentimiento |
| DB07 | EditorialHoursRecord | PK (TenantId, LocationId, ScheduleId); TZ, validez y excepciones |
| DB08 | EditorialMapReferenceRecord | PK (TenantId, LocationId, MapId); referencia opcional aprobada, sin API key persistida |
| DB09 | EditorialModerationDecisionRecord | PK (TenantId, RevisionId, DecisionId); moderador distinto del editor |
| DB10 | EditorialPublicationPointerRecord | PK (TenantId, SiteId); revision aprobada y versión CAS, rollback |
| DB11 | EditorialPreviewGrantRecord | PK (TenantId, PreviewGrantId); actor/revisión, token digest, TTL y revocación |
| DB12 | EditorialAuditAndSeoProjection | Separar en estructuras físicas si procede: audit minimizado tenant+UTC y SEO público por revisión aprobada |

DB12 es **agrupación conceptual** de dos responsabilidades, no una propuesta para almacenar información privada de auditoría junto con SEO público. En implementación deberán existir proyecciones y accesos separados para evitar exponer PII/tokens. Todas las FK de recursos tenant-bound deben incorporar TenantId donde el motor lo permita, sin depender únicamente de un ResourceId opaco. El host/slug nunca concede autoridad. La ubicación del visitante no se necesita para mostrar dirección de una sucursal.

## 3. Diez índices y restricciones hipotéticos pendientes de EXPLAIN

| ID | Clave/índice previsto | Comprobación necesaria en MySQL 8.4 efímero |
| --- | --- | --- |
| IX01 | UNIQUE EditorialSite(TenantId, SiteId) | No resolver recursos de B por SiteId |
| IX02 | UNIQUE EditorialRevision(TenantId, SiteId, RevisionNumber) | CAS y conflicto de escritura |
| IX03 | UNIQUE EditorialPage(TenantId, RevisionId, NormalizedRoute) | Colisiones/traversal por revisión |
| IX04 | INDEX EditorialSection(TenantId, PageId, Ordinal) | Orden determinista y filtro tenant |
| IX05 | INDEX EditorialContact(TenantId, SiteId, IsPublic) | PII no publicada fuera de consulta |
| IX06 | INDEX EditorialBusinessLocation(TenantId, LocationId, PublicationStatus) | Dirección solo autorizada |
| IX07 | INDEX EditorialHours(TenantId, LocationId, ValidFromUtc, ValidToUtc) | Intervalos por TZ/fecha |
| IX08 | INDEX EditorialModeration(TenantId, RevisionId, DecidedAtUtc) | Historial de aprobación independiente |
| IX09 | UNIQUE PublicationPointer(TenantId, SiteId) con campo Version | CAS real, sin sobrescritura silenciosa |
| IX10 | INDEX PreviewGrant(TenantId, RevisionId, ExpiresAtUtc, RevokedAtUtc) | Caducidad/revocación antes de lectura |

Son hipótesis; validar collation, coste de índices, cardinalidad, FK compuestas, unicidad y filtros A/B en un servicio MySQL 8.4 efímero antes de incorporarlas. No afirmar índices parciales nativos de MySQL, ni escribir DDL incompatible desde un diseño.

## 4. Seis fases de migración futuras, condicionadas y reversibles

| Fase | Acción futura y gate |
| --- | --- |
| MG01 | Inspeccionar DbContext/provider real, tablas existentes, contratos de tenancy y backup efímero verificable |
| MG02 | Diseñar expansión aditiva EF con PK/FK tenant-bound, índices y schema diff sin alterar datos live |
| MG03 | Cargar fixtures sintéticos de tenant A/B y sucursales distintas, con cleanup idempotente |
| MG04 | Probar consultas A/B, filtros antes de paginación, consentimiento de mapa y preview noindex |
| MG05 | Probar transacción CAS/moderación, rollback lógico y EXPLAIN con índices previstos |
| MG06 | Ejecutar restore de copia EF/MySQL EFIMERO, verificar reversibilidad; autorizar despliegue por proceso separado |

Ninguna fase se ejecuta en este changeset. El rollback del presente documento es git revert normal contra HEAD actual, sin reset/force-push. Un rollback de migración real requiere prueba previa, respaldo, permisos y autorización nueva, y no se deriva de LISTO_DEV documental.

## 5. Doce fixtures positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro exigido | Negativo futuro exigido |
| --- | --- | --- |
| FX01 | Crear sitio y borrador A con owner A validado | Usuario B no crea/lee sitio A |
| FX02 | Host A aprobado resuelve publicación A | Host B o X-Forwarded-Host spoofed no obtiene A |
| FX03 | Página tipada A persiste con ruta normalizada | HTML activo o ruta duplicada rechazada |
| FX04 | Moderador distinto aprueba revisión A | Editor autoaprueba o versión CAS obsoleta denegado |
| FX05 | Contacto público A visible en sitio A | PII privada o contacto B no aparece |
| FX06 | Horario A/TZ correcto se publica con excepciones | Sin TZ o ubicación B no se inventa abierto |
| FX07 | Dirección pública autorizada funciona sin mapa | Ubicación privada del visitante nunca se exige |
| FX08 | Mapa opcional aprobado ofrece enlace público | Sin consentimiento/proveedor/cuota no carga SDK |
| FX09 | Preview A vigente es no-store/noindex | Anónimo, expirado, revocado no ve draft |
| FX10 | Publicación CAS A actualiza SEO/caché A | Borrador/B no se indexa ni mezcla caché |
| FX11 | Asset público A sirve referencia permitida | URL firmada/asset B no se expone |
| FX12 | Restore EF/MySQL efímero recupera A/B y auditoría | FK inválida o snapshot ausente bloquea rollback |

Los 12 pares definen criterios, **no son pruebas ejecutadas** de xUnit, SQL, navegador o proveedor de mapas. No se afirma disponibilidad del CMS ni seguridad del runtime.

## 6. Fronteras de integración, A0–A5 y seguridad

**Domain futuro:** reglas de ownership, revision CAS, contacto/horario/ubicación pública, contenido tipado. **Application futuro:** RBAC Editor/Moderador/Publicador, membresía vigente y auditoría minimizada. **Infrastructure futuro:** EF Core10 y MySQL8.4 efímero con PK/FK compuestas, transacciones, restore. **API futuro:** tenant/host desde backend verificado y trusted proxy, errores neutros. **Angular futuro:** teclado/foco, 320px, reduced motion y fallback textual de dirección, sin cargar mapas de terceros automáticamente. **Otros proveedores:** opt-in explícito, cuota, privacidad, protección de claves; no habilitados.

**A0:** verificar PLAN/COLA/DAG exactos, M01.01–06 A5, 15 estructuras M02.01, contrato M02.02, Skill canónica, no secretos ni cambios fuera de diff. **A1 documental:** revisar DB01–DB12, IX01–IX10, MG01–MG06, FX01–FX12; ejemplos positivos/negativos NO_EJECUTADOS. **A2 estática:** A/B, proxy/Host, JWT/RBAC, autorización de mapa, PII/contactos, previews, SEO/cache y denegación segura. **A3 documental:** coherencia con Domain/Application/Infrastructure/API/Angular y G01–G16/RB01–RB08, sin implementar tablas. **A4:** fast-forward/CAS expected HEAD en dev, solo este Markdown+CHANGELOG con sección completa; Scope Lock y procedencia V5 completed/success SHA exacto, readback PLAN/COLA/DAG/BITACORA. **A5:** otro TASK_ID Javier distinto del autor con revisión real de diff, blobs, SHA, evidencia y límites.

**NO_APLICA:** backend .NET, frontend Angular, MySQL, Playwright, HTTP y mapas no son pruebas pertinentes al diff Markdown. queued/cancelled/skipped/fail/in_progress nunca PASS requerido. **Prohibido:** migraciones, geocoding, instalación de workers/eventos, DNS/TLS, Cloudflare/R2, QA/main/PROD, compras o datos reales por este documento.
