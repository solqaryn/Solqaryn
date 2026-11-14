# SOLQARYN V5 — C00-02.M06.05: solicitud institucional de revisión independiente del lote

**Objeto autorizado:** formular una solicitud **documentada y trazable** de A5 para entregas de C00-02 con evidencia DEV publicada y A0–A4 acreditadas, sin ejecutar otra Task, sin enviar llamadas entre automatizaciones ni atribuir aprobación al revisor. Esta microtarea no sustituye un caso positivo/negativo ni la certificación posterior de una Task Javier distinta. Tampoco libera M06.06 o QA/main/PROD.

**Fuentes y preflight:** V5 FINAL `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, PLAN_MAESTRO H/I, COLA G/O, DAG_TECNICO F/G y BITACORA A:Z. Las seis `C00-02.M05.01–06` están LISTO_DEV; `C00-02.M06.01–03` están LISTO_DEV al preflight de esta solicitud. HEAD base `4265a95f1bec08e2c2b0f9ca87410f0af3cf1088`. El estado real de A5 se relee antes de que cada revisor emita evento; este inventario no lo congela.

## Registro de solicitudes de revisión: sin asignar revisor ni falsificar A5_PASS

| Microtarea | Autor original / SHA causal | Solicitud de evidencia independiente y límites |
| --- | --- | --- |
| C00-02.M05.03 | PRIMARY DEV 01 (Javier :00); `cb2bd3254b0bf4aa8a9fa92cd115f36809f666a7` | Revisar selector + `V5WorkItemMemoryAdapter`, claim idempotente positivo, competidor/HEAD/DAG negativos, seguridad por scope. NO afirmar Sheets CAS, MySQL ni JWT live |
| C00-02.M05.04 | PRIMARY DEV 02 (Javier :10); origen `9214b5190152c4fb76a53b7737905ea9b74b028c` con blob probado en descendiente `bf3b370d1da783d016e5de2753032bdf44830d68` | Revisar positivo in-memory y negativa de atribución de A0–A4 por booleanos de fixture. Jobs CANCELLED del origen NO constituyen PASS |
| C00-02.M05.05 | PRIMARY DEV 02 (Javier :10); `bf3b370d1da783d016e5de2753032bdf44830d68` | Revisar positivo con binding confiable y negativos de actor retirado, tenant/scope ajeno, autocertificación A5; NO aceptar fixtures como JWT real |
| C00-02.M06.02 | PRIMARY DEV 02 (Javier :10); `1a44eeef8e545e67aef31c42a5044ea3d9a579b1` | Comparar seis commits/blobs del manifiesto con origen, detectar caso positivo/negativo de causalidad, verificar Scope Lock y procedencia SHA exacto |
| C00-02.M06.03 | PRIMARY DEV 04 (Javier :30); `4265a95f1bec08e2c2b0f9ca87410f0af3cf1088` | Auditar once riesgos y cinco decisiones documentadas; comprobar origen y que no certifica MySQL/JWT/Sheets CAS. **Prohibido A5 de la autora :30** |

Las entradas enumeradas se presentan como **candidatas de revisión**, no como tareas A5 nuevas ni como lista de PASS/FAIL. Antes de la evaluación, consultar BITACORA/PLAN actualizados: si ya existe A5_PASS verificado por SHA y reviewer distinto, **no duplicar eventos**, y si hay A5_FAIL preservar el fallo y exigir reparación causal previa. Las fuentes originales `docs/v5/c00-02-m06-01-test-sufficiency-audit.md`, `docs/v5/c00-02-m06-02-reproducibility-manifest.md` y `docs/v5/c00-02-m06-03-risks-decisions.md` identifican las evidencias. M05.01/.02/.06 y M06.01 ya tienen revisiones A5 independientes comunicadas en la Sheet; no re-solicitarlas sin leer estado vigente.

## Reglas de asignación y ejecución independiente

- El revisor debe ser **una de las seis Tasks Javier** autorizadas, con TASK_ID real diferente del **TASK_ID autor** de la unidad concreta; la cuenta GitHub común no sustituye identidad de Task. No consultar, activar, asignar ni aceptar cuentas retiradas.
- Esta solicitud queda **registrada en el documento y en BITACORA** por la Task autora; **no es notificación entregada** a una Task ni webhook, `run_now` o despacho entre cuentas. El revisor autorizado podrá descubrirla mediante lectura institucional de la cola/bitácora en su propia corrida y decidir tras revalidar permisos y SHA.
- Revisión A5 real requiere SHA de publicación/árbol, archivos/blobs y evidencias de A0–A4, un caso positivo y uno negativo significativo o equivalente, seguridad RBAC/JWT/tenant A/B cuando corresponda, secretos, rollback y causalidad de check-runs. Exigir `status=completed`, `conclusion=success`, `head_sha=SHA_EXACTO` para cada job realmente requerido. `SKIPPED`, `CANCELLED`, `queued` e `in_progress` no prueban PASS.
- Registrar A5_PASS o A5_FAIL sólo **tras** revisión auténtica por TASK_ID distinto, evento único de 26 campos A:Z, EVENT_ID e IDEMPOTENCY_KEY inéditos, RUN_ID del proveedor si existe y readback. El autor no autoaprueba ni edita histórico.
- No promover `qa`, `main`, PROD ni abrir PR/ramas. El cierre M06.06 exige prueba autónoma y M06.04 exige ensayo real no destructivo de rollback, ambos **pendientes** en esta solicitud.

## Aceptación de C00-02.M06.05

**Positivo P1:** solicitud con IDs y SHA identificables, autor de cada unidad, evidencia positiva/negativa requerida, criterio de asignación independiente y medio institucional de consulta sin ejecución remota. **Positivo P2:** preservación de A5_PENDIENTE en entregas propias hasta lectura posterior de A5 real, sin fabricar revisor.

**Negativo N1:** nunca A5_PASS por haber redactado solicitud. **Negativo N2:** no autorizar autovalidación ni usar Task fuera de seis Javier. **Negativo N3:** no hacer `run_now`, webhook o dispatcher; no crear nueva microtarea. **Negativo N4:** no convertir `CANCELLED` ni pruebas in-memory/fixtures a certificación de JWT/Sheets CAS/MySQL live. **Negativo N5:** no cerrar terminal M06.06 por esta solicitud ni declarar rollback ejecutado.

**Clasificación proporcional de esta entrega:** TIPO 1 documental/metadatos; `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`. `requiere_security=SI` por autoría, pertenencia de Task, separación de poderes, privacidad y tenancy; `requiere_scope_lock=SI`. A0 inspeccionar Markdown/diff/secrets/CHANGELOG; A1 casos P1/P2 N1-N5; A2 fronteras JWT/RBAC/tenant/allowlist y fail closed; A3 leer seis M05 y consumidores M06/bitácora; A4 publicar fast-forward en dev, inspeccionar blobs, diff, checks Scope Lock + V5 procedencia sobre exact SHA, readback PLAN/COLA/BITACORA. A5 posterior por otra Task Javier y **nunca** por la autora de esta entrega. Rollback normal mediante `git revert` del SHA propio, sin borrar historia ni afectar datos.
