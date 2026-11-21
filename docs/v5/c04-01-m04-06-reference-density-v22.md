# C04-01.M04.06 — densidad y carga percibida de referencia de planes (V22)

**PLAN D601:** Verificar densidad, carga progresiva y percepción de ligereza. Riesgo R1 (solo suite UI en DEV), Scope Lock STRICT. No cambios a componente, rutas, autorización, backend, pagos, SQL, migraciones, secretos ni configuración comercial.

**A0:** El catálogo de planes de C04-01.M04.01 es una superficie privada informativa ya existente, con tres secciones, rutas lazy y `plataformaGuard` (consulta a sesión global). PLAN E exige una meta acumulativa de M04; esta microtarea .06 no certifica server-side prices, subscriptions, JWT ni toda la fase.

**A1/A2/A3 dirigidas:** suite Playwright en Angular real/Chromium: densidad y desbordamiento a 320, 375, 768, 1280 píxeles; render de las tres tarjetas sin formularios ni tráfico de facturación; navegación con Tab real y foco visible de al menos 44 × 44 px aun con `prefers-reduced-motion`; rechazo de anónimo, 403 y 503 con mensajes de backend no expuestos. Las respuestas HTTP de la sesión son sintéticas y no demuestran JWT firmado, MySQL real, RBAC server-side ni integración con PSP. La referencia no tiene listas remotas paginadas: la prueba no reclama haber implementado streaming, skeleton o paginación. No se inventan métricas CWV; tiempo real percibido en producción: NO_MEDIDO.

**A4:** GitHub-hosted ejecuta tipado Angular, build y cuatro Playwright Chromium, más Scope Lock exact-SHA. Exigir `completed/success` de checks pertinentes (incluyendo V5/procedencia aplicable) antes de `VALIDANDO -> LISTO`; `queued/in_progress/skipped/cancelled/failure` no equivalen a PASS. Preservar owner histórico en I y BITACORA idempotente, liberar lock V17 tras readback.

**A5:** otra Task Javier independiente distinta del autor DEV04 revisa requisitos, tests, límites y logs antes de acreditar A5. No QA/main/PROD ni cobros. Rollback en DEV por `git revert` normal; sin force-push. CHANGELOG_AI.md no modificado por esta entrega test-only (la trazabilidad se registra en documento y BITACORA).
