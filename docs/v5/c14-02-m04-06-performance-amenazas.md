# SOLQARYN V20 — C14-02.M04.06 | Densidad, progresividad y ligereza de la guía privada

**Riesgo R2 (superficie privada de amenazas).** Autor DEV03, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. Rama dev y PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO fila 2077: H=PENDIENTE/I vacía al claim; COLA G/O=SI y DAG_TECNICO F=VALIDADO/G=SI. Predecesoras C14-02.M03.01–06+A5 y M04.01–05+A5 verificadas para entrar. La entrega no modifica runtime, permisos, DTO/API, datos, migraciones, JWT, tenant ni rutas.

## A0 — criterios falsables

- Chromium debe comprobar cuatro categorías visibles en una sola columna a 320 px, dos columnas a 768 y 1280 px y ausencia de scroll horizontal (tolerancia de 2 px).
- Un control adversarial inyecta CSS defectuoso de 600 px a 320 px y exige que el detector señale el desborde.
- Ante revalidación sintética demorada 700 ms, el contenido privado desaparece mientras la solicitud está pendiente y reaparece únicamente al éxito.
- Ante error 503 posterior, la guía se oculta y el mensaje de diagnóstico permanece invisible; ninguna llamada mutante POST/PUT/PATCH/DELETE.
- Una medición DOMContentLoaded real del navegador se registra por cada ancho de 320 y 1280 px, comparada con umbral de humo 15000 ms. NO certifica SLA, Web Vitals globales ni métricas de producción.
- Sin ejecutar pentest, restauración, backup, cambio de política o proveedor externo.

## A1–A3 — pruebas proporcionadas

A1: cinco pruebas Playwright/Chromium con caso positivo, caso adversarial, frontera, progresividad y error. A2: UI sintética fail-closed y ausencia de mutaciones; **NO_MEDIDO** para JWT firmado, RBAC del servidor, HTTP real y aislamiento tenant A/B. A3: integración navegador GitHub-hosted; MySQL/EF, Docker de producción y proveedores no corresponden al diff de pruebas y documentación, por tanto NO_APLICA motivado.

## A4 / A5 — evidencia y rollback

A4 exige workflow dirigido C14-02.M04.06 con 5/5 Chromium SUCCESS y comprobaciones pertinentes del SHA publicado: Scope Lock, V5, frontend/build y seguridad que corresponda. Los checks queued, in_progress, cancelled, failed o skipped no equivalen a éxito cuando sean requeridos. Registrar SOURCE_SHA, RUN/JOB, BITACORA y PLAN/COLA readback antes de cualquier LISTO_DEV. Revisión A5 genuina por otra Task Javier distinta de DEV03; nunca autoaprobación. Si hay fallo, registrar QA_DEFECT_FOUND y corregir de manera mínima tras hipótesis y evidencia.

Rollback: git revert normal del commit sobre dev, sin force-push. Prohibido QA/main/PROD, operaciones productivas y secretos.
