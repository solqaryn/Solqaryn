# SOLQARYN — Estado estándar de automatización

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
EXECUTION_PLAN_DRIVE_FILE_ID=1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=RETIRED
SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0
VAEP_DISPATCH_AUTHORIZED=FALSE
JULES_WORKERS_AUTHORIZED=0

## Estado vigente

El propietario confirmó el 22-10-2026 que eliminó las diez Tasks de ChatGPT y retiró la antigua hoja de control de Google Drive. Este repositorio no autoriza despachos, leases, reintentos ni trabajos comerciales automáticos. La eliminación de los cron VAEP y el escritor de admisión automática se integró en DEV mediante el PR #3713.

Los contratos de los proyectos anteriores son **históricos, no ejecutables**. Cualquier implementación futura debe partir de cero, con identidades nuevas y aprobación explícita, sin copiar runtime, secretos, propietarios o pruebas de otro proyecto.

## Protección permanente distinta del motor

Las pruebas de compilación, aislamiento multiempresa, seguridad, respaldo, frontend y regresiones causales siguen siendo controles técnicos. No deben depender de completar un roadmap anterior ni de la terminación de Tasks inexistentes. Las integraciones del ERP (incluida la funcionalidad comercial propia de automatizaciones M12) no se confunden con el motor retirado.

## Volver a automatizar en el futuro

Requerirá un contrato nuevo, aprobación del propietario, diseño explícito de concurrencia y derechos de escritura, aislamiento de credenciales, pruebas de cierre causal y una configuración nueva comprobada. Ningún archivo de este estado produce activaciones.
