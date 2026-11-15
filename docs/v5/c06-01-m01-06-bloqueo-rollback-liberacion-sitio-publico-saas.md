# SOLQARYN V16 — C06-01.M01.06: bloqueo, rollback y liberacion condicionada del sitio publico SaaS

**Microtarea:** C06-01.M01.06, PLAN_MAESTRO fila 871. **Responsable:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Fuente unica:** Standard de Implementacion de Automatizaciones V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Alcance:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Riesgo V15:** R0 documental sin runtime, 2026-11-15.

## 1. Salida exacta y fronteras

Cierra documentalmente la **preparacion M01** de la landing institucional futura: propuesta de valor, planes, sectores, demo, contacto, legal y SEO. Esta unidad **NO implementa** landing, rutas publicas, API de contacto, SMTP, datos de demo, SEO publicado, precio autorizado, MySQL, frontend/backend, proveedor, webhook, infraestructura ni cambios en produccion. El storefront vigente no equivale a landing corporativa. La ruta raiz actual que redirige a login tampoco es evidencia de acceso publico certificado.

La arquitectura vigente es Angular 22.2.1 standalone con Signals/Material/CDK y Zone.js, ASP.NET Core net10.0, JWT/RBAC backend, EF Core 10.0.12, Oracle MySQL 8.4 y aislamiento tenant A/B. La UI nunca confiere permisos de servidor. Mantener sin regresion el ERP privado, redirecciones, reglas RBAC y separacion entre empresas.

**Preflight de elegibilidad de este ID:** PLAN H871=PENDIENTE, I871 vacio, COLA G871=SI/O871=SI, DAG_TECNICO F832=VALIDADO/G832=SI; claim por cambio condicional de una celda y readback registrado de modo independiente. Las dependencias del DAG incluyen C06-01.M01.01-.05 con A5 de revisor Javier diferente, C04-01.M01.06 y C05-01.M01.06 con A5, ademas de bases M01 de C02/C04/C05. Estos estados se recalculan, no se copian como habilitacion manual.

## 2. Documentos fuente verificados contra blobs de dev

| Referencia | Archivo en docs/v5/ | Git blob | Limite de evidencia |
| --- | --- | --- | --- |
| M01.01 objetivo y no regresion | c06-01-m01-01-objetivo-no-alcance-sitio-publico-saas.md | 96d8280c96125af30b8f3f580c092460cb397b84 | NR01-NR12 |
| M01.02 actores/interfaces | c06-01-m01-02-actores-interfaces-sitio-publico-saas.md | abc1084b024d32391559f02c365cf4111c5f739a | A01-A12,I01-I12,F01-F08 |
| M01.03 fuentes e inventario | c06-01-m01-03-inventario-codigo-evidencia-sitio-publico-saas.md | e3d90f0f7f49da2360b11120139ff4003d4ae50e | 12 fuentes,B01-B08 |
| M01.04 riesgos y decisiones | c06-01-m01-04-riesgos-supuestos-decisiones-sitio-publico-saas.md | c2b970feee1b2ab1d62349f21af394655070a77a | R01-R12,S01-S08,D01-D08 PENDIENTE |
| M01.05 criterios | c06-01-m01-05-criterios-aceptacion-sitio-publico-saas.md | c6e254f6728a00a220216875341f8dd61065e936 | AC01-AC14,N01-N14 FUTUROS_NO_EJECUTADOS |

Las terminales `docs/v5/c04-01-m01-06-bloqueo-rollback-liberacion-catalogo-planes.md` y `docs/v5/c05-01-m01-06-gates-rollback-liberacion-design-system.md` son preparatorias: no certifican planes/precios, consentimiento, Playwright, proveedor MySQL ni portal publico. En M01.04 las decisiones D01-D08 siguen **PENDIENTES**. Cualquier cambio de los blobs fuente, sus permisos, A5 o SHA base exige revalidacion antes de ejecutar un sucesor.

## 3. Dieciseis barreras de salida BL01-BL16 para implementaciones futuras

| Gate | Obligacion verificable antes de activar | NO_GO causal |
| --- | --- | --- |
| BL01 | Propuesta, copy, CTA y publico objetivo versionados | No hay aprobacion de claims/atributos |
| BL02 | Planes, sectores, precios, moneda y limites autorizados comercialmente (D01) | Precio o prestacion inventada |
| BL03 | Aviso legal, privacidad, retencion, consentimiento y jurisdiccion aprobados (D02) | Texto placeholder o lead sin consentimiento |
| BL04 | Rutas anonimas explicitas separadas de ERP/storefront; guards intactos | Se expone dashboard o recurso privado |
| BL05 | JWT/RBAC, usuario, empresa efectiva y revocacion validados por servidor | Header/slug/UI conceden grants o acceso A a B |
| BL06 | Demo ficticia aislada, TTL y borrado comprobados | Datos de PROD o empresa ajena en demo |
| BL07 | Formulario backend con DTO minimo, anti-XSS, rate limit y control antispam | Spam, inyeccion, enumeracion o SMTP no autorizado |
| BL08 | Auditoria minimizada, no JWT/secretos/PII en logs ni cache cruzada | Filtracion, retencion indefinida o usuario B visible |
| BL09 | Canonical/title/description/redirect allowlisted por ruta | Open redirect, URL externa o metadata no aprobada |
| BL10 | Robots/sitemap publicos verificados con crawler | Indexa ERP, token o ruta empresarial privada |
| BL11 | Media responsive con fuentes confiables, terceros y cookies controlados | SDK externo filtra identificadores o cookies |
| BL12 | Accesibilidad 320px, teclado, foco, ARIA y reduced-motion probados | Foco perdido, overflow o uso obligatorio de animacion |
| BL13 | Presupuestos medidos de bundle/red/Core Web Vitals | Se declara rapidez sin mediciones |
| BL14 | AC01-AC14 y N01-N14 ejecutados con casos positivos/negativos/frontera reales | Solo hay Markdown, mocks o compilacion aislada |
| BL15 | CI Angular/backend/seguridad/HTTP/DB si aplica y rollback ensayado en SHA causal | Required checks FAIL/CANCELLED/SKIPPED/in_progress |
| BL16 | ScopeLock STRICT, V5 provenance, PLAN/COLA/DAG/BITACORA y A5 por otra Task | Autoaprobacion, formula forzada o traslado QA/main/PROD |

Estos gates no son resultados de tests. M01.06 puede quedar LISTO_DEV documental con A0-A4 proporcionados a R0; **no habilita por si sola** publicacion, QA/main/PROD ni permite afirmar que los gates runtime estan satisfechos. M02 y siguientes solo se reclaman si PLAN H/I, COLA G/O, DAG F/G, A5 y autorizaciones vivas lo permiten.

## 4. Ocho procedimientos de reversibilidad REV01-REV08 (futuros)

| Paso | Disparador | Accion y evidencia futura requerida |
| --- | --- | --- |
| REV01 | Scope/copy inexacto | Congelar publicacion, comparar version comercial aprobada y revertir cambio |
| REV02 | Guard/ruta expone ERP | Restituir router protegido, probar HTTP 401/403 y acceso anonimo minimo |
| REV03 | Acceso cross-tenant o rol revocado | Fail-closed, aislar recurso y demostrar regresion A/B |
| REV04 | Formulario/SMTP abusado | Deshabilitar solo entrada vulnerable, conservar auditoria minima |
| REV05 | Demo con datos reales o TTL roto | Aislar fixtures, invalidar demo y probar ausencia de datos reales |
| REV06 | Legal/precio/canonical sin autorizacion | Restaurar copia versionada, bloquear publicacion no aprobada |
| REV07 | Regresion movil/a11y/performance | Revertir UI puntual, medir teclado, 320px, red y motion |
| REV08 | CI/ScopeLock/HEAD invalido | Git revert normal sin force-push; verificar SHA y controles |

**Rollback de esta entrega:** revert normal del commit documental que contenga este archivo y su seccion autocontenida de `CHANGELOG_AI.md`, sin reescribir historia. REV02-REV07 son procedimientos **NO_EJECUTADOS**: requieren prueba con codigo y recursos aislados antes de aplicarse. No tocar QA, main, PROD, Aiven real, DNS, secrets ni cuentas de pago.

## 5. Matriz positiva/negativa T01-T08: FUTUROS_NO_EJECUTADOS

| ID | Positiva futura | Negativa/frontera futura |
| --- | --- | --- |
| T01 | Ruta anonima aprobada renderiza | ERP/dashboard requiere JWT y devuelve 401/403 |
| T02 | Plan mostrado coincide con catalogo firmado | Precio/claim sin aprobacion bloquea |
| T03 | Contacto con consentimiento y DTO validos | JS/XSS, vacio y solicitudes masivas rechazadas |
| T04 | Demo solo con fixtures sinteticos TTL | Tenant B/PROD no recuperables |
| T05 | Robots/sitemap/canonical limitados a publico | URL privada/externa excluida |
| T06 | 320px, teclado, ARIA, reduced motion y red medidos | Error de foco/overflow falla |
| T07 | Logs/trace minimizados y seguros | JWT/PII/secretos no persistidos |
| T08 | CI required SUCCESS y revert en entorno autorizado | SHA diferente sin equivalencia o SKIPPED/CANCELLED no libera |

Esta matriz y AC01-AC14/N01-N14 son especificaciones falsables de una futura implementacion. **0 pruebas de HTTP, MySQL, Angular, Playwright o SMTP se ejecutan por agregar este Markdown**. No reportar cantidad de casos escritos como pruebas efectivamente aprobadas.

## 6. Calidad V15 A0-A5 por riesgo y condicion de cierre

- **A0:** alcance conforme a la fila PLAN, diff de este documento y seccion unica de CHANGELOG, referencias/blobs verificables, prerequisitos y rollback.
- **A1:** revisar consistencia documental de 16 BL, 8 REV, 8 T y fuentes M01.01-.05; no sustituir pruebas funcionales reales (NO_APLICA en esta entrega R0 sin codigo).
- **A2:** JWT/RBAC/tenant A/B y privacidad permanecen barreras explicitas, pero no se modifico superficie ejecutable (tests de seguridad nuevos NO_APLICA en este diff, obligatorios para futuros cambios R2).
- **A3:** API, SMTP, MySQL, Playwright y pruebas de proveedor NO_APLICA por ausencia de cambios de integracion, no atribuirles PASS.
- **A4:** exigir publicacion atomica de documento+CHANGELOG mediante commit fast-forward/CAS en dev, diff/blobs/readback; check `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` **completed/success** del SHA de cierre. El resultado real se consigna tras la CI, no se presupone aqui.
- **A5:** A5_PENDIENTE hasta revision de otra de las seis Tasks Javier (ID distinto al autor), con facultad de rechazar; LISTO_DEV no autoriza despliegue.

**Condicion de liberacion:** solo dar LISTO_DEV a esta M01.06 con A0-A4 y readbacks auditables. Posteriormente revalidar formulas COLA/DAG, prerequisitos y A5; no editar G/O ni confundir la salida documental con aprobacion comercial/juridica, seguridad de runtime, migracion o puesta en marcha.
