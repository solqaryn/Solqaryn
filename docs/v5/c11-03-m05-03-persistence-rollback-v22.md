# C11-03.M05.03 — persistencia transaccional Outbox, V22

- PLAN_MAESTRO D1684: ejecutar integración de persistencia/proveedor cuando corresponda. E es objetivo acumulativo de M05, no una certificación global.
- Riesgo R2, prueba xUnit `Category=Integration` contra **Oracle EF Core + MySQL 8.4 efímero** en GitHub-hosted; nueva base aleatoria aislada, borrado en finally, sin tocar Aiven ni PROD.
- A1/A3: dentro de transacción de caller se persiste intención outbox y se hace rollback; nuevo contexto confirma cero filas. Una transacción posterior hace COMMIT y otro DbContext confirma durabilidad, replay con mismo EventoId y una sola fila. Transacción de tenant B con igual clave se revierte, preservando A; se comprueba aislamiento, ForbiddenAccess y conflicto ante payload distinto.
- A2: prueba aislamiento A/B físico; IUsuarioScopeService es fixture controlado, **no** acredita autenticación JWT, RBAC real, HTTP ni envío a proveedor. No hay cambios de runtime ni migraciones productivas. LIMITACION_FASE explícita.
- A4: GitHub Actions exact-SHA con compilación Release net10.0, servicio MySQL 8.4, TRX 1/1 y cero fallos/omitidas, Scope Lock; no declarar PASS hasta finalización real. A5 por una Task Javier independiente de DEV03.
- Rollback: `git revert` aditivo de los tres archivos nuevos por commit fast-forward; ningún force-push, secreto, pago, QA, main o PROD.
