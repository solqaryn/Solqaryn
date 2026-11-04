# P1-006.B — Certificación negativa del selector empresarial (DEV)

## Autoridad, dependencia y límites
- Microtarea: `P1-006.B` (CERTIFICAR); depende de `P1-006.A=LISTO`.
- Base de claim: `dev@376d90d5663afd72563b2e5935234322a549e6ff` tras merge protegido del PR #3838.
- Rama exclusiva: `work/solqaryn-p1-006-b`; Task `6adbb9fff1548191a1bd5b30d9e9984a`, GitHub `jmejia31`.
- Objetivo: probar el rechazo de enumeración empresarial entre usuarios/tenants, revocación, bloqueo sin JWT y contrato del endpoint protegido. NO concede nuevos permisos.

## Matriz de prueba dirigida
1. Actor A con membresías vigentes A+B: sólo obtiene A+B, no C de otro actor.
2. Actor B con membresía C: sólo obtiene C; la elección de A o B no le da contexto.
3. Revocar la membresía B de A: desaparece del listado y el contexto B se rechaza.
4. UsuarioId suministrado por una fuente sin autenticación: listado vacío y contexto nulo.
5. Cuenta A desactivada: listado vacío aunque la identidad anterior continúe expuesta por la sesión simulada.
6. El método `GET /tenant-context/empresas` conserva `[Authorize]` y no acepta el ID de empresa como parámetro del cliente.

La prueba xUnit usa `AppDbContext` in-memory y `ICurrentUserService` simulado; NO sustituye la aceptación E2E JWT/MySQL. Se reutiliza la evidencia de P1-006.A: PR #3838, E2E 100/100 y auto-cert PASS, pero la microtarea B requiere su propia prueba dirigida y CI de su SHA final.

## Impacto y regresión
- Cambio de pruebas y documentación solamente: sin cambios de API, contratos runtime, controladores, EF/migraciones, Angular, datos, secretos ni infraestructura.
- Riesgo: falsa conclusión sobre MySQL real a partir del proveedor in-memory. Mitigación: conservar regresión E2E/MySQL aplicable, checks de seguridad multiempresa y pruebas dirigidas; no afirmar ensayo LIVE que no se ejecutó.
- Validación: `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --filter FullyQualifiedName~P1006BSelectorEmpresarialAislamientoTests` en entorno de CI, además de Scope Lock, backend y seguridad sobre SHA exacto; verificar resultados definitivos antes de VALIDANDO/LISTO.
- Rollback: revertir exclusivamente los archivos de prueba/documentación mediante PR protegido, sin tocar P1-006.A ya integrado.
- Cierre institucional: sólo después de merge protegido, CI postmerge aplicable y readback PLAN_MAESTRO/COLA (fórmula intacta)/BITACORA.
