# SOLQARYN V17 — C03-04.M04.01: superficie privada de respuesta a incidentes

**Owner y autor:** PRIMARY DEV 03 JAVIER, Task ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Scope:** PLAN_MAESTRO fila 560, COLA G/O=SI y DAG F=VALIDADO/G=SI antes de claim. **Candado:** SQV17_C03_04_M04_01 / 2000000560. **Riesgo:** R2 por UX de seguridad/tenancy. **Repo:** solqaryn/Solqaryn@dev.

## A0
Ruta lazy protegida por `plataformaGuard`, componente aislado y solo lectura para las cuatro decisiones: contención y tenant, integridad, ensayo de restauración y rollback/revisión. Solo se muestra al existir `PlataformaSesionService.sesion()` administradora válida, obtenida del backend. Revocación oculta inmediatamente. No se crean operaciones, peticiones de restore, carga de archivos ni permisos. No sustituye el runbook real, CI, auditoría de infraestructura ni aprobación humana para producción.

## A1–A3
Cinco pruebas Vitest positivas/negativas/frontera y tres Playwright Chromium (anónimo, denegación HTTP sintética, acceso sintético responsive y cero endpoints operativos). El workflow GitHub-hosted verifica además lint, tipado y build Angular; no declarar PASS antes de resultados `completed/success`. Test con fake token/API simulada **no certifica JWT real, tenant A/B real, MySQL 8.4, restauración de backups, proveedor, RTO/RPO ni integridad de datos**. No se modifica backend, EF ni proveedor, por lo que integración MySQL/HTTP productiva es NO_APLICA al diff.

## A4–A5
Validar exact SHA dev, Scope Lock, procedencia CHANGELOG V5, CI dirigida, difusión de fuente, PLAN/COLA/BITACORA readback. Mantener VALIDANDO mientras GitHub Actions esté queued/in_progress, identificando run y SHA. Solo pasar LISTO con A0–A4; A5 de otra Task Javier distinta. Rollback por commit git revert normal; sin force-push, QA/main/PROD, secretos ni datos reales.
