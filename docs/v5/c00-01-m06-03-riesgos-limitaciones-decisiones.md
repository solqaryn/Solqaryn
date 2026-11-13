# V5 FINAL — C00-01.M06.03: registro de limitaciones, riesgos y decisiones

**Responsable del inventario:** PRIMARY DEV 01 — JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`; GitHub `jmejia31`. **Registro firmado por identidad operacional y SHA de commit**, no por firma criptográfica.  
**Fuente:** [V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); [PLAN_MAESTRO, COLA y BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Alcance `C00-01.M06.03`, **registro documental de preparación M06**, no liberación de M06 ni de la fase siguiente.  
**HEAD de inspección**: `171be4088884550d547ee5085b683f6f900a428c`. Snapshot de estados sujeto a avance concurrente: repetir readback antes de decidir promoción.

## Inventario con identificadores y huellas

| Elemento | Referencia verificada | Riesgo que delimita |
| --- | --- | --- |
| Plan V5 (2.340 microtareas) | Google Doc ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` | Una sola cola institucional, ningún ID 2341 ni V13 histórico como backlog |
| PLAN, COLA, BITACORA | Sheet ID `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` | PLAN H autoridad, COLA E fórmula y G/O elegibilidad, **BITACORA A:Z/26 columnas** con EVENT_ID A e IDEMPOTENCY_KEY R |
| Gobierno de proyecto | `.agents/skills/solqaryn-project-governance/SKILL.md` blob `304e400d5c0f89fcfdcc65caece86c34276734b5` | PROJECT_SCOPE_LOCK=STRICT; ramas, autoría y fuentes |
| Protocolo A0–A5 | `docs/AUTOMATION_V13.md` blob `008837173c78a9b371f3490ad4d35f7f8da5634b` | PASS proporcional, A5 independiente, no promoción automática |
| M05.01 corrección de contexto Angular | commit `1de966000059a53ace3faddbb1db53478cf800ff`; AppComponent blob `c73f74717e9480d913b16efea6227ea358d24cba` | Prevención de callback tardío entre contextos de ruta |
| M04.03 original branding | commit `fb20c55ed4f9a9cb09f18c9b81d3d97d84b08605` | A5_FAIL previo debe revalidarse por revisor ajeno, aunque exista corrección posterior |
| Fuente de precedencia | `DAG_TECNICO` + `COLA!G/O` por fila | Evitar falsas habilitaciones de fases ajenas y claims simultáneos |
| Referencia de contrato | `docs/v5/c00-02-m01-03-fuentes-contratos-evidencias.md` blob `58f0fc4ae93079fe96124066da8089b93e27016b` | Estados y negativas reales, sin simular revisión de otras Tasks |

## Riesgos abiertos y decisiones

| Riesgo | Estado causal observado | Decisión y dueño de cierre |
| --- | --- | --- |
| R1 — Cobertura real de aceptación M05 | `C00-01.M05.04` VALIDANDO al readback; no se debe suplir E2E requerido con descripción | Mantener esa unidad a cargo de su autor, exigir prueba de aceptación y A4 de su SHA antes de LISTO |
| R2 — Revisión original M04.03 | A5_FAIL independiente sobre el SHA original `fb20c55e` por posible callback de tema/SEO desfasado; corrección posterior `1de96600` no convierte automáticamente A5_FAIL en PASS original | Solicitar nueva A5 independiente del componente/corrección, identificar SHA revisado y conservar ambas evidencias |
| R3 — Fallos strict build de otros workflows | GitHub Actions N1.1/N1.2 `-warnaserror` detectaron `CS8604` en `PlataformaEmpresasController.cs(335)` y `xUnit2012/xUnit2013` en pruebas; no son PASS global | Asignar corrección separada bajo V5/scope autorizado. No alterar C# dentro de una tarea documental ni promover QA/PROD con gate obligatorio fallido |
| R4 — Advertencia de presupuesto UI | CI Angular reportó `storefront-producto.component.scss` ~17.59 KiB contra presupuesto warning 16 KiB; no superó error 32 KiB | Deuda visual/performance para revisión proporcional; no falsear fallo ni certificar SLA real |
| R5 — Advertencias TestServer | `C00-01.M05.02` registra advertencias `ASPDEPR004` y `ASPDEPR008` del host de test | Modernizar sólo bajo Scope propio con pruebas pertinentes; no dar por fallo de seguridad si job causal aprobó |
| R6 — Rendimiento fuera de producción | Playwright Fase11 comprueba localmente LCP≤4000ms, CLS≤0.15, INP≤500ms en fixture/ruta de CI; no mide latencia real de Vercel/Render/Aiven | Registrar PASS de CI local y **no** afirmar métricas ni SLO de PROD |
| R7 — Concurrencia y bitácora | Otras Tasks avanzan HEAD y trabajan IDs propios; PLAN/COLA pueden cambiar entre lecturas | Releer HEAD, owner, estados, fórmulas y EVENT_ID antes de cada escritura; nunca sobrescribir actividad ajena |
| R8 — Permisos y disponibilidad de Scheduled Tasks | Task Javier puede estar habilitada pero `is_self_owned_automation=false` en contexto consultante; `requires_user_action` no equivale a aprobación | Exigir identidad GitHub/Drive y Task allowlist positiva; una reanudación no suplanta permisos humanos. No tocar Alex desde Javier |

## Matriz de contradicciones: tratamiento no destructivo

| Lecturas potencialmente contradictorias | Resolución autorizada |
| --- | --- |
| `COLA!E` manual frente a `PLAN_MAESTRO!H` | PLAN H es autoridad; restaurar fórmula E únicamente con evidencia y nota auditada, nunca declarar LISTO por E manual |
| V13 histórico como 300+ pendientes frente a 2.340 V5 | Sólo V5 FINAL suministra IDs y `RESULTADO_EXIGIDO`; V13 conserva gobierno A0–A5 |
| `COLA!Q=NO` de fase no relacionada frente a `G/O=SI` con DAG de ID | Respetar precedencia real microtarea y puertas pertinentes, no extrapolar gate de otro bloque |
| CI verde de HEAD anterior frente a nuevo commit | Exigir SHA exacto o equivalencia de blobs y gates proporcionales demostrada; SKIPPED no es PASS |
| M05.01 LISTO y A5_PASS frente a M04.03 A5_FAIL original | Mantener eventos independientes por SHA; revisión M04.03 debe acreditar corrección explícitamente |
| M05 todavía con VALIDANDO frente a M06.03 G/O=SI | Se permite **documentar riesgos** M06.03 ahora, pero no se permite liberar M06.06 o la fase siguiente prematuramente |
| Ausencia local de SDK frente a Actions con .NET/Angular/MySQL | Usar runners GHA reales con jobs, logs y SHA, no elevar ausencia local a bloqueo global |
| Pauta de no autopausar frente a Requires User Action | Mantener programación; no forzar reactivación sin permisos o nueva pausa explícita vigente |

## Enmienda causal A5_FAIL — bitácora física A:Z/26 (13 de noviembre de 2026)

Revisión independiente Javier :50 detectó que la fuente original de este registro describía **21 campos** cuando el encabezado físico vigente en `BITACORA!A1:Z1` contiene **26**. Se preservan el commit original `66b640fbd7000f1e62e4c881208a9077d006bd44`, el resultado A5_FAIL y los eventos históricos. Esta corrección documental no equivale a A5_PASS y no autoriza promoción a qa/main/PROD.

**Contrato de evento completo:** A=EVENT_ID, B=FECHA_HORA_TEGUCIGALPA, C=CONTRATO_VERSION, D=ACTOR_HUMANO, E=TASK_ROLE, F=ACCOUNT_OWNER, G=TASK_ID, H=RUN_ID, I=REVIEWED_RUN_ID, J=ACTOR_AUTOMATIZADO, K=EJECUTOR_RESPONSABLE, L=REVISOR_INDEPENDIENTE, M=SCOPE_KEY, N=MICROTAREA_ID, O=OPERACION, P=ESTADO_ANTERIOR, Q=ESTADO_NUEVO, R=IDEMPOTENCY_KEY, S=BASE_HEAD, T=RESULT_HEAD, U=AUTORIZACION, V=RESULTADO, W=EVIDENCIA, X=ERROR, Y=READBACK, Z=TIMESTAMP_UTC_ORIGINAL. H queda vacío sin RUN_ID nativo comprobado. Toda nueva escritura exige ambas claves A/R únicas y readback contiguo de **26 campos**, sin filas artificiales.

**Casos positivos/negativos reproducibles en revisión de esquema:** aceptar sólo asignación ordenada de las 26 columnas con fuente/autoría probadas; rechazar 21 valores presentados como evento completo, duplicación de EVENT_ID o IDEMPOTENCY_KEY, revisor A5 igual al autor, RUN_ID inventado y cambios retroactivos a eventos anteriores. Si hay registros antiguos incompletos, diagnosticar y reconciliar sin borrar ni rellenar con datos ficticios.

**Impacto:** sólo documento de riesgos y CHANGELOG; requires_dotnet=NO, requires_node_angular=NO, requires_mysql=NO, requires_docker=NO, requires_e2e=NO; requires_security=SI (autoría, evento/A5, multitenant y permisos), requires_scope_lock=SI en SHA correctivo. A0–A3 aplicables: revisión de Markdown, esquema A:Z, fuentes V5, negativos y ausencia de secretos/alteraciones runtime. A4: fast-forward, blobs, Scope Lock exact-SHA, readback PLAN/COLA/BITACORA. Solicitar nueva revisión A5 independiente del SHA corregido; nunca falsificar PASS. Rollback por `git revert` normal, sin force-push.

## Condición de salida y auditoría proporcional

Este registro presenta inventario identificable, hashes de fuentes, riesgos y decisiones con dueño técnico y **no modifica runtime**. A0: revisar diff Markdown, secretos y no-reintroducción de controles obsoletos. A1: verificar que cada riesgo tiene causa y acción verificable, sin falso cierre de M05/M06. A2: preservar seguridad multitenant, identidad, restricciones de despliegue y datos. A3: comparar estados y hashes de PLAN/COLA/GitHub; .NET, Angular, MySQL, Docker y navegador **NO_APLICA como pruebas de este commit documental**. A4: commit directo fast-forward, Scope Lock del SHA publicado, lectura de PLAN/COLA/BITACORA y rollback.

**Pendientes antes de liberar M06:** completar M05 restante con pruebas reales, resolver A5 críticos en sus SHA, verificar gates correctos, ensayar rollback en su microtarea autorizada y certificar cierres por separado. Esta ficha **no** ejecuta `C00-01.M06.04`, `.05` ni `.06`. A5 de esta ficha queda para otro TASK_ID. No QA/main/PROD.

**Rollback documental:** `git revert` del SHA de esta ficha con HEAD revalidado; registrar compensaciones institucionales como nuevos EVENT_ID (no borrar historial).
