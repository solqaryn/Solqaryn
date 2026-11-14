# P1-015.D — Ejecucion dirigida del rechazo de acceso privilegiado (DEV)

## Autoridad y alcance
- Plan comercial inicial V3, `PLAN_MAESTRO!64`, orden 63; dependencia `P1-015.C=LISTO`. Autor de esta microtarea: **ALEX :50**, Task `6adcc98a80a48191b71a5156624c21df`, GitHub `morales35alex`.
- Rama determinista: `work/solqaryn-p1-015-d`; punto de partida `dev@0c280ab7c4b8a56b1a0674b3a3bca438d3078e1b`. No se altera codigo productivo ni permisos.
- Objetivo de un solo rechazo: usuario con autoridad global `p1015a-super` intenta autodelegarse el privilegio de plataforma. La aplicacion debe responder `ForbidResult`, preservar exactamente un rol global, registrar un solo evento `Denegado`, usuario/correlacion/motivo del actor y `EmpresaId=null`.

## Prueba dirigida y evidencia verificable
- `backend/tests/Solqaryn.Tests/P1015DPrivilegedAuditNegativeTests.cs` permite filtrar el escenario con `FullyQualifiedName~P1015D`, reutilizando la prueba asertiva `P1015APrivilegedAccessAuditTests.SelfGrant_IsDeniedAndAuditedWithoutNewPrivilege` integrada previamente. No copia ni debilita sus aserciones. La prueba existente se ejecuta de forma real cuando el wrapper se ejecuta, no solo inspecciona refleccion.
- Ejecutar `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj -c Release --filter FullyQualifiedName~P1015D --logger trx`, verificar `total>0` y `failed=0` en TRX/log, y la suite de seguridad del PR sobre **el SHA final exacto**.
- En este changeset no se atribuyen como PASS pruebas que no hayan terminado; CI puede continuar asincronicamente. Exigir Scope Lock, Seguridad multiempresa y Build/pruebas backend requeridas. Una prueba EF InMemory NO certifica DB MySQL real, HTTP/JWT remoto ni PROD.

## Limites, entrega y rollback
- Solo test, esta acta y `CHANGELOG_AI.md`; sin cambios en API/Application/Domain/Infrastructure de produccion, tablas, migraciones, JWT/RBAC, headers, roles, secretos ni entornos QA/main/PROD.
- Entregar al terminar pruebas a `VALIDANDO` sin auto-revision ni LISTO. `reviewerFor` designa otra Task diferente a la autora y es consultivo; cotejar GitHub, PLAN/COLA/BITACORA y actividad concurrente.
- Revertir, si procede, mediante un nuevo PR protegido, conservando trazabilidad.
