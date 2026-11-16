# SOLQARYN V17/V15 - C07-04.M03.06 auditoria segura para revision comercial

**Microtarea:** C07-04.M03.06, PLAN_MAESTRO fila 1135, solqaryn/Solqaryn@dev, riesgo R2 por pagos, facturas, datos comerciales, auditoria y aislamiento multiempresa. PROJECT_SCOPE_LOCK=STRICT.

## A0 - alcance y arquitectura

Se implementa V5CommercialDocumentSafeAuditPipeline en Infrastructure como envoltorio de revision del procesador aislado V5CommercialDocumentIsolatedReviewBatch de M03.05. Obtiene autoridad actual de un puerto backend independiente, comprueba identidad, membresia, rol, revocacion, tenant del recurso A/B, correlacion interna y SHA; la revision se delega al lector Infrastructure M03.04 que vuelve a comprobar permiso y propiedad persistida. No modifica Domain, Application, DI, API, SQL o infraestructura externa, no inicia un worker alojado y no crea transaccion o cola durable.

Emite solo un evento ILogger estructurado con codigo fijo, correlacion interna validada, conteo acotado y estado fijo. No registra actor, tenant, documento, cliente, monto, divisa, llave de idempotencia, JWT, SHA ni excepcion. El resultado no otorga pago, factura, exportacion, permisos, mutaciones ni certificacion de auditoria durable. Autorizacion de proveedor o efecto fiscal requieren adaptador autenticado, transaccional y auditable independiente.

## A1/A2 - pruebas adversariales

Doce pruebas xUnit .NET 10 cubren nominal sin concesion, lote nulo/vacio/excesivo, ticket malformado, tenant cruzado, SHA distinto, membresia/rol/revocacion, inyeccion de correlacion, duplicado/replay, operacion fiscal, revocacion del lector, fallos opacos de autoridad/sink, cancelacion y frontera de 16. Comprobar Backend Release, backend build, Seguridad multiempresa, Authorization files/secrets/tenant, Scope Lock y V5 en SHA exacto antes de declarar LISTO. Los asserts negativos rechazan especificamente grants y fugas de claves.

## A3 - integracion pertinente

La composicion ejecutada en proceso Infrastructure -> batch M03.05 -> lector M03.04 -> gate Application -> reglas Domain utiliza dobles de fuentes del backend. NO_APLICA integracion live MySQL 8.4/EF/migraciones, HTTP JWT, PSP/fiscal, auditoria durable, Cloudflare y Playwright porque ninguno de esos consumidores, proveedores o runtimes cambia aqui. ILogger aceptando una llamada no garantiza que exista persistencia durable.

## A4/A5 y rollback

Exigir diferencias de cuatro archivos, comprobaciones GitHub Actions completed/success causales por SHA, Scope Lock, V5 y readback PLAN_MAESTRO/BITACORA. Despues de VALIDANDO y auditorias A0-A4 reales, LISTO_DEV conserva A5_PENDIENTE para otra Task Javier. NO QA/main/PROD. Revertir mediante commit git revert ordinario en dev, sin fuerza ni reset.
