# SOLQARYN V5 — C01-01.M05.05: rechazo negativo de identidad, pertenencia y abuso tenant

**Autor:** PRIMARY DEV 04 JAVIER_CHATGPT `6aeacb7820ac8191b7d768a6f33dfa09`. Proyecto `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Contrato: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO/COLA/DAG_TECNICO. Esta entrega contiene **seis pruebas xUnit ejecutables sobre el servicio Application y reglas Domain reales**, no un documento que sustituya código.

## Escenarios de denegación ejecutables

| Caso | Situación | Oráculo de seguridad |
| --- | --- | --- |
| N01 | Contexto de A solicita tenant B | `InvalidOperationException` sin siquiera consultar la fuente |
| N02 | Fuente de persistencia entrega snapshot de B al pedir A | Rechazo aunque identificador de usuario coincida |
| N03 | Fuente retorna membresía de usuario A2 dentro de empresa A mientras actor es A1 | Rechazo sin heredar permisos entre usuarios del mismo tenant |
| N04 | Tras primera respuesta válida se revoca membresía | Segunda resolución denegada, sin conservar permisos previos |
| N05 | Tras primera respuesta válida cambia el rol de membresía | Segunda resolución denegada para el contexto autenticado viejo |
| N06 | Sucursal válida cambia propietario a B | Segunda resolución rechazada pese a conservar el mismo Id |

Las pruebas implementan una fuente de snapshots con contador en memoria, invocan el código productivo `IdentidadUniversalApplicationService.ResolverActualAsync` y `IdentidadUniversalReglas.CrearContextoVerificado`, y verifican errores y número de lecturas cuando corresponde. No falsean autenticación, JWT, RBAC por endpoint, EF/MySQL ni aislamiento de base de datos real; no hacen llamadas HTTP, migraciones ni efectos externos. El alcance positivo complementario `C01-01.M05.04` se mantiene en su propio ID/owner y estado independiente.

## Controles A0–A5 y rollback

- **A0:** Skill, Scope Lock, HEAD, DAG F=VALIDADO/G=SI, COLA G/O=SI, owner único y diff acotado a nuevo test, documento y CHANGELOG; preservar suites previas.
- **A1:** seis escenarios N01–N06 xUnit reales; el resultado PASS se atribuye solo si GitHub Actions lo acredita en SHA aplicable.
- **A2:** fallos de pertenencia tenant A/B, usuario A1/A2, rol/sucursal revocados; ninguna entrada de cliente es fuente de privilegios.
- **A3:** integración Application/Domain y xUnit sobre .NET 10 usando GitHub-hosted runner; sin MySQL, HTTP, cookies, Playwright ni proveedor de identidad externo.
- **A4:** publicación fast-forward CAS sobre dev, checks Scope Lock/procedencia V5, backend xUnit, seguridad multiempresa y authorization gate que sean pertinentes, exact SHA o equivalencia demostrada; BITACORA y PLAN/COLA readback.
- **A5:** revisión independiente de otra Task Javier distinta de la autora. Ni un PASS de test ni esta evidencia autoriza QA/main/PROD.

**Reversión:** `git revert` ordinario del commit de tests/documento/CHANGELOG, sin force-push; preservar BITACORA histórica, tenant, JWT/RBAC, endpoints, base real, secretos y configuración física de Tasks.
