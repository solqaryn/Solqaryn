# C15-04.M05.04 - V23 A0.93 - texto raiz al 200% en Chromium

Fecha 2026-11-21. SOLQARYN dev, PROJECT_SCOPE_LOCK=STRICT. DEV02 TASK_ID 6af16bb9492c8191b8e8c7d606f11507; PLAN_MAESTRO fila 2297 EN_PROGRESO, owner historico preservado. Candado propio V17 2000002297. No es un nuevo CLAIM.

## Impacto A0 y alcance real
Incremento de pruebas, no cambio de UI: Playwright Chromium visita /login y /tienda en 320, 390, 768, 1280 y 1440 px (10 observaciones). Inserta CSS temporal **html { font-size: 200% !important; }** en pagina de prueba, exige prueba numerica del factor >=1.9, h1 unico, destino visible, skip-link inicial en tienda, ausencia de body overflow, cero POST/PUT/PATCH/DELETE y captura PNG fullPage no trivial. CI debe archivar exactamente los PNG pertinentes. Este metodo prueba reflujo de texto raiz aumentado, **NO zoom nativo del navegador**, auditoria WCAG completa ni evidencia antes/despues de cambios visuales.

## Gates A1-A4
A1: aserciones positivas, fronteras de cinco anchos, foco, overflow y negativas sobre escrituras. A2: JWT/RBAC tenant y A3: MySQL/HTTP NO_APLICA al diff test-only porque no cambia runtime ni API; el alcance R2 global conserva pendientes autenticos. A4: exigir Chromium suite completa, lint/build Angular, Scope Lock, V5 provenance y todos los required checks aplicables COMPLETED SUCCESS sobre SHA exacto; no contar in-progress, skipped o fallo como PASS. Capturas de pruebas A091/A092 no se reemplazan.

## Limitaciones y continuacion
PLAN D exige auditoria global de todas las pantallas accesibles y sus permisos reales. Permanecen pendientes 160 rutas, combinaciones de roles/tenant, WCAG end-to-end, field CWV/RUM donde haya medicion, JWT/MySQL reales de rutas privadas y defectos P0/P1 pertinentes. Por ello conservar H=EN_PROGRESO I=DEV02, sin LISTO ni A5, y sin nueva reclamacion. Si CI falla, corregir causa precisa y no eliminar aserciones. A5 futura solo por otra Task Javier. No QA/main/PROD, pagos ni secretos.

Referencias V23 originales leidas y aplicadas: SKREF-03 Impeccable pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199 (foco/reflow); SKREF-05 Taste Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37 (tienda publica); SKREF-10 Angular angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9 (Angular 22 y pruebas). SKREF-04/06/11/12 NO_APLICA: ni motion, ni copy UI, ni modelado de amenaza, ni profiling .NET. Sin instalar/copy skills. Rollback: git revert normal del commit, BITACORA intacta.
