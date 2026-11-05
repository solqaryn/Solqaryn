# P1-016.A — Regresión de sesión de plataforma sin tenant (DEV)

## Origen y autoridad
- Plan Comercial Inicial Unificado, unidad P1-016.A, orden 55; `PLAN_MAESTRO!A56:M56`, dependencia `P0-012.B=LISTO` y `COLA!G56=SI` en el momento del claim.
- Autora funcional: `ALEX_CHATGPT`, Task `6adbbc9ad9e88191bea8eb69104afb25`, GitHub `morales35alex`, rama exclusiva `work/solqaryn-p1-016-a`. Se aplicó WIP global 6/1/1 y `PROJECT_SCOPE_LOCK=STRICT`.
- Base del claim: `dev@5c47317120bd4587a0570b11522acf898ef59c98`. Solo DEV; prohibidas QA, main y producción.
- Microtarea antecedente P1-013.A fue entregada a VALIDANDO con autor histórico conservado y lease liberado (BITACORA!604). La revisión preliminar de P1-014.A no asumió ownership (BITACORA!605).

## Alcance de la regresión
Se reutilizan **sin cambios productivos** el servicio `PlataformaScopeService`, el `UsuarioScopeService` y `GET /plataforma/sesion` para verificar su separación real:

1. Cuenta superadministradora con exactamente un rol global de sistema activo **sí** puede entrar en la sesión de plataforma aun cuando `VARISTOREHN` esté inactiva y no exista ninguna membresía empresarial.
2. Una identidad autenticada pero sin rol global ni membresía **no** recibe autoridad por su `Usuario.RolId` legado; la solicitud responde `Forbid` y registra rechazo de ámbito global.
3. Un administrador empresarial activo con membresía y rol de empresa **no** adquiere autoridad global ni siquiera aportando `X-Empresa-Id`; la auditoría de sesión no se asocia a una empresa.
4. El bloqueo del usuario global revoca su sesión aun cuando la identidad JWT de prueba siga autenticada.
5. Si falta autenticación HTTP, el controlador falla cerrado antes de escribir auditoría.

Archivo: `backend/tests/Solqaryn.Tests/P1016APlatformWithoutTenantTests.cs`, cinco pruebas `P1016A_`. Los escenarios incluyen verificación de `Cache-Control: private, no-store, max-age=0`, identidad exacta y ausencia de membresías/lecturas empresariales implícitas. No se conceden roles ni permisos, no se toca la empresa real: `VARISTOREHN` es **solo un objeto efímero en EF InMemory**.

## Evidencia exigida para VALIDANDO y LISTO
- Ejecutar `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1016A`, comprobar recuento positivo, cero fallos y SHA exacto en GitHub Actions.
- Ejecutar checks protegidos de backend, seguridad multiempresa y `verify-solqaryn-project-scope`, con `AUTO_CERT=PASS` para el SHA final y regresiones relacionadas. La compilación y las pruebas permanecen no certificadas **hasta leer los logs reales**.
- Revisión sustantiva por otra Task física autorizada, sin autoaprobación formal o técnica; si se requieren correcciones, conservar rama/PR y autoría original.
- Fusionar a `dev` exclusivamente por merge protegido con permisos y CI vigentes; luego CI postmerge exact SHA, `PLAN_MAESTRO`/COLA calculada/`BITACORA` con readback por Task distinta.
- **No extrapolar** estas pruebas unitarias a JWT firmado, HTTP remoto, MySQL DEV vivo, Render/Vercel o flags reales. Dichas verificaciones se hacen solo si el criterio de aceptación las requiere y una identidad autorizada puede efectuarlas.

## Riesgo, impacto y rollback
- Impacto: únicamente un archivo de pruebas xUnit, la presente acta y la nota de CHANGELOG.
- No hay modificaciones de `Domain`, Application, Infrastructure, API, frontend, migraciones, datos, secretos, permisos, roles, flags ni infraestructura.
- Riesgo residual: EF InMemory no reproduce filtros, índices, transacciones o colaciones del proveedor MySQL ni el middleware JWT real. No declarar certificación remota.
- Rollback: PR de revert protegido limitado al archivo de tests, acta y changelog; preservar auditorías e historial.
