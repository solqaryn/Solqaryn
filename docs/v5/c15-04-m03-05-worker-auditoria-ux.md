# C15-04.M03.05 — procesador acotado de observaciones UX (DEV)

## A0 / clasificación R2
Unidad real de código .NET 10 en Infrastructure, compatible con Domain/Application y con el adaptador M03.04. Procesa hasta 32 snapshots **sincrónicamente en memoria** y sólo entrega códigos de prevalidación. No crea scheduler, worker hosted, cola distribuida, proveedor nuevo, endpoint, base de datos, EF/MySQL ni datos live. Conserva PROJECT_SCOPE_LOCK=STRICT, únicamente solqaryn/Solqaryn@dev.

## Contrato de seguridad y calidad
- Contexto de lote: SHA hexadecimal exacto de 40 caracteres y tenant del servidor; todo elemento debe compartirlos. Se rechaza por completo el lote si mezcla tenant A/B o SHA, o tiene claves de idempotencia duplicadas o inseguras.
- Autoridad: se delega en `V5UxAuditIsolatedSnapshotAdapter` de M03.04 y su gate Application M03.03; membresía revocada, JWT no verificado o scope desconocido producen sólo códigos DENY. Sin uso de información del cliente como fuente de autoridad.
- Límites: 1–32 elementos, cancelación sin resultados parciales, captura defensiva del snapshot en adaptador. Salida: índice, código y cantidad; no contiene tenant, actor, SHA ni referencia de evidencia. Nunca certifica mediciones externas, A5, permisos, persistencia o producción.
- A1: nueve pruebas xUnit positivas, negativas, frontera 33, replay, tenant cruzado, SHA cruzado, revocación, errores, cancelación. A2: assertions no grants, no institutional writes y no JWT/tenant backend certificado. A3: llamadas reales Domain → Application → Infrastructure en .NET CI; no MySQL/HTTP/Browser pertinentes en este diff y se declaran NO_APLICA. A4: compilar/testear .NET 10 Release, checks seguridad/Scope Lock/V5 en SHA publicado y readback PLAN/COLA/BITACORA; ningún QUEUED/SKIPPED se cuenta PASS. A5: independiente por otra Task Javier.
- Rollback: `git revert` normal con recuperación aditiva; sin QA/main/PROD. Si CI continúa, estado VALIDANDO y reanudar REVIEW_FIRST por propietario.
