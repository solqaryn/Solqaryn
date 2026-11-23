# C15-04.M05.04 - A110: reflow de texto 200% en Chromium

Estado: TEST-ONLY R2, WIP historica DEV02. No certifica WCAG global, LISTO ni A5. Dev exclusivamente. Suite A109 previa 146 casos; A110 agrega tres pruebas Playwright (149 esperadas) en tres superficies privadas reales.

PLAN D: caso positivo de aceptacion de UX global. Cada prueba utiliza JWT firmado backend .NET 10 y MySQL 8.4 efimero, HTTP /plataforma/sesion 200, Angular 22.2.1, anchos 320/390/768/1280/1440. Verifica fuente de titulo antes y despues de texto 200%, visibilidad, ausencia de overflow body y html, y cero mutaciones HTTP. Evidencia en screenshots 100% y 200% de texto de la MISMA corrida a 320 y 1440 dentro del artefacto Playwright.

LIMITACION: duplicar font-size CSS de html en Chromium es prueba real de ampliacion de texto/reflow, NO es zoom nativo del navegador al 200%, baseline anterior al cambio de UI, WCAG global, todos los modulos ni CWV de campo. No declarar lo no ejecutado.

A0 diff, requisito; A1 positivos, negativos y responsive; A2 JWT/RBAC y negativos tenant A/B de suite existente; A3 API/MySQL 8.4/Chromium reales solo si CI se completa; A4 exigir Scope Lock, V5, frontend/backend/security pertinentes y 149/149 CI exact SHA completed/success; A5 independiente despues de cierre A0-A4 global. SKIPPED/IN_PROGRESS/FAIL no PASS.

SKREF-03 Impeccable pbakaus/impeccable pin 2149fcce39a90bb409df5f16515f316a76dc6199 y SKREF-10 Angular Developer angular/angular pin 6808966993bf2b2d455d9b6245e960a59a01b9a9 consultados en origen original sin instalar. Motion, marketing, copy, threat-model y .NET perf NO_APLICA. Rollback git revert normal tras HEAD CAS; sin QA/main/PROD, secretos ni permisos nuevos. Owner DEV02 y V17 lock 2000002297 preservados.

## A111 — corrección causal posterior al CI fallido A110

El run 45071359051, SHA f2d19f764654e0395c4441c3070f251ff14b774a, falló con **146 PASS / 3 FAIL**: los h1 de /plataforma/capacidades-v19, /plataforma/revision-arquitectura-v20 y /plataforma/eventos-outbox-v22 no crecieron al menos 150% al duplicar el tamaño raíz CSS. El primer título permanecía en 24px y los otros dos quedaron limitados por vw/clamp (36→48px, 35.2→48px); no eran fallos inventados ni permisos.

Corrección de producto R1 bajo WIP R2 de DEV02: títulos con clamp mínimo relativo en rem y término fluido `calc(1.5rem + 1vw)` y `overflow-wrap:anywhere` para conservar escala frente a ampliación de fuente en 320/390/768/1280/1440. La misma prueba A0.85 mantiene sus asserts y sus 3 casos: no relajar ni omitir FAIL. Se amplía el trigger GitHub Actions a los tres componentes, habilitando regresión causal exact-SHA; verificar 149/149 real Playwright en .NET 10 API JWT firmado y MySQL 8.4 efímero, y checks de seguridad/Scope Lock/V5/Angular pertinentes antes de certificar A1–A4 parcial. Revisión independiente A5 y cierre global pendiente: esta reparación por sí sola no acredita UX global, WCAG total ni zoom nativo 200%.

Impacto: solo tamaños/word-break en las tres superficies de lectura, sin cambios a rutas, DTO, API, contratos, JWT/RBAC, persistencia o migraciones. Rollback: git revert no destructivo de commit A111 tras revalidar HEAD y propietarios; no bajar asserts, CI ni seguridad. SKREF-03 Impeccable pin 2149fcce y SKREF-10 Angular Developer pin 68089669 consultadas en originales; motion, copy marketing, threat model y profiling .NET no aplican.

## A112 — ajuste matemático anti-clamp (sin atenuar regresión)

La primera fórmula de A111 `clamp(1.5rem, calc(1.5rem + 1vw), 2.25rem)` no garantiza crecimiento >=150% al duplicar la raíz: en 320px pasa de 27.2px a 35.2px (~129%); en 1280px pasa de 36px a 44.8px (~124%). Aunque su CI original seguía activo, se corrigió preventivamente el fallo matemático demostrado sin declarar un PASS. Las tres superficies ahora usan `1.5rem` en vista compacta y 2.25rem (outbox 2.2rem) a partir de 768px. En cada anchura fija, duplicar el root font-size duplica el H1 matemáticamente; `overflow-wrap:anywhere` conserva cortes de líneas. No se modifica el test adversarial ni se simula JWT/API/Chromium: CI real exact-SHA todavía requerida. Misma WIP histórica y V17 de DEV02, A0-A4 global y A5 no concluidos; histórico CHANGELOG preservado íntegro por blob en la reparación anterior; rollback git revert normal, nunca force-push.

## A113 - prueba incremental de tres rutas privadas

El SHA anterior e109cce9d8c7bc22238b96f00614d2d0ed362187, run 45076994971/job 137376336245, termino con 149/149 Chromium PASS, JWT real, HTTP backend y MySQL 8.4 efimero. A113 agrega A0.86 para observabilidad-v5, inventario-rutas-v19 y mensajeria-multicanal-v19: 3 casos nuevos, 5 anchos 320/390/768/1280/1440, sesion HTTP 200, H1 real, texto 200% y cero overflow/POST/PUT/PATCH/DELETE; capturas 100/200% en 320/1440 del mismo run. Suite esperada 152, sin PASS hasta CI exact-SHA.

R2 test-only: no se altera runtime, JWT/RBAC, DB ni API; triggers de componentes se amplian para evitar regresion silenciosa. A2 aislamiento A/B se conserva en otros casos de la suite. Limitaciones: CSS root duplicado no es zoom nativo 200%, sin baseline visual historico ni cobertura total de 160 rutas, WCAG global o CWV de campo. Por tanto, PLAN H2297 EN_PROGRESO con owner DEV02 y V17 lock 2000002297; A0-A4 global y A5 permanecen pendientes. SKREF-03/10 consultadas de fuentes originales fijadas; rollback git revert normal por HEAD CAS, no QA/main/PROD.

## A114 — regresión adversarial JWT empresa B sobre tres superficies privadas

La ejecución A113 quedó acreditada por GitHub-hosted SHA \`860a6069f1f7967c3921cd094aa370d8868f3d06\`, run \`45082259710\`, job \`137392821215\`: 152/152 Playwright Chromium PASS, 0 FAIL y 0 SKIPPED, con backend ASP.NET Core 10 y MySQL 8.4.13 efímero; Scope Lock, V5, builds, seguridad y CI pertinentes SUCCESS para ese SHA. Esta evidencia no prueba toda la auditoría global.

A114 agrega **tres pruebas negativas nuevas** en observabilidad, inventario-rutas y mensajería. Cada prueba usa JWT real de empresa B sin rol de plataforma: \`GET /plataforma/sesion\` devuelve 403, el guard redirige a \`/login?plataforma=denegado\`, no monta el título privado, persiste la denegación tras 300 ms, mantiene el ancho de documento dentro de 320/390/768/1280/1440 y verifica cero POST/PUT/PATCH/DELETE. Se ejecutarán en el mismo CI GitHub-hosted con .NET 10, Angular y MySQL 8.4 aislado. **155 pruebas esperadas, pendientes de CI exact-SHA tras publicar.** No declarar PASS hasta comprobar el nuevo SHA y el job; el artefacto es test-only (sin mutaciones de runtime, RBAC, DB, QA o producción).

**Limitación explícita:** la cobertura institucional de aproximadamente 160 rutas, zoom nativo 200%, WCAG completo, Core Web Vitals y capturas históricas previas siguen sin certificarse. Mantener \`PLAN_MAESTRO H2297=EN_PROGRESO\`, autor DEV02, candado V17 y A5 independiente; no fingir LISTO. Rollback: git revert normal del commit exacto, sin force-push.


## A115 — regresión nueva de branding/capacidades privada (R2 test-only)

A114 confirmado GitHub-hosted exact-SHA `4c0839177c3c52f9d129d4dd16b508981f6829a5`: job 137408927725/run 45087366260, 155/155 Chromium PASS, 0 FAIL/0 SKIP, MySQL 8.4.13 efímero, JWT firmado y Scope Lock/V5/backend/frontend/seguridad pertinentes SUCCESS. Retries opcionales SKIPPED no son PASS.

A115 añade A0.88 (plataforma HTTP 200, H1 branding, cinco anchuras 320/390/768/1280/1440, CSS raíz x2, capturas 100/200, sin escrituras) y A0.89 (tenant B JWT real HTTP 403, redirección denegada, ausencia de contenido privado y sin mutaciones). La ruta `/plataforma/capacidades-branding-v19` ya existía bajo plataformaGuard. Se esperan 157 pruebas GitHub-hosted; NO certificar el SHA nuevo antes de CI.

SKREF-03 Impeccable y SKREF-10 Angular verificados en origen/pin. Cambio sólo a Playwright, documentación y CHANGELOG; no runtime, DB, autorización ni producción. Mantener H2297 EN_PROGRESO/I DEV02 y V17 lock. Sin evidencia de zoom nativo de escritorio 200%, cobertura global de rutas, WCAG/CWV de campo, baseline visual histórica o A5 independiente. Rollback con git revert normal.

## A116 — navegación de teclado verificable en branding privada

A115 de origen `5f848bd01c8b57a802f75441ad7381231ef90002` finalizó el run `45093752528`/job `137428869850` GitHub-hosted COMPLETED/SUCCESS. Los 15 checks causales de backend, frontend, seguridad, Scope Lock, procedencia V5 y Chromium/MySQL 8.4 completaron SUCCESS; los tres retries opcionales y la migración EF condicional SKIPPED no se interpretan como PASS. La suite previa tenía 157 casos previstos.

A116 añade una prueba nueva A0.90 de teclado real sobre `/plataforma/capacidades-branding-v19`: JWT emitido por la API .NET 10, MySQL 8.4 efímero, HTTP 200 real; Tab desde el documento (sin `.focus()`), foco natural en el vínculo «Volver a plataforma», anillo calculado de al menos 2px, Enter y navegación real a `/plataforma`, sin mutaciones HTTP ni overflow global en 320/390/768/1280/1440. En 320/1440 se generan capturas del estado previo al Tab y del foco visible de la misma ejecución. El cambio agrega 1 caso: **158 previstos; no acreditados hasta CI exact-SHA COMPLETED/SUCCESS 0 FAIL/0 SKIP**, con artefactos verificables. No reemplazar con mock/screenshot sintético.

Riesgo R2 de auditoría global, diff test-only + documentación + CHANGELOG; sin rutas o permisos nuevos, ni modificación Angular, JWT, MySQL o servicios. A0 estático del incremento; A1 prueba positiva y negativa de accesibilidad real; A2 JWT/A-B de la suite combinada, sin cambios runtime; A3 GitHub-hosted API/MySQL/Chromium requerida; A4 exact-SHA Scope Lock/V5 y checks pertinentes. A5 independiente solo al completar A0-A4 del ID. SKREF-03/10 ya registradas con origen/pin en este scope; motion, copy pública, amenaza, .NET performance NO_APLICA para este incremento.

**Límite de certificación:** Tab real y CSS root x2 no prueban zoom nativo de navegador a 200%, cobertura UX integral de ~160 rutas, WCAG global, métricas CWV de campo o capturas históricas. Mantener PLAN H2297 EN_PROGRESO, I DEV02 y V17 lock 2000002297. Rollback mediante git revert no destructivo con HEAD/CAS; sin QA/main/PROD.

## A117 — rechazo adversarial de JWT manipulado en branding

A116 exact-SHA `9f2eb909b5267cd0e84d1747f8896c7dbd49245c` completó GitHub-hosted `45098638730`/`137444073749`: **158/158 Chromium PASS, 0 FAIL, 0 SKIP**, con API ASP.NET Core 10, MySQL 8.4.13 efímero y JWT real. Scope Lock, frontend, backend, seguridad y V5 del mismo SHA completaron SUCCESS; reintentos opcionales o migraciones condicionales SKIPPED no son PASS.

A117 incorpora A0.91: sesión de plataforma autenticada positiva, firma JWT adulterada, rechazo HTTP 401/403 real, guard Angular a /login, ausencia de encabezado privado, sin escritura API y sin overflow global en 320/390/768/1280/1440. **159 pruebas esperadas**, a acreditar exclusivamente cuando el nuevo SHA complete GHA/CI SUCCESS y los logs muestren 159 PASS/0 FAIL/0 SKIP.

Clasificación R2 test-only, sin cambios runtime, permisos, esquema ni migración; A0 alcance de prueba, A1 positivo/negativo, A2 firma y autorización reales, A3 HTTP/MySQL/Chromium en CI, A4 exact SHA y readback pendiente. No equivale a auditar todas las rutas. SKREF-03/10 previamente verificadas en UI; en este diff sólo prueba/documento, NO_APLICA nueva adopción externa. Persisten cobertura ~160 rutas, zoom nativo 200%, baseline histórico, WCAG/CWV globales y A5 independiente. H2297 EN_PROGRESO, I DEV02, lock V17 propio retenido. Rollback: `git revert` normal. No QA/main/PROD.


## A118 - persistencia del rechazo RBAC de empresa B despues de reload

A117 sobre SHA `ea090c14f73b6ea761475f462ff0302750e8dc80`: GitHub-hosted `45104753495`/`137463360972`, Chromium real 159/159 PASS, cero fallos o SKIP; API .NET 10 y MySQL 8.4.13 efimero, JWT firmado; Scope Lock, V5, build, seguridad y tipado pertinentes completaron SUCCESS. Los jobs auxiliares SKIPPED no se incluyen como aprobados.

A118 agrega el caso A0.92 (160 esperados, NO certificados aun para este cambio): empresa B con JWT real recibe HTTP 403, el guard redirige a /login?plataforma=denegado; la recarga completa del navegador mantiene la denegacion, no restaura el H1 privado, no genera overflow global ni escrituras HTTP POST/PUT/PATCH/DELETE. Anchos: 320, 390, 768, 1280 y 1440, backend/JWT/MySQL/Chromium reales GitHub-hosted. A0 alcance de test; A1 positivo JWT B valido y negativos de acceso/recarga; A2 aislamiento RBAC real; A3 y A4 del nuevo SHA quedan PENDIENTES hasta comprobar logs y checks del SHA exacto.

R2 test-only + documentacion + changelog, sin cambio de runtime, migracion ni permisos; SKREF-03/10 documentados previamente, sin nueva referencia externa requerida para el diff. Cobertura UX completa de unas 160 rutas, zoom nativo 200%, capturas historicas, WCAG global y CWV de campo siguen pendientes. Mantener H2297 EN_PROGRESO, I DEV02, lock V17 2000002297, A5 independiente al completar la fase. Rollback por git revert normal y CAS; prohibido QA/main/PROD.

## A119 — operabilidad real por teclado tras RBAC 403

A118 source SHA `a437f6bf35f219538389fdd7cb72efb9073812c0` completó GitHub-hosted run `45110882148`/job `137482720285`: 160/160 Chromium PASS, 0 FAIL/0 SKIP, API .NET 10, MySQL 8.4 efímero, JWT real y checks pertinentes Scope Lock/V5/backend/frontend/security SUCCESS. Cuatro jobs opcionales/condicionales SKIPPED NO son PASS.

A119 A0.93 añade **un caso adversarial nuevo** (161 esperados, NO acreditados hasta que el SHA nuevo termine CI): JWT B firmado válido pero sin RBAC global recibe HTTP 403 de `/plataforma/sesion`; Angular redirige a login con `plataforma=denegado`, sin mostrar el título privado. En cinco anchos (320/390/768/1280/1440), comprobar nombre accesible del textbox, recorrido **Tab real sin .focus()**, escritura real únicamente después de alcanzar foco, cero POST/PUT/PATCH/DELETE a la API y cero overflow global. Capturas antes del Tab y con foco, en 320/1440, de la MISMA corrida; no son baseline histórica ni zoom nativo. No hay interceptores ni mocks de HTTP.

R2 test-only; A0 diff a Playwright/documento/changelog, A1 caso positivo de teclado y negativos de auth/datos, A2 JWT/RBAC real tenant B, A3 HTTP/MySQL/Chromium de GitHub-hosted, A4 exigiendo exact-SHA SUCCESS en 161/161 y checks pertinentes. Hasta entonces, A1–A4 del incremento quedan pendientes. SKREF-03/10 ya referenciadas por el scope en su pin; no instalar referencias externas ni cambiar runtime, migraciones, permisos o arquitectura. **La cobertura UX global de ~160 rutas, WCAG completo, CWV de campo, zoom nativo 200%, baseline de imagen anterior a la funcionalidad y A5 independiente siguen NO ACREDITADOS.** Mantener PLAN H2297 EN_PROGRESO, I DEV02 y lock V17 2000002297; rollback mediante git revert normal, sin QA/main/PROD.

## A119/A0.93 — reparación causal del test de gate RBAC (23 de noviembre de 2026)

La ejecución GitHub-hosted [45122319944/job 137518678853](https://github.com/solqaryn/Solqaryn/actions/runs/45122319944/job/137518678853) del SHA `fd1b56e3068eacb1eebc8a534668dff7f3d5c2e5` concluyó **160 PASS y 1 FAIL**. El único fallo A0.93 exigía el textbox de login tras una denegación global HTTP 403, pero el JWT auténtico de empresa B seguía válido. En esa rama `LoginComponent` presenta el **gate de selección de empresa** y un botón de consola con alerta, no el formulario de credenciales. Es un defecto causal de expectativa del test, no motivo para degradar JWT/RBAC.

La corrección test-only exige alerta de 403, botón accesible y alcanzable con `Tab` auténtico sin `.focus()`, ausencia de la pantalla privada y de un formulario de login redundante, cinco viewports 320/390/768/1280/1440, capturas 320/1440, cero overflow y cero mutaciones. Mantiene backend ASP.NET Core, JWT firmado y MySQL 8.4 efímero realmente utilizados en GitHub-hosted. **161/161 NO acreditado hasta nueva CI exact-SHA**; siguen pendientes WCAG global, Core Web Vitals, zoom nativo 200 %, baseline visual histórico y A5 independiente. Preservar H2297=EN_PROGRESO, I=DEV02 y lock V17=2000002297; rollback `git revert` sin force-push, no QA/main/PROD.
