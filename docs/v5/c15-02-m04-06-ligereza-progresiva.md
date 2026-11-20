# C15-02.M04.06 - densidad, carga progresiva y ligereza

Solqaryn dev; PROJECT_SCOPE_LOCK=STRICT. Ejecuta DEV01. La acción atómica proviene de PLAN_MAESTRO D2221; E2221 resume una meta M04 acumulativa y no permite certificar migración Hostinger, Cloudflare, R2, DNS, MySQL o proveedores.

## Alcance y evidencia
A0: R1 pruebas solamente de una vista privada de arquitectura existente, sin cambiar runtime, seguridad, API, tablas, secretos, grants, DNS o infraestructura. El componente utiliza cinco referencias locales NO_VERIFICADO y plataformaGuard.
A1: Vitest con fuente diferida real en Angular TestBed: cargando sin tarjetas, estado listo limitado, revocación antes/después de resolver, datos tardíos descartados. Afirmaciones positivas y negativas.
A2: Playwright usa respuesta sintética para el guard; comprueba 403, cero contenido privado y cero HTTP mutante. NO_APLICA como prueba JWT firmado, RBAC o tenant A/B real porque este cambio es test-only y no añade runtime.
A3: GitHub-hosted Chromium real + Angular loopback en 320/375/768/1280 px; cinco tarjetas y volumen acotado, scroll horizontal ausente, métricas DOMContentLoaded y FCP local registradas. Presupuesto DCL laboratorio 30 s. No certifica dispositivos físicos ni Core Web Vitals de campo (LCP, CLS e INP NO_MEDIDO), ni experiencia universal.
A4: exigir workflow dirigido completed/success, scope lock exact SHA, V5, frontend y otros checks pertinentes; QUEUED/IN_PROGRESS/SKIPPED/FAIL/CANCELLED no cuentan como PASS. Handoff con SHA, logs, PLAN/COLA/DAG y BITACORA reales.
A5: Task Javier independiente distinta de DEV01; esta entrega no es autorización QA/main/PROD.
Rollback: git revert normal del commit publicado sobre dev, nunca force-push. Si la prueba falla, conservar VALIDANDO o EN_PROGRESO real, reparar y repetir CI causal, sin declarar éxito.
