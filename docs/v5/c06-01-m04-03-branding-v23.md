# C06-01.M04.03 — Branding dinámico público SOLQARYN (V23)

Fecha 2026-11-21; repositorio solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO!D886: branding, configuración dinámica y componentes permitidos. PLAN E es meta acumulativa, no obliga a certificar la fase M04 entera.

## Diseño y ámbito
Presentación editorial SaaS pública, sobria y accesible. Se conserva la paleta institucional. Se añade modo contraste alternable con una Signal Angular 22 y control aria-pressed por teclado real. Solamente dos estados allowlisted (institucional, contraste) y tokens CSS de componente; ningún valor de query string, slug, tenantId, cabecera, cookie ni usuario decide marca, datos o CSS. No se instala librería, no se crea endpoint ni HTTP, no se modifica login, guards ni persistencia. Branding auténtico por tenant/host exige contrato backend verificado y se excluye expresamente de esta entrega.

Scope único: frontend/src/app/features/landing/c0601-public-saas-landing.component.ts; pruebas reales Chromium en frontend/e2e/c06-01-m04-03-branding.spec.ts; workflow GitHub-hosted exact SHA. Consumidor Angular Router existente. Nada nuevo en Domain/Application/Infrastructure/API, DTO, RBAC, JWT, MySQL, pagos, migraciones ni secretos. Rollback mediante revert normal, nunca force-push.

## Skills originales ACTIVE y pins consultados
- SKREF-03 Impeccable: pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199 / .agents/skills/impeccable/SKILL.md
- SKREF-05 Taste: Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37 / skills/taste-skill/SKILL.md
- SKREF-06 Humanizer: blader/humanizer@9862685f575c65a8247f90369951df1b3416e3d6 / SKILL.md
- SKREF-10 Angular Developer: angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9 / skills/dev-skills/angular-developer/SKILL.md
Se consultan sin instalar/ejecutar dependencias externas; SKREF-04 NO_APLICA sin motion, SKREF-11 NO_APLICA sin nueva frontera de amenaza, SKREF-12 NO_APLICA sin cambio .NET/performance medida.

## A0–A5 causales y límites
- A0 R1 UI: diff restringido a componente Angular, E2E/workflow y este documento. Reactividad y tokens tipados/allowlisted.
- A1 Playwright Chromium GitHub-hosted: alternancia positiva, retorno, query maliciosa ignorada, Tab real + Enter/Espacio, control aria-pressed, ausencia de formularios/cobros, ventanas 320/390/768/1280/1440 y capturas antes/después auténticas en artefactos.
- A2 prueba negativa de no mutación HTTP ni acceso privado; backend RBAC/tenant y JWT no se alteran, aislamiento A/B de datos privados no se certifica por este UI público.
- A3 integración real Angular22/Chromium y producción build; MySQL/JWT/HTTP backend NO_APLICA para la ruta pública estrictamente informativa. PageScaleFactor Chromium 200% es zoom visual del motor, NO zoom del menú del navegador.
- A4 esperar check-run completed/success exact SHA: Scope Lock, Skill, Angular tipado/build, Chromium y controles V5 pertinentes. QUEUED/IN_PROGRESS/FAIL/SKIPPED requerido NO son PASS; conservar VALIDANDO y handoff si aún corren.
- A5 revisión genuina de otra Task Javier distinta de DEV06, autor y reparador; no autoaprobar.
El criterio premium de superficies con sidebar, modales, tablas, formularios, error HTTP 403/429/500 y branding tenant-host no es aplicable al contenido actual o corresponde a otra microtarea: **no certificado**. Tampoco se certifica toda M04 ni QA/main/PROD.

## Reparación causal de CI Chromium V23 (2026-11-21T20:05:21.144Z)
GitHub-hosted run 44943988011 job 136967130624 (SHA 03b1318c) ejecutó las tres primeras pruebas reales PASS y falló la cuarta tras timeout de 45 s al operar en PageScaleFactor 200%. El `finally` intentaba enviar CDP tras cierre del browser y ocultó la operación original. Hipótesis: Playwright pointer scroll/full-page screenshot con viewport visual escalado produjo una espera no acotada. Se conserva la comprobación real `visualViewport.scale >=1.8`, se reemplaza interacción pointer por secuencia Tab+Enter real y se evita la captura fullPage en escala CDP (las capturas before/after 320/390/768/1280/1440 anteriores sí fueron ejecutadas y se adjuntaron). El cleanup no reemplaza error original ni inventa PASS. Esta hipótesis requiere nuevo GitHub-hosted exact SHA, 4/4 PASS y checks A0–A4 de nuevo antes de LISTO. No se deshabilitan checks ni se cambia el código de producción.

## Extensión adversarial premium V23 (2026-11-21T20:08:16.658Z)
Prueba Chromium adicional independiente de los screenshots: calcula luminancia sRGB lineal y ratio WCAG AA >=4.5:1 desde getComputedStyle en modo alto contraste (en 390 y 1440 px), para navegación de cabecera, párrafo hero, nota de panel, beneficios y cierre; falla con colores reales de bajo contraste. No sustituye al modo claro ya validado por la prueba M04.05. Esta extensión requiere un nuevo SHA y resultado GitHub-hosted SUCCESS real, nunca se da por PASS a partir de texto. No se toca componente ni se debilitan test/gates.

## Corrección causal de conservación de capturas (2026-11-21T20:11:19.226Z)
La ejecución GitHub-hosted 44944666975 completó 5/5 Chromium, incluyendo contrastes WCAG AA medidos en navegador, pero el artefacto 14722609737 pesó solo 545 bytes y no conservó las diez capturas de tests que pasan: Playwright puede purgar outputs de tests exitosos. No se certifica evidencia visual por la sola llamada a test.info().attach. Reparación real: guardar PNG completos en frontend/v23-artifacts/c0601-m0403 con Node fs/promises, diez archivos concretos antes/después en anchuras 320/390/768/1280/1440, afirmación CI de existencia y tamaño positivo de los diez archivos, y upload-artifact incluyendo el directorio persistente, además de HTML/JUnit. Se mantiene la ejecución Chromium real (sin mocks) y el gate 5/5. Se exige nuevo SHA GitHub-hosted con check de persistencia PASS y readback del artefacto significativo antes de validar A0–A4/LISTO. No se modifica componente de producción ni se suprimen validaciones.
