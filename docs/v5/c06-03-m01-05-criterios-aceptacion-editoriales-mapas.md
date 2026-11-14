# SOLQARYN V5 — C06-03.M01.05: aceptación funcional y técnica de sitios editoriales y mapas

**Unidad:** C06-03.M01.05, PLAN_MAESTRO fila 942. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Repos/entorno:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Tipo:** documento de criterios M01, no implementación ni certificación funcional.

## Contrato de prueba y base observada

El objetivo futuro C06-03 es contenido editorial por tenant con páginas, bloques, horarios, contacto, ubicación y mapas opcionales, moderación, sanitización, accesibilidad y SEO, sin degradar la tienda/ERP actual. Las fuentes cotejadas, en el HEAD observado, son:

- `docs/v5/c06-03-m01-01-alcance-sitios-editoriales-mapas.md`, blob `d16fd73d5211161bdde429a3b705ec9b9d09cdcf`: invariantes NR01–NR12.
- `docs/v5/c06-03-m01-02-actores-interfaces-editoriales.md`, blob `c5f815be7ef044ffb2cd4e4910aa97dff511731a`: actores A01–A12, interfaces I01–I12 y fronteras B01–B08.
- `docs/v5/c06-03-m01-03-inventario-fuentes-editoriales.md`, blob `c4434ef2fc3cfb351a0ea5d5c819c0b5d2d560b7`: fuentes S01–S12 y brechas G01–G08.
- `docs/v5/c06-03-m01-04-riesgos-supuestos-decisiones-editoriales.md`, blob `76eda76336c6046d835583ac7080b7ec37f3762b`: riesgos R01–R12, supuestos S01–S08 y decisiones D01–D08 PENDIENTES.
- `frontend/server/seo-utils.js` blob `b0886e30cc85efa6f800e558d5284412d8c573fc`, `frontend/api/sitemap.js` blob `ee50880520c906178a7ebd31553f3ab6d28f6b2d`, `frontend/src/app/features/storefront/storefront-identidad.service.ts` blob `c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f`: SEO y storefront existentes, **no** CMS editorial implementado.

**Regla de evidencia:** todos los P/N que siguen son escenarios futuros `NO_EJECUTADOS`. Las fuentes arriba evidencian contratos y código consultado, nunca un PASS de API, navegador, MySQL, privacidad o seguridad runtime. M01.05 no autoriza nuevas rutas, roles, migraciones, proveedores cartográficos, pago, DNS, QA/main/PROD ni publicación externa.

## P01–P14: escenarios POSITIVOS FUTUROS NO_EJECUTADOS

| ID | Precondición y estímulo | Resultado observable exigido / prueba futura |
| --- | --- | --- |
| P01 | Tenant A y página pública aprobada; GET por host verificado | 200 con última versión pública de A; identificador/propietario comprobado en backend (API + A/B) |
| P02 | Usuario A con permiso editar sobre página propia | Guarda borrador propio con revisión nueva; publicación previa intacta (xUnit/API) |
| P03 | Moderador A autorizado revisa borrador de A | Aprobar/rechazar produce evento auditado, sin publicar tácitamente (API) |
| P04 | Publicador A autorizado acepta revisión aprobada | Snapshot publicado único, cambio CAS, evento idempotente y versión anterior recuperable (API+DB CI) |
| P05 | Página admite bloque textual, título, enlace y medio permitidos | Texto escapado, URL validada y bloque renderizado accesiblemente (Angular/Playwright) |
| P06 | Sucursal A con horario, zona y excepción festiva | Presenta zona/horario/aviso correctos sin confundir sede B (API+browser) |
| P07 | Contacto/dirección marcados como públicos con consentimiento | Sólo campos aprobados visibles, sin precisión privada ni dato oculto (API+browser) |
| P08 | Usuario solicita expresamente mapa de ubicación pública | Carga opcional, cuota/privacidad aprobadas y alternativa de dirección textual siempre visible |
| P09 | Mapa bloqueado, sin JS o red lenta | Se conserva teclado, foco, contacto, horarios y ruta textual independiente (browser offline) |
| P10 | Versión publicada de A bajo host canónico aprobado | Canonical/robots/sitemap reflejan solo URL publicada aprobada (SEO/HTTP) |
| P11 | Se revoca permiso editar/moderar/publicar de usuario A | Nueva solicitud falla según permisos actuales; sesión/estado frontend no sustituyen backend (API) |
| P12 | Dos empresas A/B usan slugs iguales | Resolver entrega contenidos independientes, cachés y assets segregados (API+CDN simulada) |
| P13 | Publicación v2 requiere revertir a v1 por operador autorizado | Reversión auditada normal preserva historial, referencias y cache invalidada (DB/API) |
| P14 | Visitante usa pantalla 320px, teclado, lector y bajo ancho de banda | Páginas responsivas, roles ARIA/landmarks, foco visible, imágenes diferidas y texto de mapas alternativo (Playwright+a11y) |

## N01–N14: escenarios NEGATIVOS FUTUROS NO_EJECUTADOS

| ID | Entrada maliciosa, fallo o conflicto | Denegación/resultado seguro requerido y prueba futura |
| --- | --- | --- |
| N01 | Host de B o x-forwarded-host falsificado solicita página de A | No resolver A; host sólo tras proxies confiables, respuesta neutra y noindex (HTTP) |
| N02 | TenantId, EmpresaId, slug o header cliente cambia a empresa ajena | Backend rechaza con 403/404 neutro, sin fuga de página/bloque (API A/B) |
| N03 | Editor sin rol publicar invoca endpoint de publicación | 403, ninguna versión pública modificada, evento de denegación auditable (API) |
| N04 | Moderador de B evalúa borrador de A | No lectura, comentario ni cambio de estado; aislamiento por objeto (API) |
| N05 | Preview expirado o anónimo se accede por enlace | 401/403/404 según contrato y no-store/noindex; no se guarda en sitemap (HTTP) |
| N06 | Script, evento onerror, iframe o javascript: en contenido | Rechazado/sanitizado en capa autorizada; nunca ejecutado en DOM (API+browser) |
| N07 | Contacto con datos privados o dirección sin consentimiento | No se publica PII/ubicación; sólo fallback seguro (API) |
| N08 | Horario de otra empresa/sucursal, huso inválido o intervalo incoherente | Validación falla sin reasignar horarios; mensaje neutro (API) |
| N09 | Proveedor mapa desconectado, cuota superada o no aprobado | No fuga de llave/PII ni geolocalización; aparece texto de ubicación (browser) |
| N10 | Usuarios concurrentes publican basados en misma revisión | Sólo un CAS gana; el otro recibe conflicto sin sobrescritura (API+DB efímera) |
| N11 | Caché de A calentada y host B lee mismo slug/ruta | No sirven HTML/branding/media/preview de A a B (CDN simulada) |
| N12 | Draft/página retirada se solicita por sitemap/robots o crawler | Ausente de sitemap; noindex/no-store; canonical no apunta a privado (SEO) |
| N13 | Error upstream/SEO o JSON inválido | Respuesta fail-closed, sin secreto, stacktrace, token o PII, con noindex apropiado (HTTP) |
| N14 | Cambio editorial provoca regresión a rutas tienda/ERP | Pruebas existentes siguen aprobadas y rollback normal posible; sin override de rutas ni permisos |

## Oráculos de aprobación futuros y niveles de prueba

- **Contrato/API (.NET 10):** ownership servidor, denegaciones A/B, permisos JWT/RBAC, sanitización y mensajes neutros; se requieren pruebas positivas y negativas y SHA exacto.
- **Integración MySQL/EF:** índices tenant/version, snapshots, transacciones, concurrencia CAS, rollback y auditoría; usar servicio efímero en CI, nunca base productiva.
- **Angular 22.2.1 / Playwright:** semántica de bloques, accesibilidad 320px/teclado, privacidad, carga del mapa opcional y fallbacks; no sustituye control API.
- **SEO/SSR/Proxy:** host confiable, canonical, robots, sitemap, drafts noindex/no-store y separación cache; verificar con host A/B y cabeceras falsificadas.
- **Seguridad y operación:** cuotas, secret scanning, no claves cliente, métricas sin PII; aceptar solo proveedores aprobados por autoridad comercial.

**Criterio de aprobación futura:** todos los escenarios aplicables deben tener artefacto, comando reproducible, ambiente aislado, resultado y SHA exacto; FAIL/SKIPPED/CANCELLED/queued/in_progress jamás PASS obligatorio. Las decisiones D01–D08 y gates M02/M03 deben estar resueltos antes de implementar; aprobación documental no desbloquea runtime ni QA/PROD.

## Matriz de trazabilidad hacia contratos y riesgos

| Frontera | Positivos | Negativos | Riesgos/salidas previas |
| --- | --- | --- | --- |
| Tenant/host/ownership B01/B05 | P01,P12 | N01,N02,N04,N11 | NR01/NR12, R01/R11, G01/G08 |
| Edición/moderación/publicación B02/B08 | P02,P03,P04,P11,P13 | N03,N05,N10 | NR02/NR03/NR09, R02/R03/R09 |
| Sanitización/medios B04 | P05 | N06,N13 | NR04, R04, G03 |
| Ubicación/horarios/contacto B06 | P06,P07 | N07,N08 | NR05/NR06/NR07, R05/R06 |
| Mapa/accesibilidad B07 | P08,P09,P14 | N09 | NR08/NR11, R07/R08 |
| SEO/cache/publicación B03/B05 | P10,P12 | N05,N11,N12,N13 | NR10/NR12, R10/R11 |
| ERP/rollback/fronteras generales | P13,P14 | N10,N14 | R09/R12 y contrato de no-regresión |

## Alcance efectivo y auditorías de esta microtarea

Sólo este archivo y una sección indivisible de `CHANGELOG_AI.md` cambian. No modifica Domain/Application/Infrastructure/API, DB, JWT/RBAC, Angular, mapa externo, DNS, workers, eventos ni scripts de CI. **A0:** fuente V5 y DAG individual F=VALIDADO G=SI, autor exclusivo y matriz 14P/14N completa. **A1/A2/A3 documentales:** comprobar cobertura observable, límites A/B y contradicciones; las 28 pruebas runtime anteriores son `NO_EJECUTADAS`, no PASS. **A4:** diff de dos archivos Markdown, commit fast-forward/CAS en dev, checks de Scope Lock y procedencia V5 completed/success SHA exacto, lectura PLAN/COLA/BITACORA. **A5:** revisión independiente posterior por otra Task Javier; la autora no se revisa. **Rollback:** git revert normal del commit, sin force-push ni alterar BITACORA histórica; QA/main/PROD requieren permiso separado.

**Dependencia de cierre M01.06:** no liberar sin M01.01–05, sus revisiones independientes exigibles y autorización individual del DAG; el documento M01.05 sólo define cómo comprobar en fases futuras.
