# SOLQARYN V19 — C15-04.M04.06: densidad y carga progresiva verificables

**Autor:** PRIMARY DEV05 Javier, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, PLAN fila 2293. Riesgo **R2 conservador** por test automatizado de pantalla privada; no se altera ningún backend, JWT/RBAC/tenant, DB, ruta ni producción.

## A0 — Contrato y límites

Nueva suite de tres escenarios Playwright/Chromium ejecutada sobre Angular real, en localhost y con respuesta **sintética** de `/plataforma/sesion`. Reutiliza la pantalla protegida de auditoría UX `/plataforma/auditoria-ux-v5` ya publicada por C15-04.M04.01 y el `plataformaGuard` existente, sin modificarlo. Se verifican densidad de cinco tarjetas observacionales y el render diferido hasta autorización, frente a datos que todavía son `NO_MEDIDO`. Las métricas sólo son **umbrales de regresión de esta UI de referencia**, no compromisos de rendimiento comercial ni medidas de Web Vitals. Los demás módulos públicos, ERP, builder y portal **NO** quedan certificados por esta suite.

## A1 — Oráculos positivos y negativos

1. Usuario sin sesión y 503 de sesión: cero superficies privadas, cero escritura, mensaje opaco sin DSN.
2. 320, 375, 768 y 1280 px: cinco tarjetas `NO_MEDIDO`, densidad de texto de referencia entre 100 y 2500 caracteres, sin overflow horizontal superior a 2 px y sin formularios o controles de mutación en la vista.
3. Carga progresiva con solicitud de sesión retenida: ninguna tarjeta mientras no se resuelva el GET; tras respuesta autorizada, cinco tarjetas y una única solicitud de validación.

Las pruebas adversariales detectan si una ruta deja pasar antes del guard, si hay desbordamiento, si aparecen controles de edición o si la carga revela contenidos antes de validación. Un fallo de CI **no** puede transformarse en PASS.

## A2, A3 y A4

Seguridad de frontend: navegación privada con guard vigente y 503 opaco en Chromium; token y autorización HTTP simulados, por tanto **JWT real, tenant A/B live y RBAC server-side NO_CERTIFICADOS**. Integración pertinente: GitHub-hosted Node 24/npm 11, `npm ci`, lint, build Angular, Playwright/Chromium con servidor loopback. **MySQL, EF, proveedores, contraste WCAG integral y RUM/CLS/LCP/INP reales NO_APLICAN a este diff de test**, sin afirmar resultados. A4 exige `verify-solqaryn-project-scope`, procedencia V5, pruebas Chromium y checks aplicables `completed/success` del SHA causal, además de readback PLAN/COLA/DAG/BITACORA; `queued/in_progress/skipped/cancelled` no son PASS.

## A5 y reversión

Sólo otra Task de las seis Javier, distinta de DEV05, puede emitir A5 tras inspección independiente de test, workflow, criterios y logs reales. No se introducen cambios de producto, datos, secretos, servicios, schedulers, producción ni permisos. Rollback técnico: `git revert` normal del único commit sobre `dev` después de revalidar HEAD y conflictos, sin force-push ni alterar BITACORA histórica. Estado de métricas fuera de los escenarios: **NO_MEDIDO**.
