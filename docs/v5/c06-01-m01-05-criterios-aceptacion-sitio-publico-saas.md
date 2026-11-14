# SOLQARYN V5 — C06-01.M01.05: criterios de aceptación observables del sitio público SaaS

**Unidad:** C06-01.M01.05, PLAN_MAESTRO fila 870. **Responsable:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Solo** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT, 2026-11-14.

## Estado y alcance comprobado

Define criterios de aceptación **para una implementación FUTURA** de landing institucional con propuesta de valor, planes, sectores, demo, contacto, legal y SEO. No aporta landing, metadatos publicados, precios, consentimiento formal, API de contacto, robots/sitemap, datos de demostración ni despacho SMTP. AC01–AC14 y N01–N14 son **oráculos FUTUROS NO_EJECUTADOS**, no pruebas PASS. Esta microtarea solo documenta aceptación funcional y técnica; bloqueos, rollback y liberación terminal pertenecen a M01.06 cuando su DAG sea SI.

## Evidencia fuente inspectada

- `docs/v5/c06-01-m01-01-objetivo-no-alcance-sitio-publico-saas.md` — blob `96d8280c96125af30b8f3f580c092460cb397b84`: NR01–NR12.
- `docs/v5/c06-01-m01-02-actores-interfaces-sitio-publico-saas.md` — blob `abc1084b024d32391559f02c365cf4111c5f739a`: A01–A12, I01–I12 y F01–F08.
- `docs/v5/c06-01-m01-03-inventario-codigo-evidencia-sitio-publico-saas.md` — blob `e3d90f0f7f49da2360b11120139ff4003d4ae50e`: doce fuentes y B01–B08.
- `docs/v5/c06-01-m01-04-riesgos-supuestos-decisiones-sitio-publico-saas.md` — blob `c2b970feee1b2ab1d62349f21af394655070a77a`: R01–R12, S01–S08 y D01–D08 PENDIENTES.
- `frontend/src/app/app.routes.ts` — blob `2f560c14693455b79ac4546e9333fb74586ef234`: la ruta raíz actual redirige a login, tienda y ERP son superficies distintas.
- `frontend/src/app/core/guards/auth.guard.ts` — blob `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6`: contexto tenant verificado por backend, no por usuario visitante.
- `frontend/src/app/shared/feedback-state/feedback-state.component.ts` — blob `7deb40475f0c492d09e649e7351b2915bd53d356`: estados ARIA reutilizables; no equivale a formulario.
- Skill/referencia arquitectónica del HEAD: Angular 22.2.1, ASP.NET net10.0, MySQL, JWT/RBAC y aislamiento tenant A/B.

## AC01–AC14 — condiciones de aceptación FUTURAS NO_EJECUTADAS

| Criterio | Condición observable exigida | Evidencia/suite futura necesaria |
| --- | --- | --- |
| AC01 | Landing anónima explícita, sin inferir acceso ERP | Router y Playwright GET/HTML sobre ruta aprobada |
| AC02 | Propuesta respaldada por capacidades existentes | Catálogo comercial firmado/versionado y snapshot |
| AC03 | Planes, sectores y precios trazables | Approval + fixture y visualización exacta |
| AC04 | Navegación raíz, header, footer y CTA segura | E2E móvil/desktop/teclado con foco |
| AC05 | Demo ficticia aislada con expiración | E2E contra tenant A/B y TTL real |
| AC06 | Contacto con consentimiento y validación | Prueba API positiva con DTO mínimo y auditoría |
| AC07 | Anti-spam y rate limit backend | Integración 429 sin revelar existencia de usuarios |
| AC08 | Términos y privacidad versionados y aprobados | Revisión legal y URL pública verificadas |
| AC09 | Title/description/canonical por ruta | HTTP/DOM y crawler contra entorno permitido |
| AC10 | Robots/sitemap exclusivamente públicos | Crawler URLs allowlisted excluye token/ERP |
| AC11 | Media responsive y carga ligera | Bundle/mediciones de red y fallback real |
| AC12 | Accesibilidad 320px, foco, ARIA y reduced motion | Playwright/aXe manual guiado con criterios |
| AC13 | Aislamiento multiempresa y no almacenamiento PII indebido | Pruebas seguridad A/B, logs y caché |
| AC14 | Release reversible con checks del SHA | GitHub Actions dirigido + plan rollback probado |

Los criterios requieren datos y permisos reales verificados en el SHA de implementación. Un contrato Markdown o un check de Scope Lock documental **no** reemplaza los tests.

## N01–N14 — negativas obligatorias FUTURAS NO_EJECUTADAS

| Negativa | Solicitud adversarial o regresión | Respuesta segura esperada |
| --- | --- | --- |
| N01 | Anónimo navega a dashboard/productos | 401/403 o redirección segura sin datos |
| N02 | Tenant A intenta obtener datos de B por slug/header | Negación backend y sin fuga |
| N03 | Bearer vencido o manipulado | 401 y limpieza de sesión; no autorización |
| N04 | Plan/precio sin aprobación vigente | Bloquear publicación, no inventar precio |
| N05 | Legal placeholder o consentimiento ambiguo | No publicar ni persistir lead |
| N06 | HTML/JS malicioso en copy o formulario | Rechazo/sanitizado, no ejecución |
| N07 | Contacto masivo / enumeración | 429 o respuesta neutra, sin SMTP abusivo |
| N08 | Payload incompleto o sin consentimiento | 4xx y ningún contacto registrado |
| N09 | Canonical externo o redirección open URL | Bloqueo allowlist y log sin secretos |
| N10 | Sitemap apunta a ERP, tienda privada o token | Exclusión y CI regresión fallida |
| N11 | Demo utiliza PROD, datos reales o sin TTL | Fallo cerrado, entorno aislado |
| N12 | Imagen o tercero filtrando cookies/PII | Bloqueo origen/SDK no aprobado |
| N13 | 320px, teclado, foco o ARIA insuficientes | No pasa gate accesibilidad |
| N14 | Rollback pierde guards, rutas o tenant safety | Revert y recuperación comprobada |

Para un check requerido **SKIPPED, CANCELLED, FAILED, queued o in_progress nunca significan PASS**. La prueba de seguridad debe demostrar rechazo con respuesta backend y no limitarse a la guarda Angular.

## Gates y trazabilidad de implementación posterior

1. Registrar decisiones D01–D08 aprobadas por quienes correspondan; sin ellas no publicar planes, legal o envío de contactos.
2. Implementar rutas/DTO/guards y datos solo en microtareas técnicas autorizadas por PLAN/COLA/DAG y Scope Lock, nunca por esta tabla.
3. Usar GitHub Actions del SHA concreto: frontend npm/build/tests, backend .NET, JWT/RBAC/tenant A/B, Playwright, API 4xx/429, SEO/robots y controles de consentimiento según impacto; suites efectivamente ejecutadas.
4. Verificar diff, base, regresiones, rollback normal y revisión A5 por otra Task Javier. QA/main/PROD, dominios, datos productivos y secretos requieren autorización separada.

## Validación documental, impacto y rollback de esta M01.05

**A0:** comprobar autoría, archivos fuente y 14 AC + 14 N sin prometer implementación. **A1:** coherencia de positivos y negativos descritos (pruebas futuras, NO_EJECUTADAS). **A2:** integridad de JWT/RBAC, protección tenant A/B y PII sin cambios de código. **A3:** contratos y brechas alineados con M01.01–M01.04, sin integración runtime nueva. **A4:** publicación fast-forward y comprobación exact-SHA de Scope Lock y procedencia CHANGELOG; CI de runtime no pertinente al diff Markdown = NO_APLICA, no PASS. **Diff:** este documento y sección autocontenida de CHANGELOG_AI.md, cero cambios Angular/API/MySQL/SMTP, migraciones, datos o eventos. **Rollback:** git revert ordinario en dev sin reescribir historia ni BITACORA. **A5:** independiente pendiente de otra Task autorizada, ninguna autoaprobación ni permiso de promoción.
