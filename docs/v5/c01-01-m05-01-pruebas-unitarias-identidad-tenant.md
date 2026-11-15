# SOLQARYN V5 FINAL — C01-01.M05.01: ejecución de reglas unitarias de identidad universal

**Alcance real:** pruebas xUnit .NET 10 añadidas a `backend/tests/Solqaryn.Tests/Domain/IdentidadUniversalReglasTests.cs`, sin editar código de aplicación. Autor PRIMARY DEV 03 — JAVIER_CHATGPT (`6adc2e4bd8a48191ac7d8641ed95a1a3`). Fuente: Standard V5 FINAL, PLAN_MAESTRO fila 170, `dev`, Scope Lock STRICT. Dependencias C01-01.M04.01–06 LISTO y A5 independientes, C00-03.M06.06 más gate DAG validado; owner único H/I reclamado con CAS.

## Validación de reglas y riesgo

Cinco casos nuevos (tres variantes Theory y dos Fact) complementan pruebas vigentes de `IdentidadUniversalReglas.CrearContextoVerificado`, `UsuarioEmpresa`, `ContextoTenantActual.ExigirEmpresa`:
- **P01/N01–N03:** un contexto empresa A=11 sigue siendo válido para A tras rechazar empresa 0, -1 y B=22 con excepciones tipadas. Prohíbe cruces de tenant y fallbacks a IDs inválidos.
- **P02/N04–N05:** al cambiar la membresía a rol20, se rechaza el rol19 legado o desactualizado; al presentar el rol empresarial20 coincidente se crea el contexto correcto; una revocación posterior impide crear contexto nuevo.
- **P03/N06–N08:** el mismo usuario con membresías activas separadas A=11/rol19 y B=22/rol29 obtiene contextos independientes; ExigirEmpresa cruzada deniega; membresía A no crea sesión B.

Los casos prueban reglas puras de dominio de identidad y pertenencia, no configuran autenticación JWT real, filtro EF, autorización API, ni conectan MySQL. No se inventan resultados: el PASS de compilación y suite requiere GitHub Actions del commit exacto.

## A0–A5 y rollback

**A0:** identidad/cuenta Javier, skill canónica, HEAD y Scope Lock, DAG F=VALIDADO/G=SI, COLA G/O=SI, PLAN H=PENDIENTE I vacío, claim exclusivo y diff dirigido; comprobar branch dev sin escritores activos sobre archivos afectados. **A1:** ejecutar las cinco nuevas aserciones/variantes de prueba en GitHub-hosted y preservar suite previa; comprobación de positivo/negativo. **A2:** validar aislamiento A/B, rol relacional no legacy y membresía revocada mediante excepciones fail-closed (sin simular JWT/HTTP). **A3:** incluir build .NET 10 y `Solqaryn.Tests.Domain.IdentidadUniversalReglasTests` en CI dirigida, sin atribuir integración de bases de datos o navegador. **A4:** publicación fast-forward con SHA exacto, check `verify-solqaryn-project-scope` y `SOLQARYN - Build y pruebas backend` `completed/success`, changelog V5, blobs y readback PLAN/COLA/BITACORA. Si CI requerida falla, cancela o sigue en progreso, conservar VALIDANDO/owner. **A5:** revisión independiente posterior por otra de las cinco Tasks Javier distintas del autor, sin autoaprobación.

**No-alcance:** ninguna migración, servicio, permiso, cliente, JWT, flag, backend operativo, Angular, webhook, dispatcher, QA/main/PROD o datos reales. Rollback por `git revert` normal, reejecutando pruebas, sin force-push ni modificar BITACORA histórica. Referencia SKREF-10 Angular en pin registrada consultada durante preflight; este changeset final usa solo pruebas .NET Domain y no altera Angular.