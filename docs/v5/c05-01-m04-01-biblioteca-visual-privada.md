# C05-01.M04.01 — superficie de biblioteca visual privada V5

Autor: PRIMARY DEV 05 Javier (`6adc2e55f47c81919f6374e561687076`). Rama `dev`, repositorio `solqaryn/Solqaryn`. Riesgo R1: Angular UI de consulta con guarda y sesión ya existentes. Contrato: PLAN_MAESTRO fila 740; DAG_TECNICO fila 701; candado exclusivo V17 `SQV17_C05_01_M04_01` ID 2000000740.

## A0 — Alcance
Agrega una superficie desplegable a `/plataforma` (ruta ya protegida con `plataformaGuard`) que muestra cuatro tokens de color declarativos y escala tipográfica institucional. Usa variables CSS existentes; ninguna marca empresarial ni token de tenant se convierte en CSS arbitrario. No cambia rutas, APIs, servicios, datos, migraciones, permisos, JWT, Cloudinary ni producción.

## A1/A2 — Seguridad y calidad
El contenido se renderiza solo cuando `PlataformaSesionService.sesion()` conserva ámbito Plataforma y `esAdministrador=true`. Los estados anónimo, ámbito Empresa y revocación inmediata no exhiben tokens. Tests Vitest negativos y positivos, Playwright Chromium en 320/768/1280, teclado real, no desbordamiento y sin controles de operación. La pantalla es exclusivamente informativa: no registra acciones de negocio ni consulta fuentes institucionales.

## A3 — Integración proporcional
GitHub-hosted con Angular 22.2.1: `npm ci`, test Vitest dirigido, lint, build y Chromium Playwright con API sintética. No afirmar JWT/RBAC/MySQL end-to-end reales: no se tocan servicios ni persistencia; pruebas en navegador sí son reales para la presentación.

## A4/A5 — Reglas y rollback
Requiere SHA exacto y checks dirigidos, Scope Lock, procedencia V5, frontend, seguridad y aceptación integral, todos completados con éxito cuando correspondan. Antes de CI real no declarar LISTO. Autor conserva `VALIDANDO` y SHA/RUN para recuperación por la siguiente corrida si GitHub sigue en curso. A5 es revisión independiente por otra Task Javier. Rollback: revert normal de commit en `dev` (nunca force-push); QA/main/PROD sin tocar.
