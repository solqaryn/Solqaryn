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

## V22 DEV05: prevenir reclamación duradera con tiempo UTC sin representación de reintento

Defecto causal R2: `RunTenantAsync` permitía `nowUtc` UTC cercano a `DateTime.MaxValue`. Tras reclamar y marcar un evento como `Procesando` en MySQL, un fallo del transporte activaba `nowUtc.Add(retryDelay)`, que lanzaba `ArgumentOutOfRangeException`. La reclamación quedaba duradera en `Procesando` sin ACK ni `RegistrarFallo`, hasta recuperar un claim stale. La corrección valida el intervalo UTC de reintento **antes de reclamar** tanto en el worker opt-in como en la fachada de auditoría; ningún secreto ni texto del proveedor se devuelve.

Prueba dirigida nueva en GitHub-hosted con Oracle EF + MySQL 8.4 efímero: insertar evento real; exigir rechazo de la ventana de desbordamiento tanto en worker como en fachada, `Intentos=0`, estado `Pendiente`, ningún envío ni reclamo; después ejecutar una ventana válida y confirmar entrega persistida. El workflow exige TRX **2/2**, sin `failed` ni `notExecuted`, además de Scope Lock y los checks R2 pertinentes exact-SHA. No se introduce DI, endpoint público, proveedor externo, JWT real, despliegue, pago, cambio de esquema ni consumidor persistente. Límite: **M06.06 permanece VALIDANDO** hasta demostrar la cadena global transport/HTTP/RBAC/tenant y A0-A4 y A5 independiente. `CHANGELOG_AI.md` se deja intacto para evitar truncar su histórico extenso; la evidencia aditiva se conserva aquí y en BITACORA. Rollback: `git revert` ordinario del commit causal, sin force-push.
