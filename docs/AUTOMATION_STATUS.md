# SOLQARYN — Estado estándar de automatización

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
SCHEDULED_CHATGPT_TASKS_AUTHORIZED=2
AUTHORIZED_COMMERCIAL_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6adc2e4bd8a48191ac7d8641ed95a1a3
VAEP_DISPATCH_AUTHORIZED=FALSE
JULES_WORKERS_AUTHORIZED=0
PILOT_TASK_LIMIT=6
PILOT_PRIMARY_TASKS=3
PILOT_SECONDARY_TASKS=3
PRIMARY_TASK_ROLE=PRIMARY_01
SECONDARY_TASK_ROLE=SECONDARY_02
PRIMARY_TASK_ACCOUNT=JAVIER_CHATGPT
SECONDARY_TASK_ACCOUNT=ALEX_CHATGPT

DESIGN_VERSION=2.0
DESIGN_TOPOLOGY=3_JAVIER_3_ALEX
PHYSICAL_TASKS_REGISTERED=6
MAX_CONCURRENT_SCOPES=3
LEGACY_AUTOMATION_RUNTIME=RETIRED

## M3.3 — capacidad comercial y objetivo operativo de productividad (2026-10-25)

La meta del propietario es **6 microtareas comerciales realmente LISTO por hora** como indicador de rendimiento, no garantía de una Task, permiso para falsear cierres ni cuota que rebaje CI. Cada Javier habilitada puede completar varias microtareas sucesivas en su misma ejecución cuando alcance tiempo, dependencias, revisión independiente y readbacks reales.

`MAX_CONCURRENT_SCOPES=3` es techo de **microtareas comerciales distintas activas**, con máximo un scope activo por Task comercial individualmente autorizada y rama determinista `work/solqaryn-<microtarea>`. No concede autorización a una Task adicional. Mientras solo :00 y :20 consten en `AUTHORIZED_COMMERCIAL_TASK_IDS`, como máximo dos ejecutores podrán escribir comercialmente aunque exista capacidad numérica 3.

**Exclusiones del WIP comercial:** ramas `test/solqaryn-m3-2-native-write-*` con pruebas de diagnóstico no comercial, PR puramente normativos/de gobierno, ramas fusionadas, logs y auditorías. Su uso puede requerir exclusividad sobre esa rama/recurso, pero su mera existencia no reserva un scope comercial ni bloquea candidatos independientes como P0-002.A. No clasificar `SIN_TRABAJO_ELEGIBLE` solo por un PR comercial + rama diagnóstica; computar PRs/ramas/owners vivos sin duplicarlos. P0-001.B mantiene NO_GO de paridad mientras Vercel DEV esté CANCELED; su acta documental integrada no implica LISTO.

**Métrica:** contar eventos únicos de transición a LISTO con PR fusionado, 3/3 CI del SHA final, revisión independiente y readback de PLAN_MAESTRO/COLA/BITACORA durante ventana móvil de 60 minutos. No contar ejecuciones, commits, comentarios, pruebas, PR abiertos ni documentos NO_GO como entregas. Recuperar y revisar antes de reclamar nueva tarea, reconsultar DAG tras cerrar y avanzar trabajo independiente ante bloqueos externos. Seguridad, Scope Lock y entornos fuera de DEV intactos.

## M3.2 — certificacion individual Javier PRIMARY_DEV_02 (25-10-2026)

**Autoridad propuesta condicionada a PR protegido y readback de dev.** El propietario autoriza comercialmente exclusivamente TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` (JAVIER_CHATGPT, :20 America/Tegucigalpa) para desarrollar y corregir microtareas del plan comercial vigente en ramas de trabajo hacia dev; registrar estados PLAN_MAESTRO!H:M y auditoria BITACORA con readback; revisar trabajo de otro TASK_ID (revision tecnica cruzada), y tramitar merge/cierre solo si GitHub acredita una aprobacion formal genuina desde otra identidad y CI del SHA final. No transferir permisos a otros slots; max dos scopes, exclusividad real, dependencias LISTO, sin QA/main/PROD. El cambio de permiso es efectivo solo despues del merge protegido de esta enmienda y conciliacion exacta del Sheet institucional. `CONFIG!B57` pasa a TRUE para habilitar exclusivamente ejecuciones individuales enumeradas como comerciales en este documento, nunca autoriza todas las seis Tasks por defecto.

**Evidencia ya consumida, no repetir:** commit de contenido GitHub `aaca89247d9aaf61eb21a308c2e43fd5fef24526` y archivo `automation-probes/m3-2/6adc2e4bd8a48191ac7d8641ed95a1a3.txt` en rama temporal `test/solqaryn-m3-2-native-write-20261024`; autor real la cuenta GitHub autenticada de Javier (ID 93171442), UTC `2026-10-25T15:26:15.942Z`. BITACORA!A77:Z77 contiene fingerprint `M3_2_CONTENT_WRITE_PROBE:6adc2e4bd8a48191ac7d8641ed95a1a3:2026-10-25T15:26:15.942Z`, readback 26 columnas y commit coincidente. GH_CONTENT_WRITE=PASS y DRIVE_AUDIT_WRITE=PASS observables; ello no certifica autoaprobacion, merge ni acceso comercial a cualquier recurso no probado. `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=2` solo reconoce Primary 01 y Primary 02, no Primary 03 ni Alex. `ACTIVE_DEV_WRITERS_OBSERVED` historico 1 deja de ser autoridad si contradice esta evidencia.

## M3.2 — tres escrituras de auditoría verificadas y propuesta de pruebas de contenido

Tras integrar el PR #3739, se verificaron tres comentarios GitHub y sus filas de auditoría correspondientes: Alex :10 (comentario `6392818168`, `BITACORA!A63`), Javier :20 (`6392908957`, `BITACORA!A64`) y Javier :40 (`6393029857`, `BITACORA!A67`), con identidades GitHub y fingerprints reales. Los readbacks independientes desde Javier y SOLQARYN Platform se registraron en `BITACORA!A65` y `BITACORA!A68`. Los cinco informes individuales `GH_READ=PASS` y `DRIVE_READ=PASS` siguen documentados. Estas pruebas NO acreditan `contents:write` ni commits o PR comerciales, y el origen nativo exacto de cada escritura de Sheet se sustenta en reportes de Task, no en una identidad de ChatGPT expuesta por la API de Google.

La concesion actual #3739 abarca solo Alex :10, Javier :20 y Javier :40 para comentario/Bitacora. Alex :30 y :50 mantienen `READ_ONLY`. Se propone una unica siguiente enmienda para conceder a esos dos slots auditoria no comercial y a las cinco Tasks una **prueba unica de contenido GitHub** en una sola rama aislada, con lectura posterior y verificaciones de concurrencia; una autorizacion de revision independiente solo para Alex :10 sobre PR #3738 despues de probar capacidad de contenido. **Esta propuesta no es efectiva antes de su propio merge protegido.**

Conservar sin elevar: `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, `PHYSICAL_TASKS_REGISTERED=6`, `ACTIVE_DEV_WRITERS_OBSERVED=1`, `CONFIG!B39=1`, `CONFIG!B57=FALSE`; ningun permiso comercial masivo por documento. El resto de controles de Scope Lock, QA/main/PROD, VAEP/Jules e independencia siguen intactos.

## M3.2 — habilitación diagnóstica limitada para PRIMARY_DEV_02 (:20, Javier)

**Task individual:** `6adc2e4bd8a48191ac7d8641ed95a1a3`, propietaria `JAVIER_CHATGPT`, programada cada hora a :20 (`America/Tegucigalpa`), `is_enabled=true`. El usuario aportó un informe de su ejecución del 24-10-2026 con `GH_READ=PASS`, `DRIVE_READ=PASS`, `GH_WRITE=NO_AUTORIZADO` y `DRIVE_WRITE=NO_AUTORIZADO`; `automations.peek` confirmó `last_run_time=2026-10-24T18:26:56.644003Z`. Estas lecturas fueron **reportadas** desde la ejecución y conciliadas por Javier en `TAREAS_PROGRAMADAS!O4:Z4` y `BITACORA!A51:Z51`; no son una prueba de escritura ni permiten atribuir conexiones interactivas a la Task.

**Autorización condicionada:** tras integrar mediante PR protegido y revisión independiente esta enmienda al HEAD actual de `dev`, el propietario permite **una prueba de escritura no comercial y estrictamente delimitada** únicamente desde esta Task, según la sección «Concesión de prueba M3.2 PRIMARY_DEV_02» de la autoridad operativa canónica. Esta excepción no permite desarrollo comercial, cambios a `dev`, PR funcionales, trabajo de otras Tasks, escalamiento de permisos del proveedor ni bypass de sus protecciones.

**Estado hasta readback real:** `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, `PHYSICAL_TASKS_REGISTERED=6`, `ACTIVE_DEV_WRITERS_OBSERVED=1` y `CONFIG!B57=FALSE` continúan intactos. La habilitación diagnóstica no certifica `GH_WRITE` de código ni `DRIVE_WRITE` hasta que una ejecución autentique la operación y sus lecturas posteriores; tampoco concede a la Task acceso que el proveedor niegue. Antes de cualquier autorización comercial individual hace falta verificar capacidades de escritura para crear rama/commit/PR en GitHub y editar el Sheet institucional de forma controlada, y entonces conciliar GitHub/Sheet mediante un nuevo cambio protegido. `PASS_M3_2_CERTIFICADA` permanece pendiente.

## M3.2 — dos Tasks adicionales con lecturas nativas reportadas (:10 Alex y :40 Javier)

El propietario aportó informes de ejecución de **`SECONDARY_REVIEW_01` Alex :10 (`6adbbc9ad9e88191bea8eb69104afb25`)** y **`PRIMARY_DEV_03` Javier :40 (`6adc2e55f47c81919f6374e561687076`)**. Ambos reportan `GH_READ=PASS`, `DRIVE_READ=PASS`, `GH_WRITE=NO_AUTORIZADO` y `DRIVE_WRITE=NO_AUTORIZADO`, con HEAD `41d05dda3ffb57707275132f834c40cace3f8594`, gobierno canónico coincidente y Scope Lock STRICT. Se asentaron en `TAREAS_PROGRAMADAS!O3:P3`, `TAREAS_PROGRAMADAS!O6:P6` y eventos de `BITACORA!A53:Z54`; las lecturas internas del conector provienen de informes de esas Tasks, no de una conexión compartida con Javier. `automations.peek` Javier acreditó `PRIMARY_DEV_03 is_enabled=true`, `last_run_time=2026-10-24T18:46:06.710769Z`; la cuenta Alex reportó su ejecución :10 a las 18:11 UTC, sin readback nativo de `automations.peek` desde esta cuenta humana Javier.

Junto con `PRIMARY_DEV_02` Javier :20, hay **tres diagnósticos individuales con lectura `PASS/PASS` reportada** entre las cinco Tasks aún no autorizadas para escritura. Estos tres registros son **CERTIFICACIÓN DE LECTURA CON EVIDENCIA DE EJECUCIÓN REPORTADA**, no certificación del derecho a escribir. Tras aprobación independiente, CI y merge protegido de esta enmienda, estas dos Tasks podrán ejecutar **solo** las pruebas no comerciales individualizadas en la sección de concesiones diagnósticas de la autoridad operativa canónica. No heredan los permisos una de otra, ni de los chats humanos; si el proveedor rechaza la operación no se altera la autorización.

`SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, `ACTIVE_DEV_WRITERS_OBSERVED=1`, `CONFIG!B39=1`, `CONFIG!B57=FALSE` y `PHYSICAL_TASKS_REGISTERED=6` permanecen sin cambios; `PASS_M3_2_CERTIFICADA` sigue pendiente de escrituras autenticadas. Esta concesión no autoriza commits ni revisión/aprobación de PR comerciales, ni modifica QA/main/PROD.

## Estado de microfase 3.2 — seis Tasks físicas, autorización operativa parcial

El diseño simétrico 3+3 v2.0 fue integrado en PR #3735. La microfase 3.2 confirmó seis Tasks físicas de identidad única y horarios intercalados, incluidas las dos originales con ID conservado. `PHYSICAL_TASKS_REGISTERED=6` procede de lectura nativa Javier (3/3) y readback aportado por Alex (3/3) con conciliación en Google Sheets. `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1` sigue siendo el contador de autorización conocido: habilitación y programación no equivalen a permisos efectivos en ejecución. Las cinco Tasks restantes siguen pendientes de validación operativa individual, sin concederles escritura por esta modificación. El contrato normativo es `docs/AUTOMATION_3X3_OPERATING_CONTRACT.md`.

**Inventario físico M3.2 (horas `America/Tegucigalpa`, readback en `TAREAS_PROGRAMADAS!A2:Z7`, evento `BITACORA!A42:Z42`):**

| Cuenta | Slot canónico | Minuto | Task ID físico | Evidencia de habilitación |
| --- | --- | --- | --- | --- |
| Javier | `PRIMARY_DEV_01` (`PRIMARY_01` histórico) | :00 | `6adbb9fff1548191a1bd5b30d9e9984a` | ChatGPT `automations.peek` Javier; habilitada |
| Alex | `SECONDARY_REVIEW_01` (`SECONDARY_02` histórico) | :10 | `6adbbc9ad9e88191bea8eb69104afb25` | `automations.peek` Alex reportado y asentado en Sheet; habilitada |
| Javier | `PRIMARY_DEV_02` | :20 | `6adc2e4bd8a48191ac7d8641ed95a1a3` | ChatGPT `automations.peek` Javier; habilitada |
| Alex | `SECONDARY_REVIEW_02` | :30 | `6adcc97a260881919d92bb9bacae1626` | `automations.peek` Alex reportado y asentado en Sheet; habilitada |
| Javier | `PRIMARY_DEV_03` | :40 | `6adc2e55f47c81919f6374e561687076` | ChatGPT `automations.peek` Javier; habilitada |
| Alex | `SECONDARY_REVIEW_03` | :50 | `6adcc98a80a48191b71a5156624c21df` | `automations.peek` Alex reportado y asentado en Sheet; habilitada |

**Separación de estados:** `6/6` es *inventario físico*, NO `6/6` de ejecuciones, permisos, aprobación o certificación. Conservamos `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, `ACTIVE_DEV_WRITERS_OBSERVED=1` y permisos de las cinco adicionales en `NO_VERIFICADO` hasta evidencias desde sus ejecuciones. La certificación `PASS_M3_2_CERTIFICADA` está pendiente. Un `last_run_time` no acredita entrega. No exigir `RUN_ID` nativo inobservable.

El histórico de la topología 1+1 queda abajo como evidencia, no como autoridad vigente. VAEP y Jules permanecen retirados. No exigir RUN_ID nativo que ChatGPT no expone ni falsificar resultados. La Task privada de Alex debe verificarse desde su cuenta antes de cualquier escritura de Alex.

## Archivo histórico v1.2 — 2026-10-23 — PILOTO DEV PRIMARY AUTORIZADO POR EL PROPIETARIO (reemplaza gates historicos de microfase 3)

El propietario ordena reanudar las microtareas DEV ahora. PRIMARY_01 existente de Javier es el unico escritor automatizado autorizado, SECONDARY_02 de Alex permanece revisor READ_ONLY. Se limita a solqaryn/Solqaryn, base dev, rama especifica por microtarea del Plan Comercial Inicial y PR contra dev. Prohibido merge autonomo, QA, main, PROD, Render, Vercel, secretos o cambios productivos.

El RUN_ID, RUN_URL, TASK_ID_NATIVO_DEL_RUN, correo ChatGPT, RUN_COMPLETED_NATIVO, REVIEWED_RUN_ID y EVIDENCE_REF_AUTENTICADO no son condiciones de elegibilidad ni certificacion de trabajo DEV cuando ChatGPT no los expone. NO inventar campos. Evidencia operativa suficiente: TASK_ID configurado, ID de microtarea, HEAD base real, rama y PR/commit GitHub verdaderos, checks de CI y evidencia de readback. El 4/4 de ejecuciones de diagnostico READ_ONLY deja de ser requisito del piloto DEV.

Control de colisiones: un solo escritor Primary con WIP=1, comprobar ramas/PR activos del mismo ID antes de iniciar; continuar el PR existente en vez de duplicar. GitHub proporciona HEAD y proteccion de PR. Sin CAS ni leases en Sheets. Alex revisa un PR/SHA concreto, sin REVIEWED_RUN_ID de ChatGPT ni aprobacion ficticia. Se mantienen Project Scope Lock, permisos reales, seguridad multiempresa, CI exact-head y revision humana independiente cuando GitHub lo exija.

Esta habilitacion se vuelve efectiva unicamente al fusionar con checks obligatorios este contrato a dev y comprobar el HEAD nuevo. Los prompts programados deben validar la autoridad viva antes de escribir.
## Fuentes nativas vigentes

Documento rector: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). Seguimiento manual: [STANDARD DE AUTOMATIZACIONES EN SOLQARYN](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). [STANDARD AUTORIDAD OPERATIVA VAEP](https://docs.google.com/document/d/1A525NGt09mBiZez_r6hFcdCYAdoOAwOZM--N0uXPBOQ/edit) es archivo histórico no ejecutable. El seguimiento comercial permanece en el Sheet institucional; el piloto DEV autoriza únicamente una Task escritora conocida hasta el readback individual de M3.2. Existen seis Tasks físicas registradas, de ellas dos originales con ID estable y cuatro nuevas creadas durante M3.2 desde sus cuentas respectivas. El inventario no autoriza despliegues ni escrituras adicionales.

## Archivo histórico — retirada inicial (22-10-2026, no describe el runtime actual)

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
