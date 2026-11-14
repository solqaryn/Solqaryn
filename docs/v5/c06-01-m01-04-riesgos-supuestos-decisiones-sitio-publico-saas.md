# SOLQARYN V5 — C06-01.M01.04: riesgos, supuestos y decisiones pendientes del sitio público SaaS

**Microtarea:** C06-01.M01.04, PLAN_MAESTRO fila 869. **Responsable:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; repositorio solqaryn/Solqaryn, dev, Scope Lock STRICT; fecha 2026-11-14.

## Delimitación de hallazgos

Las tablas siguientes son **riesgos hipotéticos, supuestos no acreditados y decisiones PENDIENTES**, no incidentes confirmados ni autorizaciones ejecutadas. Preparan marketing institucional futuro (planes, sectores, demo, contacto, legal, SEO). No contienen tareas operativas de SMTP, tokens, migraciones, indexación, datos reales ni puesta en producción. No sustituyen revisión independiente A5.

## Línea base inspeccionada

- C06-01.M01.01 `96d8280c96125af30b8f3f580c092460cb397b84`: NR01–NR12 sin implementación comercial.
- C06-01.M01.02 `abc1084b024d32391559f02c365cf4111c5f739a`: A01–A12 e I01–I12 como contratos futuros.
- C06-01.M01.03 `e3d90f0f7f49da2360b11120139ff4003d4ae50e`: inventario de 12 fuentes y B01–B08; oráculos P01–P06/N01–N12 marcados NO_EJECUTADOS.
- `frontend/src/app/app.routes.ts` `2f560c14693455b79ac4546e9333fb74586ef234`: raíz a login; tienda/ERP separados.
- `frontend/src/app/core/guards/auth.guard.ts` `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6`: tenant verificado por backend para ERP.
- `frontend/src/app/core/interceptors/auth.interceptor.ts` `4bdf19135ed1932a99fd09ace1f900bcccc5b25b`: Bearer y tenant solicitado sin autoridad de cliente.
- `frontend/src/app/shared/feedback-state/feedback-state.component.ts` `7deb40475f0c492d09e649e7351b2915bd53d356`: ARIA disponible, no evidencia de integración en contacto.

## R01–R12 — matriz de riesgos POTENCIALES

| ID | Riesgo futuro | Señal de falla | Evidencia / prueba requerida | Medida segura | 
| --- | --- | --- | --- | --- |
| R01 | Marketing y shell ERP confunden rutas o guards | Anónimo llega a recurso privado | Tests routing/401/403 y control backend en futuro SHA | NO_GO y revert de rutas |
| R02 | Catálogo de planes/capacidades no autorizado | Precios o prestaciones inventados | Aprobación comercial y catálogo versionado | No publicar claims |
| R03 | Demo cruza empresas o usa datos vivos | Recursos de empresa B visibles para A | Aislamiento A/B con fixtures, TTL y auditoría | Denegar y deshabilitar demo |
| R04 | Formulario de contacto usado para abuso | Spam, enumeración o abuso SMTP | Rate limit, validación y rechazo 429/4xx | Rechazo seguro, no SMTP |
| R05 | Contenido dinámico inyecta scripts | XSS reflejado o persistente en marketing | Escape, sanitización, CSP y pruebas negativas | No renderizar payload |
| R06 | SEO expone direcciones privadas | Robots/sitemap listan dashboard/PII/tokens | Allowlist y crawler en ruta publicada | No indexar y retirar mapa |
| R07 | Canonical/redirección acepta origen externo | Open redirect o phishing | Canonical de origen aprobado y rechazo | Eliminar canonical inseguro |
| R08 | Legal/copias se publican sin revisión | Política placeholder como contrato vigente | Aprobación de versión y jurisdicción | Mantener oculto |
| R09 | Datos de leads terminan en logs/caché | PII accesible o retención ilimitada | Minimización, redacción, TTL y autorización | Purgado controlado |
| R10 | Sitio inaccesible con teclado/móvil | Perdida de foco, ARIA o contraste | Pruebas Playwright 320px teclado y reduced-motion | Bloquear release UI |
| R11 | Peso o terceros dañan rendimiento | Core Web Vitals no medidos o scripts invasivos | Presupuesto de bundle, red y Lighthouse | No activar terceros |
| R12 | Reversión mezcla marketing con auth/ERP | Despliegue irreversible o degradación privada | Diff exacto, CI, separación de rutas y rollback | Revert normal y fail-closed |

No se adjudica probabilidad ni severidad numérica sin datos; un control heredado de storefront no se presume suficiente para formularios institucionales.

## S01–S08 — supuestos falsables NO_VERIFICADOS

| ID | Supuesto por validar | Evidencia actualmente ausente | 
| --- | --- | --- |
| S01 | Los planes y precios serán aprobados por responsable comercial | No se ha recibido aprobación verificable |
| S02 | Habrá texto legal vigente revisado por responsable jurídico | Sin certificación legal en este scope |
| S03 | Se permitirá una landing anónima separada del storefront | El router actual redirige raíz a login |
| S04 | El contacto tendrá API y límites backend diseñados | Ningún endpoint se declara implementado aquí |
| S05 | La demo podrá aislarse sin datos reales ni PROD | No hay certificado de fixtures/TTL |
| S06 | Metadatos/robots/sitemap podrán observarse en HTTP real | No hay test de indexación o crawler |
| S07 | La UI futura reutilizará feedback accesible y styles existentes | No se verificó integración de formulario |
| S08 | Habrá mecanismos de rollback sin cambiar auth o tenants | Debe probarse en SHA de implementación |

Cualquier supuesto falso es NO_GO para **el scope que dependa de él**, sin desactivar las seis Tasks ni congelar otras microtareas independientemente elegibles.

## D01–D08 — decisiones aún no adoptadas

| ID | Responsable de decisión por rol | Acto/documento necesario | Estado | 
| --- | --- | --- | --- |
| D01 | Propietario comercial | Aprobar claims, planes, moneda, precios y versionado | PENDIENTE |
| D02 | Responsable jurídico/privacidad | Aprobar legal, consentimiento, retención y vigencia | PENDIENTE |
| D03 | Arquitectura frontend | Seleccionar rutas públicas autorizadas y estrategia SEO/SSR | PENDIENTE |
| D04 | Seguridad backend | Aprobar DTO de contacto, antispam, throttling, auditoría y logs | PENDIENTE |
| D05 | Seguridad/QA | Certificar tenant A/B, demo ficticia, TTL y pruebas negativas | PENDIENTE |
| D06 | UX/accesibilidad | Fijar pruebas de 320px, foco, ARIA y presupuesto de rendimiento | PENDIENTE |
| D07 | Operaciones | Definir allowlist robots/sitemap, canonical y rollback probado | PENDIENTE |
| D08 | Propietario autorizado | Autorizar release y promoción por separado; hoy dev solamente | PENDIENTE |

La mención de un rol no concede autorización ni suplanta la firma humana o los controles de rama. No se crean decisiones ejecutivas desde documentación.

## Oráculos futuros P01–P06 / N01–N12

**Positivos FUTUROS NO_EJECUTADOS:** acceso a landing autorizada, catálogo aprobado, canonical/robots coherentes, contacto consentido con rate limit, demo aislada con TTL y navegación accesible móvil/teclado.

**Negativos FUTUROS NO_EJECUTADOS:** anónimo no accede ERP, empresa A no lee B, JWT vencido no autoriza, precio sin aprobación no se publica, legal placeholder no se publica, XSS rechazado, spam limitado, consentimiento obligatorio, canonical externo bloqueado, sitemap privado omitido, demo sin PROD y fallback/rollback seguro. Estas listas son oráculos de la fase posterior, no suites ejecutadas ni PASS reclamados.

## Impacto, comprobación y reversión

**Diff de M01.04:** dos Markdown (este archivo y CHANGELOG_AI.md), sin Angular/backend/MySQL/SMTP, contratos de API, JWT/RBAC, IAM, tenant, UI, secretos o infraestructura. **Validación A0–A4:** inspección estática de contratos y referencias, consistencia de 12 riesgos / 8 supuestos / 8 decisiones y sus NO_GO; GitHub Scope Lock exact SHA y check de procedencia CHANGELOG. Tests runtime del backend/frontend no se atribuyen como PASS por ser ajenos a documentación; se etiquetan NO_APLICA al changeset. **Rollback:** git revert no destructivo en dev; conservar PLAN, COLA, BITACORA y pistas históricas. **A5:** obligatoriamente otra Task Javier distinta de la autora; no autoriza QA/main/PROD ni un dispatcher.
