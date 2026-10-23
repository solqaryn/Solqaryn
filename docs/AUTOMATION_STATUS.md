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
PILOT_TASK_LIMIT=2
PILOT_PRIMARY_TASKS=1
PILOT_SECONDARY_TASKS=1
PRIMARY_TASK_ROLE=PRIMARY_01
SECONDARY_TASK_ROLE=SECONDARY_02
PRIMARY_TASK_ACCOUNT=JAVIER_CHATGPT
SECONDARY_TASK_ACCOUNT=ALEX_CHATGPT

## Fuentes nativas vigentes

Documento rector: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). Seguimiento manual: [STANDARD DE AUTOMATIZACIONES EN SOLQARYN](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). [STANDARD AUTORIDAD OPERATIVA VAEP](https://docs.google.com/document/d/1A525NGt09mBiZez_r6hFcdCYAdoOAwOZM--N0uXPBOQ/edit) es archivo histórico no ejecutable. No existen Tasks ni jobs comerciales autorizados.

## Estado vigente

El propietario confirmó el 22-10-2026 que eliminó las diez Tasks de ChatGPT y retiró la antigua hoja de control de Google Drive. Este repositorio no autoriza despachos, leases, reintentos ni trabajos comerciales automáticos. La eliminación de los cron VAEP y el escritor de admisión automática se integró en DEV mediante el PR #3713.

Los contratos de los proyectos anteriores son **históricos, no ejecutables**. Cualquier implementación futura debe partir de cero, con identidades nuevas y aprobación explícita, sin copiar runtime, secretos, propietarios o pruebas de otro proyecto.

## Certificación de retirada VAEP y ramas (23-10-2026)

GitHub [PR #3728](https://github.com/solqaryn/Solqaryn/pull/3728) y [run 40008279563](https://github.com/solqaryn/Solqaryn/actions/runs/40008279563) confirman la eliminación de las 47 ramas adicionales, con lectura posterior de exactamente tres ramas protegidas: `dev`, `qa` y `main` (PROD). Las 13 propuestas heredadas aún abiertas fueron cerradas sin merge. No se fusionó código histórico a DEV. Las tres ramas conservadas mantuvieron los controles de GitHub, y QA/PROD no se actualizaron.

Del árbol actual de DEV se retiraron 1.499 archivos VAEP heredados, incluidas tres acciones manuales históricas con capacidades de escritura; la historia de Git conserva los antecedentes. El mecanismo excepcional de limpieza debe ser eliminado por PR protegido posterior, sin establecer automatización recurrente. Este certificado es solo sobre gobierno del repositorio: **no** altera ni activa `AUTOMATION_RUNTIME=RETIRED`, la topología futura, la hoja comercial, los permisos de proveedores ni los despliegues pendientes por comprobar.

## Protección permanente distinta del motor

Las pruebas de compilación, aislamiento multiempresa, seguridad, respaldo, frontend y regresiones causales siguen siendo controles técnicos. No deben depender de completar un roadmap anterior ni de la terminación de Tasks inexistentes. Las integraciones del ERP (incluida la funcionalidad comercial propia de automatizaciones M12) no se confunden con el motor retirado.

## Contrato nuevo en preparación (microfase 1)

`docs/AUTOMATION_AUTHORITY.md` define exclusivamente la política del piloto de doble ejecutor humano paritario (`EJECUTOR_PRINCIPAL_01` Javier / `EJECUTOR_PRINCIPAL_02` Alex; `PROPIETARIA_INSTITUCIONAL` SOLQARYN Platform). Ninguna Task se crea por esta referencia. Los valores de `AUTOMATION_RUNTIME=RETIRED`, `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, `VAEP_DISPATCH_AUTHORIZED=FALSE` y `JULES_WORKERS_AUTHORIZED=0` siguen siendo la única verdad operativa de esta fase. Las 4 microfases requieren cierres separados; el contrato no implica autorización de despliegue o escritura programada.

## Enmienda v1.2 - 1 Primary Javier + 1 Secondary Alex

El nuevo contrato autoriza solamente el *diseno* de dos Tasks futuras,
una por cuenta: `PRIMARY_01` en ChatGPT Javier y `SECONDARY_02` en ChatGPT
Alex. Ambos siguen como ejecutores humanos con igual autoridad; SOLQARYN
Platform conserva la propiedad institucional. Los marcadores de plan
`PILOT_TASK_LIMIT=2`, `PILOT_PRIMARY_TASKS=1` y `PILOT_SECONDARY_TASKS=1`
NO prueban creacion ni autorizan cron, escritura, claims o revisiones.

En esta enmienda sigue `AUTOMATION_RUNTIME=RETIRED`,
`SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`. Cada cuenta debe certificar
separadamente Task, identidad, conectores, programacion, readback y permisos
en microfase 3. No recuperar un motor retirado ni crear ambas Tasks bajo
Javier como alternativa a la cuenta independiente de Alex.

## Microfase 2 - estructura de Google Sheets (23-10-2026)

MICROFASE_2_SHEETS_READBACK=PASS
MICROFASE_2_SHEETS_ID=1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU
MICROFASE_2_SHEET_TABS=8
MICROFASE_2_PREPARED_TASK_SLOTS=2
MICROFASE_2_LOGICAL_CAPABILITIES=5
MICROFASE_2_PHYSICAL_TASKS_VERIFIED=0

La estructura del Google Sheets existente fue leida, configurada y verificada.
Hay exactamente dos reservas de rol (Primary Javier y Secondary Alex),
ambas `NO_CREADA` y sin `TASK_ID`; no se han certificado Tasks fisicas
en sus cuentas. Las cinco capacidades permanecen `NO_ACTIVA`.

El readback comercial confirma 1.123 microtareas, 1.123 IDs unicos,
cero dependencias invalidas/ciclicas y 15.722 formulas intactas.
Hay 16 validaciones estrictas verificadas en Google Sheets y 38 casos
negativos **simulados aisladamente, no ejecuciones de escritura reales**.
La evidencia exacta, huellas y alcance de rollback figuran en
[`AUTOMATION_MICROFASE_2_CERTIFICATION_20261023.md`](AUTOMATION_MICROFASE_2_CERTIFICATION_20261023.md).

Esta constancia no significa que un PR este aprobado ni que las Tasks
existan o esten habilitadas. Antes de cualquier fase posterior se
requiere cierre por flujo GitHub protegido y autorizacion especifica.
Permanecen `AUTOMATION_RUNTIME=RETIRED`,
`SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, VAEP retirado y Jules cero.

## Volver a automatizar en el futuro

Requerirá un contrato nuevo, aprobación del propietario, diseño explícito de concurrencia y derechos de escritura, aislamiento de credenciales, pruebas de cierre causal y una configuración nueva comprobada. Ningún archivo de este estado produce activaciones.

## CI vigente y archivo histórico (22-10-2026)

La rama `dev` utiliza `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend` como checks obligatorios, independientes de las antiguas fases. No se requieren checks Fase 7 ni el workflow `vaep-engine-ci.yml`.

Los recibos que se conservan en `vaep/evidence/` son **archivos históricos de auditoría, nunca estado vivo**. No se pueden usar para seleccionar, reclamar, despachar o cerrar tareas. El backup cifrado programado de DEV protege infraestructura y no es una Task comercial; se preserva.

El código de la tienda, inventario, seguridad, correo, migraciones y las regresiones que protegen funciones reales del ERP sigue sujeto a las pruebas actuales del código, independientemente del nombre de su plan de origen.

## Depuracion de scripts y CI historicos (22-10-2026)

El workflow manual `matrix-governance.yml` y sus diez validadores Python de matrices N8.17/N8.18 pertenecen al gobierno VAEP historico y se retiran del arbol de ejecucion. La evidencia original se conserva en Git y en los documentos historicos, sin reactivar su autoridad. No se eliminan pruebas reales del ERP ni checks requeridos del ruleset.

Los servidores y validadores de prueba de correo, seguridad de runtime, auditoria de codigo y comparaciones PDF conservan exactamente sus funciones con nombres neutros: `smtp_capture_server.py`, `validate_email_delivery.py`, `validate_runtime_safety.py`, `audit_source_quality.py`, `compare_pdf_manifests.py` y `compare_pdf_renders.py`. Sus consumidores de CI se actualizan conjuntamente. No se modifican los contratos de ejecucion de Tasks ni los entornos QA/PROD.
