# SOLQARYN V5 — C06-02.M01.06: bloqueos, reversión y contrato de salida de landing por tenant

**Microtarea:** C06-02.M01.06 (PLAN_MAESTRO fila 907), preparación M01 terminal. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente institucional única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Entorno:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

## Resultado comprobable y frontera de autoridad

Este contrato **documental** deja explícitas las condiciones para iniciar el diseño M02 de una landing pública administrable por empresa. No implementa una landing, host binding, preview, publicación, JWT/RBAC nuevo, persistencia, migración, CDN, DNS/TLS ni un proveedor. La salida de M01 no significa aprobación comercial de dominios, resolución efectiva de tenant, seguridad HTTP, E2E, despliegue a QA/main/PROD ni permiso para usar infraestructura de pago.

La disponibilidad institucional de esta unidad se derivó de PLAN_MAESTRO/COLA/DAG_TECNICO por ID exacto, no de una lista fija: `COLA!G/O=SI`, `DAG_TECNICO!F=VALIDADO,G=SI`, después de A5 independientes de `C06-02.M01.01–05` y los prerrequisitos calculados `C02-01.M01.06+A5` y `C05-02.M01.06+A5`. La validación en M01.06 **no equivale a A5**: M02 solo podrá continuar si su propio DAG recalculado da SI después de LISTO_DEV + A5 de esta unidad.

## Matriz causal de las cinco entradas documentales

| Unidad | Evidencia Git preservada | Aporte y exclusión |
| --- | --- | --- |
| M01.01 | `d749c986ad03bf20bded35a3afc87d2b05d4e9fd` | NR01–NR12: alcance, prohibiciones y salvaguardas; sin host binding runtime |
| M01.02 | `050ef36f55d80e367e097f4d4699718663c91d9d` | A01–A12, I01–I12, B01–B08: actores, interfaces y fronteras futuras |
| M01.03 | `3fd8977ff3a001e01178e81e08b206e8a46511e1` | S01–S14 fuentes inspeccionadas y G01–G10 brechas NO CERRADAS |
| M01.04 | `0001b11cfca6a136eec8627a338134c01f7e0fae` | R01–R12 riesgos potenciales, S01–S08 supuestos, D01–D08 decisiones PENDIENTES |
| M01.05 | `3d1c31001ffa89729dcfa9d712a9cd9f629d1da1` | AC01–AC14 y N01–N14: criterios FUTUROS NO_EJECUTADOS |

Los hashes son de documentos del árbol Git; no certifican pruebas HTTP, MySQL, browser, proxy o datos reales. La existencia de un storefront público y de `hostFromRequest` en `frontend/server/seo-utils.js` no demuestra un resolver seguro de host a tenant. La lectura de `x-forwarded-host` exige un futuro análisis explícito de proxy confiable; no se afirma explotación actual.

## G01–G16: puertas de bloqueo que el diseño M02 debe preservar

| ID | Bloqueo y condición para su posterior levantamiento |
| --- | --- |
| G01 | Host/slug nunca confiere autoridad. Resolver únicamente por mapeo server-side con propiedad verificada; rechazar spoofed Host/Forwarded. |
| G02 | Tenancy A/B: separar contenido, branding, rutas, assets, caché y metadatos; denegación por defecto ante contexto ausente. |
| G03 | Editor y publicador requieren permisos RBAC distintos en backend, membresía vigente, revocación y auditoría; guards Angular no autorizan. |
| G04 | Host sin validación/alias aprobado no crea binding, DNS, TLS ni exposición de marca; prueba de posesión fuera de esta unidad. |
| G05 | Preview solo con autorización, token temporal revocable, respuesta `no-store`/`noindex`; borrador jamás en sitemap. |
| G06 | Publicación futura utiliza revisión inmutable y CAS/versionado; un editor no publica mediante un flag UI. |
| G07 | Slug/ruta/host canonicalizados; bloquear traversal, homógrafos y colisiones antes de indexar. |
| G08 | Builder de bloques tipados sin HTML/JS/SQL libre, sanitización, CSP y límites de contenido. |
| G09 | SEO público: canonical, robots y sitemap solo para host aprobado y versiones publicadas; sin PII ni previews. |
| G10 | CDN/cache requiere clave por host+tenant+versión y control de proxy upstream; jamás mezclar A/B. |
| G11 | Assets de Cloudinary actual o futuro proveedor exigen ownership y política pública por tenant; R2 futuro sin activación. |
| G12 | Diseño de tablas, índices y migrations EF/MySQL queda pendiente de análisis, servicio efímero CI y rollback; no tocar DB real. |
| G13 | Fallo de integraciones retorna identidad neutra y error mínimo, sin secretos, URLs firmadas ni marca ajena. |
| G14 | Accesibilidad y ligereza: teclado/foco, 320 px, reduced motion, estados claros y presupuestos de carga; Playwright posterior. |
| G15 | Publicación exige logs sin token ni PII y correlación de cambios/rollback; ningún dispatcher/webhook se crea aquí. |
| G16 | QA/main/PROD, DNS/TLS, Hostinger, Cloudflare y proveedores reales necesitan autorización independiente, backup, smoke y reversión. |

**Clasificación:** G01–G16 son **condiciones de implementación futura abiertas**. Se prohíbe marcarlas aprobadas por esta documentación o utilizar el estado `LISTO` de M01 como evidencia funcional.

## Contrato de salida para M02 y límites de la futura M03

1. **Entrada documental a M02:** conservar los cinco artefactos y sus hashes; construir contratos tipados de entidades, DTO, errores, configuración, persistencia, eventos conceptualizados y dependencias sin cambiar runtime.
2. **Separación por capas:** Domain define invariantes; Application define casos de uso y política; Infrastructure implementará repositorios/EF y validación real de ownership; API sólo recibirá contexto ya verificado. Frontend no convierte `Host`, `X-Empresa-Id`, slug, cookie o `localStorage` en autoridad.
3. **No-regresión:** rutas públicas actuales de tienda, `dashboard` privado y mecanismos JWT/RBAC existentes no se sustituyen ni se abren por diseño de landing.
4. **Preflight de código futuro:** antes de M03 revisar componentes concretos, propuesta de API/DTO, modelado de host/proxy, seguridad A/B, migraciones reversibles y trabajo concurrente de otros owners.
5. **Pruebas exigidas en implementación:** .NET 10/xUnit positivos y negativos; Angular 22.2.1 con Playwright/Chromium donde afecte UI; EF/MySQL 8.4 efímero si existe persistencia; Scope Lock y seguridad de auth por SHA exacto.
6. **A5 independiente:** cada cierre necesita revisor de otro de los seis TASK_ID Javier, con diff, blobs, pruebas pertinentes y BITACORA. Solo un PASS real sobre la autoría distinta habilita la siguiente dependencia.

## RB01–RB08: estrategia de reversión futura

| ID | Escenario de fallo y protección |
| --- | --- |
| RB01 | Host equivocado: rechazar/retirar binding previsto y restituir mapeo aprobado, sin redirigir a tenant B. |
| RB02 | Permiso inválido: revocar acceso y conservar audit trail, nunca elevar roles o saltar filtros. |
| RB03 | Preview indexado: invalidar token, aplicar noindex/no-store y verificar robots/sitemap; sin borrar trazabilidad. |
| RB04 | Versión publicada incorrecta: restablecer última versión aprobada mediante CAS, preservar borradores y ventas. |
| RB05 | Cache contaminada: invalidar por tenant/host/version y comprobar A/B antes de reabrir exposición. |
| RB06 | Migración fallida: backup y restore probado en servicio MySQL efímero, sin ejecución sobre Aiven real por esta unidad. |
| RB07 | Proveedor de medios caído: degradar a fallback neutro sin filtrar assets/PII ni cambiar proveedores en producción. |
| RB08 | DNS/TLS/CDN no aprobado o defectuoso: no ejecutar cambio; ante despliegue futuro autorizado, plan de reversión y smoke separado. |

**Rollback del changeset documental actual:** `git revert` normal del commit en `dev` si procede; no sobrescribir HEAD concurrente, no force-push, no borrar eventos históricos de BITACORA.

## Oráculos mínimos futuros y aceptación de este documento

**P01–P06 FUTUROS NO_EJECUTADOS:** host aprobado resuelve tenant A; preview autorizado es noindex; publicación con RBAC/CAS y versión aprobada; SEO solo publicado; cache A/B segregada; rollback conserva historial.

**N01–N12 FUTUROS NO_EJECUTADOS:** host/forwarded spoofed, tenant B sobre A, `X-Empresa-Id` arbitraria, slug traversal, preview anónimo/vencido, borrador en sitemap, editor sin publish, versión CAS obsoleta, XSS/HTML activo, caché cruzada, asset privado expuesto y cambio DNS/TLS sin permiso deben fallar cerrado. Son casos de diseño, **no** resultados de tests reales.

**A0:** cotejar ID, fuentes, cinco blobs, requisitos del plan y 16 bloqueos sin inventar autorizaciones. **A1:** verificar coherencia documental entre P/N y RB, sin llamar PASS a HTTP, xUnit o browser. **A2:** revisar límites tenant/JWT/RBAC/PII y no-regresión, sin certificar controles runtime. **A3:** correspondencia entre M01.01–05 y gates M02, sin despliegue o integración ejecutada. **A4:** exigir diff de solo dos Markdown (este documento y CHANGELOG), Scope Lock STRICT y procedencia V5 `completed/success` en el SHA publicado, readbacks PLAN/COLA/DAG/BITACORA. **A5:** pendiente de otra Task Javier, nunca autocertificación.

**Estado de salida de la M01:** preparación documental definida. No se afirma instalada la landing, certificada la seguridad de host, completadas G01–G16 ni autorizada la migración. El gate siguiente sigue siendo el cálculo vivo de Sheets y la revisión independiente.
