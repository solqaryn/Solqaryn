# SOLQARYN V17 — C15-01.M04.04 navegación, mensajes y prevención de doble envío

**Autor:** PRIMARY DEV 03 Javier, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`; repositorio `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`; PLAN fila 2183, V17 lock 2000002183.

## A0 / R2 conservador
Contrato M04.04: verificar formularios, navegación, mensajes y prevención de doble acción de la ruta privada `/plataforma/trazabilidad-cobertura-v5`. La superficie es de **solo lectura**, por diseño no hay formularios ni mutaciones de backend: el requisito de bloquear segundo envío se satisface comprobando la ausencia completa de controles de escritura, **no** fingiendo que existe un formulario. Cuatro pruebas Chromium adicionales, sin cambiar código de ejecución, rutas, guards, modelo, datos, JWT/RBAC, tenants, auditoría, EF/MySQL, secrets ni producción. Revertir commit normalmente si hubiera regresión.

## A1, A2, A3
Cuatro pruebas E2E reales: (1) acceso anónimo y 403 sintético con mensaje opaco/no filtración; (2) leyendas `NO_MEDIDO`, `SIN_VERIFICAR` y nota sin aprobación, sin botones o formularios; (3) teclado Enter sobre enlace de retorno y ausencia de solicitudes mutadoras durante navegación; (4) ausencia de cualquier control de doble envío y existencia de un único enlace de navegación. Se conservan las pruebas previas de M04.01 y M04.05, con CI Chrome real en GitHub-hosted, lint/build y Vitest. Las aserciones fallan si se reintroducen mutaciones, aparece filtración, desaparece navegación o se falsifica certificación.

**Seguridad:** sesiones HTTP de navegador simuladas, sin JWT firmado real, MFA, tenant A/B backend ni grants productivos. Integración aplicable únicamente Angular + Chromium; MySQL/EF/providers NO_APLICA por no tocarlos. P0/P1 cero solo si CI/inspección lo demuestran.

## A4/A5 y sucesoras
Requerir checks dirigidos `completed/success` exact-SHA (o equivalencia de blobs), Scope Lock, V5, aceptación integral cuando aplique y readback de PLAN/COLA/DAG/BITACORA. `SKIPPED`/`QUEUED`/`IN_PROGRESS` no son PASS. A5 por otra Task Javier; este documento no acredita revisión ni autoriza QA/main/PROD.
