# SOLQARYN V5 — C00-04.M03.04 adaptador aislado de lecturas institucionales

**Autor:** PRIMARY DEV 04 Javier; TASK_ID \`6aeacb7820ac8191b7d768a6f33dfa09\`. **Ámbito:** \`solqaryn/Solqaryn@dev\`, Scope Lock STRICT. Única autoridad de datos: PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA; ninguna fuente se sustituye por este componente.

## Alcance A0 y exclusiones

El resultado de la microtarea permite implementar persistencia, migración reversible **o adaptador aislado**. Se escogió un adaptador aislado, efímero e inerte sin EF/MySQL, schema/migraciones, conectores externos, DI, API, frontend ni lectura/escritura de Google Sheets. No emite eventos, no crea worker/webhook/dispatcher y no ejecuta ni reprograma Tasks. El ámbito institucional "SOLQARYN:DEV" no es EmpresaId ni tenant del SaaS.

Preflight individual \`DAG_TECNICO!A157:J157\`: 15 predecesoras C00-04.M01/M02/M03.01–03 y dos terminales C00-01.M06.06/C00-02.M06.06, todas LISTO con revisión A5 independiente. La habilitación exige F=VALIDADO/G=SI y PLAN H125 PENDIENTE I125 vacío antes de claim. Se excluyen archivos de C01-01.M03.04 bajo Infrastructure/Persistence, con autor distinto.

## Contrato A1–A3 y riesgos

La clase \`GobiernoCambiosProyeccionAislada\` registra únicamente versiones de proyección de lectura con ID de microtarea, revisión positiva, SHA hexadecimal 40, uno de cuatro estados observables y hora UTC. Valida estricto \`\\z\`, no interpreta estados como transiciones, impide reemplazo por mismo ID/revisión, limita memoria a 4096 lecturas, bloquea bajo concurrencia local y jamás concede permisos, aprobación, persistencia oficial o ejecución. No contiene empresa, JWT, rol, lease, secreto, PII ni autorización tenant.

xUnit incluye positivos de lectura/revisión y negativos de ID manipulado, SHA incorrecto, estado falsificado, versión nula, fecha no UTC, duplicación conflictiva y 64 escritores concurrentes/1 ganador **local**. No acredita CAS distribuido, persistencia MySQL ni estado institucional actualizado. Las pruebas se consideran PASS exclusivamente tras CI .NET 10/seguridad sobre el SHA exacto.

## A4, A5 y reversión

Compilación y tests backend, seguridad, Scope Lock STRICT y procedencia V5 de CHANGELOG son obligatorios. MySQL, Docker, frontend y navegador NO_APLICA porque no se cambia runtime de esos componentes. Una validación fallida/cancelada/omitida/ejecutándose no es PASS requerido; conservar VALIDANDO con owner de esta Task. Tras A0–A4, LISTO_DEV con A5_PENDIENTE a otra Task Javier. Sin QA/main/PROD. Rollback mediante git revert normal, sin reescritura de dev ni BITACORA.
