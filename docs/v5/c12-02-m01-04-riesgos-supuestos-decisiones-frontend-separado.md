# SOLQARYN V5 — C12-02.M01.04: riesgos, supuestos y decisiones del frontend separado

**Unidad:** C12-02.M01.04 · fila PLAN_MAESTRO 1769 · C12-02 Frontend separado, M01. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Corte Git:** `3996cc6950497f587ef1656e60f95d2772d8b9e2` en `solqaryn/Solqaryn@dev` (2026-11-14). **Fuente única:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). `PROJECT_SCOPE_LOCK=STRICT`.

## Objeto y evidencia utilizada

Registrar peligros potenciales, hipótesis falsables y decisiones técnicas todavía abiertas de la **separación futura** de Angular 22.2.1, API proxy, CDN, headers, caché, DNS/TLS, build y rollback. **No se ha desplegado ninguna nueva infraestructura**, DNS, CDN, Hostinger ni Cloudflare. No hay pruebas de rendimiento/live, migraciones, QA, main, PROD, secretos, webhooks o eventos nuevos. Se preservan Vercel frontend, Render API, Aiven MySQL y Cloudinary conforme arquitectura versionada; esto no es un readback de proveedores.

Documento de inventario predecesor `C12-02.M01.03`: blob `878c620a20e48de70003f84f777a8bffc5fd11a1`, diez fuentes Git y doce brechas **G01-G12 sin prueba causal de vulnerabilidad**. Fuentes primarias versionadas: `frontend/package.json` `e6d030f643421cb343cc12617e742469e88223c4`, `frontend/angular.json` `8b00f8fee80fdfafe1864e233be7d5f20ad444e3`, `frontend/vercel.json` `5f12bcf667a21afcfc4314b66787ff66627ae666`, `frontend/scripts/vercel-ignore-build.mjs` `81d9c7fe6639da0fee1a23dac34e337f3c994507`, `frontend/src/app/app.routes.ts` `2f560c14693455b79ac4546e9333fb74586ef234`, `frontend/src/app/app.config.ts` `cd77384e713014015d83b348955b3d83573348b3`, `render.yaml` `f64d291425e2bbfdd9b6466d268753f8f8d86b3d`.

## R01–R16 — riesgos potenciales, NO_CONFIRMADOS

| Riesgo | Posible fallo a investigar | Mitigación o evidencia futura |
|---|---|---|
| R01 | Desalineación branch/proyecto Vercel | Aislamiento por project ID y readback antes de release |
| R02 | SKIPPED de ignore-build interpretado como PASS | Separar status de skip de test/build causal |
| R03 | Proxy modifica 401/403 o cabeceras | Pruebas negativas de contrato de auth |
| R04 | Tenancy por URL o header controlado por cliente | API mantiene JWT/RBAC tenant A/B |
| R05 | Cache compartida de HTML autenticado | No-store y pruebas cross-user/cross-tenant |
| R06 | CSP demasiado permisiva | Revisión de headers efectivos y XSS |
| R07 | CORS wildcard con credenciales | Allowlist exacta por ambiente |
| R08 | Secreto embebido en bundle Angular | Escáner de artefactos y variables públicas |
| R09 | SEO publica PII o recursos privados | Prueba robots/sitemap/SEO con rutas sensibles |
| R10 | Asset stale con HTML nuevo | Versionado/hash e invalidación controlada |
| R11 | Rutas SPA profundas devuelven 404 | Smoke de fallback con autenticación |
| R12 | Cambio de DNS/TLS sin rollback | Orden separada y evidencia de reversión |
| R13 | E2E no reproduce backend actual | CI exact SHA y datos efímeros |
| R14 | Diferencia Vercel/Render dev vs QA | Verificación explícita por ambiente |
| R15 | Accesibilidad/bundle pesada | Presupuesto de performance y pruebas teclado/foco |
| R16 | Rollback restaura código pero no configuración | Versionar flags/headers y runbook por entorno |

La tabla identifica riesgos condicionales, no incidentes confirmados; su prioridad real debe derivarse de pruebas, alcance y autoridad de la fase que corresponda. Ningún riesgo justifica modificar secretos o despliegues existentes desde esta microtarea.

## S01–S10 — supuestos falsables, NO_VERIFICADOS

| Supuesto | Hipótesis | Evidencia requerida para aceptarlo |
|---|---|---|
| S01 | Vercel sigue siendo frontend operativo documentado | Consultar deployment autorizado antes de tomar como live |
| S02 | Render expone API bajo rutas del ambiente | Probar URL y health cuando se autorice |
| S03 | Aiven continúa persistencia de backend | No inferir conectividad desde YAML |
| S04 | Cloudinary sigue siendo integración de medios | No migrar a R2 por este documento |
| S05 | Headers de vercel.json llegan al edge | Verificar captura HTTP por ambiente |
| S06 | Proxy conserva Authorization y códigos | Verificar con tests 401/403 y tokens A/B |
| S07 | Los guards Angular no conceden permisos | Confirmar backend RBAC en pruebas negativas |
| S08 | Caché de assets no afecta respuestas privadas | Revisar claves/headers efectivos |
| S09 | Build Angular se reproduce desde lockfile | Ejecutar CI exact SHA y comparar manifest |
| S10 | Rollback futuro será reversible sin pérdida | Ensayar plan en entorno autorizado |

## D01–D10 — decisiones pendientes, NO_APROBADAS

| Decisión | Tema | Alternativa/procedimiento a resolver |
|---|---|---|
| D01 | Frontera de proxy | Conservar ruta actual o definir nueva mediante cambio controlado |
| D02 | Política de caché | Establecer reglas por clase pública/privada y tenant |
| D03 | Dominio y TLS | Elegir con autorización posterior; sin cambios ahora |
| D04 | Headers CSP/CORS/HSTS | Auditar políticas efectivas antes de ajustar |
| D05 | Aislamiento de proyectos | Vinculación explícita dev/qa/prod por ID y rama |
| D06 | Estrategia SEO | Definir fallback SSR/bot sin PII |
| D07 | Artefactos y manifest | Elegir firma/hash + retención para rollback |
| D08 | Observabilidad | Correlacionar frontend-proxy-API sin datos sensibles |
| D09 | Presupuestos UX | Fijar límites medibles de bundle/latencia/accesibilidad |
| D10 | Criterios de promoción | Gate A5 y permiso separado para QA/main/PROD |

Ninguna Dxx es autorización de compra, cambio DNS, nueva cuenta, migración R2, ampliación de scopes de API ni promoción de entorno.

## Pares de escenarios de aceptación futuros (NO_EJECUTADOS)

- **P01/N01:** ruta privada con 401/403 correctos / cliente sustituye tenant B y recibe acceso indebidamente.
- **P02/N02:** cache de assets públicos respeta hash / página autenticada o JWT de A se sirve a B.
- **P03/N03:** build dev reproduce manifest / SKIPPED o deployment Vercel con límite de cuota se trata como PASS.
- **P04/N04:** SEO de páginas públicas correcto / crawler indexa factura, sesión o endpoint privado.
- **P05/N05:** CSP/CORS permite orígenes y scripts aprobados / wildcard con credenciales o script no aprobado.
- **P06/N06:** rollback por SHA restaura página/headers / configuración remota queda incongruente sin readback.
- **P07/N07:** teclado y foco recorren navegación / overlay o bundle degradado bloquea accesibilidad.
- **P08/N08:** proxy conserva tracing seguro / logs incluyen Authorization, credenciales o PII.

## Criterios de gobernanza y rollback

**A0:** Skill local=cánonica, SHA `3996cc6950497f587ef1656e60f95d2772d8b9e2` con Scope Lock SUCCESS, PLAN H1769/I vacío antes de CAS y owner asignado únicamente tras compare-and-swap; COLA G/O=SI y DAG F1730=VALIDADO/G1730=SI. **A1 documental:** cobertura R01–R16, S01–S10, D01–D10 y pares positivos/negativos futuros. **A2 documental:** JWT/RBAC/tenant A/B, CORS, CSP, secretos y datos privados protegidos por fronteras existentes. **A3 documental:** referencias cruzadas de inventario C12-02.M01.03 y rutas Angular/Vercel/Render; pruebas runtime externas NO_APLICA por ausencia de cambios ejecutables. **A4:** diff únicamente docs/v5 y CHANGELOG, commit fast-forward CAS y checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en completed/success sobre SHA exacto, más readback PLAN/COLA/BITACORA. **A5:** revisión posterior por otra Task Javier, nunca por autoría propia.

**Rollback:** commit `git revert` normal del cambio documental, sin reset/force-push; BITACORA histórica inmutable. **Dictamen:** preparación documental de riesgos, no funcionalidad ni infraestructura productiva certificada.
