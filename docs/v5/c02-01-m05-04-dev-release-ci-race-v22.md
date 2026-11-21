# C02-01.M05.04 — V22: reparación del race en CI de release DEV (2026-11-20)

Repositorio único: solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. Actor rescatista DEV05 (6adc2e55f47c81919f6374e561687076). Owner histórico DEV03 conservado en PLAN_MAESTRO!I317. Riesgo R2 (CI/gate), sin operaciones de pago, secretos, cambio productivo ni despliegue manual.

## Hallazgo causal

El job release 136656870945 de GitHub Actions (workflow 44843325936) falló el 2026-11-21 00:22 UTC con CI_NOT_SUCCESS:SOLQARYN - Seguridad multiempresa. El check de seguridad del mismo HEAD 55d94fc6a1754e707ba60d8e19eac5cc15f7eecc terminó después en COMPLETED/SUCCESS, junto con backend y Scope Lock. Un cron independiente consulta los checks antes de su finalización; el rechazo de seguridad era correcto, pero el race evita la recuperación en el mismo run. NO reinterpretar el fallo anterior como PASS ni afirmar release Vercel.

## Cambio y límite

Solo el script scripts/dev-vercel-release.mjs cambia. Mientras siga el mismo HEAD exacto, espera de manera acotada hasta 36 lecturas, separadas por 5 segundos, de los tres checks requeridos. Ante FAIL/SKIPPED/CANCELLED terminal, SHA cambiado o timeout, falla cerrado antes de consultar/provisionar Vercel. No inicia trabajos, no reutiliza un PASS de otro SHA y no altera los requisitos originales. El workflow y proyecto de destino siguen siendo exclusivamente DEV. El próximo run real de release es la prueba operacional pendiente; no se publica una conclusión exitosa anticipada.

## Evidencia de calidad A0–A5

- A0: diff localizado en verificación GitHub CI, sin cambios al runtime .NET/Angular, API, EF, migración, esquema, JWT/RBAC ni secretos; rollback por git revert normal en dev.
- A1: seis asserts reproducidos localmente con Node 22: todos success, check faltante, in_progress, skipped, failure y SHA inválido; los seis PASS. La ruta de consulta GitHub y espera está pendiente de CI GitHub-hosted.
- A2: conserva fail-closed en controles multiempresa, Scope Lock y backend; HTTP JWT/RBAC aplicación NO_APLICA al script de gate (no sustituye pruebas del alcance original).
- A3: MySQL/Angular/Chromium NO_APLICA al diff de script; la integración viva GitHub Actions del nuevo SHA debe comprobarse, el release DEV no certifica runtime comercial.
- A4: obtener check verify-solqaryn-project-scope, V5 provenance y pruebas pertinentes del NUEVO SHA exacto; para certificar este gate se requiere además la siguiente ejecución real de la política release con sus tres checks COMPLETED/SUCCESS y log. Hasta entonces WAIT_CI, sin LISTO.
- A5: pendiente de Task Javier distinta de DEV03 autor y DEV05 reparador, tras revisión genuina; nunca autoprobación.

C02-01.M05.04 sigue VALIDANDO. SQL de última preferencia, JWT reissue y navegación Angular autorizada siguen pendientes de pruebas causales, no se atribuyen a esta reparación. No activar QA/main/PROD ni pagos reales.
