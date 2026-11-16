# C11-04.M05.06 — regresion, densidad y accesibilidad proporcional

Gobierno: SOLQARYN dev, PROJECT_SCOPE_LOCK=STRICT. Autor DEV03, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. Fila PLAN 1723, lock V17 2000001723. Riesgo R2 por observabilidad/seguridad/CI.

## A0: alcance y rollback
La entrega agrega 4 pruebas nuevas Playwright y un workflow CI con dos jobs. No altera funcionalidades, API, proveedores externos, EF/MySQL, JWT, permisos, sesiones ni producción. Reutiliza la integración REAL in-process de ILoggerProvider/LoggerFactory de M05.03 con xUnit, y la ruta UI Angular ya protegida. Rollback es git revert normal sobre dev, sin force-push, conservando BITACORA.

## A1 a A3: casos que fallan ante defectos
- Positivo: real Chromium a 320/375/768/1280; 4 puntos, 2 controles, sin overflow. Ningún request a exportadores. Foco visible mediante Tab real (sin focus programático), outline de 2 px o mayor.
- Carrera: segundo GET retenido, cero tarjetas viejas, botón deshabilitado, aria-live y recuperación al liberar exactamente dos GET.
- Negativos: 503 posterior a 200 elimina tarjetas y diagnóstico privado, 403 no muestra branding ni puntos. Servidor de sesión es fixture sintético, no JWT real.
- Integración .NET: xUnit Release en runner GitHub-hosted invoca LoggerFactory y proveedor en proceso ya implementado. No se afirma persistencia MySQL, exporter externo, telemetría productiva, seguridad HTTP o tenant A/B real.
- Rendimiento: solo presupuesto estructural de DOM y ausencia de consultas externas. Core Web Vitals, tiempos de red, SLO y coste permanecen NO_MEDIDO; no inventar un benchmark.

## A4 y A5
Exigir checks GitHub Actions completed/success del SHA real para ambos jobs, Scope Lock, V5, frontend/backend/seguridad que correspondan. SKIPPED/CANCELLED/IN_PROGRESS no son PASS. Si CI externa sigue corriendo, mantener VALIDANDO con owner, RUN y SHA, y revisar primero al regresar. A5 solo otra Task Javier distinta del autor. Sin QA/main/PROD ni cambios en infraestructura.
