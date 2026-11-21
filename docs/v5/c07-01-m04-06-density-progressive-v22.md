# C07-01.M04.06 — densidad y carga progresiva de catálogo (V22)

- PLAN D1033 exige verificar densidad, carga progresiva y percepción de ligereza. El resultado de fase E "Completa M04" es acumulativo, no exige certificar toda M04 con esta prueba .06.
- A0 / riesgo R1: entrega test-only; no cambia runtime, JWT, RBAC, datos, API, SQL ni permisos. La superficie es el catálogo ERP de productos existente, accesible por alias privado /catalogo-universal-v22 a /productos; servicios y autorización existentes se conservan.
- A1: tres casos Playwright/Chromium reales: a) 375px/1280px, mobile cards y tabla de escritorio, sin overflow ni precarga de página adicional; b) GET de productos retenido muestra carga accesible y no éxito falso; c) HTTP 200 malformado no se presenta como vacío exitoso y reintento GET recupera el catálogo sin escritura.
- A2: JWT, HTTP y tenant A en los fixtures son sintéticos, no JWT firmados ni MySQL real. Sólo prueban el render Angular y el control de acceso enrutado ya existente; seguridad del backend permanece fuera del diff. Integración MySQL/API real NO_APLICA al diff test-only, NO es PASS.
- A3: navegador Chromium en GitHub-hosted y dos viewports; rendimiento de dispositivo físico y CWV/RUM de campo NO_MEDIDO. No se afirma toda M04 ni aceptación de producción.
- A4: requieren 3 tests ejecutados, 0 failed/skip; Angular typecheck/build; Scope Lock, V5 changelog/procedencia y CI causal completed SUCCESS exact SHA. CANCELLED/QUEUED/IN_PROGRESS/SKIPPED no son PASS. Mantener VALIDANDO si CI pendiente.
- A5: otro Task Javier distinto del autor DEV05 con revisión genuina. Rollback git revert normal, sin alterar estados ni historia.
- El archivo CHANGELOG_AI.md grande no puede ser leído íntegramente mediante el conector GitHub disponible: preservar su blob íntegro sin modificar, registrar limitación y exigir check V5 existente. Esta nota aditiva no sustituye el changelog canónico.
