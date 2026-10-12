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

- Certificación exact-head vigente: GitHub Actions run `37284138818`, HEAD `1561a89244c8688a114103fd8de4b22260707329`; Pomelo, net10 aislado, Oracle EF10 net10, compatibilidad Oracle Connector/NET y `Dictamen Fase 6` terminaron `success`. [Fase 6 exact-head](https://github.com/solqaryn/Solqaryn/actions/runs/37284138818).
- En el job Oracle Connector/NET, el resultado capturado fue `retryContractCompatible=true` y `oracle1213RetryCompatible=true`; el gate deriva `exception_contract` de ambos resultados y terminó `success`.
- En Pomelo, la prueba dedicada “Tests de retry y excepción MySqlConnector” terminó `success`, seguida por la suite completa de integración MySQL: 28/28.
- En la lane Oracle EF10/net10 del mismo HEAD pasaron 2336/2336 pruebas unitarias y 22/22 pruebas de integración; el contrato de retry del provider registró dos intentos efectivos. El probe aislado exact-head `37283228362` también pasó en la versión de código que quedó en el mismo commit de código `0282717`.
- El dictamen exact-head registró `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`; `PHASE7_EXECUTED=false`.

## Alcance

La prueba 1213 valida el tipo/código Oracle mediante una excepción Oracle construida para el contrato; no afirma que esta ejecución haya producido un deadlock físico. El lock wait 1205 sí es inducido contra MySQL efímero. No cambian dependencias ni configuración de producción. No se ejecuta Fase 7 ni se despliega a QA, `main` o PROD.

**Punto 8: CERRADO.** Retry provider-neutral 1205/1213 y el límite de tres intentos comprobados; no se amplía la lista de errores transitorios.

MAPA_ARQUITECTURA: SIN_CAMBIO.


## Revalidación exact-head posterior — 2026-10-06

- HEAD de `dev`: `8dcdaeb620caef7b625d3d8ca10094df039e7f62`. Certificación Fase 6 post-merge: [run 37422330522](https://github.com/solqaryn/Solqaryn/actions/runs/37422330522), evento push sobre ese SHA, conclusión `success`; pasaron sus cinco lanes y el dictamen.
- Se inspeccionó `UnitOfWork` en ese mismo SHA: `MaxRetryCount = 3`; sólo 1205/1213 se clasifican como transitorios; el 1062 esperado se traduce aparte a `UniqueConstraintViolationException`; otras excepciones se propagan sin retry.
- En Pomelo/MySQL 8.4, `UnitOfWorkRetryTests` pasó 9/9 y la suite de integración completa pasó 28/28.
- En la lane Oracle EF10/net10, los casos del mismo contrato pasaron, incluyendo 1205, tope de tres intentos, 1062 sin retry, error desconocido sin retry y 1213; total de unitarios: 2336/2336. La lane Oracle registró `ORACLE10_PROVIDER_LANE_RUNTIME=PASS attempts=2`.
- Verificación del harness exact-head: mantiene una transacción bloqueadora real en el MySQL efímero, configura `innodb_lock_wait_timeout=1` en la conexión de la operación y exige al menos dos intentos. Para Oracle Connector/NET, 1213 se inyecta como excepción del driver para probar clasificación/retry; no se presenta como deadlock físico.
- Política intacta: tres intentos máximo; únicamente 1205/1213 transitorios; 1062 separado; errores ajenos no reintentados. Sin cambios productivos, dependencias ni configuración; no se ejecutó Fase 7 ni se tocó QA/`main`/PROD.

**Revalidación del punto 8: PASS en dev, HEAD `8dcdaeb...`.**
