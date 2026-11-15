# SOLQARYN V5 — C01-01.M04.05: responsive, teclado, foco, etiquetas y contraste

**Origen:** STANDARD V5 FINAL, PLAN_MAESTRO fila 168. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Entorno:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT.

## Entrega y límite real
Nueva regresión browser Chromium en Playwright, con Angular local efímero y SIN credenciales, servicios productivos ni peticiones autenticadas. Reutiliza login y authGuard existentes, no crea privilegios, endpoints, rutas ni CSS ficticio. Valida en 360x740 la visibilidad de entradas y botón sin recorte horizontal, en 390x844 el orden de tabulación y el botón para mostrar contraseña, en 1366x768 contraste calculado del encabezado sobre su tarjeta >=4.5:1, y el redireccionamiento anónimo de /usuarios hacia /login sin mostrar superficie privada.

## Validación A0–A5
A0: diff aditivo de Playwright E2E, workflow de CI, documentación y CHANGELOG; sin cambios Angular de runtime, JWT, RBAC, tenant, API, MySQL ni infraestructura. Se requieren validaciones del SHA exacto.
A1: cuatro pruebas positivas/negativas en navegador real, sin inventar aprobación mientras el job siga queued/in_progress/failed/cancelled.
A2: prueba la redirección anónima a nivel UI; no certifica defensa HTTP backend ni aislamiento MySQL tenant A/B, ni reemplaza controles servidor.
A3: GitHub-hosted Chromium y build Angular/lint con Node y npm fijos, servidor dev local sin backend PROD.
A4: Scope Lock STRICT y procedencia V5, diff, check-runs exact-SHA y readback PLAN/COLA/BITACORA antes de LISTO_DEV.
A5: pendiente de Task Javier distinta del autor; NO QA/main/PROD.
**Rollback:** revert Git normal sin force-push.
