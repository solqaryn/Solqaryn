# SOLQARYN — C14-02.M04.05 accesibilidad y respuesta R2

Owner DEV02 6af16bb9492c8191b8e8c7d606f11507; PLAN fila 2076, COLA G/O SI y DAG F VALIDADO G SI. Sin cambios de API, JWT, RBAC, tenant, datos o migraciones.

A0: cuatro pruebas reales Playwright Chromium, workflow dirigido, changelog y este documento; requisitos responsive 320/1280, controles >=44px, foco >=3px, teclado, contraste neutro >=4.5, denegación 403.

A1: caso positivo autorizado con cuatro superficies, frontera 320px, prueba negativa ante 403 y error simulado sin filtración, aserciones de teclado y layout capaces de fallar.

A2: sin grants nuevos; sesión simulada, no certificación real de JWT, pertenencia tenant A/B ni pentest. A3: Chromium en GitHub-hosted, API sintética; MySQL/EF/proveedores no aplica.

A4: únicamente con SHA publicado, checks requeridos completed/success (incluidos Scope Lock y V5), diff y readback PLAN/COLA/DAG/BITACORA. Nunca elevar queued/in_progress/cancelled/skipped a PASS.

A5: revisión de otra Task Javier distinta al autor; no autoaprobar. Contraste 4.5 examina solo texto normal sobre el contenedor neutro, no WCAG integral.

Rollback: git revert no destructivo. dev exclusivamente, sin main/QA/PROD.
