# P1-016.D — Rechazo dirigido de sesion de plataforma sin tenant (DEV)

## Autoridad y alcance
- Plan Comercial Inicial Unificado, unidad P1-016.D (orden 57, `PLAN_MAESTRO!58`), dependencia P1-016.C = LISTO, `COLA!G58=SI` al iniciar.
- Task autora: Javier :40, `TASK_ID=6adc2e55f47c81919f6374e561687076`, cuenta JAVIER_CHATGPT, GitHub jmejia31.
- Branch `work/solqaryn-p1-016-d` creada desde dev `0c280ab7c4b8a56b1a0674b3a3bca438d3078e1b`. Unico editor de esta microtarea en la ocurrencia nativa.
- No se modifican rutas, controllers, RBAC, JWT, DTO, persistencia, migraciones, secretos, flags, QA, main ni PROD.

## Prueba negativa unica (fixture sintetica)
Un administrador **empresarial**, miembro de la Empresa A, carece de `UsuarioRolPlataforma` y envia `GET /plataforma/sesion` con cabecera `X-Empresa-Id` de A. La cabecera comercial no le concede privilegio global.

`backend/tests/Solqaryn.Tests/P1016DDirectedDenialTests.cs` invoca la prueba existente `P1016APlatformWithoutTenantTests.P1016A_AdminEmpresarialNoHeredaAccesoGlobalPorRolNiPorHeader`, que construye contexto aislado EF InMemory por GUID, `UsuarioEmpresa`, rol `AmbitoAutorizacion.Empresa`, peticion HTTP sintetica y controller real `PlataformaSesionController`.

Resultado causal esperado de la prueba heredada: el scope comercial puede resolver Empresa A, el scope global devuelve `null`, el controlador responde `ForbidResult`, se registra exactamente una auditoria `Rechazado` con `EmpresaId=null`, y no se concede `UsuarioRolPlataforma`.

## Comando dirigido y gate de evidencia
```bash
dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --filter FullyQualifiedName~P1016D --logger "trx;LogFileName=p1016d.trx"
```

CI debe probar `total>0`, `failed=0`, Scope Lock y los checks requeridos exact-SHA; luego revisar diff y seguridad multiempresa antes de entregar VALIDANDO. Este documento **no es un reporte de PASS**: no hay salida TRX local disponible en el entorno de esta Task y no se afirma ningun resultado hasta verificar GitHub Actions.

## Riesgos, limites y rollback
EF InMemory no sustituye la validacion DEV HTTP/JWT o MySQL real, ni acredita despliegue. Un fallo bloquea solo P1-016.D y se corrige en esta misma rama/PR sin bypass de CI. Revertir unicamente test y acta mediante nuevo PR protegido, preservando bitacora e historia. Revision y cierre LISTO quedan para otro TASK_ID diferente de la autora, con merge protegido y CI postmerge causal.
