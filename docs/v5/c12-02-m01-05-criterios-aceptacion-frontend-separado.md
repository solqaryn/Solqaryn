# SOLQARYN V5 — C12-02.M01.05: aceptación funcional y técnica observable

**ID:** C12-02.M01.05; PLAN_MAESTRO fila 1770, C12-02 Frontend separado, M01. **Autor:** PRIMARY DEV 01 JAVIER; TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Corte:** `solqaryn/Solqaryn@dev` `ac4ce826607f8964e57af8b04b3cffd59cb94ee3`, 2026-11-14. **Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `PROJECT_SCOPE_LOCK=STRICT`.

## Alcance y evidencia

Esta fase define **pruebas y oráculos FUTUROS_NO_EJECUTADOS** para build Angular 22.2.1, SPA, proxies, cache, headers, SEO, accesibilidad y rollback. No implementa ni habilita CDN, DNS, SSL, Hostinger, Cloudflare, API nueva, JWT, worker, eventos, DB, QA/main/PROD ni secretos. Vercel/Render/Aiven/Cloudinary permanecen como arquitectura declarada; no existe lectura de proveedor live en este scope.

**Fuentes rastreables:** `frontend/package.json` blob `e6d030f643421cb343cc12617e742469e88223c4`; `frontend/angular.json` `8b00f8fee80fdfafe1864e233be7d5f20ad444e3`; `frontend/vercel.json` `5f12bcf667a21afcfc4314b66787ff66627ae666`; `frontend/scripts/vercel-ignore-build.mjs` `81d9c7fe6639da0fee1a23dac34e337f3c994507`; `frontend/src/app/app.routes.ts` `2f560c14693455b79ac4546e9333fb74586ef234`; `frontend/src/app/app.config.ts` `cd77384e713014015d83b348955b3d83573348b3`; `render.yaml` `f64d291425e2bbfdd9b6466d268753f8f8d86b3d`. Inventario C12-02.M01.03, blob `878c620a20e48de70003f84f777a8bffc5fd11a1`, y riesgos C12-02.M01.04, blob `52771fbe9ce803cce4af7ce41232d2381a494524`.

## AC01–AC16 / N01–N16: observables, todos NO_EJECUTADOS

| ID | Criterio positivo futuro | Rechazo negativo futuro | Evidencia exigida |
|---|---|---|---|
| AC01/N01 | Build Angular reproducible por SHA y lockfile | Build SKIPPED/CANCELLED tratado como PASS | CI exact-SHA + manifest |
| AC02/N02 | SPA profunda vuelve a cargar tras refrescar URL | Fallback SPA responde 404 erróneo | Smoke browser |
| AC03/N03 | Ruta privada requiere 401/403 backend | Guard de cliente sustituye backend RBAC | HTTP API tokens |
| AC04/N04 | Proxy conserva token y status HTTP | JWT filtrado en logs o HTTP 200 sobre 403 | Logs + tests |
| AC05/N05 | Tenant A no lee B a través del proxy | Cabecera/URL tenant B suplanta identidad | A/B negativo |
| AC06/N06 | Assets con hash y TTL explícito | HTML mezcla assets obsoletos incompatibles | Manifest + headers |
| AC07/N07 | HTML autenticado no se comparte entre usuarios | CDN sirve sesión de A a B | Cache-Control + dos sesiones |
| AC08/N08 | CSP efectiva permite scripts aprobados | CSP permite script ajeno o bloquea app | Captura CSP + browser |
| AC09/N09 | CORS permite solo orígenes previstos | Wildcard con credenciales | Preflight cross-origin |
| AC10/N10 | SEO/sitemap incluye solo públicos | Factura o datos privados indexados | Crawler/PII |
| AC11/N11 | Config env no exporta secretos | JWT/DB password dentro del JS distribuido | Bundle scanner |
| AC12/N12 | Binding de Vercel distingue ramas | Commit dev llega a QA/prod por error | Readback proyecto |
| AC13/N13 | Proxy expone rutas coherentes con Render | Proxy cambia backend destino sin control | Contrato request/response |
| AC14/N14 | Accesibilidad conserva teclado/foco | Overlay o lazy route bloquea foco | Playwright a11y |
| AC15/N15 | UI respeta presupuesto UX medible | Bundle degrada navegación sin alarma | stats y navegador |
| AC16/N16 | Rollback restaura versión/headers | Git revert deja edge distinto y sin smoke | Runbook/manifest |

La ejecución futura debe producir logs/artefactos y probar tanto éxito como denegación. No se aceptan mocks para acreditar proveedor CDN live, ni un check en cola, cancelado o SKIPPED como PASS de una prueba requerida.

## NF01–NF10: requisitos no funcionales, pendientes de medición

- **NF01:** Trazabilidad SHA->artifact->entorno y reversión; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.
- **NF02:** Privacidad de PII/JWT en logs y HTML; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.
- **NF03:** Aislamiento tenant A/B comprobado por API; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.
- **NF04:** Umbrales de bundle/latencia medidos antes de aprobar; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.
- **NF05:** Toolchain Angular 22.2.1 y Zone.js 0.16.3 fijados; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.
- **NF06:** Teclado, foco, labels y contraste verificados; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.
- **NF07:** Cache pública/privada y expiración diferenciadas; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.
- **NF08:** Correlación frontend-proxy-API sin token en log; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.
- **NF09:** Restauración/rollback probado en entorno autorizado; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.
- **NF10:** Separación dev/qa/prod y permisos explícitos; instrumento, entorno y umbral se fijarán cuando la etapa de implementación/QA lo autorice.

No se reportan números fabricados de LCP, INP, CLS, tamaño de bundle, latencia, disponibilidad, RTO/RPO ni presupuesto en dinero. La decisión final de aceptación solo procede con medidas reales proporcionadas por CI/herramienta autorizada y evidencia por SHA.

## Gobierno A0–A5 y rollback

**A0:** Skill instalada y canónica cotejadas; `PROJECT_SCOPE_LOCK=STRICT`; PLAN H1770=PENDIENTE/I vacío antes de CAS, COLA G/O=SI, DAG F1731=VALIDADO/G1731=SI; owner Task único y readback. **A1 documental:** 16 positivos y 16 negativos específicos como requisitos futuros; no se afirma ejecución. **A2 documental:** JWT/RBAC/tenant A/B, secretos, PII, caché, CORS/CSP y entornos separados. **A3 documental:** concordancia con código Angular/vercel/render e inventario/riesgos C12-02 previos; pruebas runtime NO_APLICA para solo Markdown. **A4:** Git commit fast-forward a dev de solo este documento y CHANGELOG; controles Scope Lock y V5 provenance completed/success sobre SHA exacto, más readback PLAN/COLA/BITACORA. **A5:** revisor otra Task Javier, no autoaprobación; no habilitar M01.06/M02 ni promoción desde preparación.

**Rollback:** `git revert` normal del changeset documental; sin reescribir historia, BITACORA, secretos o infraestructura. **Dictamen:** criterios técnicos formulados, **ninguna funcionalidad de frontend separado ejecutada ni certificada**.
