# SOLQARYN V5 — C06-03.M02.04: configuración tipada, ámbitos y feature flags seguros

**Alcance DOCUMENTAL:** especificación de configuración futura para sitios editoriales/mapas; NO implementa opciones .NET/Angular, base MySQL, API, worker, geocodificación, SDK, servicio pago ni evento. **Autor:** PRIMARY DEV 06 JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Repositorio:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

## 1. Fuentes y límites
Autoridad: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA) y Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU. Preflight por ID exacto: PLAN_MAESTRO fila 947 PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI; claim exclusivo mediante findReplace atómico de H947 con una ocurrencia, luego owner TASK_ID en I947 con readback. C06-03.M01.06 (SHA 0dc07b4509e3f74c9b867fc450b3b5abda63075b) publicó G01–G16 y RB01–RB08 con A5 independiente. M02.01 (SHA 08ee3779cebc0eef388abefb8b9b57e6085982bd) define 15 estructuras y 16 invariantes; M02.02 (SHA 7bb13fea68c56c6297136d39c3424d985bf28a46) define contratos API/DTO/errores futuros; M02.03 es una especificación de persistencia independiente, nunca una migración aplicada. Las dependencias M02 no autorizan M03 ni QA/main/PROD.

El backend debe resolver actor, tenant, rol, propiedad de sitio/sucursal, aprobación editorial y host confiable antes de evaluar flags. No derivar permisos ni tenant de Host, Forwarded, slug, localStorage, cookie del navegador o flags UI. La UI solo refleja acceso; JWT/RBAC y ownership se verifican server-side. Mapas son opcionales: dirección textual PUBLICADA y aprobada es fallback, nunca ubicación privada del visitante.

## 2. Catálogo de veinte opciones TIPADAS PROPUESTAS, no instaladas

| ID | Nombre conceptual / tipo | Ámbito | Default y control fail-closed |
| --- | --- | --- | --- |
| CFG01 | editorial.enabled / bool | tenant | false; no editor habilitado |
| CFG02 | editorial.draftMutationsEnabled / bool | tenant/site | false, además RBAC Editor activo |
| CFG03 | editorial.moderationRequired / bool | plataforma | true no rebajable |
| CFG04 | editorial.separatePublisherRequired / bool | plataforma | true; no autoaprobación |
| CFG05 | editorial.previewEnabled / bool | tenant/site | false; grant revocable |
| CFG06 | editorial.previewTtlSeconds / entero positivo opcional | plataforma | sin valor => negar preview; TTL requiere decisión autorizada |
| CFG07 | editorial.publicationEnabled / bool | tenant/site | false; CAS y auditoría obligatorios aun si true |
| CFG08 | editorial.approvedHostBindingRequired / bool | plataforma | true; host desconocido => 404 neutro |
| CFG09 | editorial.publishedSeoOnly / bool | plataforma | true; draft/preview noindex no-store |
| CFG10 | editorial.publicContactEnabled / bool | site | false; solo contacto expresamente aprobado |
| CFG11 | editorial.publicHoursEnabled / bool | site/location | false; horarios vigentes y TZ real |
| CFG12 | editorial.explicitTimeZoneRequired / bool | plataforma | true; jamás inventar abierto ahora |
| CFG13 | editorial.mapMode / enum TEXT_ONLY, LINK_APPROVED, EMBED_APPROVED | tenant/site | TEXT_ONLY; sin terceros por default |
| CFG14 | editorial.mapProvider / enum NONE o proveedor allowlist | plataforma | NONE; cero proveedor alternativo |
| CFG15 | editorial.mapConsentRequired / bool | plataforma | true; sin consentimiento no cargar tercero |
| CFG16 | editorial.mapQuotaPerPeriod / entero positivo opcional | plataforma | ausente => cero solicitudes externas |
| CFG17 | editorial.visitorGeolocationAllowed / bool | plataforma | false; no perfilar visitantes |
| CFG18 | editorial.publicAssetsEnabled / bool | tenant/site | false; verificar ownership de cada asset |
| CFG19 | editorial.auditStrict / bool | plataforma | true; fallo de auditoría bloquea publicación |
| CFG20 | editorial.cachePartitionMode / enum TENANT_HOST_REVISION | plataforma | modo fijo; contexto inválido => no cache pública |

**No se agregan** estas claves a appsettings, opciones DI, UI ni almacenamiento. Son contratos para fases de implementación, no evidencia de que exista un servicio/feature flag. Los nombres concretos se reconciliarán con clases reales antes de desarrollarlos. TTL, cuota, presupuesto, proveedor, consentimiento y claves comerciales exigen datos/autorizaciones adicionales: jamás inferir valores en cero falsamente aprobados ni introducir secretos en repo.

## 3. Semántica de precedencia y seguridad

1. Política plataforma establece máximos de seguridad inmutables: tenant A/B, JWT y membresía vigente, separación Editor/Moderador/Publicador, binding host aprobada con proxy confiable, auditoría, sanitización, consentimiento y cache tenant/host/revisión.
2. Configuración tenant verificado **solo restringe** política plataforma; site y sucursal con ownership real pueden restringir tenant, jamás ampliarlo. Un true UI/site no revoca un false en nivel superior ni concede permisos. No existe tenant default a partir del Host.
3. Valores nulos, mal tipados, enum desconocido, versión obsoleta, error de lectura, membresía revocada, host ambigua o proveedor sin autorización producen denegación segura. Cambios requieren versión y auditoría de actor/tenant minimizados.
4. Publicación requiere revisión aprobada, CAS esperado, permisos server-side y auditoría estricta. Si la persistencia/auditoría falla, no afirmar publicación. Preview siempre privado, revocable, no-store/noindex, sin sitemap o cache compartida.
5. Mapas nunca capturan geolocalización del visitante por defecto, no cargan JS, tiles, SDK, tracker ni hacen llamadas externas sin opt-in, permiso comercial, cuota y consentimiento; si no hay proveedor válido, proyectar solo dirección pública textual aprobada.
6. Contacto, horarios, assets, sitemap y cache solo reflejan versión PUBLICADA y sucursal aprobada del mismo tenant. Intervalos de horarios sin TZ validada no se publican como vigentes; no inferir apertura actual.

## 4. Doce pares positivo/negativo de aceptación futura, NO EJECUTADOS

| Par | Positivo futuro | Negativo exigido |
| --- | --- | --- |
| PN01 | Tenant A activa editor bajo política válida | Tenant B no hereda ni lee flag de A |
| PN02 | Editor A autorizado modifica draft con CAS | Editor B/revocado no escribe aunque flag=true |
| PN03 | Moderador distinto revisa draft | Editor no se autoaprueba por flag frontend |
| PN04 | Publicador autorizado publica revisión auditada | Versión vieja o fallo auditoría bloquea |
| PN05 | Preview vigente noindex/no-store | Token expirado o revocado no expone draft |
| PN06 | Host aprobado A muestra publicación A | Host desconocido o forwarded spoofed no revela B |
| PN07 | Contacto público aprobado aparece en A | Datos privados o contacto B no sale |
| PN08 | Horario con TZ/sucursal verificados se proyecta | TZ ausente o sucursal B no inventa horario |
| PN09 | Sin mapas se muestra dirección pública textual | Ningún SDK externo, tracking ni geolocalización privada |
| PN10 | Enlace de mapa opcional autorizado y consentido | Sin cuota/proveedor/consentimiento => cero requests |
| PN11 | Asset A público aprobado se sirve con owner | Asset privado o B no expone URL |
| PN12 | Revocación de feature corta nuevos accesos | Cache/SEO de draft o tenant B nunca persiste |

Estas filas NO son resultados xUnit, ASP.NET Core HTTP, Angular, MySQL, Chromium, geocoding o proveedor. La implementación posterior exige CI dirigida sobre SHA aplicable, pruebas A/B positivas/negativas, permisos, cuota y rollback probado.

## 5. Impacto, auditorías A0–A5 y rollback

Impacto **actual**: este Markdown y sección autocontenida en CHANGELOG_AI.md; sin cambios a Domain, Application, Infrastructure, API, frontend, tablas, contratos ejecutables, red, worker o secretos. Impacto futuro por diseñar: Domain fija invariantes; Application aplica permisos/CAS; Infrastructure implementa persistencia/MySQL 8.4 y adaptadores aprobados; API ASP.NET Core 10 resuelve contexto y JWT; Angular 22.2.1 solo consume proyecciones seguras. No cambiar Render, Vercel, Aiven, Cloudinary, DNS, Hostinger, R2, Cloudflare, QA/main/PROD.

**A0 estática:** PLAN ID/H/I/K y claim atómico, COLA G/O, DAG F/G, M01.06 y A5, fuentes M02, arquitectura/skill/Scope Lock, ausencia de secretos. **A1 documental:** verificar 20 opciones, ámbitos, defaults restrictivos y 12 pares P/N sin llamarlos pruebas de runtime. **A2 estática:** tenant A/B, host/forwarded, RBAC, publicación CAS, privacidad/geo, mapa opt-in, assets y secretos. **A3 documental:** coherencia con 15 entidades de M02.01, 10 DTO/API propuestos M02.02 y migración aún futura M02.03, sin modificar ejecución real. **A4:** commit dev fast-forward/CAS HEAD esperado, diff solo documento y CHANGELOG; GitHub Scope Lock y V5 procedencia completed/success exact SHA; PLAN/COLA/DAG/BITACORA readback. Compilaciones, MySQL, Playwright y proveedores sin cambio runtime: NO_APLICA justificado, nunca convertir queued/in_progress/cancelled/skipped en PASS. **A5:** otra Task del allowlist Javier distinta de autora revisará diff, SHA, blobs, seguridad y CI causal. No habilitar un gate técnico que exija A5 hasta su PASS.

Rollback documental: git revert normal respetando HEAD concurrente, preservando BITACORA. Los ajustes reales futuros exigirán revocación de flags, preview/grants, host binding, cache por tenant y publicación CAS auditada. **PROHIBIDO** desplegar proveedor externo, webhooks, eventos, dispatcher, migraciones, geolocalización o servicios de pago por este diseño.
