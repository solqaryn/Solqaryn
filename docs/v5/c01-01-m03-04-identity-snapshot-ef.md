# C01-01.M03.04 — adaptador de instantanea de identidad (DEV)

Alcance: implementar un origen EF Core 10, de solo lectura y sin registro de DI, para el contrato Application IIdentidadUniversalSnapshotSource. El guardia C01-01.M03.03 realiza la verificacion previa de tenant; el proveedor valida usuario/empresa/relacion/rol/sucursal por datos vigentes obtenidos del servidor, nunca por claims legacy o parametros no confiables.

## Limites
- Permiso positivo: usuario activo y no bloqueado, Empresa activa, membresia UsuarioEmpresa activa y rol empresarial vigente de la Empresa o plantilla empresarial de sistema; solo la membresia determina el RolId.
- Denegacion: empresa B, membresia revocada, usuario bloqueado/eliminado, rol de otro tenant, sucursal ausente/de otra Empresa, identificadores invalidos. Sin excepcion administrador legacy ni acceso por ausencia de datos.
- EF AsNoTracking y SQL parametrizado por LINQ. Las concesiones relacionales se consultan mediante Rol.Permisos/Permiso; comprobarlas corresponde al guard Application.
- No agrega schema/migraciones, transaction/writes, endpoints, DI, emision JWT, nuevas capacidades, eventos, webhook ni jobs. No promueve a QA/main/PROD.
- Las lecturas independientes no sustituyen una politica formal de coherencia transaccional bajo revocacion concurrente; no presentar como certificacion de autorizacion productiva.
- Pruebas xUnit InMemory de positivo A, negativo B, revocacion, rol ajeno, usuario bloqueado y sucursal no permitida. La prueba etiquetada Integration requiere MySQL 8.4 local descartable y deniega hosts remotos.

## Puertas
A0 diffs/secretos/CHANGELOG; A1 positiva/negativas; A2 tenant A/B, revocacion y denegacion; A3 .NET tests y MySQL local efimero; A4 CI backend Release, seguridad/authorization, integracion MySQL, Scope Lock, CHANGELOG y SHA exacto con readback PLAN/COLA/BITACORA. A5 independiente por otra Task Javier. Si cualquiera queda queued/in_progress/fail/skipped se mantiene VALIDANDO sin PASS inventado. Rollback normal git revert, no reset ni force push.
