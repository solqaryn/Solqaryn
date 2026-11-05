# P1-014.C — Caso negativo del flujo guiado de alta (DEV)

## Alcance y autoridad
- Plan Comercial Inicial: P1-014.C, orden 52, PLAN_MAESTRO fila 53; dependencia P1-014.A = LISTO, COLA G53 = SI.
- Task autora: SECONDARY_REVIEW_01, Alex :10, `6adbbc9ad9e88191bea8eb69104afb25`; identidad GitHub `morales35alex`.
- Selector: REVIEW_FIRST sin entregas VALIDANDO ajenas elegibles (P1-011.C/P1-013.A/P1-015.C/P1-016.C son autoría histórica de esta Task); RECOVER sin scope propio EN_PROGRESO; CLAIM WIP 5/6 -> 6/6, responsabilidad 0/1 -> 1/1.
- Rama exclusiva `work/solqaryn-p1-014-c`, base `dev@7a9730cdc3b39a98d37237774ac907f888a0a414`, CAS de estado institucional con una coincidencia y readback, fingerprint `SQ-P1-014-C-ALEX10-CLAIM-7A9730CD-20261105`.

## Una condición medible y resultado esperado
**Condición negativa:** aun si el operador tiene autoridad global y está habilitado el aislamiento comercial, desactivar `MultiEmpresa:PlatformOnboardingEnabled` debe impedir el alta controlada desde el flujo guiado. La UI de tres pasos no puede reemplazar los gates del backend.

**Fixture sintético existente:** `PlataformaEmpresasOnboardingSecurityTests.P1013A_AltaRechazadaPorCualquierGateApagado(onboarding: false, aislamiento: true)` crea controlador y `AppDbContext` EF InMemory con operador global autorizado. Invoca `PlataformaEmpresasController.AltaControlada` con nombre y administrador existente del fixture.

**Resultado esperado verificable:** `ObjectResult.StatusCode == 503`, cero registros nuevos `Empresa`, `UsuarioEmpresa` y `ConfigEmpresa`. No promover permisos globales por el wizard.

**Prueba dirigida nueva:** `P1014CGuidedOnboardingCaseTests.P1014C_NoCrearEmpresaSinGateOnboarding`, un wrapper que ejecuta las aserciones existentes sin duplicar su lógica ni manipular configuración remota. Filtrar con `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --filter FullyQualifiedName~P1014C`.

## Fronteras y aceptación
- No se modifican Angular, servicios productivos, DTO, JWT/RBAC, transacciones, migraciones, flags, secretos, cuentas ni QA/main/PROD.
- EF InMemory comprueba el resultado lógico del controlador y efectos sobre el contexto de prueba. **No** prueba HTTP/JWT real, MySQL 8.4 remoto ni valores efectivos de flags de DEV.
- Esta acta describe expectativas; no certifica PASS por sí misma. Para entregar a VALIDANDO exigir prueba dirigida >0 PASS y 0 FAIL, CI exact HEAD incluyendo Scope Lock AUTO_CERT=PASS, evidencia GitHub, transición y liberación con readbacks institucionales. Revisión y cierre LISTO por TASK_ID diferente del autor, merge protegido y CI postmerge SHA exacto.
- Rollback: PR protegido que revierta únicamente la nueva prueba, este acta y su registro en CHANGELOG_AI; preservar historial y auditoría.
