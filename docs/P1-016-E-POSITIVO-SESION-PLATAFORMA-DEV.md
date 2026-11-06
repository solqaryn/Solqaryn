# P1-016.E — Prueba positiva de sesión de plataforma sin tenant (DEV)

## Autoridad y alcance

- Fuente única: Plan Comercial Inicial Unificado, `PLAN_MAESTRO!59`, orden 58, dependencia P1-016.D=LISTO.
- Ejecución nativa: PRIMARY_DEV_01, Javier :00, `TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a`; GitHub `jmejia31`. Autoría histórica de P1-016.E: este TASK_ID.
- Rama única: `work/solqaryn-p1-016-e`, creada desde `dev@9ec3107e6e6375b10ea75cfdd8ffb5bdeaa649db` con verificación de ausencia previa.
- Solo añade un wrapper de prueba xUnit, esta acta y una entrada en CHANGELOG_AI. No cambia UI, API, JWT, RBAC, rutas, entidades, migraciones ni configuración de servidores.
- La selección por `developerFor` es orientativa: el propietario autorizó `FALLBACK_PENDING_V4` para tareas pendientes libres durante el run nativo.

## Prueba positiva causal

`backend/tests/Solqaryn.Tests/P1016EDirectedPositiveTests.cs` ejecuta el positivo real de la fixture existente `P1016APlatformWithoutTenantTests.P1016A_SuperadminSinMembresiasEntraAunqueEmpresaPilotoEsteInactiva`.

Escenario: usuario con rol global válido, **sin membresías empresariales**, con Empresa Piloto desactivada. Aun si la solicitud incluye cabecera `X-Empresa-Id`, la autorización global no depende de una empresa activa ni de una membresía comercial.

Aserciones reutilizadas de la fixture: scope de plataforma no nulo; scope comercial ausente; respuesta `OkObjectResult`; `Cache-Control: private, no-store, max-age=0`; una auditoría `SesionPlataforma` con resultado `Exito`, el actor correcto y `EmpresaId=null`; no se crean `UsuarioEmpresa`.

## Evidencia exigida y límites

Comando causal:

```bash
dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --filter FullyQualifiedName~P1016E --logger "trx;LogFileName=p1016e.trx"
```

La salida debe demostrar `total>0`, `failed=0`, Scope Lock, Seguridad multiempresa y Build/pruebas backend en SUCCESS del SHA exacto. En el runtime de este autor `dotnet` no está disponible; **no se atribuye PASS local**. La CI de GitHub es asíncrona y sus resultados deben leerse por otra Task revisora.

El escenario es HTTP sintético con EF InMemory; **no certifica HTTP/JWT/MySQL remoto DEV** ni flags/deployments. No se altera ni se deshabilita ningún control de acceso. Un fallo se corrige en la misma rama/PR después de comprobar autoría, concurrencia y evidencia técnica nueva.

## Cierre y reversión

Entregar `VALIDANDO` con PR/SHA, liberar propietario operativo y no autoevaluar independientemente. Solo otra `TASK_ID` podrá emitir `TECH_REVIEW_PASS`; merge protegido y CI postmerge exacta son previos al cierre `LISTO` con readback PLAN/COLA/BITACORA.

Rollback por nuevo PR protegido que revierta exclusivamente prueba, acta y changelog; conservar trazabilidad y seguridad de QA/main/PROD.
