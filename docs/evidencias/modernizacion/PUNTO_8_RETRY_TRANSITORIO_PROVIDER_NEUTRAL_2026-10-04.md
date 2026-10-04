# Punto 8 — Retry transitorio desacoplado del provider MySQL

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — 1205/1213 conservan el retry acotado a tres intentos y se reconocen sin acoplar `UnitOfWork` a un tipo de excepción concreto.** La clasificación se mantiene en Infrastructure y acepta únicamente los tipos conocidos de MySqlConnector y Oracle Connector/NET.

## Cobertura comprobada

- La suite unitaria de `UnitOfWorkRetryTests` cubre 1205, 1213 y el máximo de tres intentos con `MySqlConnector.MySqlException`; además comprueba que errores desconocidos no se reintentan.
- La lane Oracle Connector/NET provoca un lock wait real 1205 y exige que la operación se repita (`attempts >= 2`).
- El mismo `UnitOfWork`, ejecutado con Oracle Connector/NET, recibe una `MySql.Data.MySqlClient.MySqlException` con `Number=1213`, revierte el primer intento y completa el segundo. La prueba exige exactamente dos intentos y lee de vuelta `Number` para validar el contrato del tipo Oracle.
- El resultado Oracle queda incorporado al booleano `exception_contract` del gate: si falla el reconocimiento/retry Oracle 1213, el contrato del provider no puede salir PASS.
- Política sin cambios: sólo 1205 y 1213 son transitorios; máximo tres intentos; duplicate key 1062 continúa su ruta separada; errores ajenos no se reintentan.

## Certificación exact-head

- Commit probado: `9a93e43110430123364710d49f91ec741d9e2e84`.
- GitHub Actions run `37234393499`: los cinco jobs del gate de Fase 6 terminaron `success`, incluido el job Oracle Connector/NET y el dictamen final.
- El resultado de Oracle registró `retryContractCompatible=true` para 1205 real y `oracle1213RetryCompatible=true`; `exception_contract=true`.
- Dictamen registrado: `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`. El job también confirma `PHASE7_EXECUTED=false`.

## Alcance

La prueba 1213 valida el tipo/código Oracle mediante una excepción Oracle construida para el contrato; no afirma que esta ejecución haya producido un deadlock físico. El lock wait 1205 sí es inducido contra MySQL efímero. No cambian dependencias ni configuración de producción. No se ejecuta Fase 7 ni se despliega a QA, `main` o PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.
