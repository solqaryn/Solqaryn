# C13-04.M04.02 — Estados verificables de revisión visual

Ámbito único: `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT. Task autora DEV02 `6af16bb9492c8191b8e8c7d606f11507`. Riesgo R2 conservador (autorización global UI); sin QA/main/PROD ni integraciones comerciales.

## Alcance real A0

Se amplía la superficie privada Angular ya existente `/plataforma/experiencia-visual-v5` con estados de carga, vacío, éxito **solo de revalidación de sesión**, error seguro y sin permiso. La ruta conserva `plataformaGuard`, que consulta `GET /plataforma/sesion` y depende de validación backend. El usuario empresarial y el anónimo no reciben privilegios por esta vista.

El inventario integral de rutas/módulos, sus fases PLANIFICADO / EN_DESARROLLO / LISTO_TECNICO / NO_ACCESIBLE y la verificación global siguen **SIN_VERIFICAR** mientras no exista una fuente versionada conectada al producto y acreditada con A0–A5. Éxito de sesión NO equivale a módulo implementado, disponibilidad QA ni firma del propietario. La vista no consulta Sheets ni GitHub desde el navegador.

## A1–A4 y límites

- A1: TestBed Angular dirigido verifica vacío, único GET, carga, éxito de sesión sin inventario certificado, 403, 503 opaco, observable vacío y revocación; Playwright Chromium de anónimo, respuesta exitosa sintética, viewport 320 y teclado Tab real.
- A2: `plataformaGuard` preexistente y `PlataformaSesionService.obtener()` con sesión invalidada antes de revalidar; error o pérdida de sesión oculta presets. No hay grants nuevos ni datos de tenant. Las respuestas browser son **sintéticas**: no certifican JWT/RBAC/tenant A/B HTTP reales.
- A3: Node24/Angular22 compilación, pruebas TestBed y Playwright Chromium reales en GitHub-hosted; EF/MySQL, proveedores y HTTP backend real NO_APLICA a este diff visual.
- A4: exigir checks dirigidos y obligatorios completed/success del SHA exacto, `verify-solqaryn-project-scope`, V5 y readback PLAN/COLA/BITACORA. SKIPPED/QUEUED/IN_PROGRESS/FAIL nunca son PASS requerido.
- A5: revisión independiente por otra Task Javier, con capacidad real de rechazar; no autoprobación.
- Rollback: git revert normal del único commit, preservar bitácora. No existe nueva API, migración, scheduler, secreto, producción, ni estado técnico inventado.

Requisitos: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, sección `SOLQARYN_OWNER_GLOBAL_QA_V1`; PLAN_MAESTRO ID `C13-04.M04.02`; artefactos `docs/v5/c13-04-m02-06-dependencias-contrato-salida-experiencia-visual.md` y `frontend/src/app/app.routes.ts`.
