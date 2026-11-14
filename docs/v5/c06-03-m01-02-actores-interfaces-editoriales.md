# SOLQARYN V5 — C06-03.M01.02: actores, entradas, salidas y superficies editoriales

**Unidad:** C06-03.M01.02 (PLAN_MAESTRO fila 939), **autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`; **fecha:** 2026-11-14; **autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); **único ámbito:** `solqaryn/Solqaryn`, `dev`, Scope Lock STRICT. **Entrega documental**, no implementación.

## Propósito y estado real

El alcance futuro de C06-03 incluye páginas por empresa, bloques de contenido, ubicación, horarios y contactos públicos, y un mapa opcional. Este documento distingue actores, entradas, salidas y límites de confianza exigibles para una implementación futura con moderación, sanitización, accesibilidad y SEO. **No crea** editor, rutas, datos, API, publicación, mapa, geocodificación, proveedor, servicios, webhooks, secretos, tokens ni RBAC. El DAG `VALIDADO/SI` de esta M01 permite solo preparación; M01.06 y los cambios runtime requieren gates posteriores.

Se cotejan las fuentes siguientes en `dev`: `docs/v5/c06-03-m01-01-alcance-sitios-editoriales-mapas.md` (blob `d16fd73d5211161bdde429a3b705ec9b9d09cdcf`), `frontend/src/app/app.routes.ts` (`2f560c14693455b79ac4546e9333fb74586ef234`), `frontend/src/app/features/storefront/storefront-identidad.service.ts` (`c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f`), `frontend/src/app/core/interceptors/auth.interceptor.ts` (`4bdf19135ed1932a99fd09ace1f900bcccc5b25b`), `frontend/src/app/core/guards/auth.guard.ts` (`b652a7af6343bdae83a4dc9f3065d9fc1aa120f6`), `frontend/server/seo-utils.js` (`b0886e30cc85efa6f800e558d5284412d8c573fc`), `frontend/api/robots.js` (`7ae514cfa9d5d645caba899dc10d2ccf0a9b34f9`) y `frontend/api/sitemap.js` (`ee50880520c906178a7ebd31553f3ab6d28f6b2d`). Las rutas y SEO existentes atienden principalmente la tienda; **no** prueban un CMS editorial. Esta lectura acotada no certifica MySQL/API/HTTP ni ausencia de otro código.

## A01–A12: actores y autorización futura

| ID | Actor | Entrada esperada | Salida o denegación exigida |
| --- | --- | --- | --- |
| A01 | Visitante anónimo | Host/slug público aprobado | Página publicada, nunca borrador, PII o administrador |
| A02 | Cliente/lector | Búsqueda y navegación pública | Horarios/contacto publicados de la empresa correcta |
| A03 | Editor de empresa | JWT, tenant servidor y permiso editar | Borrador propio; denegar publicación implícita |
| A04 | Moderador | Contenido pendiente y permiso moderar | Aprobar/rechazar contenido del mismo tenant con evidencia |
| A05 | Publicador | Versión aprobada y permiso publicar | Snapshot versionado/idempotente, auditoría y CAS |
| A06 | Admin de empresa | Configuración editorial de su empresa | Solo marca, sucursal y recursos de su ámbito |
| A07 | Operador global | Rol global auditado | No confundir control-plane con editor ni omitir autorización |
| A08 | Contacto responsable | Teléfono/correo/horario propuesto | Publicación solo con ownership y aprobación |
| A09 | Visitante que usa mapa | Elección explícita de ver ubicación | Mapa opcional y alternativa textual; no geolocalización forzada |
| A10 | Proveedor cartográfico | Coordenadas públicas mínimas autorizadas | Sin secreto expuesto ni envío innecesario de PII |
| A11 | Buscador/crawler | Ruta canónica pública | Solo contenido publicado, draft/preview noindex |
| A12 | Auditor/revisor | Log con identificadores mínimos | Trazas de cambios sin JWT, secretos o PII privada |

**Estos permisos son FUTUROS**; ningún rol nuevo se concede con este documento.

## I01–I12: interfaces, datos y respuestas seguras futuras

| ID | Interfaz prevista | Entrada / control servidor | Salida o rechazo |
| --- | --- | --- | --- |
| I01 | Resolver página pública | Host/tenant verificado + slug normalizado | Versión publicada del tenant, o 404 neutro |
| I02 | Índice de bloques | Página aprobada, orden, tipo permitido | Bloques renderizables sanitizados, sin scripts |
| I03 | Edición de borrador | JWT+tenant+RBAC, revisión de versión | Guardar solo borrador propio con CAS |
| I04 | Flujo de moderación | Identificador de revisión y permiso distinto | Estado aprobado/rechazado auditado |
| I05 | Publicación | Versión moderada + RBAC publicar | Snapshot y evento idempotente, o conflicto |
| I06 | Perfil de ubicación | Coordenadas públicas, precisión y consentimiento | Dirección apta, sin domicilio privado |
| I07 | Horarios/sucursales | Zona horaria, excepciones y ownership | Horarios de sucursal correcta o fallback claro |
| I08 | Contacto | Teléfono, email, URL validados | Información aprobada, enlaces seguros |
| I09 | Mapa externo opcional | Provider aprobado y parámetros mínimos | Mapa con alternativa textual y no tracking obligatorio |
| I10 | SEO/canonical/robots | Host y versión publicada confirmados | Indexación solo autorizada; drafts noindex |
| I11 | Cache y medios | Tenant, versión, visibilidad y asset autorizado | Caché particionada; no fuga entre A/B |
| I12 | Auditoría/reversión | Rol/versión y motivo autorizado | Revert normal sin borrar historial |

**Flujos propuestos, no existentes:** lector → resolución segura en backend → página publicada → HTML accesible, horarios/contacto y mapa opcional; editor → JWT/tenant autorizado → borrador → moderación → publicación versionada → auditoría. Ni cabecera Host ni `X-Empresa-Id` del cliente se convierten en autoridad.

## B01–B08: fronteras de confianza

- **B01** Visitante anónimo vs tenant y contenido publicado, con denegación neutra a host/slug no autorizado.
- **B02** Editor/tenant vs moderador/publicador: separación RBAC y ownership por objeto.
- **B03** Borrador/preview vs web pública y robots/sitemap: noindex, no-store y expiración cuando corresponda.
- **B04** Contenido editorial externo vs HTML/renderizador: allowlist de bloques, sanitización server-side y CSP cuando se implemente.
- **B05** Empresa A vs B: rutas, branding, imágenes, caché y errores nunca se comparten.
- **B06** Ubicación/horarios/contacto públicos vs PII privada, consentimiento, geolocalización y zona horaria.
- **B07** Mapa/proveedor externo vs secretos, cuotas, privacidad y accesibilidad: no activar ningún proveedor por esta fase.
- **B08** Publicación/versionado vs concurrente/rollback; DNS, PROD y seguridad permanecen fuera de autorización.

## Oráculos P01–P06 y N01–N12 FUTUROS NO_EJECUTADOS

**Positivos futuros:** P01 lector de A recibe página aprobada de A; P02 editor guarda borrador propio pero no publica; P03 moderación registra decisión y usuario; P04 horarios/sucursal/zona correctos y texto alternativo al mapa; P05 crawler ve solo URL canónica publicada; P06 rollback recupera versión y mantiene historia.

**Negativos futuros:** N01 Host/slug de B no muestra A; N02 editor sin permiso de publicación es denegado; N03 borrador no se indexa; N04 HTML malicioso no ejecuta scripts; N05 contacto externo no verificado se rechaza; N06 coordenada privada no se expone; N07 horario de otra sucursal no se mezcla; N08 proveedor de mapa caído no oculta información textual; N09 caché de A no se sirve a B; N10 cambio concurrente pierde CAS sin sobrescribir; N11 no hay geolocalización automática; N12 error/log no filtra JWT, datos privados ni secretos.

**No se ejecutaron esos escenarios en browser/API/MySQL.** Son criterios para M03/M05 y revisión posterior.

## Impacto, controles y rollback

Solo cambia `docs/v5` y `CHANGELOG_AI.md`. No se tocan Domain/Application/Infrastructure/API, Angular, JWT/RBAC, migraciones, DB, workers, SMTP, Cloudinary, mapas externos, DNS/SSL, hosting, secretos, métricas ni datos. Dependencias preparatorias según `DAG_TECNICO!A900:J900` F=VALIDADO G=SI, PLAN y COLA; no implican autorización productiva.

A0: correspondencia con Plan, M01.01 y blobs fuente. A1–A3 documentales: revisar que actores, entradas, salidas y fronteras cubren positivo y negativo; runtime **NO_APLICA**, jamás PASS inventado. A4: verificar publicación fast-forward CAS y exact-SHA, Scope Lock y procedencia CHANGELOG SUCCESS, luego readback PLAN/COLA/BITACORA. A5 independiente requerido de otra Task Javier; no autoaprobación. Rollback con `git revert` normal y evidencia aditiva; no QA/main/PROD.
