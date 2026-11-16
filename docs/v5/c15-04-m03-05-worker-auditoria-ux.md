# C15-04.M03.05 — procesador acotado de observaciones UX (DEV)

## A0 / clasificación R2
Unidad real de código .NET 10 en Infrastructure, compatible con Domain/Application y con el adaptador M03.04. Procesa hasta 32 snapshots **sincrónicamente en memoria** y sólo entrega códigos de prevalidación. No crea scheduler, worker hosted, cola distribuida, proveedor nuevo, endpoint, base de datos, EF/MySQL ni datos live. Conserva PROJECT_SCOPE_LOCK=STRICT, únicamente solqaryn/Solqaryn@dev.

## Contrato de seguridad y calidad
- Contexto de lote: SHA hexadecimal exacto de 40 caracteres y tenant del servidor; todo elemento debe compartirlos. Se rechaza por completo el lote si mezcla tenant A/B o SHA, o tiene claves de idempotencia duplicadas o inseguras.
- Autoridad: se delega en `V5UxAuditIsolatedSnapshotAdapter` de M03.04 y su gate Application M03.03; membresía revocada, JWT no verificado o scope desconocido producen sólo códigos DENY. Sin uso de información del cliente como fuente de autoridad.
- Límites: 1–32 elementos, cancelación sin resultados parciales, captura defensiva del snapshot en adaptador. Salida: índice, código y cantidad; no contiene tenant, actor, SHA ni referencia de evidencia. Nunca certifica mediciones externas, A5, permisos, persistencia o producción.
- A1: nueve pruebas xUnit positivas, negativas, frontera 33, replay, tenant cruzado, SHA cruzado, revocación, errores, cancelación. A2: assertions no grants, no institutional writes y no JWT/tenant backend certificado. A3: llamadas reales Domain → Application → Infrastructure en .NET CI; no MySQL/HTTP/Browser pertinentes en este diff y se declaran NO_APLICA. A4: compilar/testear .NET 10 Release, checks seguridad/Scope Lock/V5 en SHA publicado y readback PLAN/COLA/BITACORA; ningún QUEUED/SKIPPED se cuenta PASS. A5: independiente por otra Task Javier.
- Rollback: `git revert` normal con recuperación aditiva; sin QA/main/PROD. Si CI continúa, estado VALIDANDO y reanudar REVIEW_FIRST por propietario.

## Reparación causal V18 del A5_FAIL P2 (revalidación pendiente)
El `IReadOnlyCollection<Candidate>.Count` del llamante **no es autoridad**: la enumeración se hace en un único snapshot local con límite físico de 32; el elemento 33 ocasiona `BATCH_CAPACITY_DENIED` sin evaluación parcial. Se detectan cancelaciones antes y después de cada `MoveNext`; cualquier fallo del enumerador produce `INVALID_WORK_SNAPSHOT` sin exponer su mensaje ni resultados parciales. La política de confianza de tenant/SHA, las puertas Application y la prohibición de escritura permanecen intactas.

Se agregaron cuatro pruebas xUnit adversariales: colección que declara Count=1 pero entrega 33, colección que declara 999 y entrega 1, enumerador que falla después de un elemento y cancelación durante la enumeración. **Estos casos no se declaran PASS hasta GitHub Actions del SHA reparado** (Backend Release, seguridad, autorización, Scope Lock y procedencia V5). El A5_FAIL original se conserva; requiere un nuevo A5 de otra Task Javier, sin habilitación ficticia de DAG ni QA/main/PROD. Rollback por revert normal del commit de reparación.
