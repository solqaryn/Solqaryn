# SOLQARYN V16 — C12-02.M01.06: bloqueo, rollback y liberación del frontend separado

**ID:** C12-02.M01.06, PLAN_MAESTRO fila 1771, C12-02 Frontend separado, M01. **Autor/owner:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fuente única:** Standard de Implementación de Automatizaciones V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repos/rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **V15:** R0 documental sin runtime, 2026-11-15.

## 1. Resultado y frontera del alcance

Documento terminal de **preparación M01**, no migración ni despliegue. Define condiciones de rechazo, reversibilidad y autorización posterior para frontend Angular separado, build, CDN, dominio/TLS, proxy, headers, caché, SPA/routing y UX. La arquitectura declarada actualmente incluye Angular 22.2.1 en Vercel, API en Render, MySQL Aiven y medios Cloudinary; **esta documentación no comprueba estados live ni autoriza trasladar estos servicios a Hostinger, Cloudflare, VPS, QA/main/PROD o cambiar DNS/cuentas**. No ejecutar infraestructura, nuevas APIs, CI de promoción, secret rotation o billing. El ERP privado y el storefront existente permanecen separados y protegidos.

**Preflight ID:** PLAN H1771=PENDIENTE e I1771 vacío antes del CLAIM; COLA G1771/O1771=SI y DAG A1732 F=VALIDADO/G=SI, tras A5 de M01.01–.05 y predecesores requeridos. Se reclamó con CAS exclusivo H1771=EN_PROGRESO, owner I1771=6af16bb9492c8191b8e8c7d606f11507, readback. Haber habilitado el documento no libera implementación, Hostinger, DNS, QA/main/PROD ni proveedores reales.

## 2. Evidencia de referencia y límites

Fuentes directamente cotejadas en `dev`:
- `docs/v5/c12-02-m01-04-riesgos-supuestos-decisiones-frontend-separado.md` blob `52771fbe9ce803cce4af7ce41232d2381a494524`: riesgos potenciales R01–R16, supuestos S01–S10 y decisiones **D01–D10 PENDIENTES**. No son fallos confirmados ni permisos.
- `docs/v5/c12-02-m01-05-criterios-aceptacion-frontend-separado.md` blob `0758f037ae4910c73d6617f63d5bafc753acfb8d`: 16 pares AC/N y NF01–NF10, todos FUTUROS_NO_EJECUTADOS.
- Inventario previo M01.03 citado en dichos documentos, blob `878c620a20e48de70003f84f777a8bffc5fd11a1`, no equivale a una prueba de proveedor actual.
- Fuentes de código citadas sin ejecutar pruebas de proveedor: `frontend/angular.json` blob `8b00f8fee80fdfafe1864e233be7d5f20ad444e3`; `frontend/vercel.json` `5f12bcf667a21afcfc4314b66787ff66627ae666`; `frontend/src/app/app.routes.ts` `2f560c14693455b79ac4546e9333fb74586ef234`; `render.yaml` `f64d291425e2bbfdd9b6466d268753f8f8d86b3d`. Referencias históricas sujetas a comparación al implementar.

D01–D10 no están resueltas: proxy, caché, DNS/TLS, políticas de headers, aislamiento de proyectos, SEO, firma de artefactos, observabilidad, presupuestos UX y promoción. Estas decisiones requieren dueño/permiso y mediciones reales; **no se aprueban por esta etapa M01**.

## 3. Bloqueos BL01–BL16 para implementar en un ambiente autorizado FUTURO

| ID | Señal verificable de NO_GO | Control exigido |
| --- | --- | --- |
| BL01 | Identidad de rama/proyecto/deployment indeterminada | No promover ni atribuir estado live desde un SHA documental |
| BL02 | Ignore-build SKIPPED/CANCELLED confundido con PASS | Exigir build y test causal realmente completado |
| BL03 | Proxy altera 401/403 o elimina Authorization | Rechazar cambio hasta test HTTP auth/roles |
| BL04 | Tenant proviene de URL/header editable por visitante | Exigir ownership backend JWT/RBAC y prueba A/B |
| BL05 | HTML autenticado pasa por cache compartida | No-store o segmentación y prueba cross-user |
| BL06 | CSP permite script no aprobado o bloquea ERP | CSP efectiva inspeccionada con browser |
| BL07 | CORS wildcard combinado con credenciales | Orígenes exactos aprobados y preflight negativo |
| BL08 | Build Angular expone secretos o configuración privada | Escanear artefacto y mantener secretos server-side |
| BL09 | Sitemap o bot indexa ERP, PII o token | Allowlist de rutas públicas y prueba crawler |
| BL10 | HTML referencia asset obsoleto incompatible | Manifest versionado, invalidación y smoke de refresh |
| BL11 | Ruta SPA profunda recibe 404 o bypass de guard | Prueba de router/fallback y autorización backend |
| BL12 | DNS/TLS/CDN modificados sin rollback ensayado | NO_GO hasta permiso de entorno y revert real probado |
| BL13 | Pruebas E2E ejecutan backend diferente de SHA | Correlacionar API/manifest, ambiente y log |
| BL14 | Configuración dev/QA/PROD se confunde | Verificar IDs/ramas/cuentas por separado, sin promoción |
| BL15 | Accesibilidad o presupuesto se da por aprobado sin medición | Playwright teclado/320px/foco y métricas reales |
| BL16 | Rollback de Git no restaura headers/proxy remotos | Exigir readback de configuración/artefacto y protocolo multi-capa |

Cada bloqueo se evaluará en SHA/ambiente concreto, con pruebas negativas, antes de habilitar una transición técnica. Los resultados de CI en cola o cancelados no acreditan éxito requerido. La UI de frontend no reemplaza autorización de API.

## 4. Ocho pasos REV01–REV08 de reversión FUTURA_NO_EJECUTADA

| ID | Procedimiento condicionado y evidencia que deberá conservar |
| --- | --- |
| REV01 | Capturar SHA, manifest, build, proyecto, rama, headers y configuración del ambiente permitido |
| REV02 | Congelar únicamente la publicación afectada, conservando ERP y servicios existentes |
| REV03 | Restaurar bundle/assets firmados y hash de versión anterior, sin reconstruir desde HEAD diferente |
| REV04 | Revertir o restaurar proxy y fallback SPA aprobado, sin eliminar auth backend |
| REV05 | Restituir CSP/CORS/HSTS y cache-control controlados con capturas comparables |
| REV06 | Invalidar solamente caches segmentadas autorizadas, sin exponer sesión o tenant B |
| REV07 | Ejecutar smoke 401/403, A/B, deep-links, assets, 320px, keyboard y logs sin PII |
| REV08 | Documentar incidente, revert SHAs, permiso, pruebas y decidir re-publicación con revisión independiente |

El rollback real de hosting requiere permiso expreso del ambiente y acceso vivo a proveedor, backups/versiones y readback HTTP. **git revert del contrato documental** no equivale a rollback de edge, DNS, TLS, reverse proxy o cache de proveedor.

## 5. Doce puertas LG01–LG12 de liberación FUTURA

| ID | Puerta | Evidencia de PASS requerida al implementar |
| --- | --- | --- |
| LG01 | Autoridad de cambio de hosting | Identidad y permiso para proyecto/ambiente explícitos, no inferidos |
| LG02 | Scope de rutas | Inventario SPA, público, storefront y ERP con guards conservados |
| LG03 | Build reproducible | npm ci, Angular build, manifest exact SHA y 0 fallos |
| LG04 | Proxy/API auth | 401/403, JWT y RBAC backend, tenant A/B negativo acreditados |
| LG05 | Cache | No-store en privadas, hash para assets, test A/B y stale asset |
| LG06 | Headers efectivos | CSP, CORS/HSTS y preflight browser/curl en ambiente autorizado |
| LG07 | SEO/privacidad | robots/sitemap/canonical allowlisted sin rutas privadas |
| LG08 | Secretos | Bundle y logs escaneados, sin credenciales expuestas |
| LG09 | Observabilidad | Correlación frontend/proxy/API con datos mínimos y alertas medidas |
| LG10 | UX accesible | 320px, foco, teclado, reduced-motion y presupuesto medido |
| LG11 | Rollback reproducido | Versión, assets, headers, proxy y cache restaurados con smoke |
| LG12 | Control institucional | CI PASS causal exact SHA, Scope Lock, V5, A0–A5 y autorización entorno |

No deducir que un build está OK porque la aplicación tiene una configuración de `angular.json`. No publicar números de latencia, disponibilidad, CWV, pesos o RTO/RPO si no hay medición con numerador/denominador. El check original de este documento solo acredita el alcance documental y la procedencia del CHANGELOG.

## 6. Ocho pares positivos/negativos de pruebas FUTURAS_NO_EJECUTADAS

| Par | Prueba positiva futura | Negativo/fallo que debe detectarse |
| --- | --- | --- |
| T01 | Deep-link de SPA permitida carga tras refrescar | Ruta privada no devuelve shell como autenticada |
| T02 | Proxy conserva JWT y backend devuelve 401/403 legítimos | Token revocado/tenant B no se convierte en 200 |
| T03 | Bundle Angular SHA coincide con manifest reproducible | SKIPPED/CANCELLED no se computa PASS |
| T04 | Asset público tiene hash/TTL coherente | HTML autenticado no queda compartido entre A y B |
| T05 | CSP/CORS bloquea origen/script no aprobado | Wildcard con credenciales o inline inseguro falla |
| T06 | robots/sitemap muestran rutas públicas aprobadas | ERP, token, PII o endpoint privado no se indexan |
| T07 | Teclado/foco/320px y motion accesible | Overlay, scroll horizontal y animación forzada falla |
| T08 | Rollback restaura bundle, proxy, headers y cache | Revert parcial sin readback no libera servicio |

**Ejecución:** 0/8 parejas runtime/browser/API ejecutadas en esta microtarea, **8/8 especificadas**. Al implementar UI Angular deberán ejecutar builds/tipado, Playwright Chromium (teclado, 320px, reduced motion, navegación), pruebas reales de proxy 401/403 y tenant A/B, smoke dev y contratos de cache/header con datos aislados; proveedores no probados se declaran NO_MEDIDO.

## 7. A0–A5 causales, DAG y rollback de ESTA entrega

**A0:** alcance exclusivamente documento M01.06 y sección autocontenida de `CHANGELOG_AI.md`, sin cambiar .NET, Angular, CSS, YAML, CSP, DNS o servicios. **A1:** cobertura documental de BL16, REV08, LG12 y 8 parejas; los tests funcionales son futuros, no PASS ejecutados. **A2:** documenta fail-closed de JWT/RBAC/tenant A/B, proxy, secretos, CORS/CSP, SSRF, PII y caches; ninguna nueva superficie runtime, test de backend/browser nuevo NO_APLICA al diff Markdown. **A3:** coherencia con M01.04/M01.05, inventario y fuentes citadas; frontend/build/proxy/CDN/hosting/HTTP real NO_APLICA hoy al diff. **A4:** commit fast-forward/CAS a `dev`, controles exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success, readback PLAN H/I, COLA, DAG y BITACORA. **A5:** requiere auditoría individual de otra Task Javier, con capacidad A5_FAIL, no autoría propia.

Una salida M01.06 `LISTO_DEV` únicamente documenta este contrato; el DAG M02 exige A5 verdadero y gates de sucesoras. **Rollback propio:** `git revert` normal del commit documental, sin reset/force-push, sin borrar BITACORA, cambiar horarios, producción, proveedores, DNS, QA o main.
