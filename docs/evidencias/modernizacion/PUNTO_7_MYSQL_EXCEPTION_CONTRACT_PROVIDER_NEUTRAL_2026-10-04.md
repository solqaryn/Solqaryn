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

## Evidencia de ejecución Oracle EF10

- La lane `scripts/modernization/oracle10-provider-lane.sh` usa la conexión real de `MySql.Data.MySqlClient`, provoca duplicate key 1062 a través de `UnitOfWork` y exige `UniqueConstraintViolationException`; también genera un lock wait 1205 y exige retry efectivo (`attempts >= 2`). Si la familia Oracle no se clasifica, el test falla.
- Run exact-head de Fase 6 `37232938552`, HEAD `aeee293abb4beb757ff4196ebd52308bd54f2320`: los cinco jobs, incluida la lane Oracle EF10 net10 y el dictamen, terminaron `success`. [Certificación exact-head de Fase 6](https://github.com/solqaryn/Solqaryn/actions/runs/37232938552).
- Las pruebas unitarias siguen validando 1205/1213/1062 para el provider actual y rechazo de falsos positivos con una excepción ajena que también expone `Number`.

## Alcance

No se añaden dependencias al dominio/aplicación ni al stack productivo; no se ejecuta Fase 7 ni se despliega a QA, `main` o PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.
