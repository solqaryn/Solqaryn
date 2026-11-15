# SOLQARYN V5 — C06-03.M02.06: dependencias y contrato de salida de sitios editoriales y mapas

**Clase:** cierre de contratos DOCUMENTALES M02; no acredita un CMS, mapas operativos o endpoints. **Autor:** PRIMARY DEV 01 JAVIER_CHATGPT (TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`). **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15.

## 1. Autoridad, preflight y no-alcance

La fuente ejecutable única es STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. El ID C06-03.M02.06 aparece en PLAN_MAESTRO fila 949 H=PENDIENTE sin responsable antes del claim, COLA G/O=SI y DAG_TECNICO fila 910 F=VALIDADO/G=SI. Claim por CAS exclusivo con readback H949=EN_PROGRESO, I949=TASK_ID propio. Los predecesores exactos del DAG son C06-03.M01.01–06+A5, C06-03.M02.01–05+A5 y C06-02.M01.06+A5. Todos se contrastaron por ID con PLAN K y documentación; las cinco revisiones M02 independientes son distintas de sus Tasks autoras.

Este documento NO modifica tablas, API, UI, JWT/RBAC, DNS, TLS, Hostinger, Cloudflare/R2, Aiven real, proveedores de mapas, ubicación privada del visitante, eventos, outbox, worker, colas, webhooks, Tasks, secretos, QA/main/PROD o pagos. La disponibilidad de esta unidad documental no libera por sí misma M03. No inventar estado `SI` ni tests ejecutados.

## 2. Cinco contratos fuente y evidencia verificable

| Unidad | Commit / blob preservado en dev | Resultado y límites |
| --- | --- | --- |
| C06-03.M02.01 | `08ee3779cebc0eef388abefb8b9b57e6085982bd` / `77d4e7a6f9a6327f6a4d3998bcfc56ccc80db013` | 15 entidades, 16 invariantes de ownership A/B y 12 pares futuros; sin tablas |
| C06-03.M02.02 | `7bb13fea68c56c6297136d39c3424d985bf28a46` / `ba46a9f4ad3a54a3db7606d8dcd9e4ef8bd9b0f0` | 10 operaciones/DTO, 16 errores, 12 pares futuros; sin controladores |
| C06-03.M02.03 | `946b54d6ff573a6b63dc8ab5f6bd469f3bfa1ea9` / `838504982fe29aa65b170e03717d212056f75519` | 12 estructuras propuestas, 10 índices, seis fases de migración y 12 fixtures futuros; sin DDL |
| C06-03.M02.04 | `678586324a92eaf8f5d8650983af50d27ddf7eb9` / `70f0a81ebf3fea4577729b73a3c2ed1833e14fa4` | 20 opciones/flags tipados con defaults restrictivos y 12 pares futuros; sin flags runtime |
| C06-03.M02.05 | `bf1f19dc2d2efc29cfb0fbf7ba6a91caf429c463` / `81bbd49a054f328caaf0e861065d8ab0d43441a8` | 12 eventos-intención, 12 invariantes idempotentes, 12 pares futuros; NO eventos emitidos |

Cada entrega anterior figura LISTO con A5 independiente evidenciada en PLAN K (M02.01–03 y M02.05 revisadas por Task 06; M02.04 revisada por Task 01 de autoría Task 06). Los diffs fuente consisten en su documento y sección autocontenida de CHANGELOG, sus blobs persistían en el HEAD de la revisión y Scope Lock/V5 exact-SHA finalizaron SUCCESS; una compilación backend cancelada o en curso no se contó como PASS. La terminal C06-03.M01.06 (SHA `0dc07b4509e3f74c9b867fc450b3b5abda63075b`, blob `e51e254064bcaf53449ea66efeac7b5612966121`) conserva dieciséis gates G01–G16 y ocho escenarios RB01–RB08, nunca sustituibles por esta aprobación documental.

## 3. Dieciséis gates de entrada a futura implementación M03

| Gate | Evidencia causal que deberá exigirse antes de activar runtime |
| --- | --- |
| G01 | Binding host→tenant aprobado y verificado server-side; Host/Forwarded no son autoridad |
| G02 | Tenant A/B segregado antes de queries, caché, paginación, assets y SEO |
| G03 | JWT vigente, membresía activa y RBAC Editor/Moderador/Publicador separados |
| G04 | Draft y revisión tipada; rechazo de HTML/JS no permitido, rutas maliciosas y traversal |
| G05 | Publicación sólo de revisión inmutable, aprobación separada y CAS auditable |
| G06 | Preview revocable/expirable, no-store/noindex; nunca entra en sitemap público |
| G07 | Moderación con actor distinto y fail-closed ante revocación o cambio de versión |
| G08 | Contacto, dirección y horarios públicos aprobados por tenant y sucursal, con TZ válida |
| G09 | Mapa opt-in: consentimiento, proveedor/cuota/clave aprobados y fallback textual sin SDK |
| G10 | No solicitar, inferir ni almacenar ubicación privada del visitante para mostrar dirección |
| G11 | SEO canonical/robots/sitemap sólo para publicación/host autorizados, sin PII |
| G12 | Caché particionada por tenant+host+versión; borrar proyecciones obsoletas sin cruzar A/B |
| G13 | EF Core 10/MySQL 8.4 efímero, PK/FK tenant-bound, EXPLAIN y rollback/restauración probados |
| G14 | API con errores neutros y correlationId saneado, sin JWT, claves, URLs firmadas ni datos B |
| G15 | Angular 22.2.1 accesible a teclado/foco/320px/reduced motion, tests browser reales |
| G16 | Promoción QA/main/PROD, DNS/TLS, R2, costos o servicios externos requieren aprobación independiente |

**Estos gates NO están satisfechos por el cierre M02.** Si falla host binding, rol, versión, aislamiento, auditoría, consentimiento, mapa o prueba, el futuro código debe denegar de manera segura, no degradar permisos ni escoger tenant B. La fase M03 no consiste en escribir especificaciones: requiere código compilable y pruebas CI pertinentes; este documento no declara accesibilidad, pentest, rendimiento, pruebas de mapa ni migraciones realizadas.

## 4. Interfaces futuras y pruebas positivas/negativas requeridas

| Capa | Entregable futuro verificable | Negativo obligatorio |
| --- | --- | --- |
| Domain | Estados Site/Revision/Page/Section y ownership por TenantId | Tenant B/ID forjado, revisión obsoleta, HTML activo |
| Application | Validación JWT/RBAC, moderación independiente, publicación CAS y auditoría | Rol revocado, editor autoaprobador, preview expirado |
| Infrastructure | EF 10/MySQL efímero, filtros A/B, índices tenant-bound, rollback y snapshots | FK cruzada, clave idempotente reutilizada, restore fallido |
| API | DTO seguros, Host/proxy confiables, errores neutros y cache por tenant | Host forjado, consulta privada B, versionado incompatible |
| Angular | UI editable/publicable permitida, preview noindex, fallback textual, teclado/320px | UI flag no concede permisos, terceros sin consentimiento |
| Operación | Correlación sin secretos/PII y reintento acotado; CI y rollback reales | Duplicado/outbox no autorizado, bucle/consumo no acotado |

Todo lo anterior son requisitos **FUTUROS_NO_EJECUTADOS**. Su validación positiva y negativa debe realizarse sobre código real (xUnit, Angular/Playwright, integración MySQL efímera, Scope Lock SHA exacto) en microtareas autorizadas, con respeto al DAG y al propietario. La cuenta GitHub compartida no prueba independencia A5: únicamente TASK_ID distinto y evidencia real.

## 5. Ocho planes de rollback propuestos, no ejecutados

| Caso | Contrato de degradación y restauración futura |
| --- | --- |
| RB01 | Host o alias incorrecto: denegar, retirar vínculo no aprobado sin redirigir a B |
| RB02 | Rol/preview revocado: invalidar grants y caché, conservar auditoría minimizada |
| RB03 | Draft en SEO: eliminar indexación, no-store/noindex, reconstruir sitemap aprobado |
| RB04 | Publicación equivocada: CAS a última revisión aprobada y conservar historial/drafts |
| RB05 | Cache A/B cruzada: purgar solo partición tenant+host+versión y verificar pruebas negativas |
| RB06 | MySQL/EF migración fallida: restore probado en instancia efímera; prohibido datos reales |
| RB07 | Mapa/proveedor caído: mostrar dirección textual autorizada sin scripts ni ubicación privada |
| RB08 | DNS/TLS/CDN/cloud no autorizado: no ejecutar cambio, detener despliegue y documentar decisión |

Rollback de este **cambio documental** exclusivamente: `git revert` normal desde HEAD actualizado tras verificar cambios concurrentes; conservar BITACORA histórica, nunca force-push ni reset.

## 6. Auditorías de esta microtarea y condición de salida

**A0:** Skill canónica, identidad, Scope Lock STRICT y checks del HEAD; PLAN H949/I949, COLA G/O, DAG F/G, cinco documentos y sus A5 auténticas, M01.06 y C06-02.M01.06, cambios acotados. **A1 documental:** cinco fuentes, 16 gates, seis rutas por capas y ocho rollback, coherencia con objetivo editorial/mapas; tests futuros no se cuentan. **A2 estática:** revisar tenant A/B, host/proxy, JWT/RBAC, CAS, moderación, PII, preview, mapas opt-in, cuotas y errores fail-closed. **A3 documental:** integración por contratos M01/M02, Domain/Application/Infrastructure/API/Angular, rollback/caché/SEO sin alteración de ejecución. **A4:** publicar por fast-forward/CAS expected SHA en dev con diff de solo este Markdown y CHANGELOG autocontenido; Scope Lock y procedencia V5 deben ser `completed/success` en exact-SHA, luego readbacks de PLAN/COLA/DAG/BITACORA. **A5:** otro TASK_ID Javier distinto del autor, revisión original independiente, antes de desbloquear M03 técnica.

Compilaciones backend/frontend, Playwright, MySQL, geocoding y mapas son NO_APLICA para un diff exclusivamente Markdown y **nunca PASS si `queued`, `in_progress`, `cancelled`, `skipped` o `failed`**. Este documento NO implementa eventos ni autoriza webhook/dispatcher, PR/branch extra, secretos, migraciones, QA/main/PROD, host real, proveedores o compras.
