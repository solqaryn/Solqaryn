# C09-04.M04.05 — accesibilidad y responsive de mensajería privada (DEV04 V19)

Proyecto solqaryn/Solqaryn; rama dev; PROJECT_SCOPE_LOCK=STRICT. TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. PLAN fila 1428; candado V17 2000001428. Riesgo R2 por permisos y evidencia browser, no producción.

## Alcance A0

Verificar de manera dirigida la superficie privada Angular ya implementada por C09-04.M04.01, sin modificarla ni atribuirse su autoría. Nuevo archivo Playwright y workflow aislados por SHA. No crear interfaz artificial, endpoint, credencial, proveedor SMTP o WhatsApp ni enviar mensajes. La sesión del navegador es sintética; la protección backend real no se certifica.

## Evidencia causal requerida

A1: 4 pruebas Playwright con rechazos anónimo/403, 4 anchos, árbol semántico, contraste numérico de h1, Tab real desde inicio, foco y Enter. A2: panel oculto ante denegación y cero requests mutantes o fuga de mensaje privado. A3: Chromium real GitHub-hosted; MySQL, JWT firmado, SMTP y WhatsApp NO_APLICA porque este diff solo agrega pruebas sin tocar servicios. A4: Scope Lock SUCCESS SHA exacto, frontend build, CI dirigida, readback PLAN/COLA/BITACORA y blobs. A5: exclusivamente otra Task Javier.

No interpretar color o foco puntual como cumplimiento WCAG total de todo el SaaS. A0-A4 pendientes hasta completion real SUCCESS de workflows pertinentes. Rollback git revert normal. No QA, main ni PROD.
