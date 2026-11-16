# C13-04.M03.06 — Reparación P2 posterior a A5_FAIL independiente

Autor DEV06, TASK_ID 6af16bbad26881918c283abe06e1f32a. Revisor que descubrió el P2: DEV01, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Original commit 8ad8f0774a5d9aae15363781e18413a46cc2fba5. Rama dev, Scope Lock STRICT, PLAN fila 1999, V17 candado 2000001999. No se borra el A5_FAIL histórico.

## Problema y alcance
El recibo `Recorded=true` podía devolverse aun cuando `ILogger.IsEnabled(Information/Warning)` era false y el delegate `LoggerMessage.Define` omitía el evento. Un recibo así afirmaba auditoría inexistente. Riesgo R2 por auditoría/integridad y semántica de fallos. Reparación mínima: comprobar el nivel requerido antes de emitir, responder `AUDIT_SINK_DISABLED` y `Recorded=false` sin filtrar excepciones; el guard está en el `try` que opaca fallos del proveedor.

## Pruebas A0-A3
Se mantienen casos existentes de emisión, denegación, límites, concurrencia y errores. Se añaden casos adversariales IsEnabled false para Information y Warning, NullLogger y excepción al consultar IsEnabled (debe permanecer opaca). Sin modificaciones JWT, RBAC, tenant real, MySQL, API, browser, permisos ni producción. Test .NET 10 Release ejecutado por GitHub-hosted SHA-dirigido; no fingir éxito hasta comprobar logs y checks pertinentes. `Recorded=true` sigue representando envío a logger habilitado, **no persistencia duradera**: la interfaz ILogger no acredita entrega a almacenamiento externo.

## A4, A5 y rollback
Cerrar LISTO solo con diff causal, pruebas xUnit efectivamente ejecutadas >0, CI obligatoria exact SHA, Scope Lock y V5 SUCCESS y readback. Someter a NUEVA revisión A5 de otra Task Javier distinta al autor. No autoaprobar, no borrar defectos, no QA/main/PROD. Reversión git revert normal fast-forward sin rewrite.
