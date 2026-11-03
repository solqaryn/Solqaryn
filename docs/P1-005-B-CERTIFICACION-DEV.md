# SOLQARYN — P1-005.B | Certificación independiente de roles empresariales DEV

Estado de preparación: **VALIDANDO**. Alcance del Plan Comercial Inicial `PLAN_MAESTRO!A35`. P1-005.A está integrado y documentado en `dev@a654d03d6ca646306ae4f0fde31ee582b238c2ac`; PR A #3823, HEAD `296a3228`, APPROVED por Javier en ese HEAD, checks de PR y Scope Lock postmerge PASS.

## Evidencia requerida para declarar LISTO

- Correr `scripts/ci/p1-005-b-live-enterprise-roles-dev.mjs --self-test` sin credenciales en PR, con Scope Lock y sintaxis Node 24.
- **Solo después de PR de B aprobado y fusionado**, correr el job LIVE separado por `push dev` con secretos del environment `DEV`. Debe consultar el backend Render DEV desplegado con la versión de A, dos tenants A/B de prueba (IDs 2/3) y MySQL Aiven DEV exacto.
- Comprobar JWT y administrador A/B con membresías únicas y activas verificadas en MySQL, ausencia de roles de plataforma, aislamiento de GET roles/usuarios y rechazo de lectura entre empresas.
- Negativos HTTP 403: A asignando rol empresarial ajeno, rol de plataforma, editando usuario B; sin autenticación no hay acceso administrativo; A/B sin acceso a consola plataforma. Ningún negativo se cuenta como PASS si se obtiene 200.
- Positivo: edición idempotente de administrador QA A con **el mismo** nombre, rol y sin cambio de contraseña; comprobar membresía intacta, ausencia de grants plataforma y auditoría `Editar` durable.
- Positivo adicional: usuario QA existente en A, de nombre estrictamente de prueba, sin rol de administrador, sin membresías adicionales ni autoridad global. Cambiar temporalmente su rol a otro rol A propio/no administrador, verificar en MySQL tanto `Usuarios.RolId` como `UsuarioEmpresas.RolId`, auditoría `AsignarRol`, y **restaurar el rol inicial por API**, verificando de nuevo ambos roles. Si falla o la restauración no se acredita, NO_GO y acción de recuperación; no alterar credenciales.
- No se crean usuarios/roles ni se cambia ningún registro productivo. SQL se limita a `SELECT`, sin DML; solo API auditable muta el rol temporal del fixture QA y lo restaura. Los eventos de auditoría se conservan.
- A no se reabre. B solo pasa LISTO después de LIVE SUCCESS sobre SHA real, revisión y Scope Lock exact-head, readback de `dev`, conciliación PLAN/COLA/BITACORA y verificación de que Render DEV publicó A.

## Arquitectura e impacto

API/Angular/Domain/Application/Infrastructure: **sin cambios**. Solo script `scripts/ci/p1-005-b-live-enterprise-roles-dev.mjs` y workflow GH `.github/workflows/p1-005-b-live-enterprise-roles-dev.yml`, bajo permisos `contents:read` y environment `DEV`. Las contraseñas de A/B y MySQL se inyectan como secretos sin imprimir. MySQL bajo identidad exacta y consultas `SELECT`. No modificar QA/main/PROD, proveedor, migrations, users reales, tareas ni políticas de autorización.

### Riesgos / fail-closed

Si Render sigue en versión anterior de P1-005.A, las rutas autorizadas fallarán; no alterar el middleware ni admitir HTTP 503 como 403. Si no existen usuario QA seguro o roles alternativos A/B, detener en preflight sin writes. Si falla una restauración, registrar `P1005B_NO_GO=QA_ROLE_RESTORE_UNVERIFIED` e impedir cierre; se requiere conciliación manual por operador autorizado, nunca intentar rescate con rol global ni SQL DML. Reversión del código de pruebas mediante PR protegido.

### Evidencia pendiente

No declarar LIVE PASS hasta comprobar en el job de GitHub las marcas `P1005B_MYSQL_TWO_TENANT_PREFLIGHT=PASS`, `P1005B_HTTP_JWT_A_B_ISOLATION=PASS`, `P1005B_OWN_ROLE_IDEMPOTENT_EDIT_AUDIT=PASS`, `P1005B_ROLE_ASSIGNMENT_REVOCATION_MYSQL=PASS` y `P1005B_LIVE_DEV_HTTP_JWT_MYSQL=PASS` sin fallos y con readback MySQL.
