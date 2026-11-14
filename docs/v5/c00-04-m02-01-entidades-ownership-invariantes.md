# SOLQARYN V5 FINAL — C00-04.M02.01: entidades, propiedad e invariantes del gobierno de cambios

**Resultado exigido:** definir entidades, ownership, relaciones e invariantes del gobierno de cambios y decisiones. Este contrato es **documental**: no implementa API, migración, cola, dispatcher, webhook, IAM, aprobación automática, regla productiva ni séptima Task. Fuente única: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## 1. Entidades lógicas y propiedad de la decisión

| Entidad lógica | Identificador / fuente de autoridad | Responsable y límite | Relación |
| --- | --- | --- | --- |
| `MicrotareaCambio` | `PLAN_MAESTRO!A`, ID del catálogo fijo de 2.340 | `PLAN_MAESTRO!H` es único estado mutable; `I` es titular de claim, no autorización comercial | Una microtarea tiene cero o un owner efectivo y muchos eventos |
| `SolicitudDecision` | ID de microtarea + decisión local `Dnn`, citada dentro del artefacto | Autor documenta alcance, razón, reversibilidad, riesgo y autoridad requerida; no ejecuta por el acto de redactar | Una microtarea puede documentar varias decisiones |
| `AutoridadOperacion` | Orden del propietario y política vigente en `dev` | Autorizar documentación o commit DEV no equivale a autorización QA/main/PROD, datos productivos, pago, dominio o secreto | Cada operación sensible debe asociarse a autorización distinta, verificable y vigente |
| `ClaimExclusivo` | `PLAN_MAESTRO!I` + `TASK_ID` nativo + evidencia temporal | Solo una de las seis Tasks Javier habilitadas; no suplantar otra cuenta; conservar autor original | Claim une un owner y una microtarea; relevo necesita exclusión y CAS comprobables |
| `DependenciaTecnica` | `DAG_TECNICO!A:G` por ID; F=`VALIDADO`, G=`SI` | Las fórmulas `COLA!G/O` filtran, pero no conceden claim ni liberan prerequisitos faltantes | Un ID depende de cero o más predecesores comprobados |
| `RiesgoYMitigacion` | `Rnn` dentro del mismo documento y evidencia en J:L | Autor registra amenaza, impacto, control, limitación y decisión pendiente; crítico sin evaluar = NO_GO | Riesgo puede bloquear una o más decisiones, sin cambiar estados ajenos |
| `ArtefactoYRevision` | ruta/blobs, commit SHA, diff y check-runs con `head_sha` exacto | Autor conserva procedencia; revisor A5 debe tener TASK_ID distinto, autorizado entre las otras cinco Javier | Un commit tiene varias evidencias; una revisión A5 corresponde a SHA y autor concretos |
| `EventoInstitucional` | `BITACORA!A:Z`, EVENT_ID A, IDEMPOTENCY_KEY R | Append único de 26 campos, nunca edición histórica ni RUN_ID inventado | Una microtarea emite muchos eventos; las claves no se reutilizan |
| `PlanReversion` | commit padre, diff, dependencias y condición de abortar | Revert normal y compensación auditable; no force-push ni reset ni revert de DB productiva por Git | Una decisión ejecutable futura requiere rollback proporcional y propietario real |

Estas entidades son **esquemas conceptuales**, no tablas EF/MySQL, DTO nuevos ni permisos efectivos. Una referencia histórica en BITACORA no convierte a un ejecutor retirado en owner autorizado. Un A5_PASS previo a la nueva versión de un artefacto no certifica su SHA posterior.

## 2. Invariantes observables y denegaciones

- **I01 — identidad y autoría:** cada operación usa el mismo ID V5 en PLAN, COLA, DAG y BITACORA. Conservar `TASK_ID` del autor, titular de claim y revisor como identidades distintas; seis Javier autorizadas por `AGENTS.md`/Scope Lock.
- **I02 — estados:** la única transición ordinaria es `PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO`; `COLA!E` permanece espejo por fórmula de `PLAN_MAESTRO!H`. `VALIDANDO` no significa auditoría aprobada.
- **I03 — propiedad:** para reclamar se exige H=PENDIENTE, I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y prerequisitos realmente satisfechos. Un revisor no se convierte en autor. Si existe lease/escritura de otro owner, abstenerse; relevo solo mediante CAS verificable.
- **I04 — decisiones:** decisión documental = propuesta; autorización de DEV ≠ permiso productivo. El resultado M02.01 no crea aprobación humana, scheduler, webhook, API para aprobar, ni operación con privilegios de plataforma.
- **I05 — calidad:** candidato revisable entra `VALIDANDO` antes de A0–A3; A0–A4 reales y proporcionales preceden LISTO_DEV; A5_PENDIENTE se registra para otro TASK_ID, y A5_PASS independiente es necesario pero insuficiente para cualquier promoción.
- **I06 — integridad:** el evento institucional contiene 26 campos A:Z, EVENT_ID e IDEMPOTENCY_KEY únicos y RUN_ID únicamente si el proveedor lo acredita. No se truncan ni se reescriben eventos.
- **I07 — evidencia Git:** commit directo fast-forward `dev`, padre y SHA causal; `verify-solqaryn-project-scope` y procedencia CHANGELOG obligatorios en `completed/success` exact-SHA. `SKIPPED`, `CANCELLED`, `IN_PROGRESS` o SUCCESS de un SHA distinto no prueban un gate requerido.
- **I08 — aislamiento:** ni un flag de UI, fila Sheets, cambio documental ni dueño de Task confieren JWT, RBAC o acceso a tenant B. Secretos, PII, permisos y cuentas de otras personas permanecen aislados.
- **I09 — reversibilidad:** antes de escritura, clasificar si es reversible. Ningún gate documental autoriza cambios irreversibles de MySQL, DNS, proveedores o pagos; rollback técnico exige prueba y autorización por operación.
- **I10 — concurrencia:** releer HEAD y ownership antes de publicar, nunca sobrescribir CHANGELOG de otros; cada sección `##` incluye juntas todas sus viñetas de procedencia.

## 3. Matriz de relaciones y seis fuentes de M01

La entrada contractual son las seis microtareas `C00-04.M01.01–06`, **todas LISTO** según `PLAN_MAESTRO!H110:H115`. Las cinco primeras tienen A5 correctiva independiente sobre SHA `8ee14f8b40114041a587e9a38f188851c9807505`; M01.06 tiene evidencia independiente en su registro. Cada fuente conserva su blob verificable:

| Predecesor | Artefacto de evidencia `docs/v5/` | Blob del archivo |
| --- | --- | --- |
| M01.01 | `c00-04-m01-01-alcance-gobierno-cambios.md` | `bebf45dad2f2be1f5597d2f3840f5642048fcc39` |
| M01.02 | `c00-04-m01-02-actores-superficies.md` | `fda09fc62876023a8fe786e8c1066fac1f554c15` |
| M01.03 | `c00-04-m01-03-fuentes-codigo-evidencias.md` | `1659f27bbee144fc50abee22e98d45c61c1316f3` |
| M01.04 | `c00-04-m01-04-riesgos-supuestos-decisiones.md` | `14a42f137ec0f205156a031a2969789b03a35a57` |
| M01.05 | `c00-04-m01-05-criterios-aceptacion.md` | `e480b4eac5b966aca492e557ff2f3a7d0da714ad` |
| M01.06 | `c00-04-m01-06-bloqueo-rollback-liberacion.md` | `62dd929c1e26e3fade77a88d58c91997eb2ac183` |

`DAG_TECNICO!A145:J145` registra `C00-04.M02.01`, F=VALIDADO/G=SI con esas seis dependencias y superficie `docs/v5/governance-m02-ownership`. La salida M02.01 **no** libera M02.02–06, M03, IAM, migraciones, runtime o QA/main/PROD. Consumidores: futuras M02.02 (API/DTO), M02.03 (persistencia), M02.04 (configuración), M02.05 (eventos) y M02.06 (dependencias), cada una sujeta a gate individual.

## 4. Criterios de aceptación positivos y negativos (documentales)

| Caso | Resultado verificable |
| --- | --- |
| P01 | ID presente, seis M01 LISTO, owner inicialmente libre, COLA G/O=SI y DAG F/G válidos → claim exclusivo y readback, sin autoría doble |
| P02 | Entidades y relaciones identifican fuente única de estados, owner, decisión, riesgo, autorización, evento y A5 sin introducir tablas/servicios |
| P03 | Documento y sección CHANGELOG autocontenida publicados FF con Scope Lock/procedencia exact-SHA, readback institucional y A5_PENDIENTE → LISTO_DEV documental |
| N01 | COLA G/O=SI sin DAG individual VALIDADO o un predecesor incompleto → NO_GO, aunque haya reserva de capacidad |
| N02 | Claim con I ocupado o revisor igual al autor → rechazo; no reasignar ni autoaprobar |
| N03 | A5_FAIL histórico sin corrección del SHA, o prueba requerida SKIPPED/CANCELLED/IN_PROGRESS → sin acreditación ni promoción |
| N04 | Evento con 21–25 columnas, duplicado o RUN_ID inventado → rechazo de la inserción |
| N05 | Petición de QA, main, PROD, MySQL productivo, dominio, secretos, pagos o bypass tenant → NO_GO sin autorización adicional y pruebas específicas |
| N06 | CHANGELOG con encabezado vacío, viñetas atribuidas al otro autor o parent obsoleto → no publicar hasta conciliar |
| N07 | Documento que afirma aprobación JWT, migración EF, pruebas HTTP/E2E o ejecución de workers por leer fuentes → falla de aceptación |
| N08 | Solicitud de septima Task Javier, usar cuenta Alex o inventar ID 2.341 → rechazo por allowlist y plan |

Los oráculos P/N son inspecciones de contrato, no tests de API, MySQL, Chromium ni autenticación real.

## 5. Impacto, verificaciones y rollback

**Archivos del cambio:** este Markdown y un bloque nuevo, íntegro y aditivo de `CHANGELOG_AI.md`. Sin alteración de interfaces/DTO, Domain/Application/Infrastructure/API, Angular, MySQL, EF, migraciones, workers, workflows, secretos, entornos ni infraestructura. Las seis fuentes M01 son solo lecturas; la BITACORA institucional registra cada transición del ID.

**A0:** integridad Markdown/tablas/rutas y SHA de fuentes, diff sin secretos ni encabezados CHANGELOG vacíos. **A1:** casos P01–P03 y N01–N08 con oráculos documentales. **A2:** denegaciones de identidad, JWT/RBAC/tenant/PII, separación de revisor y entornos. **A3:** seis predecesores, DAG, PLAN/COLA y esquema BITACORA A:Z. **A4:** tras commit FF comprobar SHA exacto y los dos checks de gobernanza pertinentes, diff/blobs/parent, rollback y readback de PLAN/COLA/BITACORA. **A5:** otra Task Javier distinta del autor; pendiente tras LISTO_DEV.

`requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, `requiere_security=SI_DOCUMENTAL`, `requiere_scope_lock=SI`. Las herramientas técnicas no afectadas quedan **NO_APLICA**, nunca PASS sintético.

**Rollback:** revert ordinario exclusivo del commit documental en `dev` tras inspeccionar consumidores y HEAD; registrar cualquier corrección institucional con nuevos eventos sin eliminar historia. No force-push, no cambios a `qa`, `main` ni producción.
