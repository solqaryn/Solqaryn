# SOLQARYN — Estado estándar de automatización

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=RETIRED
SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0
VAEP_DISPATCH_AUTHORIZED=FALSE
JULES_WORKERS_AUTHORIZED=0

## Fuentes nativas vigentes

Documento rector: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). Seguimiento manual: [STANDARD DE AUTOMATIZACIONES EN SOLQARYN](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). [STANDARD AUTORIDAD OPERATIVA VAEP](https://docs.google.com/document/d/1A525NGt09mBiZez_r6hFcdCYAdoOAwOZM--N0uXPBOQ/edit) es archivo histórico no ejecutable. No existen Tasks ni jobs comerciales autorizados.

## Estado vigente

El propietario confirmó el 22-10-2026 que eliminó las diez Tasks de ChatGPT y retiró la antigua hoja de control de Google Drive. Este repositorio no autoriza despachos, leases, reintentos ni trabajos comerciales automáticos. La eliminación de los cron VAEP y el escritor de admisión automática se integró en DEV mediante el PR #3713.

Los contratos de los proyectos anteriores son **históricos, no ejecutables**. Cualquier implementación futura debe partir de cero, con identidades nuevas y aprobación explícita, sin copiar runtime, secretos, propietarios o pruebas de otro proyecto.

## Protección permanente distinta del motor

Las pruebas de compilación, aislamiento multiempresa, seguridad, respaldo, frontend y regresiones causales siguen siendo controles técnicos. No deben depender de completar un roadmap anterior ni de la terminación de Tasks inexistentes. Las integraciones del ERP (incluida la funcionalidad comercial propia de automatizaciones M12) no se confunden con el motor retirado.

## Contrato nuevo en preparación (microfase 1)

`docs/AUTOMATION_AUTHORITY.md` define exclusivamente la política del piloto de doble ejecutor humano paritario (`EJECUTOR_PRINCIPAL_01` Javier / `EJECUTOR_PRINCIPAL_02` Alex; `PROPIETARIA_INSTITUCIONAL` SOLQARYN Platform). Ninguna Task se crea por esta referencia. Los valores de `AUTOMATION_RUNTIME=RETIRED`, `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, `VAEP_DISPATCH_AUTHORIZED=FALSE` y `JULES_WORKERS_AUTHORIZED=0` siguen siendo la única verdad operativa de esta fase. Las 4 microfases requieren cierres separados; el contrato no implica autorización de despliegue o escritura programada.

## Volver a automatizar en el futuro

Requerirá un contrato nuevo, aprobación del propietario, diseño explícito de concurrencia y derechos de escritura, aislamiento de credenciales, pruebas de cierre causal y una configuración nueva comprobada. Ningún archivo de este estado produce activaciones.

## CI vigente y archivo histórico (22-10-2026)

La rama `dev` utiliza `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend` como checks obligatorios, independientes de las antiguas fases. No se requieren checks Fase 7 ni el workflow `vaep-engine-ci.yml`.

Los recibos que se conservan en `vaep/evidence/` son **archivos históricos de auditoría, nunca estado vivo**. No se pueden usar para seleccionar, reclamar, despachar o cerrar tareas. El backup cifrado programado de DEV protege infraestructura y no es una Task comercial; se preserva.

El código de la tienda, inventario, seguridad, correo, migraciones y las regresiones que protegen funciones reales del ERP sigue sujeto a las pruebas actuales del código, independientemente del nombre de su plan de origen.

## Depuracion de scripts y CI historicos (22-10-2026)

El workflow manual `matrix-governance.yml` y sus diez validadores Python de matrices N8.17/N8.18 pertenecen al gobierno VAEP historico y se retiran del arbol de ejecucion. La evidencia original se conserva en Git y en los documentos historicos, sin reactivar su autoridad. No se eliminan pruebas reales del ERP ni checks requeridos del ruleset.

Los servidores y validadores de prueba de correo, seguridad de runtime, auditoria de codigo y comparaciones PDF conservan exactamente sus funciones con nombres neutros: `smtp_capture_server.py`, `validate_email_delivery.py`, `validate_runtime_safety.py`, `audit_source_quality.py`, `compare_pdf_manifests.py` y `compare_pdf_renders.py`. Sus consumidores de CI se actualizan conjuntamente. No se modifican los contratos de ejecucion de Tasks ni los entornos QA/PROD.
