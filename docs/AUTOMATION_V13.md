# SOLQARYN — V13: CAPACIDAD 1000/1000/1000/1000 Y MULTICIERRE FIFO
CAPACITY_POLICY_VERSION=V13_1000_FIFO_BATCH
MAX_CONCURRENT_SCOPES=1000
MAX_OPEN_WORK_BRANCHES=1000
MAX_OWNED_SCOPES_PER_TASK=1000
MAX_ACTIVE_WRITES_PER_TASK=1000

**Vigencia:** candidato exclusivo en PR #3932; la nueva capacidad entra en vigor solo tras revision formal independiente del HEAD final, checks requeridos, merge protegido a dev y sincronizacion institucional con readback. Mientras #3932 no este fusionado, no operar como si 1000 ya estuviera vigente.

## Autoridad y limites
- PROJECT_SCOPE_LOCK=STRICT, solqaryn/Solqaryn, rama dev, ocho Task IDs bajo autorización protegida de 2026-11-09 (seis Javier, dos Alex); solo Plan Comercial Inicial de Drive. No reactivar VAEP/Jules ni Tasks retiradas; no crear otras Tasks sin aprobación expresa y readback. Preservar horarios e identidades anteriores, QA/main/PROD, seguridad y rulesets.
- Los cuatro maximos de 1000 son techos, no cuotas: scopes EN_PROGRESO + VALIDANDO distintos, ramas de trabajo abiertas, scopes por Task y escrituras activas por Task. Nunca contar duplicado un scope ni refs historicas cerradas. Conteo sin lectura real => NO_GO para nuevo trabajo. La capacidad no concede permisos ni autoriza escritores simultaneos del mismo scope.
- Prohibidos CAS de Google Sheets, leases, bloqueos persistentes de fila, asignaciones anticipadas, cambios destructivos de historia y force-push. Detectar conflictos reales de editor/autor con GitHub, PLAN_MAESTRO, COLA y BITACORA en el momento de ejecucion.

## Desarrollo habilitado y custodia
- PENDIENTE de plan P/I8/G0 con COLA!O=SI, responsable libre y predecesoras VALIDANDO o LISTO puede desarrollarse inmediatamente, sin esperar CI ajena ni vaciar VALIDANDO. COLA!G=SI exige todas las predecesoras LISTO y es obligatorio para merge y LISTO; no modificar ni falsear formulas.
- Antes del trabajo real registrar EN_PROGRESO, responsable/TASK_ID autentico, rama unica y evento idempotente BITACORA; hacer readback de formulas de COLA. Al completar codigo, prueba causal y PR original, registrar VALIDANDO, liberar al autor de la custodia, preservar provenance y dejar CI trabajando asincronamente. Si no termino trabajo real, conservar EN_PROGRESO verificable.
- CHANGELOG_AI.md es historial, NO limite cuantitativo 1000. OTHER_CHECKS_PENDING es un gate de CI sobre checks relevantes, NO contador WIP. Los check runs en curso se registran y se reevalúan en otra ocurrencia; nada de polling pasivo que agote una Task.

## FIFO estricto y descarga de multiples LISTO por una sola Task
- En cada ocurrencia, primero conciliar los PR ya MERGED pendientes de LISTO, conservando la secuencia y los gates postmerge. Despues ordenar TODOS los PR OPEN a dev por created_at y, en empate, numero. Nunca adelantar PR posterior por tener CI verde si el primero de la cola esta bloqueado.
- La Task puede resolver tantos cierres ajenos consecutivos como permita su ejecucion real (1, 3, 5, 10, 50 o mas) **sin tope de 1 LISTO por corrida**. Sin embargo cada cierre se valida y ejecuta por separado, releyendo PR, nuevo dev, SHA, review, CI, protecciones, dependencias, PLAN/COLA/BITACORA, y comprobando readback idempotente. Un merge puede ensuciar los siguientes PR: no autorizar merges en lote desde una foto vieja.
- **Barrera del autor:** si el siguiente PR/cierre en FIFO fue desarrollado por el mismo AUTHOR_TASK_ID que la Task actual, parar inmediatamente todos los cierres de ese turno, incluido cualquier PR posterior. No revisar, cerrar ni aprobar la propia entrega. Ejemplo PR1/PR2 de otros, PR3 propio, PR4 de otro => solo PR1 y PR2 pueden quedar LISTO, después desarrollar una PENDIENTE libre.
- Si el siguiente PR ajeno esta bloqueado por CI pendiente/fallida, G=NO, conflicto, HEAD stale, revision faltante, proteccion o editor activo, nunca saltarlo. Diagnosticar una vez, reparar solo el scope autorizado y sin force-push cuando sea seguro; si no, tomar una PENDIENTE libre O=SI. Desarrollar esa microtarea y entregarla VALIDANDO realmente; si sigue en curso, preservar EN_PROGRESO.

## Gates no negociables de cada cierre
- Prueba causal y regresion proporcional, seguridad multitenant/RBAC, revision tecnica de TASK_ID diferente al autor en HEAD final, y review formal independiente para gobierno/workflows/seguridad cuando lo exija el proveedor.
- PR CLEAN y protecciones vigentes, Scope Lock, Seguridad multiempresa y Build backend requeridos en SUCCESS exact SHA, Directed comercial aplicable SUCCESS; COLA!G=SI para merge. SKIPPED, PENDING, FAILED, CANCELLED y OTHER_CHECKS_PENDING requeridos NUNCA son PASS.
- Fusion protegida de uno en uno; verificar checks POSTMERGE requeridos en MERGE_SHA cuando corresponda, marcar LISTO unicamente con PLAN_MAESTRO/COLA/BITACORA readback y evento idempotente. reviewerFor y developerFor son preferencias; no reservas ni permiso para autoaprobar usando otra cuenta.
- Conservar la regla de Scope Lock sobre CI opcional/relevante, el no-bypass ante fallo causal, la prohibicion de merge fuera FIFO, el no-CAS y el alcance exclusivo dev.

## Implantacion y rollback
- Crear este contrato exclusivamente dentro del PR abierto #3932, preservando su test P2-017.D. No reparar conflictos del PR ni los posteriores hasta terminar la implantacion normativa, codigo y pruebas.
- Sincronizar CONFIG, TAREAS_PROGRAMADAS, AUTOMATIZACIONES y ocho prompts existentes por sus cuentas propietarias sin cambiar identidades, RRULE ni estado habilitado; comprobar readback por Task. Las dos Tasks de Alex requieren acceso propio; NO afirmar 6/6 si solo existen 4/4 accesibles.
- BITACORA se dimensiona para miles de eventos sin borrar historia. Despues del merge protegido y readback efectivo, reconciliar el mismo PR y PRs posteriores en ramas originales con tests y CHANGELOG aditivos, sin PR auxiliares, reescritura ni autoaprobacion. Rollback protegido y documentado, nunca ocultar eventos.

## Adenda actual 2026-11-09 — 6 Javier + 2 Alex, V13 vigente sin alterar cuatro techos
Task Javier nueva :10 ID 6af16bb9492c8191b8e8c7d606f11507 y nueva :50 ID 6af16bbad26881918c283abe06e1f32a, distintas de las Tasks Alex :10/:50. La nueva autorización protegida entra en vigor sólo tras review independiente, required CI exact-HEAD, merge a dev y readback del Sheet. Las dos nuevas figuran HABILITADAS físicamente según readback nativo y Sheets (is_enabled=true), pero mantienen NO_GO comercial hasta merge protegido y readback institucional. Su misión posterior es idéntica a las otras cuatro Javier: recuperar exclusivamente los 52 PR VALIDANDO originales por FIFO, sin nuevos scopes, falsa aprobación ni falsos LISTO. No modificar las Tasks Alex, sus permisos, ni QA/main/PROD.

