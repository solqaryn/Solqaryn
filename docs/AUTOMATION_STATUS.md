# SOLQARYN — Estado estándar de automatización

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1
VAEP_DISPATCH_AUTHORIZED=FALSE
JULES_WORKERS_AUTHORIZED=0
PILOT_TASK_LIMIT=2
PILOT_PRIMARY_TASKS=1
PILOT_SECONDARY_TASKS=1
PRIMARY_TASK_ROLE=PRIMARY_01
SECONDARY_TASK_ROLE=SECONDARY_02
PRIMARY_TASK_ACCOUNT=JAVIER_CHATGPT
SECONDARY_TASK_ACCOUNT=ALEX_CHATGPT

## 2026-10-23 — PILOTO DEV PRIMARY AUTORIZADO POR EL PROPIETARIO (reemplaza gates historicos de microfase 3)

El propietario ordena reanudar las microtareas DEV ahora. PRIMARY_01 existente de Javier es el unico escritor automatizado autorizado, SECONDARY_02 de Alex permanece revisor READ_ONLY. Se limita a solqaryn/Solqaryn, base dev, rama especifica por microtarea del Plan Comercial Inicial y PR contra dev. Prohibido merge autonomo, QA, main, PROD, Render, Vercel, secretos o cambios productivos.

El RUN_ID, RUN_URL, TASK_ID_NATIVO_DEL_RUN, correo ChatGPT, RUN_COMPLETED_NATIVO, REVIEWED_RUN_ID y EVIDENCE_REF_AUTENTICADO no son condiciones de elegibilidad ni certificacion de trabajo DEV cuando ChatGPT no los expone. NO inventar campos. Evidencia operativa suficiente: TASK_ID configurado, ID de microtarea, HEAD base real, rama y PR/commit GitHub verdaderos, checks de CI y evidencia de readback. El 4/4 de ejecuciones de diagnostico READ_ONLY deja de ser requisito del piloto DEV.

Control de colisiones: un solo escritor Primary con WIP=1, comprobar ramas/PR activos del mismo ID antes de iniciar; continuar el PR existente en vez de duplicar. GitHub proporciona HEAD y proteccion de PR. Sin CAS ni leases en Sheets. Alex revisa un PR/SHA concreto, sin REVIEWED_RUN_ID de ChatGPT ni aprobacion ficticia. Se mantienen Project Scope Lock, permisos reales, seguridad multiempresa, CI exact-head y revision humana independiente cuando GitHub lo exija.

Esta habilitacion se vuelve efectiva unicamente al fusionar con checks obligatorios este contrato a dev y comprobar el HEAD nuevo. Los prompts programados deben validar la autoridad viva antes de escribir.
## Fuentes nativas vigentes

Documento rector: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). Seguimiento manual: [STANDARD DE AUTOMATIZACIONES EN SOLQARYN](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). [STANDARD AUTORIDAD OPERATIVA VAEP](https://docs.google.com/document/d/1A525NGt09mBiZez_r6hFcdCYAdoOAwOZM--N0uXPBOQ/edit) es archivo histórico no ejecutable. No existen Tasks ni jobs con autoridad comercial. La instalación física de dos Tasks piloto READ_ONLY, diferenciada de su autorización operativa, se describe en microfase 3.

## Estado vigente

El propietario confirmó el 22-10-2026 que eliminó las diez Tasks de ChatGPT y retiró la antigua hoja de control de Google Drive. Este repositorio no autoriza despachos, leases, reintentos ni trabajos comerciales automáticos. La eliminación de los cron VAEP y el escritor de admisión automática se integró en DEV mediante el PR #3713.

Los contratos de los proyectos anteriores son **históricos, no ejecutables**. Cualquier implementación futura debe partir de cero, con identidades nuevas y aprobación explícita, sin copiar runtime, secretos, propietarios o pruebas de otro proyecto.

## Certificación de retirada VAEP y ramas (23-10-2026)

GitHub [PR #3728](https://github.com/solqaryn/Solqaryn/pull/3728) y [run 40008279563](https://github.com/solqaryn/Solqaryn/actions/runs/40008279563) confirman la eliminación de las 47 ramas adicionales, con lectura posterior de exactamente tres ramas protegidas: `dev`, `qa` y `main` (PROD). Las 13 propuestas heredadas aún abiertas fueron cerradas sin merge. No se fusionó código histórico a DEV. Las tres ramas conservadas mantuvieron los controles de GitHub, y QA/PROD no se actualizaron.

Del árbol actual de DEV se retiraron 1.499 archivos VAEP heredados, incluidas tres acciones manuales históricas con capacidades de escritura; la historia de Git conserva los antecedentes. El mecanismo excepcional de limpieza debe ser eliminado por PR protegido posterior, sin establecer automatización recurrente. Este certificado es solo sobre gobierno del repositorio: **no** altera ni activa `AUTOMATION_RUNTIME=RETIRED`, la topología futura, la hoja comercial, los permisos de proveedores ni los despliegues pendientes por comprobar.

## Protección permanente distinta del motor

Las pruebas de compilación, aislamiento multiempresa, seguridad, respaldo, frontend y regresiones causales siguen siendo controles técnicos. No deben depender de completar un roadmap anterior ni de la terminación de Tasks inexistentes. Las integraciones del ERP (incluida la funcionalidad comercial propia de automatizaciones M12) no se confunden con el motor retirado.

## Archivo histórico — contrato de preparación de microfase 1 (sin vigencia operativa)

`docs/AUTOMATION_AUTHORITY.md` define exclusivamente la política del piloto de doble ejecutor humano paritario (`EJECUTOR_PRINCIPAL_01` Javier / `EJECUTOR_PRINCIPAL_02` Alex; `PROPIETARIA_INSTITUCIONAL` SOLQARYN Platform). Ninguna Task se crea por esta referencia. En aquella microfase los valores `AUTOMATION_RUNTIME=RETIRED`, `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, `VAEP_DISPATCH_AUTHORIZED=FALSE` y `JULES_WORKERS_AUTHORIZED=0` eran la verdad operativa; la autoridad actual es exclusivamente el encabezado `ACTIVE_DEV` de este archivo, no este registro histórico. Las 4 microfases requieren cierres separados; el contrato no implica autorización de despliegue o escritura programada.

## Archivo histórico — enmienda inicial v1.2 (sin activación)

El contrato v1.2 autorizó inicialmente el *diseno* de dos Tasks,
una por cuenta, antes de los readbacks físicos registrados en microfase 3: `PRIMARY_01` en ChatGPT Javier y `SECONDARY_02` en ChatGPT
Alex. Ambos siguen como ejecutores humanos con igual autoridad; SOLQARYN
Platform conserva la propiedad institucional. Los marcadores de plan
`PILOT_TASK_LIMIT=2`, `PILOT_PRIMARY_TASKS=1` y `PILOT_SECONDARY_TASKS=1`
NO prueban creacion ni autorizan cron, escritura, claims o revisiones.

En aquel corte de la enmienda se mantenían `AUTOMATION_RUNTIME=RETIRED` y
`SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`. Entonces cada cuenta debía certificar
separadamente Task, identidad, conectores, programacion, readback y permisos
en microfase 3. No recuperar un motor retirado ni crear ambas Tasks bajo
Javier como alternativa a la cuenta independiente de Alex.

## Archivo histórico — microfase 2, estructura de Google Sheets (23-10-2026)

MICROFASE_2_SHEETS_READBACK=PASS
MICROFASE_2_SHEETS_ID=1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU
MICROFASE_2_SHEET_TABS=8
MICROFASE_2_PREPARED_TASK_SLOTS=2
MICROFASE_2_LOGICAL_CAPABILITIES=5
MICROFASE_2_PHYSICAL_TASKS_VERIFIED=0

La estructura del Google Sheets existente fue leida, configurada y verificada.
En el corte original de microfase 2 había dos reservas de rol (Primary Javier
y Secondary Alex), ambas `NO_CREADA` y sin `TASK_ID`. Esa evidencia
histórica no certifica instalación; la creación y habilitación posterior de
ambas Tasks se registran separadamente en microfase 3. Las cinco capacidades
comerciales permanecen `NO_ACTIVA`.

El readback comercial confirma 1.123 microtareas, 1.123 IDs unicos,
cero dependencias invalidas/ciclicas y 15.722 formulas intactas.
Hay 16 validaciones estrictas verificadas en Google Sheets y 38 casos
negativos **simulados aisladamente, no ejecuciones de escritura reales**.
La evidencia exacta, huellas y alcance de rollback figuran en
[`AUTOMATION_MICROFASE_2_CERTIFICATION_20261023.md`](AUTOMATION_MICROFASE_2_CERTIFICATION_20261023.md).

Esta constancia no significa que un PR este aprobado ni que las Tasks
existan o esten habilitadas. Antes de cualquier fase posterior se
requiere cierre por flujo GitHub protegido y autorizacion especifica.
En aquel corte se mantenían `AUTOMATION_RUNTIME=RETIRED`,
`SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, VAEP retirado y Jules cero. El encabezado de este documento conserva la autoridad operativa actual.

## Archivo histórico — microfase 3 READ_ONLY, previa al piloto DEV (23-10-2026)

**Inventario físico informado, independiente de autorización comercial:**

- **Primary Javier:** `TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a`, propietaria de cuenta ChatGPT declarada Javier; `is_enabled=true`, `RRULE:FREQ=HOURLY;BYMINUTE=30;BYSECOND=0`, zona `America/Tegucigalpa`. Verificado directamente mediante `automations.peek` de Javier; última actividad observable `2026-10-23T22:37:15.829987Z`, que **no acredita terminación del run**.
- **Secondary Alex:** `TASK_ID=6adbbc9ad9e88191bea8eb69104afb25`, propietaria de cuenta ChatGPT declarada Alex; `is_enabled=true`, `RRULE:FREQ=HOURLY;BYMINUTE=5;BYSECOND=0`, zona `America/Tegucigalpa`, según readback independiente declarado por Alex `2026-10-23T21:00:52.262389Z`. La sesión de Javier **no tiene lectura nativa de la Task ni del historial privado de Alex**, por lo que no equivale a una verificación presente desde su cuenta.
- Referencia única del plan: Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Seguimiento único: Google Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (`TAREAS_PROGRAMADAS!A2:Z3`, `CONFIG`, `BITACORA`); ambas cuentas humanas Javier y Alex tienen permiso `writer`, SOLQARYN Platform `owner`.
- Contrato operativo v1.2, corrección [PR #3731](https://github.com/solqaryn/Solqaryn/pull/3731) ya incorporada: `RUN_ID` nativo puede sustituirse **solo en microfase 3 READ_ONLY** por `EVIDENCE_REF` auténtica de una ejecución individual, programada y finalizada, aceptada por revisor independiente. No inventar IDs ni acreditar `RUN_COMPLETED` con `last_run_time`.
- **Certificación de ejecuciones individuales: 0/4** a la fecha de esta conciliación. `RUN_ID` y resultado finalizado no comprobados de manera suficiente en ambas cuentas. El informe de la Primary en [Issue #3730](https://github.com/solqaryn/Solqaryn/issues/3730) sigue siendo una publicación humana, no un certificado de run; Alex debe validar la fuente independiente.
- En aquel corte dos Tasks físicamente habilitadas **NO** equivalían a dos Tasks comercialmente autorizadas: se mantenían `AUTOMATION_RUNTIME=RETIRED`, `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0`, `VAEP_DISPATCH_AUTHORIZED=FALSE` y cinco capacidades `NO_ACTIVA`. El piloto `ACTIVE_DEV` aprobado posteriormente permite **solo** a Primary abrir ramas y PRs de `dev` según la autoridad vigente; no reactiva VAEP, no autoriza despliegues ni añade Tasks. La programación `:05` de Alex revisa la Primary `:30` de la hora anterior cuando exista una evidencia finalizada publicada.

Esta sección registra hechos observados y límites, no crea, modifica, habilita ni eleva el permiso de ninguna Task. Los resúmenes de microfase 2 anteriores son fotografías históricas; para estado operativo manda esta sección y la lectura actual de proveedores, con la autorización global limitada por las claves de inicio de documento.

## Automatización adicional futura, fuera del único piloto DEV

Requerirá un contrato nuevo, aprobación del propietario, diseño explícito de concurrencia y derechos de escritura, aislamiento de credenciales, pruebas de cierre causal y una configuración nueva comprobada. Ningún archivo de este estado produce activaciones.

## CI vigente y archivo histórico (22-10-2026)

La rama `dev` utiliza `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend` como checks obligatorios, independientes de las antiguas fases. No se requieren checks Fase 7 ni el workflow `vaep-engine-ci.yml`.

Los recibos que se conservan en `vaep/evidence/` son **archivos históricos de auditoría, nunca estado vivo**. No se pueden usar para seleccionar, reclamar, despachar o cerrar tareas. El backup cifrado programado de DEV protege infraestructura y no es una Task comercial; se preserva.

El código de la tienda, inventario, seguridad, correo, migraciones y las regresiones que protegen funciones reales del ERP sigue sujeto a las pruebas actuales del código, independientemente del nombre de su plan de origen.

## Depuracion de scripts y CI historicos (22-10-2026)

El workflow manual `matrix-governance.yml` y sus diez validadores Python de matrices N8.17/N8.18 pertenecen al gobierno VAEP historico y se retiran del arbol de ejecucion. La evidencia original se conserva en Git y en los documentos historicos, sin reactivar su autoridad. No se eliminan pruebas reales del ERP ni checks requeridos del ruleset.

Los servidores y validadores de prueba de correo, seguridad de runtime, auditoria de codigo y comparaciones PDF conservan exactamente sus funciones con nombres neutros: `smtp_capture_server.py`, `validate_email_delivery.py`, `validate_runtime_safety.py`, `audit_source_quality.py`, `compare_pdf_manifests.py` y `compare_pdf_renders.py`. Sus consumidores de CI se actualizan conjuntamente. No se modifican los contratos de ejecucion de Tasks ni los entornos QA/PROD.
