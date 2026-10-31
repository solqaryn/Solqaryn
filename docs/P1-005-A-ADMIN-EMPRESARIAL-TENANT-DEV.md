# P1-005.A - Alta/edicion de usuarios con rol del tenant autorizado (DEV)

Alcance: ADMIN EMPRESARIAL, crear y editar usuarios sin aceptar roles privados de otra empresa, roles globales de plataforma ni roles heredados sin propietario. No se reabre P1-001.B ni se altera su PR.

## Control implementado

- El filtro RBAC de Usuarios determina la empresa autorizada en HttpContext y exige permisos Usuarios:Crear/Editar y Usuarios:AsignarRol donde corresponda.
- Antes de crear o editar, UsuariosController pasa exclusivamente ese EmpresaId verificado a IUsuarioService.ValidarRolEmpresaAsync.
- UsuarioService rechaza empresa no autorizada, rol inexistente/inactivo/eliminado y cualquier Rol que no cumpla Rol.EsAsignableAEmpresa(empresaId). Solo se admite rol empresarial propio o plantilla de sistema compartida.
- Se verifica antes de las escrituras y de las auditorias; no se alteran datos existentes ni la semantica de UsuarioEmpresa.

## Superficies e impacto

- Application: IUsuarioService + UsuarioService (validacion de rol declarada). API: UsuariosController POST/PUT (mismas rutas y DTO).
- Domain, Infrastructure, migraciones, JWT y frontend: sin cambios. La identidad global Usuario.RolId no sustituye membresia real de UsuarioEmpresa ni autoriza acceso cruzado.
- Riesgo compatible: otros consumidores directos de UsuarioService deben utilizar validacion contextual; esta entrega fortalece rutas API de alta/edicion, no declara certificacion global de todas las llamadas internas.
- Rollback: revert protegido del changeset; no se requiere rollback MySQL ni cambio de proveedores.

## Pruebas y limites

- Prueba dirigida: dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --filter FullyQualifiedName~P1005ATenantAdminUserRoleBoundaryTests
- Negativas: rol privado de B, rol de plataforma, rol legacy sin owner, ausencia de tenant, POST/PUT sin persistencia tras rechazo.
- Positivas: rol privado de A y plantilla empresarial sistema. Sin MySQL real ni JWT HTTP en este archivo; quedan como pruebas complementarias/CI, nunca PASS supuesto.
- Cierre solo tras todos los checks aplicables, aprobacion independiente mientras auto-cert no este activa en dev, merge protegido y readback institucional.
