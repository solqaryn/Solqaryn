# P1-011.C — Caso medible: alta atómica de administrador nuevo (DEV)

## Identidad, fuente y alcance

- Microtarea **P1-011.C**, orden comercial **46**, PLAN_MAESTRO fila 47, dependencia **P1-011.A=LISTO** (COLA G47=SI). Referencia de continuidad indicada por el plan: P1-011.B; no se presupone que sea dependencia adicional.
- Task autora ALEX :10, ID 6adbbc9ad9e88191bea8eb69104afb25, ALEX_CHATGPT, GitHub morales35alex. Rama exclusiva work/solqaryn-p1-011-c, reclamada desde dev@d6151c5edced0c4aec69551e258d8c293b0ba8aa con readback institucional; WIP 6/1/1.
- **Único objetivo**: seleccionar una condición negativa medible, su fixture y su resultado esperado. No reimplementar la API ni duplicar pruebas existentes.

## Condición negativa única

**Condición:** si falla la persistencia de RegistroAuditoria *después* de crear Usuario, Empresa, ConfigEmpresa y UsuarioEmpresa en la misma transacción MySQL para un administrador nuevo, debe rechazarse el alta y revertirse la transacción sin dejar ninguna entidad parcial.

**Fixture reproducible, aislado:** MySQL 8.4 efímero del job de integración, empresa sintética **Empresa rollback auditoria nueva** y usuario sintético **p4-rollback-audit**, actor de plataforma del fixture y banderas MultiEmpresa:PlatformOnboardingEnabled=true y MultiEmpresa:CommercialIsolationCertified=true solo dentro de la prueba. Inyectar InvalidOperationException al guardar auditoría con el mecanismo de prueba existente. Eliminar la base temporal en finally. No usar usuarios, empresas ni secretos reales.

**Resultado esperado, NO ejecutado en esta microtarea:** excepción de tipo InvalidOperationException. Las consultas posteriores IgnoreQueryFilters devuelven **cero usuarios** con NombreUsuario=p4-rollback-audit y **cero empresas** con Nombre=Empresa rollback auditoria nueva. Los totales de ConfigEmpresa, UsuarioEmpresa y RegistroAuditoria permanecen en su baseline anterior al intento, sin otorgar un UsuarioRolPlataforma. Persistencia parcial de cualquier entidad implica **FAIL**. Un 200 HTTP o el resultado EF InMemory no sustituyen la prueba MySQL.

**Referencia técnica que ya implementa el negativo (reutilizar, no duplicar):** backend/tests/Solqaryn.Tests/API/Controllers/PlataformaEmpresasMySqlTransactionIntegrationTests.cs, método AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal, segmento de auditoría fallida para p4-rollback-audit. Antecedente documental docs/P1-011-A-ALTA-ATOMICA-ADMIN-DEV.md, PR #3851. P1-011.C documenta una expectativa ya comprobable en el código, sin afirmar que se haya ejecutado ahora.

## Corrección causal de CI dirigida (caso PREPARADO, no MySQL ejecutado)

La primera CI del PR #3860 sobre SHA eed13307d1bdc9cbc9043f3bfad931e8c4b13b79 rechazó correctamente `FullyQualifiedName~P1011C` porque aún no existía prueba con ese prefijo: `DIRECTED_SCOPE_NO_GO total=0`. Para que la preparación tenga un gate dirigido, se agregó **una prueba de contrato/routing** en `backend/tests/Solqaryn.Tests/API/Controllers/P1011CCaseRoutingTests.cs`: comprueba por reflexión que el fixture original conserva el método de integración xUnit y su clasificación `Category=Integration`. **Esta comprobación no prueba el rollback ni sustituye ejecutar MySQL**; el caso funcional original sigue separado en CI con MySQL efímero. Cualquier PASS de la prueba de routing solo significa que existe la ruta de validación causal, no que la transacción esté certificada.

## Validación, seguridad y cierre

- Para aceptar este caso, exigir job con MySQL temporal 8.4 y ConnectionStrings__DefaultConnection de CI, TRX/log con prueba de integración ejecutada (recuento >0, cero fallos) sobre SHA exacto; junto a regresiones backend, JWT/RBAC/tenancy, Scope Lock y los checks aplicables.
- Para LISTO se exige revisión técnica por otro TASK_ID, merge protegido, CI posterior en el SHA de merge y readback PLAN_MAESTRO/COLA/BITACORA; esta Task autora no emite TECH_REVIEW_PASS ni aprobación formal de su propio cambio.
- Sin cambios de API/DTO, backend productivo, UI, RBAC, esquemas/migraciones, datos, credenciales, despliegues, flags de DEV remoto, QA, main o PROD. Rollback documental mediante PR protegido que revierta el acta y su entrada CHANGELOG_AI.md.

**Estado de este documento: CASO PREPARADO, NO PRUEBA EJECUTADA, NO CERTIFICACIÓN FUNCIONAL.**
