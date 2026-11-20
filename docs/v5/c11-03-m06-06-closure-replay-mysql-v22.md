# C11-03.M06.06 — rescate V22 de cierre de Outbox

**Riesgo R2; alcance de la acción PLAN D1693:** cerrar estado y liberar lote únicamente con evidencia suficiente. No se ejecuta liberación artificial.

## Incremento verificable (solo dev)
- Se añade test xUnit `C1103M0606ReplayOrderMysql84Tests` contra Oracle EF y MySQL 8.4 efímero, con una base aleatoria y eliminación al finalizar. Simula transporte que recibe un mensaje y devuelve timeout ambiguo, reintento posterior con la misma clave, ejecución ordenada del segundo evento A, exclusión completa de B, CAS de ACK stale y protección de datos del error persistido.
- El transporte es **simulado** y su deduplicación es una propiedad del destinatario simulado; **NO** acredita un proveedor real, webhook comercial ni entrega exactly-once del runtime.
- CI GitHub-hosted dirigida requiere `net10.0` Release, MySQL físico 8.4, TRX exactamente 1/1 aprobado, 0 failed, 0 notExecuted, Scope Lock exact-SHA. Un job en curso o SKIPPED no es PASS.
- Predecesoras M05.01–06, M06.01–05 y C10-04.M06.06 constan LISTO+A5 por evidencia del plan, pero esto **no reemplaza** la acreditación acumulativa del outbox productivo.

## LIMITACION_FASE y puerta de cierre
- No se certifica aún JWT/RBAC real por tenant, la cadena HTTP/worker/proveedor real, deduplicación persistida en el consumidor, monitorización SLO, firma/revocación, ni la liberación de la siguiente fase.
- `PLAN H=VALIDANDO` hasta superar A0–A4 causales de cierre, integraciones pertinentes y checks requeridos exact-SHA. A5 por otra Task Javier distinta del autor. No QA/main/PROD, pagos, secretos o infraestructura.
- Rollback mediante commit `git revert` no destructivo del changeset aditivo, preservando BITACORA y cambios concurrentes.
