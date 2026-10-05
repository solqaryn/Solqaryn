# Punto 9 — Contrato funcional de duplicate key 1062

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — la duplicidad esperada conserva el mismo error de Application con Pomelo/MySqlConnector y Oracle Connector/NET; no se reintenta y los 1062 de otros índices no se traducen erróneamente.**

## Contrato fijado

- Sólo el error MySQL 1062 del índice `IX_TipoClientes_EsPredeterminadoUnico` se traduce a `UniqueConstraintViolationException`.
- El nombre funcional permanece `TipoClientePredeterminadoUnico`.
- El mensaje permanece exactamente: `Conflicto de concurrencia: Ya existe otro tipo de cliente marcado como predeterminado único. Inténtalo de nuevo.`
- La excepción conserva como `InnerException` el `DbUpdateException` original y su excepción de provider.
- La ruta 1062 se ejecuta una sola vez (no se reintenta). Un 1062 con otro índice conserva y propaga el `DbUpdateException` original sin traducción.
- `ExceptionHandlingMiddleware` mantiene la respuesta HTTP 409 para `UniqueConstraintViolationException`, por lo que las capas superiores siguen recibiendo el contrato de conflicto de SOLQARYN.

## Pruebas y certificación exact-head

- `UnitOfWorkRetryTests` comprueba una tentativa, nombre, mensaje y excepción interna originales, y comprueba que un 1062 de otro índice no se traduce.
- `scripts/modernization/oracle10-provider-lane.sh` reproduce el duplicate real contra MySQL con Oracle EF Core 10/Connector/NET y falla si cambia el tipo Application, nombre, mensaje, número interno, familia de provider o número de intentos.
- Fase 6 exact-head vigente: run `37285385270`, HEAD `d6598389bcf99599fe1a0a3b973e7ecb0979b421`; los cuatro jobs de compatibilidad/provider/net10 y el dictamen terminaron `success`. `P0=0`, `P1=0`; Fase 7 no ejecutada. [Run del gate de Fase 6](https://github.com/solqaryn/Solqaryn/actions/runs/37285385270).
- En ese mismo HEAD, Oracle EF10/net10 pasó 2336/2336 pruebas unitarias y 22/22 de integración. `UnitOfWorkRetryTests` aprobó tanto la traducción esperada como el rechazo de otro índice.
- El job Oracle Connector/NET registró `duplicateTranslated=true`, `duplicateNumber=1062`, `duplicateException=UniqueConstraintViolationException -> MySql.Data.MySqlClient.MySqlException`; el gate sólo aprueba si también se cumplen el tipo provider y los contratos de retry.
- El job Pomelo validó los casos 1062 y concluyó la suite de integración completa con 28/28; la verificación de retry/excepción concluyó `PASS`.

## Alcance

No cambia la política ni el código productivo de traducción; se refuerza la certificación del contrato existente en el provider candidato. No se añade dependencia ni se ejecuta Fase 7 o despliegues a QA, `main` o PROD.

**Punto 9: CERRADO.** 1062 esperado conserva el contrato funcional, no se reintenta y los duplicados de índices distintos no se traducen; verificado en ambos providers.

MAPA_ARQUITECTURA: SIN_CAMBIO.
