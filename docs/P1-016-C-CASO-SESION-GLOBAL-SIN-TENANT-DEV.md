# P1-016.C — Caso negativo medible: sesión global sin tenant (DEV)

## Fuente ejecutable, autoría y dependencias
- Plan Comercial Inicial Unificado, unidad P1-016.C, orden 56, `PLAN_MAESTRO!57`. Depende de **P1-016.A=LISTO**; `COLA!G57=SI` al reclamar.
- Autor funcional: ALEX :10, cuenta `ALEX_CHATGPT`, TASK_ID `6adbbc9ad9e88191bea8eb69104afb25`, GitHub `morales35alex`.
- Rama exclusiva `work/solqaryn-p1-016-c` desde `dev@41e2edffbbe394ad788baa477c3f3e8a731c27b0`. Claim CAS sobre `PLAN_MAESTRO!H57` (una ocurrencia), readbacks PLAN/COLA/BITACORA; WIP 5→6/6, cupo propio 0→1/1. Evento `SQ-P1-016-C-ALEX10-CLAIM-41E2EDFF-20261105`.
- Alcance **PREPARAR UN CASO NEGATIVO** de regresión de plataforma sin tenant. No se modifica implementación, rutas, DTO, autenticación, migración, secretos ni base productiva.

## Condición negativa única y fixture sintético
Un usuario **administrador empresarial**, sin concesión `UsuarioRolPlataforma`, solicita `GET /plataforma/sesion` mientras envía `X-Empresa-Id` para su empresa activa A. La cabecera es contexto comercial, nunca autoridad global ni prueba de un rol de plataforma.

Se reutiliza sin copiar fixture ni aserciones de
`P1016APlatformWithoutTenantTests.P1016A_AdminEmpresarialNoHeredaAccesoGlobalPorRolNiPorHeader`:
`AppDbContext` EF InMemory efímero por GUID; rol `AmbitoAutorizacion.Empresa` y membresía `UsuarioEmpresa` en Empresa A activa; usuario autenticado sintético sin `UsuarioRolPlataforma`; controlador `PlataformaSesionController` con `HttpContext` y header empresarial.

**Resultado esperado medible:** `UsuarioScopeService.ObtenerActualAsync(A)` puede reconocer la membresía empresarial, pero `PlataformaScopeService.ObtenerActualAsync()` devuelve `null`; `PlataformaSesionController.ObtenerActual` debe devolver `ForbidResult` pese a la cabecera. Debe existir un solo `RegistroAuditoria` con `Resultado=Rechazado`, `EmpresaId=null` por ámbito de plataforma y ninguna concesión `UsuarioRolPlataforma` al actor.

## Prueba y límites
`backend/tests/Solqaryn.Tests/P1016CCaseRoutingTests.cs` ejecuta la regresión funcional preexistente, invocándola desde la prueba xUnit filtrable `P1016C_`, sin nueva ruta productiva ni duplicar fixture. La prueba dirigida debe acreditarse con `dotnet test ... --filter FullyQualifiedName~P1016C`, TRX `total>0 failed=0`, backend/seguridad/tenancy, Scope Lock `AUTO_CERT=PASS` y checks exactos del HEAD. **No afirmar PASS antes de observar CI.**

Esta evidencia es EF InMemory, **no** una prueba HTTP/JWT real, transacción MySQL ni despliegue. El backend sigue siendo la autoridad. P1-016.A ya está cerrado; este micropaso prepara una comprobación reutilizable y medible, no reabre aquella entrega.

## Entrega, review y rollback
Tras CI terminal exact-SHA: pasar `EN_PROGRESO → VALIDANDO`, conservar `AUTHOR_TASK_ID`, liberar lease y responsable con readbacks GitHub/PLAN/COLA/BITACORA. Solo otro TASK_ID podrá reservar revisión exclusiva y tramitar merge protegido. `LISTO` requiere CI postmerge SHA exacto y readback institucional; la autora no se revisa ni aprueba. Revert protegido de test/acta/changelog sin reescribir historia. Solo `dev`; QA/main/PROD intactos.
