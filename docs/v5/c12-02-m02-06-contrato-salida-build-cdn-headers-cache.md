# SOLQARYN V16 — C12-02.M02.06: contrato de salida de build, CDN, headers, caché y dominio

**Microtarea:** C12-02.M02.06, PLAN_MAESTRO fila 1777. **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Riesgo V15:** R0 exclusivamente documental, rama solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN/COLA/DAG vigentes, docs/QUALITY_RISK_AUDIT_V15.md.

**Resultado exigido:** fijar dependencias y salida contractual de M02 sobre build, CDN, dominio, headers, caché y despliegue independiente. **No** cambia proveedor de CDN, DNS, Vercel, Render, Cloudflare, producción, certificados, cabeceras live, routing, API, secretos, MySQL, workflows ni cuenta comercial; no realiza smoke de dominio o despliegue.

## A0 — predecesores y límites de autoridad

- El DAG vigente exige C12-02.M01.01–06+A5, C12-02.M02.01–05+A5, C05-04.M01.06+A5 y C06-01.M01.06+A5. Al reclamar: PLAN H1777=PENDIENTE/owner vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI. Esto habilita **documentación R0**, nunca promoción o cambios de DNS/producción.
- M02.01 define contratos conceptuales de recursos/identidad, M02.02 API/DTO/errors sin endpoints, M02.03 persistencia/índices futuros sin migraciones, M02.04 opciones tipadas y flags deshabilitados, M02.05 eventos/correlación futuros sin dispatcher. Las pruebas y el rollback de cada capa se consideran por su propio riesgo; ninguno acredita producción.
- Frontend Angular 22.2.1 construye recursos públicos y privados, backend ASP.NET Core 10 valida JWT/RBAC, revocación, tenant A/B y recursos. MySQL 8.4/EF Core no se modifica. CDN, proxy, host, subdominio, origin y caché NO pueden convertirse en autoridad de empresa ni saltarse el backend.
- Sin inventar cifras: TTFB, Cache Hit Rate, LCP/INP/CLS, p95, ancho de banda, rendimiento bajo carga, DNS TTL, disponibilidad pública, costos, certificado, humo público y rollback real = NO_MEDIDO/NO_VERIFICADO. No definir budgets o umbrales como aprobados sin medición y autorización.
- Separar scopes: los builds son comprobaciones de artefactos, un smoke público requiere entorno de ensayo autorizado; desplegar requiere identidad/política/rollback adicional. Un build de GitHub Actions SUCCESS **no certifica servicio live, TLS, DNS, CDN ni origin**.
- No introducir nuevos servicios pagos ni agentes; no cambiar las seis Tasks programadas ni sus RRULE.

## G01–G16 — puertas técnicas y operativas para ejecución futura

| Gate | Evidencia futura verificable exigida | Sin evidencia |
| --- | --- | --- |
| G01 | SHA, dueño, entorno y alcance de archivo/build permitidos | NO_GO a publicación |
| G02 | Build Angular/Node tipado con artefactos y hashes identificados | No certificar frontend |
| G03 | Backend Release, dependencias y pruebas relevantes si hay cambio API | No atribuir integración |
| G04 | Inventario de dominios/hosts/origins y proveedor autorizado | No cambiar DNS |
| G05 | Separación física dev/QA/PROD y autorización de despliegue individual | No promover |
| G06 | TLS/headers HTTPS/CSP/HSTS/CORS/redirects probados en entorno legítimo | Sin PASS de seguridad live |
| G07 | Cache-Control por público/privado, no-cache para JWT/PII/cookies | No cache privado compartido |
| G08 | Vary, invalidación y partición por tenant A/B/usuario/rol/versión | Denegación fail-closed |
| G09 | Proxy/host validation: spoofing, SSRF, X-Forwarded y Host header negativos | No confiar en host cliente |
| G10 | JWT/RBAC y revocación server-side, IDOR/tenant B->A rechazados | No habilitar ruta privada |
| G11 | Versionado de assets/manifest, invalidación atómica y rollback conocido | No cutover |
| G12 | Smoke positivo/negativo de rutas, errores y API sin fuga de datos | No afirmar smoke |
| G13 | Cuotas de CDN, costo, facturación y observabilidad aprobadas | Sin contratar/activar |
| G14 | Evidencia reproducible de latencia/cache/errores con muestras/ventana | NO_MEDIDO |
| G15 | CI exact-SHA o equivalencia estricta de blobs y Scope Lock/V5 SUCCESS | No acreditar A4 |
| G16 | PLAN/COLA/DAG/BITACORA readback, A0–A4 y A5 de otra Task Javier | No liberar etapa |

Los gates G01–G16 son condiciones contractuales futuras, no comprobaciones ejecutadas. Una entrada con status QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAILED no constituye PASS exigible. Un commit descendiente sólo es evidencia cuando los blobs del scope, dependencias y ejecución de tests son equivalentes demostrablemente.

## Fronteras HTTP, caché y tenant

- Host/Origin/X-Forwarded-Host/Subdominio llegan de fuentes no confiables salvo proxies explícitamente configurados; un host correcto no acredita tenant ni rol.
- Para recursos privados, exigir JWT validado en backend, membresía y permiso vigente, pertenencia tenant A/B y recurso; no cachear cookie, tokens, PII, perfil, lista de empresas, documentos o respuestas autorizadas en caché global/compartida.
- CDN público sólo puede almacenar contenidos deliberadamente públicos, sin secrets ni URLs firmadas. Un usuario deslogueado no debe heredar cache privado. Vary y clave de caché se definen por contexto confiable para futuras integraciones; TTL y purgas deben ser atómicos o con backout.
- CSP/CORS/Referrer-Policy/Permissions-Policy, cookies seguras, HSTS y redirecciones requieren compatibilidad y pruebas reales; ningún valor se sustituye directamente en este documento. No debilitar headers para conseguir build verde.
- Si se requiere rollback, restaurar versión anterior de assets/configuración desde una revisión auditada sin alterar datos, secretos o registros históricos. En dev, para este Markdown solamente aplica git revert normal.

## A1–A3: T01–T12, pares positivos/negativos FUTUROS_NO_EJECUTADOS (0/12)

| Test | Positivo futuro | Negativo, error o frontera futura |
| --- | --- | --- |
| T01 | Build Angular tipado produce artefacto y SHA reproducible | Build fallido/incompleto se presenta PASS |
| T02 | Host autorizado enruta al origin esperado | Host o X-Forwarded-Host inyectado altera tenant |
| T03 | Recurso público versionado usa caché autorizada | HTML con sesión/token entra en cache compartida |
| T04 | Privado A autenticado obtiene sólo A | Tenant B/revocado recibe datos de A |
| T05 | CSP/CORS permite origen legítimo autorizado | Wildcard o esquema inseguro acepta origen ajeno |
| T06 | TLS y redirect seguros funcionan en entorno de prueba | HTTP downgrade o cookie no Secure se acepta |
| T07 | Cache invalidation tras nueva versión refleja assets correctos | Desfase HTML/JS sirve versión y permisos viejos |
| T08 | Rutas vacía/error/404 devuelven headers y estados correctos | Error 500 incluye secreto o PII |
| T09 | TTFB/cache hit con N/perfil/ventana permite medir | Sin denominador, muestras o ambiente = NO_MEDIDO |
| T10 | Despliegue aislado autorizado verifica origen y rollback | Push dev se considera aprobación QA/PROD |
| T11 | Plan con cuotas/permiso impide gasto no aprobado | Se configura servicio pago por defecto |
| T12 | CI exact-SHA y A5 independiente permiten cerrar gate | CANCELLED/SKIPPED/IN_PROGRESS se cuentan PASS |

Estos casos **NO SE EJECUTARON (0/12)**. No hay prueba real de CDN, DNS, HTTPS, HTTP, navegador, Playwright, MySQL, rendimiento, costos o rollback de infraestructura. A1 especifica oráculos; A2 inspecciona estáticamente tenant A/B/JWT/RBAC/SSRF/CSP/CORS/caché y secretos, no los certifica live. A3 Angular/npm/backend/HTTP/CDN/Playwright/MySQL = NO_APLICA en diff documental R0.

## RB01–RB08 — contención y reversión futuras

- RB01: capturar SHA, versión de artefacto, manifest y configuración actual con dueño autorizado.
- RB02: fijar matriz de dominios/origins/tenant antes de modificación de proxy/CDN.
- RB03: aislar o desactivar caché privada ante sospecha de fuga, preservar auditoría.
- RB04: volver a release previo por mecanismo del proveedor con aprobación explícita, no por esta Task.
- RB05: restaurar headers CSP/CORS/HSTS seguros y verificar negativos antes de recuperar tráfico.
- RB06: invalidar assets versionados y permitir rollback sin mezclar shells/JS antiguos.
- RB07: reconciliar disponibilidad, costos, certificados y errores tras restore autorizado.
- RB08: en este scope Markdown dev, git revert aditivo contra HEAD nuevo; preservar BITACORA y A5.

## A4–A5 y autorización de siguiente fase

A0: dependencias y alcance M02 y diff de dos Markdown, sin código ni infraestructura. A1: G16/RB8/T12 futuros definidos, 0 pruebas live. A2: controles estáticos de caché/tenant/JWT/CSP/CORS, sin seguridad runtime modificada. A3: NO_APLICA a .NET, Angular, HTTP, MySQL, CDNs/proveedor, pues no hay cambios ejecutables. A4: commit FF/CAS exclusivamente en dev, exact-SHA verify-solqaryn-project-scope y V5 CHANGELOG completed/success, readbacks PLAN/COLA/DAG/BITACORA; CI ajena in_progress nunca PASS. A5: otra Task Javier distinta del autor revisa diff/blobs/criterios/CI y tiene facultad de rechazar.

LISTO_DEV contractual **no autoriza** DNS, Vercel/Render, Cloudflare/R2, QA/main/PROD, secrets ni operaciones comerciales. No hay despliegue independiente real, smoke de dominio ni migraciones ejecutadas. Rollback documental seguro git revert normal.
