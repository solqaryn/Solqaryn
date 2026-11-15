# SOLQARYN V5 — C06-03.M01.06: bloqueos, reversión y salida de sitios editoriales y mapas

**Fecha:** 2026-11-15. **Autor:** PRIMARY DEV 04 JAVIER_CHATGPT (`6aeacb7820ac8191b7d768a6f33dfa09`). **Único ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** Standard V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Alcance y requisitos de entrada

Este cierre **documental de M01** consolida los bloqueos para un futuro módulo de páginas editoriales por tenant, horarios, dirección, contacto, mapas opcionales, moderación, SEO y publicación. No constituye un CMS implementado ni activa dominio, geocodificación, mapa, API, schema EF/MySQL, Angular, JWT/RBAC, CDN, DNS, R2, proveedores externos ni producción.

Preflight por ID exacto: `C06-03.M01.06` corresponde a PLAN fila 943, H=PENDIENTE/I libre antes de la reclamación, COLA G/O=SI y DAG_TECNICO fila 904 F=VALIDADO/G=SI. Las cinco unidades previas están LISTO con A5 independientes acreditados en PLAN K, sin atribuir aprobación a sus autores. El gate DAG también incluye predecesores cruzados de C02, C05 y C06-02; no se sustituyen por este documento.

## 2. Procedencia y evidencia de las cinco entradas

| Entrada | Blob observado en dev | Aporte y límite |
| --- | --- | --- |
| M01.01 — `docs/v5/c06-03-m01-01-alcance-sitios-editoriales-mapas.md` | `d16fd73d5211161bdde429a3b705ec9b9d09cdcf` | NR01–NR12, no regresión y exclusiones |
| M01.02 — `docs/v5/c06-03-m01-02-actores-interfaces-editoriales.md` | `c5f815be7ef044ffb2cd4e4910aa97dff511731a` | A01–A12, I01–I12, B01–B08 |
| M01.03 — `docs/v5/c06-03-m01-03-inventario-fuentes-editoriales.md` | `c4434ef2fc3cfb351a0ea5d5c819c0b5d2d560b7` | S01–S12, brechas G01–G08 abiertas |
| M01.04 — `docs/v5/c06-03-m01-04-riesgos-supuestos-decisiones-editoriales.md` | `76eda76336c6046d835583ac7080b7ec37f3762b` | R01–R12 riesgos potenciales, S01–S08 supuestos y D01–D08 decisiones pendientes |
| M01.05 — `docs/v5/c06-03-m01-05-criterios-aceptacion-editoriales-mapas.md` | `3b4ec22673eea45b78772e235280040ce447fd53` | P01–P14 y N01–N14, FUTUROS NO_EJECUTADOS |

Las fuentes de código de M01.03/M01.04 incluyen `frontend/src/app/app.routes.ts`, `frontend/server/seo-utils.js`, `frontend/api/sitemap.js` y `frontend/src/app/features/storefront/storefront-identidad.service.ts`. Ya soportan comportamientos de tienda y SEO acotados; no prueban una autorización general de dominio→tenant ni sitios editoriales publicados. La lectura de `X-Forwarded-Host` en `hostFromRequest` requiere futuro análisis de confianza de proxy; no es evidencia por sí sola de exploit ni de aislamiento certificado.

## 3. G01–G16: bloqueos vinculantes para M02 y posterior código

| Gate | Condición para cualquier avance técnico futuro |
| --- | --- |
| G01 | Resolver tenant por binding host aprobado en backend; host, slug y cabeceras no dan autoridad |
| G02 | Rechazar contenido A/B cruzado antes de consulta, caché, render o paginación |
| G03 | Separar Editor, Moderador y Publicador por permisos backend y membresía vigente |
| G04 | Nunca mostrar borradores en rutas, robots, sitemap ni canonical públicos |
| G05 | Preview necesita grant revocable/expirable y respuesta no-store/noindex |
| G06 | Publicación requiere versión inmutable, aprobación distinta, CAS y auditoría |
| G07 | Bloques usan tipos cerrados, sanitización y CSP; no ejecutar HTML/JS libre |
| G08 | Contactos y horarios deben atribuirse a su tenant/sucursal verificada |
| G09 | Ubicación pública debe ser aprobada; no inferir ubicación privada del visitante |
| G10 | Mapas externos son opcionales, requieren consentimiento, política de cuota, seguridad de claves y autorización comercial |
| G11 | No desplegar geocodificador, scripts de terceros, servicios pagos ni cookies de tracking sin aprobación |
| G12 | Canonical/robots/sitemap verifican host, tenant, versión publicada y exclusión de PII |
| G13 | CDN y assets públicos exigen ownership; caché particionada por tenant+host+versión |
| G14 | Accesibilidad: teclado, foco, ARIA, 320px, movimiento reducido y carga ligera; CI browser real futura |
| G15 | Persistencia y migraciones exigen EF/MySQL 8.4 efímero, backup/rollback y seguridad A/B; ninguna DB real ahora |
| G16 | QA/main/PROD, DNS/TLS, Cloudflare/R2, pagos y configuraciones reales necesitan autorización separada, no un gate documental |

**G01–G16 siguen siendo puertas futuras abiertas**. M01.06 no puede presentar una decisión D01–D08 pendiente como aprobada ni certificar P/N de aceptación como ejecutadas. Un error, permiso revocado, host no validado o contexto nulo devuelve denegación segura, no otro tenant por defecto.

## 4. RB01–RB08: reversión y controles de fallo futuros

| Caso | Respuesta y reversión prevista |
| --- | --- |
| RB01 — Binding erróneo | Detener publicación, retirar alias no aprobado, conservar propietario registrado y no redirigir a B |
| RB02 — Rol revocado | Denegar inmediatamente, conservar evento minimizado, no elevar permiso visual |
| RB03 — Draft público | Invalidar preview, noindex/no-store, purgar índice y revisar sitemap sin borrar evidencia |
| RB04 — Versión equivocada | Restaurar última revisión aprobada con CAS y auditoría; preservar drafts y facturas |
| RB05 — Cache cruzada | Invalidar solo partición tenant/host/versión; bloquear hasta pruebas A/B |
| RB06 — Mapa/proveedor caído | Fallback textual de dirección aprobada, sin filtrar API keys o geolocalización |
| RB07 — Migración fallida | Backup y restore probados en MySQL efímero; no ejecutar sobre Aiven real aquí |
| RB08 — DNS/TLS/CDN fallido | No publicar sin autorización, smoke, host binding y plan de reversión independiente |

No hay comandos ejecutados ni transacciones reales de rollback en este documento. Las acciones RB01–RB08 son contratos propuestos para una fase autorizada posterior.

## 5. Handoff M02, pruebas futuras y salida real

**M02 sólo con gate institucional vivo:** después de que esta M01.06 sea LISTO_DEV y reciba A5 independiente, releer DAG y COLA por cada ID. La documentación M02 puede modelar entidades/ownership, DTO, persistencia, opciones, eventos conceptuales y criterios terminales. No se crean APIs, DNS, mapa ni migración por tener un documento M02. Los estados VALIDANDO no liberan una dependencia técnica que exige LISTO+A5.

A1 futuro: .NET 10 con permisos backend, ownership A/B, rutas, revisión CAS y denegación de borrador. A2 futuro: JWT/RBAC, proxy, sanitización de bloques, PII/SEO, cache A/B y claves de terceros. A3 futuro: Angular 22.2.1/Playwright en navegador, 320px, teclado, foco, mapa con fallback, MySQL efímero cuando exista persistencia. A4: checks obligatorios `completed/success` sobre SHA exacto más readbacks; CANCELLED/SKIPPED/IN_PROGRESS nunca PASS requerido. A5 por una Task Javier distinta de la autora.

P01–P14/N01–N14 de M01.05 son **escenarios NO_EJECUTADOS**, no se contabilizan como tests reales. La prueba de esta entrega es documental: cotejo de las cinco fuentes y sus blobs, gates, procedencia y salida segura. Exigir Scope Lock y procedencia V5 SUCCESS sobre el commit que incorpore este documento y sección autocontenida de `CHANGELOG_AI.md`; compilaciones runtime no causales a Markdown se registran NO_APLICA. Reversión con `git revert` normal en dev, manteniendo BITACORA histórica intacta. Sin QA/main/PROD.
