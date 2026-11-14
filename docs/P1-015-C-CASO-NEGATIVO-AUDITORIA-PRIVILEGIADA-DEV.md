# P1-015.C — Caso negativo medible: autodelegación privilegiada (DEV)

## Fuente, identidad y alcance
- Micropaso comercial **P1-015.C**, orden 54, PLAN_MAESTRO fila 55, depende de **P1-015.A=LISTO**. Selecciona **una** condición negativa, fixture y resultado esperado; no cambia el contrato de la API.
- Autor funcional: ALEX :10, `ALEX_CHATGPT`, TASK_ID `6adbbc9ad9e88191bea8eb69104afb25`, GitHub `morales35alex`. Rama `work/solqaryn-p1-015-c` creada desde `dev@d6151c5edced0c4aec69551e258d8c293b0ba8aa`. Claim exclusivo auditado en BITACORA fila 636.
- Superficie existente: `POST /plataforma/delegaciones`, `PlataformaDelegacionesController.Conceder` y `RegistrosAuditoria`. Reutiliza la regresión `P1015APrivilegedAccessAuditTests.SelfGrant_IsDeniedAndAuditedWithoutNewPrivilege` ya integrada, sin crear permiso ni endpoint nuevo.

## Única condición negativa
Un superadministrador de plataforma autenticado intenta **autodelegarse** autoridad de plataforma, enviando como `UsuarioId` su propia identidad y un motivo no vacío. Este intento no puede conceder otro privilegio, aunque el actor ya sea global.

**Fixture sintético:** EF Core InMemory con rol de plataforma activo `P1015A_GLOBAL` (`AmbitoAutorizacion.Plataforma`), rol administrativo de empresa `P1015A_EMPRESA`, usuario `p1015a-super` vinculado a un único `UsuarioRolPlataforma`, `p1015a-delegado`, `p1015a-empresa` y empresa sintética `P1-015 tenant A`. Controlador con actor `p1015a-super`, correlación `p1015a-self-denied`, `UsuarioId = Superadmin.Id` y motivo `Autodelegacion que debe rechazarse`. Sin credenciales reales ni recursos remotos.

**Resultado esperado (medible):** `ForbidResult`; exactamente **un** evento `RegistrosAuditoria` de resultado `Denegado` asociado al `UsuarioId` real del actor, con el mismo motivo y `CorrelationId`, `EmpresaId=null` por ámbito de plataforma; `UsuarioRolesPlataforma.CountAsync() == 1` (ninguna concesión nueva). El evento de rechazo debe conservar trazabilidad, no registrar un éxito.

## Estrategia de evidencia
- La prueba `P1015C_AutodelegacionPrivilegiada_DebeNegarseYAuditarse` ejecuta el método xUnit **existente** de P1-015.A desde una prueba dirigida filtrable P1015C. No copia su fixture ni sus aserciones; verifica en EF InMemory el caso negativo completo.
- Ejecutar `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj -c Release --filter FullyQualifiedName~P1015C`, comprobar TRX con total > 0, fallos = 0 y CI exact SHA. También Scope Lock, regresión de seguridad/tenancy y checks aplicables. **El mero archivo o commit NO equivale a PASS**.
- EF InMemory no demuestra transacción MySQL, HTTP/JWT remoto, ni despliegue. El control de autorización servidor sigue siendo la fuente de verdad. No introducir `X-Empresa-Id` como identidad de plataforma.
- Cierre: otro TASK_ID autorizado distinto del autor revisará el SHA exacto, integrará mediante PR protegido cuando corresponda y verificará CI `push` postmerge; luego PLAN_MAESTRO/COLA/BITACORA readbacks. Prohibido autoaprobar y autopublicar LISTO.

## Impacto y rollback
Solo nuevo test de reutilización y acta/changelog. Sin modificaciones a Domain/Application/Infrastructure/API productivos, rutas, DTO, DB, migraciones, JWT/RBAC, observabilidad, secretos ni entornos QA/main/PROD. Rollback mediante PR protegido de revert del test/acta/changelog, preservando auditoría histórica.
