# V5 FINAL — C00-02.M01.03: lectura causal de contratos, código y evidencias

**Tipo:** documental (lectura e inventario), sin cambio de lógica ejecutable.  
**Responsable:** PRIMARY DEV 01 — JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`.  
**Fuente única de microtareas:** Google Doc [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) (2.340 IDs).  
**Repositorio:** `solqaryn/Solqaryn`, únicamente `dev`; HEAD de inspección `e59691ec83e31b2a00eb440555c9f21ffea0e56b`. **No es certificación de las demás microtareas ni de QA/PROD.**

## Propósito y resultado exigido

Leer contratos, código y evidencia antes de declarar cerrado el modelo de trabajo de C00-02. La salida de M01 es definir **cola, dependencias, autoría, estados y cierre sin duplicados**, mediante un contrato de estados y al menos un ejemplo rastreable. No introducir un motor paralelo de claims, PR históricos, ni convertir un resultado documental en certificación de funcionalidades aún no probadas.

## Matriz de fuentes verificadas

| Fuente | Identificador verificable | Autoridad sobre |
| --- | --- | --- |
| V5 FINAL, Google Doc | `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` | Identidades comerciales, `RESULTADO_EXIGIDO`, fases y precedencias |
| PLAN_MAESTRO, Google Sheet | `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, columna H | Estado autoritativo editable por ID |
| COLA, misma Sheet | columnas E, G, I, O; vistas P/Q | Estado reflejado, dependencias calculadas, custodio y elegibilidad; **no editar manualmente A:Q** |
| BITACORA, misma Sheet | **A:Z (26 columnas)**; EVENT_ID en A, RUN_ID en H, IDEMPOTENCY_KEY en R y TIMESTAMP_UTC_ORIGINAL en Z | Evidencia, autoría verificable y deduplicación de transiciones |
| `AGENTS.md` | blob Git `d7d6b2bd4abd015a332ee4a88c31a65eb2928c12` | Autoridad operativa y arquitectura del agente |
| `docs/PROJECT_SCOPE_LOCK.md` | blob Git `a92ae64d46a82b5255919012b474f7ece430ed6b` | Scope Lock/identidad y límites |
| `docs/AUTOMATION_V13.md` | blob Git `008837173c78a9b371f3490ad4d35f7f8da5634b` | A0–A5, secuencia y gates dev |
| `docs/AUTOMATION_AUTHORITY.md` | blob Git `21f8f15621f235a5677b389f3785e07a8443c4c8` | Autorización y separación de agentes |
| `.agents/skills/solqaryn-project-governance/SKILL.md` | blob Git `304e400d5c0f89fcfdcc65caece86c34276734b5` | Governance y arquitectura validada contra HEAD |

Los hashes anteriores son **SHA de blobs**, no SHA de checks CI. El check `verify-solqaryn-project-scope` y los demás controles deben corroborarse para el **SHA de commit realmente publicado**. Un check de un commit anterior no sustituye el del nuevo commit.

## Fórmulas y prueba observada de elegibilidad (fila COLA 40)

- `COLA!E40 = IF($A40="","",PLAN_MAESTRO!H40)`: espejo de `PLAN_MAESTRO!H40`, no valor manual.
- `COLA!G40` obtiene `DEPENDENCIAS_OK` mediante `VLOOKUP` contra `DAG_TECNICO` (y fallback condicionado); la lectura del 13-nov-2026 devolvió `SI`.
- `COLA!O40 = IF($A40="","",IF(AND($E40="PENDIENTE",$I40="",$G40="SI"),"SI","NO"))`: `SI` sólo con estado pendiente, sin custodio y dependencia OK.
- `COLA!Q40=NO` pertenece a otra puerta de fase (P40=`C09-03`); no debe reinterpretarse como veto global si la precedencia aplicable a este ID en V5/DAG permite preparación M01.
- El `TASK_ID` del autor y la hora se conservan en PLAN, se reflejan en COLA y quedan auditados en BITACORA. Toda edición concurrente exige relectura antes de cada transición.

## Máquina de estados y contrato contra duplicados

1. **PENDIENTE:** sólo se selecciona si `COLA!G=SI`, `O=SI`, sin dueño y sin conflicto de otro Scope. La selección no constituye PASS.
2. **EN_PROGRESO:** claim idempotente sobre un único ID, custodio y hora en PLAN; COLA devuelve el mismo responsable. No bloquear filas independientes.
3. **VALIDANDO:** únicamente con candidato documental/código completo y fingerprint; registrar en BITACORA **antes** de A0–A3. No liberar autoría.
4. **LISTO_DEV:** requiere A0 calidad estática, A1 funcional, A2 seguridad, A3 integración y A4 posintegración realmente aplicables. Subpruebas de tecnología no afectada se dejan `NO_APLICA` con motivo y no se presentan como tests ejecutados.
5. **A5:** un TASK_ID **distinto del autor** revisa el SHA/evidencia en otra operación; `A5_PENDIENTE` no permite promoción a `qa/main/PROD`. A5_FAIL conserva la trazabilidad y bloquea la promoción.

**Invariantes:** `PLAN_MAESTRO!H` es la autoridad; `COLA!E` es fórmula; cada evento nuevo debe mapear las **26 columnas A:Z** y tener `EVENT_ID` e `IDEMPOTENCY_KEY` únicos; RUN_ID debe quedar vacío si no existe una ejecución nativa acreditable; ningún claim cambia permisos, tokens, ramas o cuentas. Fallos de un job no aplicable no se maquillan como PASS, y los gates obligatorios no se omiten.

## Ejemplo real de ciclo (no plantilla ficticia)

`C00-01.M05.01` fue reclamado por `6adbb9fff1548191a1bd5b30d9e9984a`, pasó a VALIDANDO y publicó el commit `1de966000059a53ace3faddbb1db53478cf800ff`. La evidencia dirigida incluye 3/3 pruebas unitarias Angular en workflow Priority4, regresión M10 Playwright 12/12 y Scope Lock SUCCESS sobre el mismo SHA. Sólo tras revisar esa evidencia, en la lectura de PLAN/COLA se certificó `LISTO_DEV`, con A5 pendiente de otra Task. Eventos idempotentes comprobados en BITACORA: `SQ-V5-C00-01-M05-01-J00-A4-1DE96600-20261113`, `...-A5P-...` y `...-LISTO-...` (eventos históricos cuya longitud física debe auditarse individualmente; **no se presumen 26 campos ni se reescriben retroactivamente**). Es evidencia **de ese ID**, no aprobación de todo M05.

**Escenario negativo — exclusión:** `C00-01.M05.03` estaba `VALIDANDO` bajo otra Task; cualquier nuevo claim de ese ID debe rechazarse sin reescribir dueño, estado o eventos.  
**Escenario negativo — CI:** jobs `SKIPPED` no cuentan como PASS cuando su prueba es necesaria; un SHA anterior tampoco certifica un candidato posterior.  
**Escenario negativo — idempotencia:** si el EVENT_ID ya está en BITACORA, leerlo en lugar de añadirlo por segunda vez.

## Corrección de contrato A:Z/26 tras A5_FAIL independiente — 2026-11-13

El readback de `BITACORA!A1:Z1` confirmó **26 encabezados físicos**, no A:U/21. La revisión independiente A5 del SHA documental original `81141057ceafea341df2a8b646ba93f79b63df20` detectó la contradicción y su A5_FAIL **se conserva**. Este commit corrige exclusivamente la documentación; no declara A5_PASS, no reabre una microtarea LISTO ni autoriza QA/main/PROD.

**Esquema actual por posición:** A EVENT_ID; B FECHA_HORA_TEGUCIGALPA; C CONTRATO_VERSION; D ACTOR_HUMANO; E TASK_ROLE; F ACCOUNT_OWNER; G TASK_ID; H RUN_ID; I REVIEWED_RUN_ID; J ACTOR_AUTOMATIZADO; K EJECUTOR_RESPONSABLE; L REVISOR_INDEPENDIENTE; M SCOPE_KEY; N MICROTAREA_ID; O OPERACION; P ESTADO_ANTERIOR; Q ESTADO_NUEVO; R IDEMPOTENCY_KEY; S BASE_HEAD; T RESULT_HEAD; U AUTORIZACION; V RESULTADO; W EVIDENCIA; X ERROR; Y READBACK; Z TIMESTAMP_UTC_ORIGINAL.

**Caso positivo:** evento nuevo con 26 posiciones en orden, A y R únicos, fuente y autor verificados, Z UTC válido y readback posterior. H permanece vacío si no existe RUN_ID nativo comprobado.

**Negativos obligatorios:** rechazar evento de 21 campos como si fuera completo, duplicados de A o R, reutilización de identidad/revisor ajenos, A5_PASS sin TASK_ID revisor distinto y evidencia real, SHA anterior usado como PASS de candidato nuevo y creación de filas vacías. Los registros históricos de 21 posiciones, si existen, quedan preservados para reconciliación auditada; no se completan inventando datos ni se borra historial.

**Alcance y validaciones:** TIPO1 documental. requires_dotnet=NO, requires_node_angular=NO, requires_mysql=NO, requires_docker=NO, requires_e2e=NO. requires_security=SI (identidad, idempotencia, A5, límites), requires_scope_lock=SI para el SHA documental nuevo. A0–A3: diff/Markdown, fuentes y readback físico A:Z, casos positivos/negativos anteriores; A4: publicación fast-forward, SHA, Scope Lock, blobs y readback. Se necesita **nueva A5 independiente** sobre el SHA corregido. Rollback: git revert normal del commit correctivo, nunca force-push.

## Impacto, riesgos y A0–A5 para esta entrega

La única modificación prevista es esta ficha + entrada aditiva en `CHANGELOG_AI.md`. **C#/.NET, Angular/Node, SQL/MySQL, Docker, browser y proveedores: NO_APLICA** como validaciones de cambio de runtime (no se modifica ese código). Sí son obligatorios revisión Markdown y contratos/celdas fuente, ausencia de secretos, coherencia V5/V13, SHA exacto de commit documental, Scope Lock aplicable y readback PLAN/COLA/BITACORA. No se han modificado fórmulas, datos de clientes, permisos, configuración de Tasks ni despliegues.

**Rollback:** `git revert` del commit documental tras verificar HEAD, sin reset/force-push; cualquier reversión institucional se registra con eventos compensatorios, nunca se borra BITACORA. La revisión independiente A5 corresponde a otro TASK_ID. No se considera aprobado un ciclo V5 entero por este inventario.
