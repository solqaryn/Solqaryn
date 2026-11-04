# P1-015.A — Auditoría de acceso privilegiado (DEV)

## Identidad, alcance y autoridad

- Microtarea: `P1-015.A` (PLAN_MAESTRO fila 54, prioridad P1, orden 53).
- TASK_ID autor: `6aeacb7820ac8191b7d768a6f33dfa09` (Javier :30), pool `JAVIER_CHATGPT`.
- Rama exclusiva: `work/solqaryn-p1-015-a`; base `dev@fa10d1a92c90ee06d693a3c89bc8586d5ffc9be2`.
- Dependencia `P0-012.B=LISTO`, WIP global 6 y máximo una microtarea propia por Task.
- Se trata de la revisión y cobertura de **lectura y cambio de autoridad global sensible**; no abre privilegios, permisos ni endpoints adicionales.

## Implementación existente reutilizada

| Operación real | Control de autoridad | Evidencia de auditoría |
|---|---|---|
| `POST /plataforma/delegaciones` | `PlataformaScopeService.ObtenerActualAsync` valida rol global de sistema, usuario activo y asignación única actual contra persistencia | `PlataformaDelegacionesController.Auditar` registra `UsuarioId`, `NombreUsuario`, `Motivo` validado, `CorrelationId`, `EmpresaId=null`, ámbito y origen Plataforma, resultado |
| `GET /plataforma/delegaciones/empresas` | `PlataformaDelegacionService.ObtenerDelegadoActualAsync`, doble revalidación antes de entregar datos | Auditoría estrictamente persistida antes de responder, motivo fijo `Lectura delegada explícita`, actor efectivo, correlación y `EmpresaId=null` |
| `DELETE /plataforma/delegaciones/{usuarioId}` | Solo superadmin real, sin autorevocación arbitraria | Motivo explícito, usuario actor, referencia y correlación dentro de la operación |
| Intento de autodelegación | Rechazo `Forbid` sin conceder el permiso | Evento `Denegado` con actor, motivo y correlación |
| Acceso por administrador de empresa o motivo vacío | Rechazo fail-closed | No se registra éxito ni se concede el rol |

`EmpresaId=null` en estas operaciones significa **ámbito de plataforma**, no fuga ni elección de un tenant. Los cambios empresariales por tenant conservan su `EmpresaId` en auditorías propias; jamás debe inferirse el tenant desde `X-Empresa-Id` no autenticado. `CorrelationId` debe proceder de `HttpContext.TraceIdentifier` normalizado por el middleware, no del header en crudo.

## Nueva cobertura automatizada

`backend/tests/Solqaryn.Tests/P1015APrivilegedAccessAuditTests.cs` contiene seis pruebas sobre controllers y servicios reales con EF InMemory y doble rol global/empresarial: concesión global auditada, lectura delegada auditada sin contaminación de tenant por header, revocación y bloqueo posterior, autodelegación rechazada/auditada, rechazo de identidad solo empresarial, y motivo vacío rechazado.

Ejecución dirigida solicitada:

```bash
dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1015APrivilegedAccessAuditTests --logger "trx;LogFileName=p1015a.trx"
```

**Estado al publicar este changeset:** pruebas redactadas, todavía sin resultado de CI ni TRX constatado. No atribuir PASS a falta de evidencia. Exigir ejecución con recuento >0, controles exact HEAD, Scope Lock y revisión técnica por otro TASK_ID. GitHub `AUTO_CERT=PASS` no es una revisión humana.

## Riesgos, limitaciones y reversión

- Pruebas InMemory: comprueban el contrato de autorización y persistencia lógica, **NO** certifican transacciones de MySQL 8.4 ni un servicio HTTP/JWT remoto DEV; cuando el gate lo exija, ejecutar la regresión de integración correspondiente.
- No se modificó `RegistroAuditoria`, esquema ni migraciones, servicios productivos, flags, roles, secretos, QA, main o PROD.
- El `GET /plataforma/empresas` superadmin ya registra lectura de directorio global con actor/correlación; su motivo no está exigido como entrada contractual. No introducir un nuevo header de motivo no autenticado sin diseño separado.
- Rollback: revertir **solo** las pruebas/documentación de P1-015.A mediante un PR protegido si el changeset falla; no eliminar auditoría histórica ya existente.
- Cierre: después de PR fusionado protegido y CI posmerge exact-SHA, **otro TASK_ID distinto del autor** valida evidencias y registra `LISTO` con readback de PLAN_MAESTRO, COLA calculada y BITACORA, una sola vez.
