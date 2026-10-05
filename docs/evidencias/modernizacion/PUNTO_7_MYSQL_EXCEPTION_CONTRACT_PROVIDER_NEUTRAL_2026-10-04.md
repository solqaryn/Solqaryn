# Punto 7 — Contrato de excepciones MySQL independiente del provider

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**PASS — el conocimiento de excepciones concretas está confinado a Infrastructure y el contrato que consume la aplicación es provider-neutral.** No se exponen tipos MySqlConnector ni Oracle Connector/NET en Domain o Application.

## Evidencia de código

- `Infrastructure/Services/MySqlProviderErrorClassifier.cs` reconoce sólo los nombres exactos `MySqlConnector.MySqlException` y `MySql.Data.MySqlClient.MySqlException`; obtiene el `Number` público como `int`, recorre excepciones internas y falla cerrado para tipos desconocidos o propiedades inaccesibles.
- `UnitOfWork` consume el valor clasificado para reintentar únicamente 1205/1213 y traducir el duplicate-key 1062 del índice esperado a `Application.Exceptions.UniqueConstraintViolationException`.
- Los repositorios que implementan idempotencia/clave duplicada usan el mismo clasificador, no un `catch` directo a una excepción concreta del driver.
- Búsqueda completa de `backend/src/Domain` y `backend/src/Application`: cero referencias a `MySqlConnector`, `MySql.Data`, `MySqlException` o Pomelo.

## Evidencia de ejecución exact-head

- La lane `scripts/modernization/oracle10-provider-lane.sh` usa la conexión real de `MySql.Data.MySqlClient`, provoca duplicate key 1062 a través de `UnitOfWork` y exige `UniqueConstraintViolationException`; también genera un lock wait 1205 y exige retry efectivo (`attempts >= 2`). Si la familia Oracle no se clasifica, el test falla.
- Run exact-head de Fase 6 `37281303914`, HEAD `342fb3fdac50b9f2a39d853293288c18fbe81393`: Pomelo/MySqlConnector, compatibilidad Oracle Connector/NET, lane Oracle EF10 net10 y `Dictamen Fase 6` terminaron `success`. En el job Pomelo también terminaron `success` los pasos específicos de retry/excepción MySqlConnector y toda la suite de integración MySQL. [Certificación exact-head de Fase 6](https://github.com/solqaryn/Solqaryn/actions/runs/37281303914).
- En ese mismo HEAD, la lane final Oracle EF10 net10 terminó `success`; por tanto el provider alterno recorrió el contrato requerido de 1062 y retry, además de la certificación integral de esa lane.
- La certificación confirma el comportamiento; la evidencia de aislamiento arquitectónico se comprobó directamente en el árbol exacto: `MySqlProviderErrorClassifier` y `UnitOfWork` son los consumidores de los tipos provider-specific y la búsqueda en `backend/src/Domain` y `backend/src/Application` devolvió cero referencias a `MySqlConnector`, `MySql.Data`, `MySqlException` o Pomelo.

## Alcance

No se añaden dependencias al dominio/aplicación ni al stack productivo; no se ejecuta Fase 7 ni se despliega a QA, `main` o PROD.

**Punto 7: CERRADO.** Evidencia exact-head de Fase 6 y contrato provider-neutral verificado en código y en las lanes MySqlConnector/Oracle.

MAPA_ARQUITECTURA: SIN_CAMBIO.
