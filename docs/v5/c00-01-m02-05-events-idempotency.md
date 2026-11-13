# C00-01.M02.05 - Eventos, idempotencia, correlacion y limites

**Fuente:** V5 FINAL, `C00-01.M02.05`, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; estado `PLAN_MAESTRO!H`, `COLA!E/G/O` y `BITACORA!A:U` del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. Lectura de codigo desde `dev@3d43ce48fb8fadff7af428a74804885cf124d10c`.

## Alcance y limites de evidencia

La microtarea **define un contrato documental** para mecanismos ya presentes; no altera middleware, entidades, API, SQL, eventos enviados, jobs, JWT, RBAC ni politicas de infraestructura. La correspondencia se verifica contra rutas y hashes reales. No se interpreta ningun documento historico de VAEP/Jules como autoridad operativa. No se han ejecutado .NET, MySQL, HTTP, browser ni un proveedor externo en vivo para este contrato: `NO_RUNTIME_CERTIFICATION`.

## Matriz de mecanismos y fronteras comprobables

| Superficie real | Fuente Git blob en dev | Contrato observado | Limite / rechazo |
| --- | --- | --- | --- |
| Correlacion HTTP | `backend/src/API/Middleware/CorrelationIdMiddleware.cs` (`3b76dca41bb4bc0b44f89c2a4664c79907f9f242`) | Header `X-Correlation-ID`, maximo 64 caracteres seguros ASCII alfanumericos o `- _ . :`; asigna `HttpContext.Items`, `TraceIdentifier` y scope de log | Un valor malformado o largo se sustituye por GUID; header cliente NO autentica ni determina `EmpresaId` |
| Correlacion inventario | `backend/src/Application/Common/MovimientoInventarioCorrelationId.cs` (`d8dd439214874ed44ff2f7cb2845b159e09bba23`) | Origen tipado compra, venta, consumo o ajuste; identificador generado con GUID; el caller debe reutilizar uno por transaccion | `origenId <= 0` se rechaza; no confundir trazabilidad con permiso tenant o clave de idempotencia |
| Idempotencia de orden de compra | `backend/src/Domain/Entities/OrdenCompra.cs` (`ee8ae6ff044e40325fd49022ff77bc2ed1373967`) | `IdempotencyKey` no vacia y <=128; `IdempotencyFingerprint` SHA-256 de 64 caracteres; clave y huella se conservan juntas | No reemplazar clave/huella de una orden ya vinculada; la entidad por si sola NO demuestra unicidad SQL ni consumo HTTP transaccional |
| Idempotencia outbox | `backend/src/Domain/Entities/MensajeOutbox.cs` (`891572626c19cd6cf8f522fca5ec876333286ffb`) | `EmpresaId` y `ClaveIdempotencia` requeridos; limite declarado 200 para clave y 160 para `CorrelationId` opcional | `EmpresaId` no es autoridad de navegador, y no deben copiarse payload, clave o secretos a logs |
| Aplicacion outbox | `backend/src/Application/Services/MensajeOutboxService.cs` (`a00d353c474d7d7f785a58f443c66c5f12a542f7`) | Scope autorizado debe existir y `scope.EmpresaId == empresaId`; consulta duplicados por empresa y clave | Mismatch lanza `ForbiddenAccessException`; idempotencia no justifica enviar efecto externo fuera del tenant ni saltar persistencia |

## Contrato de eventos, retries y limites transversales

1. Una intencion con efecto externo debe preservar identidad durable tenant-bound, idempotencia y auditoria; `CorrelationId` ayuda a rastrear pero no valida identidad, firma ni autorizacion.
2. Una repeticion con la misma clave y payload equivalente debe resolverse sin duplicar efecto; el reuso con payload distinto requiere conflicto o rechazo. No declarar que cualquier ruta lo implementa sin pruebas dirigidas.
3. El mecanismo de Outbox requiere separar registrar intencion, reclamar, despachar, reintentar y dead-letter; cada transicion debe mantener `EmpresaId`, intentos esperados y observabilidad libre de PII/secretos.
4. Los datos `EmpresaId`, slug, host, header `X-Correlation-ID` y JWT no validado NO otorgan acceso. Una peticion de empresa A sobre empresa B debe fallar cerrado por backend con membresia vigente y RBAC.
5. Una clave de idempotencia no es un secreto de autenticacion. Evitar persistir valores sensibles en trazas; preservar el `IdempotencyFingerprint` y nunca asumir que un valor de header representa tenant valido.
6. Los errores de proveedor, carreras y reintentos necesitan limites configurados, backoff y estados terminales; un `SKIPPED`, `PENDING`, `FAILURE` o un check de otro SHA NO es PASS.
7. Este contrato no cambia migraciones, indice unico, provider, serializer ni programa de reintentos; una prueba de aislamiento MySQL/HTTP real queda pendiente de la microtarea de implementacion correspondiente.

## Salida y criterio de aceptacion

**Positivo documental:** la matriz identifica cinco fuentes reales con blob de 40 caracteres, limites, negativos, dependencias y rollback. **Negativos obligatorios:** rechazar elevacion de tenant por header, aceptacion de `origenId <= 0`, eliminacion de fingerprint, omision de `ForbiddenAccessException`, conversion de `SKIPPED` a PASS, ausencia de A5 independiente y rollback destructivo. La prueba Node 22 `node --check` + `node --test` dirigida solo valida la integridad de este documento, no la aplicacion real.

La salida individual exige PENDIENTE -> EN_PROGRESO -> VALIDANDO antes de A0-A3; A0 calidad estatica y secretos, A1 contrato funcional documental, A2 seguridad documental, A3 prueba Node dirigida, commit directo fast-forward dev y A4 sobre SHA exacto publicado con CI tecnica y readback PLAN/COLA/BITACORA. Solo entonces `LISTO_DEV_A0_A4_PASS;A5_PENDIENTE` para **otro TASK_ID**; `A5_PASS` independiente precede QA/main/PROD y tampoco autoriza desplegar. No declarar completo el lote M02 por este ID. Rollback reversible mediante `git revert` no destructivo tras verificar HEAD, preservando historia y evidencia apendicular; nunca `git reset --hard`, force-push ni borrado de BITACORA. Las imagenes historicas inexistentes siguen `NO_APLICA`.
