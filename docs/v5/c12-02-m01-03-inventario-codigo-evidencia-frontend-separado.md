# SOLQARYN V5 — C12-02.M01.03: inventario de código y evidencia del frontend separado

**Microtarea:** C12-02.M01.03, PLAN_MAESTRO fila 1768, C12-02 Frontend separado, fase documental M01. **Autor:** PRIMARY DEV 01 JAVIER, Task `6adbb9fff1548191a1bd5b30d9e9984a`. **Corte:** `solqaryn/Solqaryn@dev` `1424d493368302467532067821a0a3d3f7fe2499` (2026-11-14). **Fuente autorizada:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `PROJECT_SCOPE_LOCK=STRICT`.

## Alcance y exclusiones

Este documento **lee contratos, código y evidencia disponibles sin suponer certificación** para una separación futura de frontend Angular, build, CDN, proxy API, headers, caché, dominio y rollback. Ningún CDN, DNS, TLS, frontend externo, worker, webhook, evento ejecutable o cuenta nueva se activa; no se cambian secretos, API, base de datos, QA, main ni PROD. El despliegue actual descrito por la arquitectura sigue siendo Vercel/frontend, Render/API, Aiven/MySQL y Cloudinary/medios. No se verificó estado remoto de esos proveedores ni un smoke público.

Se leyeron las diez fuentes siguientes mediante GitHub autenticado en el mismo SHA. El documento previo `C12-02.M01.02` ya establece A01-A12 actores, I/O01-I/O12 interfaces y F01-F12 fronteras; no se duplican como una nueva autoridad.

## Inventario de diez fuentes Git

| N | Ruta | Blob SHA exacto | Contrato observado |
|---|---|---|---|
| 1 | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Angular/Material 22.2.1, Zone.js 0.16.3, npm scripts |
| 2 | `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Builder Angular, configuraciones y file replacement por ambiente |
| 3 | `frontend/vercel.json` | `5f12bcf667a21afcfc4314b66787ff66627ae666` | Rewrites SEO/API, fallback SPA, headers/caché declarados |
| 4 | `frontend/scripts/vercel-ignore-build.mjs` | `81d9c7fe6639da0fee1a23dac34e337f3c994507` | Skip/build según project ID y branch; SKIPPED no es PASS |
| 5 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Angular routes, guards de UX y carga diferida |
| 6 | `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | provideRouter, HTTP interceptors, zone change detection |
| 7 | `frontend/src/environments/environment.ts` | `fcc3144df39328605e7d7b561d4024b930fdfd59` | development local production:false y localhost:5005 |
| 8 | `render.yaml` | `f64d291425e2bbfdd9b6466d268753f8f8d86b3d` | API Render dev/qa/prod declaradas por YAML, no readback live |
| 9 | `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Scope Lock STRICT, dev y veto QA/main/PROD |
| 10 | `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Stack Angular/.NET, seguridad JWT/RBAC/tenant e infraestructura |

**Cruce funcional y límites:** el frontend declara `ng build` y `ng build --configuration production`; no se ejecutaron por esta microtarea. El rewrite Vercel `/api/:path*` a `/api/backend-proxy?path=:path*` está versionado, pero no verificado con request live. El script `vercel-ignore-build.mjs` selecciona rama/proyecto y puede omitir builds; no autoriza omitir CI requerida ni convertir SKIPPED/CANCELLED en SUCCESS. El environment local usa `http://localhost:5005`, no constituye target PROD. Los guards Angular son UX; la API conserva JWT, RBAC, tenant A/B, rate limits y controles reales. Caché y CDN no pueden cachear respuestas autenticadas entre usuarios o empresas.

## G01–G12: brechas de evidencia a resolver en fases futuras

Estas son **faltas de evidencia en esta revisión**, no vulnerabilidades ni errores productivos demostrados.

- **G01:** Manifest/hash del build Angular por commit.
- **G02:** Smoke en navegador de rutas SPA profundas y assets.
- **G03:** Prueba HTTP del proxy conservando 401/403 sin transformar estados.
- **G04:** Prueba API contra acceso cruzado A/B a través del proxy.
- **G05:** Cabeceras CSP/HSTS/nosniff/CORS efectivas en dominio autorizado.
- **G06:** Cache-Control/no-store de HTML autenticado y prueba cross-user.
- **G07:** SEO/robots/sitemap sin exponer PII o URL privadas.
- **G08:** Readback real binding project/branch y comportamiento ignore-build.
- **G09:** Análisis reproducible del bundle para descartar secretos.
- **G10:** Inventario autorizado de DNS/TLS y plan de reversión.
- **G11:** Pruebas de teclado/foco/bundle budget en navegador.
- **G12:** Prueba de rollback de versión con smoke posterior.

## Escenarios P/N, todos FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Rechazo obligatorio |
|---|---|---|
| P01/N01 | Rutas y guards permiten UX normal | Una URL directa no puede omitir RBAC API |
| P02/N02 | Proxy conserva 401/403 y cuerpo | No transforma error de auth en 200 |
| P03/N03 | Asset estático obtiene caché conforme contrato | Nunca cachea HTML/JWT de usuario B para A |
| P04/N04 | Build autorizado por rama genera artefacto | SKIPPED no se contabiliza como PASS |
| P05/N05 | SEO publico muestra URLs permitidas | Nunca expone factura o datos cross-tenant |
| P06/N06 | Rollback restaura versión trazable | No modifica QA/PROD sin orden |

Son criterios de diseño; **no** se ejecutaron Playwright, HTTP/API con tokens reales, CORS live, MySQL, smoke, DNS/TLS o pruebas de infraestructura por elaborar esta documentación.

## Seguridad, trazabilidad y rollback

**A0:** Skill instalada y canónica cotejadas; Scope Lock STRICT, SHA `1424d493368302467532067821a0a3d3f7fe2499` con `verify-solqaryn-project-scope` completed/success; PLAN H1768=PENDIENTE/I vacío antes de CAS, COLA G/O=SI, DAG F1729=VALIDADO/G1729=SI; owner propio único confirmado con readback.

**A1 documental:** diez rutas/blobs positivos comprobados; negativo: no inferir CDN, smoke o release desde YAML/Markdown. **A2 documental:** preserve JWT/RBAC/tenant A/B, privacidad de PII, secrets fuera del bundle y separación QA/PROD. **A3 documental:** referencias cruzadas Angular/vercel/render/guards coherentes, sin pruebas de integración live. **A4 esperado:** commit fast-forward de este documento y CHANGELOG en dev, verify Scope Lock + V5 provenance exact SHA y readbacks Git/PLAN/BITACORA. Suites runtime ajenas a este diff Markdown = NO_APLICA motivado, nunca PASS. **A5:** otra Task Javier distinta del autor debe revisar independientemente antes de habilitar otra promoción; no autoaprobar.

**Rollback:** `git revert` normal del commit documental, sin force-push ni modificación retroactiva de BITACORA. Ninguna certificación de frontend separado funcional, infraestructura provisionada, migración o costos.
