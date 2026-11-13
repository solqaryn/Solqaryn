# SOLQARYN V5 FINAL — C00-02.M02.05: eventos, idempotencia, correlación y límites

**Tipo:** contrato documental M02.05 del modelo de trabajo para automatizaciones, sin cambiar código, Sheets, reglas de rama ni infraestructura. **Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repo:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuente única:** Standard de Implementación V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

**Resultado exigido:** definir los eventos, las identidades estables, las claves de idempotencia, la correlación SHA y los límites de concurrencia para la cola V5, sin duplicar microtareas ni convertir fórmulas en despachador. **Evidencia mínima:** contrato de estados más un ejemplo de ciclo y pruebas positivas/negativas reproducibles a nivel documental. Esta unidad no implementa un servicio de mensajería, una transacción distribuida ni CAS nativo de Sheets. M01.06 del mismo bloque se verificó `LISTO`; `COLA!G/O=SI` y no existe regla DAG individual para M02.05, por lo que se conserva el gate de dependencias anterior, sin adelantos artificiales.

## 1. Entidades lógicas y fuente de autoridad

| Entidad | Identidad y origen | Invariante |
| --- | --- | --- |
| Microtarea V5 | `PLAN_MAESTRO!A` | Exactamente uno de los 2.340 IDs canónicos; no crear ID 2.341 |
| Estado y dueño | `PLAN_MAESTRO!H/I` | H es único estado editable; I identifica TASK_ID autorizado y conserva autoría |
| Cola reactiva | `COLA!E/G/O` | E consulta H por fórmula; G/O son gates derivados, nunca asignación ni lock |
| Dependencia técnica | `DAG_TECNICO!A:G` | Regla VALIDADO y prerrequisitos reales; sin entrada individual, gate conservador |
| Publicación DEV | SHA de commit y su padre | `dev` fast-forward serializado; ningún PR, rama, force-push o merge ficticio |
| Evento institucional | `BITACORA!A:Z` | 26 celdas; append-only, EVENT_ID e IDEMPOTENCY_KEY únicos, sin sobrescribir historia |
| Revisión independiente | `REVIEWER_TASK_ID` distinto del autor | A5_PENDIENTE tras A0–A4; A5_PASS/FAIL solo por otra de las seis Tasks Javier |
| Ejecución nativa | `RUN_ID` verificable o vacío | No inferir un RUN_ID por hora, prompt, commit o listado de Tasks |

La autoridad comercial es V5 FINAL; el modelo de seis ejecutores Javier proviene de `docs/AUTOMATION_STATUS.md` vivo. Entradas de Alex u otros ejecutores en auditoría son **historial**, no autorización para nuevos claims o A5.

## 2. Contrato de evento y correspondencia física A:Z

Los campos físicos, en su orden real, son: A `EVENT_ID`, B `FECHA_HORA_TEGUCIGALPA`, C `CONTRATO_VERSION`, D `ACTOR_HUMANO`, E `TASK_ROLE`, F `ACCOUNT_OWNER`, G `TASK_ID`, H `RUN_ID`, I `REVIEWED_RUN_ID`, J `ACTOR_AUTOMATIZADO`, K `EJECUTOR_RESPONSABLE`, L `REVISOR_INDEPENDIENTE`, M `SCOPE_KEY`, N `MICROTAREA_ID`, O `OPERACION`, P `ESTADO_ANTERIOR`, Q `ESTADO_NUEVO`, R `IDEMPOTENCY_KEY`, S `BASE_HEAD`, T `RESULT_HEAD`, U `AUTORIZACION`, V `RESULTADO`, W `EVIDENCIA`, X `ERROR`, Y `READBACK`, Z `TIMESTAMP_UTC_ORIGINAL`.

- `EVENT_ID`: identificador estable de una transición/auditoría concreta, distinto para etapas diferentes. La repetición del mismo intento **no** puede crear otro evento semánticamente idéntico.
- `IDEMPOTENCY_KEY`: composición canónica `V5|<MICROTAREA_ID>|<OPERACION>|<SHA_O_FINGERPRINT>|<TASK_ID_AUTORIZADO>`. Los delimitadores y el SHA/fingerprint deben fijarse antes del primer append, no variar por retry.
- `TASK_ID` es el actor nativo acreditado; `EJECUTOR_RESPONSABLE` conserva la autoría original. `REVISOR_INDEPENDIENTE` solo se llena tras revisión real por Task distinta.
- `BASE_HEAD` y `RESULT_HEAD` vinculan el evento al parent y al SHA de código/documentación realmente publicado. Un blob/tree no publicado **no** es commit ni SUCCESS CI.
- `FECHA_HORA_TEGUCIGALPA` lleva offset `-06:00`; `TIMESTAMP_UTC_ORIGINAL` conserva UTC; no inventar horas de runs no visibles. H/I quedan vacíos cuando no hay identificadores nativos acreditados.
- La carga W/X no contiene contraseña, JWT, cookies, tokens, PII innecesaria, connection strings, claves privadas ni credenciales; usar fingerprints no secretos y links internos auditables.

## 3. Máquina de estados y exclusividad

| Operación | Precondición real | Efecto permitido |
| --- | --- | --- |
| CLAIM | H=PENDIENTE, I vacío, COLA G/O=SI, DAG/prerrequisitos y seguridad comprobados | H=EN_PROGRESO, I=TASK_ID; readback inmediato; evento idempotente |
| CANDIDATE_READY | Implementación/artefacto revisable y propiedad aún original | H=VALIDANDO y fingerprint **antes** de iniciar A0–A3 |
| AUDIT_A0_A3 | VALIDANDO, diff y riesgos pertinentes | registrar resultados verdaderos; `NO_APLICA` solo por subpruebas tecnológicas ajenas |
| PUBLISH | A0–A3 exigibles PASS, HEAD revalidado | commit directo FF `dev`, sin force, parent causal y pruebas CI de SHA cuando aplican |
| VERIFY_A4 | SHA publicado, checks requeridos exact SHA y readback | A4_PASS o VALIDANDO con causa si gate falta/falla |
| CLOSE_DEV | A0–A4 PASS y A5_PENDIENTE en evento institucional | H=LISTO, I conserva autoría, evento `LISTO_DEV`, COLA E confirma |
| INDEPENDENT_A5 | revisora distinta del autor, ambas Javier autorizadas | A5_PASS o A5_FAIL por SHA, sin reescribir A0–A4 ni autoproclamar QA/PROD |

Las filas con H=EN_PROGRESO/VALIDANDO no se reclaman otra vez por una Task distinta. Para relevo de dueño retirado, documentar inactividad efectiva, ausencia de escritor, CAS condicional, readback y owner previo; si el backend no ofrece CAS seguro, **no afirmar exclusividad ni tomar propiedad**. `MAX(24,3*N)` es una meta de inventario de elegibles, **no** permiso para abrir fases, incumplir DAG ni crear ejecutores.

## 4. Ejemplo causal de ciclo documental (solo contrato, no evento real)

Para un ID ficticio **dentro de los 2.340** y un TASK_ID real autorizado: `PENDIENTE` con I vacío + G/O=SI → claim único → `EN_PROGRESO` → documento con hash → `VALIDANDO` → A0/A1/A2/A3 documentales sobre diff → commit FF `dev` → Scope Lock `completed/success` del SHA publicado → A4 readback → `A5_PENDIENTE` → `LISTO_DEV`. Un segundo Task distinto revisa A5. Este párrafo ilustra contrato; **no es prueba de haber ejecutado** esa cadena. La fila A:Z real debe registrar cada operación con su propia clave estable.

## 5. Casos de validación documental dirigidos

| Caso | Signo | Estímulo y oráculo verificable |
| --- | --- | --- |
| P01 | + | Un solo claim sobre PENDIENTE y gates verdaderos → H=EN_PROGRESO, dueño único y COLA E por fórmula |
| P02 | + | Documento en VALIDANDO con A0–A3 y Scope Lock exact SHA → A4/ A5_PENDIENTE y cierre DEV atribuible |
| P03 | + | Retry de lectura/reporte sin escritura nueva → no altera el dueño ni crea un segundo evento |
| N01 | - | `EVENT_ID` o `IDEMPOTENCY_KEY` duplicado → rechazar append semántico, conservar primero |
| N02 | - | Evento con 21–25 celdas en lugar de 26 → rechazar; nunca truncar A:Z |
| N03 | - | Task autora pretende A5 de su SHA → rechazar aun con Scope Lock SUCCESS |
| N04 | - | Una PENDIENTE tiene G=SI pero O=NO, o prerrequisito real falta → no reclamar |
| N05 | - | Otra Task posee EN_PROGRESO/VALIDANDO y no hay CAS/exclusividad de relevo → no sobrescribir |
| N06 | - | Check required in_progress/cancelled/skipped o head_sha diferente → no llamar A4_PASS |
| N07 | - | 26 campos pero RUN_ID sintetizado, timestamps falsos o secretos en evidencia → rechazar |
| N08 | - | Se intenta liberar M01.06/M06.06 o QA/main/PROD con VALIDANDO provisional o A5_FAIL → bloquear |
| N09 | - | Actor no pertenece al allowlist de seis Javier → no claim, revisión A5 ni rescate físico |

Estas filas son **casos de aceptación del contrato**; no representan tests MySQL/HTTP/runner ni validan un CAS que Google Sheets no proporciona automáticamente.

## 6. Impacto, pruebas y rollback

**Impacto:** únicamente este Markdown y entrada aditiva de `CHANGELOG_AI.md`; consumidores institucionales PLAN/COLA/DAG/BITACORA permanecen con fórmulas y datos sin cambios por este commit. No cambia API, DTO, EF Core, JWT, BCrypt, RBAC, tenant A/B, UI, procesos de CI ni servicios. Riesgos transversales: exclusividad, secreto en bitácora, fuente de autoridad y revisión cruzada.

**Auditorías aplicables:** A0 diff/Markdown/secretos y referencias físicas; A1 verificar P01–P03 y N01–N09 como oráculos (sin fabricar ejecución); A2 revisar JWT/RBAC/tenancy y datos sensibles, permisos por TASK_ID, A5 independiente; A3 comprobar contrato V5 con columnas y fórmulas reales y predecesor M01.06; A4 exige SHA FF exacto y check Scope Lock pertinente, evidencia causal y readback. `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`. A5 de otra Task Javier, sin promoción QA/main/PROD.

**Rollback:** un `git revert` aditivo del commit tras revalidar HEAD y conflictos; nunca reset/force-push, ni borrar eventos institucionales. Si se revierte, registrar nuevo evento idempotente explicando causa y SHA, sin modificar retrospectivamente BITACORA.
