# P4 — Microfase 1: identidad de plataforma de Javier (DEV)

## Objetivo y alcance

Verificar la cuenta exacta `Javier` en MySQL Aiven DEV, su rol heredado y su autoridad global real. **Esta microfase no asigna permisos, no cambia contraseñas, no reactiva la empresa histórica inactiva y no crea empresas.** SOLQARYN es la plataforma, no un tenant empresarial.

Repositorio: `solqaryn/Solqaryn`. Rama y Environment GitHub permitidos: `dev` y `DEV`. Prohibidos QA, PROD y `main`.

## Operación autorizada por el propietario — 2026-10-15 (DEV únicamente)

El 2026-10-15 se confirmó directamente en Aiven DEV con el run #38793182381:
la cuenta persistida es `javier` (minúsculas), una sola coincidencia, activa, con rol empresarial `Administrador` (`Ambito=1`) y una membresía empresarial inefectiva debido a que la única empresa está inactiva. No existe ningún rol administrador de plataforma activo ni ninguna asignación global previa.

El propietario **autoriza expresamente** crear un rol administrador de plataforma propio de SOLQARYN y asignarlo exclusivamente a esa cuenta, sin modificar contraseñas, rol heredado, membresía empresarial ni empresa inactiva. La ejecución está implementada en:

- `.github/workflows/p4-javier-authorized-platform-grant.yml` (trigger protegido: push a `dev` con marcador único `[P4-JAVIER-OWNER-APPROVED]`; rollback separado por confirmación explícita).
- `scripts/ops/p4_javier_platform_grant.py` (verificación de cuenta, rol global único, transacción de asignación y dos registros de auditoría; comandos `preflight`, `apply`, `verify`, `rollback`).

**Respaldo previo obligatorio:** reutilizar `scripts/backup_dev.sh` para dump real cifrado AES-256 de `solqaryn_dev`; efectuar restore del *mismo artefacto* únicamente en MySQL 8.4 descartable mediante `scripts/restore_dev.sh`, exigiendo SHA-256 y conteos equivalentes. Si falta la clave de cifrado, falla la restauración, cambia la identidad, el schema o el alcance, STOP: **cero DML de asignación**. Artefacto cifrado retenido temporalmente 14 días en GitHub Actions, sin texto claro ni secretos publicados.

**Aplicación transaccional y fail-closed:** comprobar `solqaryn/Solqaryn`, rama `dev`, `solqaryn_dev` en el host Aiven autorizado, cuenta única `javier` activa, único tenant preexistente inactivo, rol empresarial original intacto, ausencia de cualquier asignación global y ausencia de otro administrador de plataforma activo. Adquirir advisory lock MySQL. En una transacción crear únicamente `Roles.NombreNormalizado=SOLQARYN_ADMIN_PLATAFORMA` con `Ambito=2`, `EmpresaId=NULL`, `EsAdministrador=1`, `EsSistema=1` y su única fila activa `UsuarioRolesPlataforma` para la cuenta. Insertar dos filas `RegistrosAuditoria` con operador externo indicado, motivo, SHA de código y correlación; no fingir autorización previa de JWT/rol. Hacer readback desde otra conexión de MySQL y exigir que el nuevo rol no tenga ningún permiso empresarial en `RolPermisos`.

**Reversión sin borrado destructivo:** mediante dispatch sobre `dev` y confirmación explícita `REVOKE_ONLY_P4_JAVIER`, volver a crear backup + restore previo, luego desactivar exclusivamente la asignación y el rol creados por esta operación, en una única transacción auditada. STOP si otra cuenta comparte el rol, si se añadieron permisos, si cambió el ownership, o si la identidad es ambigua. Las filas de auditoría permanecen. Nunca borrar ni cambiar `Usuarios`, `UsuarioEmpresas` o `Empresas`.

**Límite deliberado:** este bootstrap da autoridad global en MySQL, pero NO activa la navegación del frontend ni otorga acceso libre a datos comerciales de clientes. Las microfases 2–4 prueban autenticación independiente, RBAC de plataforma, UX y aislamiento A/B. No activar `CommercialIsolationCertified` ni `PlatformOnboardingEnabled`.


## Ejecución

La auditoría `.github/workflows/p4-platform-javier-identity-audit.yml` se ejecuta en la primera fusión del workflow a `dev`, y permite revalidación posterior mediante `workflow_dispatch` desde `dev`. Consulta con `START TRANSACTION READ ONLY` y TLS requerido la base `solqaryn_dev` utilizando los secretos **ya existentes en GitHub Environment DEV**; jamás copia secretos Render ni registra datos sensibles.

La evidencia consta de:
- `EXACT_LOGIN_COUNT`: diagnóstico de capitalización. La identidad autoritativa exige `CASE_INSENSITIVE_LOGIN_COUNT=1`, se muestra en `STORED_LOGIN` y se valida su estado real.
- `CASE_INSENSITIVE_LOGIN_COUNT`: exige una sola cuenta coincidente sin distinguir mayúsculas, bloqueando identidades ambiguas.
- `ACCOUNT_STATUS`: `ACTIVE` requiere `Usuarios.Activo`, `!Bloqueado`, `!Eliminado`.
- `LEGACY_ROLE`: sólo diagnóstico; `Usuarios.RolId` NO concede autoridad de plataforma.
- `ACTIVE_PLATFORM_ADMIN_ASSIGNMENTS`: válida únicamente con `UsuarioRolesPlataforma.Activa`, usuario activo y rol de ámbito `Plataforma` (`Roles.Ambito=2`), `EmpresaId=NULL`, rol activo/no eliminado y `EsAdministrador=1`.
- `ACTIVE_PLATFORM_ADMIN_ROLE_CANDIDATES`: verifica disponibilidad de rol global verdadero.
- Membresías empresariales y conteos de empresas únicamente informativos; ninguna empresa inactiva concede acceso.

`PLATFORM_IDENTITY_PRESENT` sólo cuando hay exactamente un usuario activo y exactamente **una** asignación global administrativa válida. En cualquier otro estado: `STOP_NO_AUTOMATIC_PRIVILEGE`, con evidencia y sin mutación. Esto no equivale todavía a poder entrar al frontend: esa validación corresponde a las microfases 2–4.

## Condiciones previas de asignación, ahora autorizadas (no saltables)

Si falta `UsuarioRolesPlataforma`, no modificar `SeedAdmin`, `Usuarios.RolId`, `UsuarioEmpresas` ni `Empresas`. La presente autorización sí permite crear un nuevo rol global específico de SOLQARYN y su membresía, únicamente tras backup real restaurado en CI y validación transaccional. El JWT o el rol empresarial heredado no justifican autoridad global.

La asignación autorizada requiere y verifica:
1. Constancia de identidad/autenticación de la cuenta exacta `Javier`, y cuenta vigente, no bloqueada ni eliminada.
2. Ausencia de rol global administrativo activo y de cualquier colisión con `SOLQARYN_ADMIN_PLATAFORMA`. Crear exactamente un rol nuevo de plataforma (`Ambito=2`, `EmpresaId=NULL`, `EsAdministrador=1`, `EsSistema=1`, activo) y no importar permisos empresariales.
3. Inspección de todas las asignaciones `UsuarioRolesPlataforma` existentes, incluidas inactivas y de otros roles. Si hay ambigüedad o asignación duplicada, STOP.
4. Backup DEV verificado y aprobación de cambio de privilegios con motivo, operador, alcance, SHA y readback.
5. Únicamente tras esas condiciones, ejecutar el script transaccional parametrizado que crea el rol y `UsuarioRolesPlataforma` e inserta dos entradas en `RegistrosAuditoria` con actor externo propietario, ámbito de cambio, acción, motivo y SHA. No registra contraseñas, hash ni JWT.
6. Readback posterior contra MySQL desde conexión nueva, sin ninguna empresa activada y sin permisos de negocios otorgados. Si no se puede completar el commit, rollback de la transacción. Para revertir después, desactivar exclusivamente la asignación y el rol nuevos, con auditoría y comprobación de ownership. El test HTTP negativo de usuario empresarial corresponde a la microfase backend.

No relajar el guard multiempresa, no tocar `CommercialIsolationCertified`, y no conceder acceso libre a datos privados comerciales por tener rol de plataforma.

## Secretos y contrato Render

`SeedAdmin__Username` / `SeedAdmin__Password` están **retirados** del contrato actual de Render; su eventual presencia en una instalación antigua no determina privilegios. `SeedAdmin` sólo crea el usuario si no existe. El dato autoritativo es MySQL DEV y la membresía de plataforma verificada.

## Condición de cierre

Microfase 1 `LISTO` requiere: identidad única activa; backup cifrado y su restauración equivalente PASS; creación y membresía global verificadas contra Aiven DEV; bitácora de asignación; CI de código, SHA y rollback documentado; cero cambios de empresa, contraseña o rol heredado. Nunca declarar acceso web habilitado antes de las microfases posteriores. 
