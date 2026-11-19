# C14-04.M06.05 — Solicitud formal de revisión A5 independiente del lote

**Corte:** 18 de noviembre de 2026, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo de esta entrega:** R0, solo dossier de solicitud; NO cambia implementación, permisos, despliegue ni datos. **Autor de la solicitud:** DEV06 `6af16bbad26881918c283abe06e1f32a`.

## A0 — Objeto de la solicitud y alcance permitido

Se solicita a **una Task Javier de distinto TASK_ID que NO haya sido autor ni reparador de la unidad revisada** realizar su propia A5 sobre cada microtarea M06 de C14-04 que alcance estado LISTO con A0–A4 verdaderos. La solicitud persiste en Git y en el registro institucional; NO crea Tasks, NO ejecuta `run_now`, NO cambia RRULE, NO asigna artificialmente un revisor ni implica A5_PASS.

La solicitud de **auditoría de lote M06** no debe confundirse con el permiso para publicar una release. Los seis M05 anteriores tienen LISTO+A5, pero son controles específicos de readiness y pruebas, no la aceptación terminal C14-04.M06.06 ni una autorización R3.

## Candidatas y estado de corte

| Unidad M06 | Estado institucional al preparar la solicitud | Necesidad exacta de revisión |
| --- | --- | --- |
| [M06.01](https://github.com/solqaryn/Solqaryn/commit/8f28c76289c61c26154b2a7354db348c8cee0207) · autor DEV06 | LISTO_DEV R0; A5 independiente **pendiente** | Comprobar seis referencias M05 + A5 reales, matriz de pruebas reales contra mocks, no-garantías JWT/tenant/producción, procedencia y reversión; revisor distinto de DEV06 |
| [M06.02](https://github.com/solqaryn/Solqaryn/commit/822bfd0b1303a4c9104911667854003ae3fc49ee) · autor DEV06 | LISTO_DEV R0; A5 independiente **pendiente** | Verificar manifiesto siete SHA, rutas y diffs genuinos, logs CI por SHA, instrucciones reproducibles **no ejecutadas** aquí y ausencia de PASS falsos; revisor distinto de DEV06 |
| M06.03 · otro autor Javier | VALIDANDO al corte; todavía **no aprobable** | Inspeccionar riesgos abiertos, estados y CI, esperar solo su A0–A4 real y exclusión de escritor por ID; NO adelantar A5 |
| M06.04 · autor aún sin reclamar al corte | PENDIENTE al corte; ensayo de rollback todavía **no realizado** | Exigir prueba ejecutada aislada y adversarial de rollback sin pérdida de datos legítimos, negando falsos ensayos; descartar operaciones en producción |
| M06.05 · esta solicitud DEV06 | EN_PROGRESO al redactar; pasará a VALIDANDO/LISTO solo tras evidencia A0–A4 R0 | Revisor independiente de DEV06 debe verificar que solicitud es real, trazable y NO equivale a aceptar los otros scopes |
| M06.06 · cierre de lote | PENDIENTE, DAG `NO` mientras falte cualquier M06.01–05 + A5 verdadera | No habilitar ni marcar LISTO antes de cinco A5 y pruebas pertinentes; no escribir `SI` manualmente |

**Nota de consistencia temporal:** estos son datos de corte, no una reserva ni snapshot permanente. Para actuar se requiere releer `PLAN_MAESTRO` H/I/K/L, `COLA` G/O, `DAG_TECNICO` F/G y BITACORA A:Z por ID; otra Task puede avanzar legítimamente cualquiera de las unidades.

## A1 — Contrato de revisión A5 adversarial y falsable

El revisor independiente deberá abrir los commits, leer requisitos, diffs, archivos concretos y pruebas, y emitir **PASS o FAIL** con una hipótesis adversarial verificable:

- **A5 positiva:** evidencias originales y correctivas vinculadas al SHA o equivalente probado de blobs, check necesario `completed/success`, autor/reparador/revisor distintos, PLAN H=LISTO, BITACORA sin repetición del mismo `EVENT_ID`.
- **A5 negativa — mock:** un `page.route`, localStorage token o xUnit in-process no acredita JWT firmado, RBAC HTTP, aislamiento real entre tenants, SQL ni producción. Si se presenta como prueba productiva, emitir `A5_FAIL` con severidad y regresión; conservar historial.
- **A5 negativa — falsa CI:** `SKIPPED`, `CANCELLED`, `QUEUED`, `IN_PROGRESS` o éxito de compilación genérica no son pruebas del check requerido. No aprobar por una cantidad agregada de pruebas sin filtro/log.
- **A5 negativa — rollback:** llamar `git revert` sobre un repositorio sintético sólo demuestra ese entorno; no certifica rollback de migración, recuperación DB ni RPO/RTO. Si M06.04 no contiene ensayo pertinente y seguro, no aprobar.
- **A5 frontera — cambio de autor o dependencia:** si hay un nuevo commit que cambia código, pruebas o configuración causal, verificar equivalencia de blobs o revalidar. Si la fórmula DAG no refleja A5 real, comparar BITACORA/K antes de cualquier proyección auditada; no forzar puertas.

La firma del revisor debe documentar `TASK_ID` distinto del autor del cambio y del reparador, SHA comprobado, checks y readback, causa de PASS/FAIL y timestamp UTC en BITACORA idempotente. Un A5_FAIL bloqueante no se borra; sólo nueva corrección real y nueva revisión independiente permiten resolverlo.

## A2/A3 — Frontera de seguridad e integración

El diff de esta solicitud es texto R0 sin runtime, por lo que JWT/MySQL/Chromium y proveedores productivos son `NO_APLICA` a esta **modificación documental**. Esas mismas capacidades son `SIN_VERIFICAR/NO_MEDIDO` para un go-live real hasta probarlas según alcance y permisos. Un revisor A5 nunca puede usar este dossier como autorización R3, despliegue a QA/main/PROD, cambio DNS/secrets, restauración de datos reales o ejecución concurrente sobre otro scope.

La review necesita exclusión V17 por cada fila, CAS/readback y ausencia de escritor para ese ID; un owner histórico sin run activo no bloquea automáticamente revisar VALIDANDO, pero una ejecución nativa IN_PROGRESS escribiendo ese ID sí obliga a no intervenir.

## A4 — Publicación y seguimiento sin aprobación automática

La entrega R0 debe quedar en un commit fast-forward en `dev`; acreditar checks exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` y readback PLAN/BITACORA antes de marcar LISTO. Se registrará `A5_SOLICITADA`, **no** `A5_PASS`, para M06 pendientes. El pipeline V20 puede continuar mientras otro revisor autorizado elige un ID libre con su propio candado. El cierre terminal exige DAG y A5 real de las cinco M06 previas.

**Rollback:** `git revert` normal tras verificar HEAD, sin force-push ni reescritura histórica. Nunca borrar BITACORA; ningún cambio de producción.