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
