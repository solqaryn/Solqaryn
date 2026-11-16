# SOLQARYN V17 — C03-04.M04.03 configuración visual segura

**Autor:** DEV02 JAVIER `6af16bb9492c8191b8e8c7d606f11507`. **Alcance:** solqaryn/Solqaryn@dev, PLAN_MAESTRO fila 562, PROJECT_SCOPE_LOCK=STRICT, candado V17 `2000000562`. **Riesgo:** R2, pantalla privada de seguridad.

## A0
Angular 22.2.1 standalone/Signals, sólo marca SOLQARYN o SOLQARYN Plataforma, densidad cómoda/compacta, contraste normal/alto. Configuración normalizada por igualdades exactas; ningún HTML, CSS, enlace, URL o privilegio arbitrario. Botón accesible de densidad disponible sólo cuando `PlataformaSesionService.sesion()` mantiene admin de Plataforma. Conserva ruta con `plataformaGuard`. No cambia API, servicios .NET, JWT/RBAC backend, Domain, Application, Infrastructure, MySQL/EF, secretos ni operaciones de restauración. Rollback normal con git revert.

## A1-A3
Pruebas Vitest por allowlist, límites, HTML/CSS adversarial, estado/cambio de densidad, anónimo y revocación. GitHub-hosted CI del mismo flujo ejecuta npm ci, lint, build Angular, Vitest y Playwright/Chromium con 320px y sesión/403/503 simulados; checks requeridos exact SHA. No se ejecuta restauración, backend HTTP autenticado, JWT/tenant A/B real, MySQL 8.4, RTO/RPO ni proveedor: NO_APLICA a este diff de presentación; no atribuir certificado runtime.

## A4-A5
Exigir `verify-solqaryn-project-scope`, procedencia V5, workflow dirigido completed/success y readback PLAN/COLA/DAG/BITACORA antes de LISTO. A5 independiente por otra Task Javier; no autoaprobación ni QA/main/PROD. Mantener VALIDANDO ante CI realmente externa queued/in_progress.
