# SOLQARYN V5 — C06-03.M02.02: contratos API, DTO, errores y compatibilidad editorial/mapas

**Alcance:** especificación documental M02, SIN rutas API, controllers, services, DTO compilados, proveedor de mapa, geocodificación ni base MySQL. **Autor:** PRIMARY DEV 05 JAVIER_CHATGPT, TASK_ID 6adc2e55f47c81919f6374e561687076. **Repositorio:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT, 2026-11-15.

## 1. Autoridad y entradas

Fuente comercial única: Standard de Implementación de Automatizaciones V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); Sheet institucional 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU. Preflight C06-03.M02.02: PLAN fila 945 H=PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI, M01 terminal y A5 independiente. Reclamo exclusivo CAS antes de modificar archivos. La M02.01 (SHA 08ee3779cebc0eef388abefb8b9b57e6085982bd, doc blob 77d4e7a6f9a6327f6a4d3998bcfc56ccc80db013) define 15 estructuras y 16 invariantes de propiedad/tenant y mapas opcionales; M01.06 (SHA 0dc07b4509e3f74c9b867fc450b3b5abda63075b, blob e51e254064bcaf53449ea66efeac7b5612966121) mantiene G01–G16, RB01–RB08 y 14 pares P/N futuros. Este contrato no amplía autorizaciones.

No inferir que el storefront o las rutas SEO actuales prueban un editor de contenidos, host binding, frontend autenticado para mapas, geocodificador, proveedor externo o permiso para usar DNS/TLS. El backend verifica JWT, actor, membresía, tenant y recurso, nunca Host, X-Forwarded-Host, slug, cookie, localStorage o campos de cliente como autoridad. Mapas son opcionales y deben degradarse a dirección pública aprobada sin tracking si el proveedor no está autorizado.

## 2. Diez operaciones lógicas FUTURAS — NO ENDPOINTS IMPLEMENTADOS

| ID | Operación conceptual y respuesta segura | Rechazo negativo |
| --- | --- | --- |
| API01 | Leer sitio editorial publicado por host aprobado y ruta | Host desconocido/forjado no revela contenido B |
| API02 | Leer contacto público/sucursal aprobada | No exponer datos privados o de tenant ajeno |
| API03 | Consultar horarios con zona y excepción pública | No mezclar sucursal B ni inferir horarios no verificados |
| API04 | Listar drafts propios con cursor tenant-bound | Actor sin editor, cursor cruzado o membresía revocada denegado |
| API05 | Crear/editar página editorial tipada con CAS | HTML/JS activo, revisión antigua o tenant B rechazados |
| API06 | Solicitar moderación de draft vigente | Editor no asigna aprobación sin verificación backend |
| API07 | Previsualizar revisión privada con grant expirable | Anónimo/token vencido/preview indexable rechazados |
| API08 | Aprobar/publicar una versión con rol Publicador distinto | Autoaprobación, Host no verificado o CAS obsoleto denegados |
| API09 | Revertir publicación a versión aprobada anterior | Sin backup, rol o revisión compatible no se ejecuta |
| API10 | Obtener proyección pública de dirección/enlace de mapa opt-in | Sin consentimiento/clave/proveedor aprobado no carga tercero |

Las operaciones son verbos de diseño y NO fijan rutas REST nuevas. Los controladores existentes deberán inspeccionarse en microfase de código y conservar compatibilidad. Nunca devolver referencias privadas, API keys, JWT, URL firmada o localización no consentida.

## 3. Diez DTO conceptuales, origen de confianza y validación

| DTO propuesto | Campos mínimos | Regla server-side fail-closed |
| --- | --- | --- |
| EditorialPublicQuery | Host/ruta canonicalizados, cursor acotado | Resolver binding aprobado, tenant efectivo en backend |
| EditorialPublicProjection | SiteId opaco, Revision publicada, Sections públicas, Seo | Excluir draft, preview, tracking y recursos privados |
| EditorialDraftInput | Secciones tipadas, expectedVersion, idempotencyKey | No aceptar tenant/rol de navegador como grant |
| EditorialDraftView | RevisionId, estado, preview permitida, actor minimizado | Requiere rol Editor y ownership vigente |
| EditorialContactView | Teléfono/email público aprobado, sucursal y timestamp | Sin exposición de datos de empleados/tenant B |
| EditorialHoursView | LocationId, TZ, intervalos y excepciones validadas | Sin TZ no inventar apertura ni horas de B |
| EditorialMapProjection | Dirección pública y URL permitida/ausente | Sin proveedor autorizado mostrar texto, no JS remoto |
| EditorialModerationRequest | RevisionId, decisión y expectedVersion | El moderador debe tener permiso activo distinto del editor |
| EditorialPublicationDecision | RevisionId, aprobador, version CAS y auditRef | Solo servidor confirma publicación auditada |
| EditorialError | Code allowlisted, correlationId saneado, detalle mínimo | Nunca SQL, JWT, host secreto, payload B ni stack trace |

M02.03 debe diseñar persistencia e índices tenant-bound para estas proyecciones y la autorización real; M02.04 opciones tipadas y un mapa OFF por defecto; M02.05 eventos meramente conceptuales, SIN dispatch/worker/webhook. Los tipos anteriores no existen como clases de runtime por este documento.

## 4. Catálogo de dieciséis errores propuestos NO RESPUESTAS HTTP OBSERVADAS

| Código | Semántica pública prevista |
| --- | --- |
| E01 UNAUTHENTICATED | 401 JWT ausente, revocado o expirado |
| E02 EDIT_FORBIDDEN | 403 editor sin membresía/RBAC vigente |
| E03 RESOURCE_NOT_VISIBLE | 404/403 neutro para tenant B o privado |
| E04 HOST_NOT_APPROVED | 404/409 para host o alias sin propiedad |
| E05 FORWARDED_CONTEXT_UNTRUSTED | 400/403 sin proxy confiable, sin tenant fallback |
| E06 REVISION_STALE | 409 CAS obsoleto, sin diff confidencial |
| E07 BLOCK_SCHEMA_INVALID | 400/422 para tipo o HTML/JS no permitido |
| E08 MODERATION_NOT_ALLOWED | 403 para moderador inválido o autoaprobación |
| E09 PUBLICATION_FORBIDDEN | 403/423 sin publicador/host/auditoría estrictos |
| E10 PREVIEW_GRANT_INVALID | 403 vencido/revocado, no indexable |
| E11 LOCATION_NOT_APPROVED | 404 para sucursal no pública/tenant ajeno |
| E12 HOURS_UNVERIFIED | 409 no afirmar abierto/horarios sin TZ |
| E13 MAP_PROVIDER_DISABLED | 200 proyección textual neutra, sin cargar JS |
| E14 ASSET_NOT_VISIBLE | 404 sin exponer URL firmada de B |
| E15 AUDIT_STRICT_FAILED | 503 con rollback y sin falso éxito |
| E16 CONTRACT_VERSION_MISMATCH | 409/422 sin permisos por valores ausentes |

Los códigos/HTTP son propuestas, no cambios al middleware real. No definir E13 como éxito de geocodificación; solo fallback textual de dirección pública. Un error nunca activa un proveedor alternativo de pago, envía ubicación del visitante ni cambia tenant.

## 5. Compatibilidad, permisos, host, privacidad y límites

- **Versión:** cambios de seguridad, tenant, moderación, publicación o CAS requieren versión explícita. Clientes viejos no reciben permisos por defaults ausentes.
- **Aislamiento:** tenant/contexto y ownership se resuelven server-side; filtrar antes de consulta, paginación, caché y respuesta. Host desconocido o forwarded spoofed no escoge tenant por defecto.
- **RBAC:** Editor, Moderador y Publicador separados; revalidar membresía y revocación en cada mutación. Guard Angular solo mejora UX.
- **Proyección pública:** mostrar solo versión aprobada, sucursal/contacto/horario validados, sin borrador, PII, JWT ni URLs privadas. Canonical/sitemap únicamente de versión publicada.
- **Preview:** grant acotado/revocable con no-store/noindex, nunca cache pública ni sitemap.
- **Mapas:** opcionales, con opt-in, proveedor/clave/cuota autorizados, privacidad y fallback textual; no cargar automáticamente scripts ni inferir geolocalización privada.
- **Persistencia/rollback:** EF Core 10/MySQL 8.4 con FK tenant y pruebas efímeras en M03; ningún DDL/migración real forma parte de M02.02.
- **Seguridad general:** CSP, sanitización, límites de entrada/rate limiting y auditoría minimizada. No dejar HTML/JS arbitrario ni tracking cruzado.

## 6. Doce pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| PN01 | Host aprobado A entrega publicación A | Host B/desconocido/forged no devuelve A |
| PN02 | Editor A crea draft A con CAS | Editor B o revocado no escribe A |
| PN03 | Página pública tipada renderiza sin scripts | HTML/JS activo o traversal denegado |
| PN04 | Moderador distinto valida revisión | Editor autoaprueba o versión obsoleta se rechaza |
| PN05 | Publicador autorizado activa versión y audita | Rol visual/flag UI no autoriza |
| PN06 | Preview A grant vigente es noindex/no-store | Anónimo o expirado no obtiene draft |
| PN07 | Horario con TZ/sucursal verificada es público | Horarios inventados o sucursal B no salen |
| PN08 | Contacto autorizado A se muestra en sitio A | Contacto privado o B no se filtra |
| PN09 | Dirección pública aparece sin proveedor | Ubicación privada de visitante no se exige |
| PN10 | Mapa aprobado puede enlazarse de forma segura | Sin consentimiento/cuota/proveedor no cargar scripts |
| PN11 | Canonical/sitemap solo publicado correcto | Draft, preview o host no aprobado no indexa |
| PN12 | Rollback CAS conserva historial y auditoría | Versión B/snapshot ausente deniega sin pérdida |

Son oráculos de diseño, no pruebas .NET, Angular, HTTP, Playwright, MySQL o proveedor externo ejecutadas. Su ejecución requiere implementación posterior con seguridad y entorno CI reproducible.

## 7. Auditorías de esta microtarea y rollback

**A0:** PLAN H/I y CAS, COLA G/O, DAG F/G, M01.06+A5, M02.01, Skill canónica, CHANGELOG autocontenido. **A1 documental:** cotejar 10 operaciones, 10 DTO, 16 errores y 12 pares con el resultado V5, sin presentar tests futuros como PASS. **A2 estática:** tenant A/B, JWT/RBAC Editor/Moderador/Publicador, Host/proxy, PII, ubicación privada, mapas opt-in y denegación. **A3 documental:** capas Domain/Application/Infrastructure/API/Angular, contratos de M02 y no regresión de storefront, sin cambio runtime. **A4:** publicar fast-forward/CAS expected HEAD, diff solo este documento y CHANGELOG, checks Scope Lock y V5 completed/success exact SHA, readbacks PLAN/COLA/DAG/BITACORA. **A5:** revisor otra Task Javier distinta de autora antes de sucesoras técnicas.

**NO_APLICA causal:** CI .NET/Angular, MySQL, mapas y browser no evalúa un diff Markdown. Que un job esté queued, in_progress, cancelled o skipped NO es PASS. **Prohibido:** endpoints, workers, geocoding, API keys, DNS/TLS, Cloudflare/R2, QA/main/PROD, datos reales y compras. Rollback de documentación: git revert normal después de leer HEAD, sin force-push ni reescribir BITACORA.
