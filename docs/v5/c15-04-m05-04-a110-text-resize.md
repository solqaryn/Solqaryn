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
