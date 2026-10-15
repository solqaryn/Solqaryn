# P4 — Microfase 1: identidad de plataforma de Javier (DEV)

## Objetivo y alcance

Verificar la cuenta exacta `Javier` en MySQL Aiven DEV, su rol heredado y su autoridad global real. **Esta microfase no asigna permisos, no cambia contraseñas, no reactiva VARISTOREHN y no crea empresas.** SOLQARYN es la plataforma, no un tenant empresarial.

Repositorio: `solqaryn/Solqaryn`. Rama y Environment GitHub permitidos: `dev` y `DEV`. Prohibidos QA, PROD y `main`.

## Ejecución

La auditoría `.github/workflows/p4-platform-javier-identity-audit.yml` se ejecuta en la primera fusión del workflow a `dev`, y permite revalidación posterior mediante `workflow_dispatch` desde `dev`. Consulta con `START TRANSACTION READ ONLY` y TLS requerido la base `solqaryn_dev` utilizando los secretos **ya existentes en GitHub Environment DEV**; jamás copia secretos Render ni registra datos sensibles.

La evidencia consta de:
- `EXACT_LOGIN_COUNT`: exactamente 1 es obligatorio, comparación sensible a mayúsculas.
- `CASE_INSENSITIVE_LOGIN_COUNT`: detectar si el login real usa capitalización distinta.
- `ACCOUNT_STATUS`: `ACTIVE` requiere `Usuarios.Activo`, `!Bloqueado`, `!Eliminado`.
- `LEGACY_ROLE`: sólo diagnóstico; `Usuarios.RolId` NO concede autoridad de plataforma.
- `ACTIVE_PLATFORM_ADMIN_ASSIGNMENTS`: válida únicamente con `UsuarioRolesPlataforma.Activa`, usuario activo y rol de ámbito `Plataforma` (`Roles.Ambito=2`), `EmpresaId=NULL`, rol activo/no eliminado y `EsAdministrador=1`.
- `ACTIVE_PLATFORM_ADMIN_ROLE_CANDIDATES`: verifica disponibilidad de rol global verdadero.
- Membresías empresariales y conteos de empresas únicamente informativos; ninguna empresa inactiva concede acceso.

`PLATFORM_IDENTITY_PRESENT` sólo cuando hay exactamente un usuario activo y exactamente **una** asignación global administrativa válida. En cualquier otro estado: `STOP_NO_AUTOMATIC_PRIVILEGE`, con evidencia y sin mutación. Esto no equivale todavía a poder entrar al frontend: esa validación corresponde a las microfases 2–4.

## Preparación de una eventual asignación, NO ejecutable automáticamente

Si falta `UsuarioRolesPlataforma`, **no** modificar `SeedAdmin`, `Usuarios.RolId`, `UsuarioEmpresas`, `Roles` o `Empresas` para forzar un acceso. El hecho de ser administrador empresarial o poseer JWT no equivale a autoridad global.

Una eventual asignación requiere, **en un acto posterior y autorizado por el propietario**:
1. Constancia de identidad/autenticación de la cuenta exacta `Javier`, y cuenta vigente, no bloqueada ni eliminada.
2. Un rol preexistente de plataforma (`Ambito=2`, `EmpresaId=NULL`, `EsAdministrador=1`, activo y no eliminado), validado en MySQL. Si no existe un rol único correcto, STOP y revisar el catálogo, sin crearlo por defecto.
3. Inspección de todas las asignaciones `UsuarioRolesPlataforma` existentes, incluidas inactivas y de otros roles. Si hay ambigüedad o asignación duplicada, STOP.
4. Backup DEV verificado y aprobación de cambio de privilegios con motivo, operador, alcance, SHA y readback.
5. Únicamente tras esas condiciones, inserción **transaccional y parametrizada** de `UsuarioRolesPlataforma(UsuarioId,RolId,Activa)` y entrada de `RegistrosAuditoria` que registre actor autorizado, ámbito plataforma, acción, motivo y referencia, **sin credenciales, hash ni JWT**. No hay SQL de inserción automática en este changeset.
6. Validación posterior con la misma auditoría, además de un test negativo con usuario empresarial. Si la asignación de plataforma no queda íntegra, rollback de la transacción; para revertir luego, desactivar exclusivamente la asignación nueva tras revisión de dependencias y auditoría.

No relajar el guard multiempresa, no tocar `CommercialIsolationCertified`, y no conceder acceso libre a datos privados comerciales por tener rol de plataforma.

## Secretos y contrato Render

`SeedAdmin__Username` / `SeedAdmin__Password` están **retirados** del contrato actual de Render; su eventual presencia en una instalación antigua no determina privilegios. `SeedAdmin` sólo crea el usuario si no existe. El dato autoritativo es MySQL DEV y la membresía de plataforma verificada.

## Condición de cierre

Microfase 1 `LISTO` requiere resultados reales MySQL, coincidencia única y decisión explícita: autoridad ya existente **o** ausencia diagnosticada con asignación preparada para revisión. Nunca declarar el acceso global habilitado en frontend hasta microfases posteriores. 
