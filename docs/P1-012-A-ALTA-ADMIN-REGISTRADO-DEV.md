# P1-012.A — regresión de alta con administrador registrado (DEV)

## Autoridad y alcance

- Plan Comercial Inicial Unificado V3, microtarea `P1-012.A`, orden 47, `PLAN_MAESTRO!A48:M48` y `COLA!G48=SI` al reclamar. Dependencia `P0-012.B=LISTO`.
- Responsable autorizado `JAVIER_CHATGPT`, Task física `6adbb9fff1548191a1bd5b30d9e9984a` (:00), login GitHub `jmejia31`; única microtarea propia bajo WIP 6/1/1.
- Rama `work/solqaryn-p1-012-a` creada desde `dev@4ac3fe76cc7f98c40a2aa1160c92560e1aab6bb2`, sin reutilizar una rama de otro scope.
- Se **reutiliza** la implementación original del endpoint `POST /plataforma/empresas/alta-controlada` procedente de PR #3683; no se introduce un segundo flujo de alta.

## Impacto técnico y de seguridad

- Cambios limitados a una regresión xUnit existente, este documento y `CHANGELOG_AI.md`; sin cambios productivos de API, Application, Infrastructure, Domain, frontend, esquema, migraciones, datos, permisos, entornos ni secretos.
- El test `P1012A_PlataformaCreaEmpresaConAdminRegistradoSinEscaladaGlobal` conserva el flujo de administrador por ID previamente registrado y amplía aserciones: no se crea otra identidad, conserva el rol original, permanece activo, no recibe `UsuarioRolPlataforma`, y se mantienen la membresía empresarial, la configuración y la auditoría.
- La regresión usa `AppDbContext` con EF InMemory y un controlador con identidad de prueba; **no acredita** un despliegue HTTP/JWT real, transacciones MySQL ni activación de onboarding. La prueba MySQL efímera existente en `PlataformaEmpresasMySqlTransactionIntegrationTests` cubre la secuencia relacional, pero su ejecución debe verificarse por CI real y no inferirse.
- La revisión cruzada por TASK_ID distinto del autor es técnica y **no equivale** a un `APPROVED` de otra identidad GitHub. La Task propietaria no autoaprueba ni marca LISTO su entrega.

## Validación y cierre exigidos

1. Ejecutar suite dirigida exact-SHA con `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1012A`; exigir al menos 1 PASS y 0 FAIL verificables en el log/TRX.
2. Exigir Scope Lock con `AUTO_CERT=PASS` confiable, seguridad multiempresa, backend y todos los checks aplicables terminales exact-SHA; clasificar el riesgo de integración con PR #3851 y preservar ramas.
3. Fusionar únicamente mediante el PR protegido hacia `dev`, sin bypass de reglas, fuerza ni revisión propia.
4. Antes de LISTO, comprobar el merge SHA en historial `dev`, CI postmerge `push` y check-runs del SHA exacto, revisión técnica de otro TASK_ID habilitado y readback `PLAN_MAESTRO`/`COLA` fórmula/`BITACORA`.

**Rollback:** revert mediante PR protegido solo del test y la documentación; conservar evidencia institucional. No cambiar ni borrar datos legítimos de empresas o cuentas.
