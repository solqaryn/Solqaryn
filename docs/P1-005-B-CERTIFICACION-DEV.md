# SOLQARYN — P1-005.B | Certificación independiente de roles empresariales DEV

Estado de preparación: **VALIDANDO**. Alcance del Plan Comercial Inicial `PLAN_MAESTRO!A35`. P1-005.A está integrado y documentado en `dev@a654d03d6ca646306ae4f0fde31ee582b238c2ac`; PR A #3823, HEAD `296a3228`, APPROVED por Javier en ese HEAD, checks de PR y Scope Lock postmerge PASS.

## Aclaración de autorización y corrección LIVE (2026-11-02 Honduras)

El propietario autorizó expresamente usar **todas las empresas y usuarios existentes de DEV** como datos de prueba. Sus identidades, las claves de secreto de GitHub Environment DEV y la distinción entre slots históricos A/B/C están documentadas en [DEV_EMPRESAS_USUARIOS_PRUEBA.md](DEV_EMPRESAS_USUARIOS_PRUEBA.md). Los secretos nunca se copian ni revelan.

El segundo job postmerge de #3825 (SHA `b14dc2f5`, run `41569475372`) se detuvo en `P1005B_SAFE_QA_CANDIDATES_A=0` y `P1005B_NO_GO=SAFE_QA_USER_A_REQUIRED` antes de mutar. Esto **no** indica usuarios faltantes en DEV ni fallo confirmado del backend: era una restricción artificial a nombres `qa_`/roles no administrativos. La solución usa las cuentas administrativas A/B existentes para toda la prueba de JWT/RBAC/aislamiento y una cuenta adicional efímera, creada y eliminada lógicamente con API auditada, solo para el cambio real de rol. Se comprueba la restitución exacta y `Activo=0,Eliminado=1` de la cuenta efímera. Las tres identidades originales permanecen intactas.

Se exige un **nuevo** job LIVE SUCCESS del commit integrado, comprobación de restauración, y conciliación de Sheets antes de declarar LISTO.

## Ajuste de contrato HTTP tras LIVE postmerge del PR #3826 (2026-11-02 Honduras)

El run `41572046099`, intento #2, commit DEV `042270af`, **superó** el preflight MySQL A/B, el login y aislamiento HTTP/JWT y los negativos de roles de plataforma. La única detención posterior fue `P1005B_NO_GO=A_CANNOT_EDIT_B_HTTP_404`: el test exigía exclusivamente `403` ante `PUT /usuarios/{idB}` desde el administrador A, aunque el controlador llama a `GetByIdAsync` antes de editar y, con filtrado de alcance, devuelve `404` para identidades invisibles al tenant. La misma prueba de lectura cross-tenant ya acepta `403` o `404` como rechazo.

Se alinea **solo la expectativa del test**: la edición cruzada debe devolver exactamente `403` o `404`, nunca `2xx`, `400`, `401` o `5xx`, y además se comprueba por consultas SELECT a MySQL que la identidad y membresía de B mantuvieron sus campos relevantes intactos (incluida fecha de actualización). `P1005B_CROSS_TENANT_EDIT_DENIED_MYSQL=PASS` requiere ambas condiciones. No se modifica backend, autorización, fixture o datos. El resultado `404` no equivale a editar B; previene enumeración de identidades ajenas.

El cierre aún requiere **una nueva LIVE real postmerge**, restauración del rol efímero, borrado lógico verificado y conciliación del plan. No contabilizar como PASS el run fallido anterior.

## Corrección adicional: SQL ANSI_QUOTES tras LIVE postmerge #3827 (2026-11-02 Honduras)

La ejecución DEV del merge #3827 (`dev@390ca582`, run `41572965177`) acreditó `P1005B_MYSQL_TWO_TENANT_PREFLIGHT=PASS`, `P1005B_HTTP_JWT_A_B_ISOLATION=PASS` y `P1005B_CROSS_TENANT_EDIT_DENIED_MYSQL=PASS`: el acceso cruzado se rechaza y la cuenta B sigue intacta. La siguiente consulta, todavía antes de editar roles, falló con **MySQL ERROR 1054 Unknown column ':'** y `P1005B_NO_GO=MYSQL_SELECT_FAILED`.

Causa: `membershipDigest` concatenaba conteos usando `":"` como cadena SQL. Con el modo de SQL observado en Aiven DEV, las comillas dobles delimitan identificadores y `":"` se interpreta erróneamente como columna. Se reemplaza exclusivamente por `literal(':')`, que genera `CONVERT(UNHEX('3a') USING utf8mb4)`, independiente de `ANSI_QUOTES`. El contrato offline prueba el SQL resultante, incluyendo IDs y separadores, y rechaza expresiones heredadas con comillas dobles. Sin SQL DML ni cambios a backend, roles, credenciales o empresas. Se requiere nueva LIVE postmerge real: los PASS previos no sustituyen cambio/revocación, auditoría y limpieza.

## Evidencia requerida para declarar LISTO

- Correr `scripts/ci/p1-005-b-live-enterprise-roles-dev.mjs --self-test` sin credenciales en PR, con Scope Lock y sintaxis Node 24.
- **Solo después de PR de B aprobado y fusionado**, correr el job LIVE separado por `push dev` con secretos del environment `DEV`. Debe consultar el backend Render DEV desplegado con la versión de A, dos tenants A/B de prueba (IDs 2/3) y MySQL Aiven DEV exacto.
- Comprobar JWT y administrador A/B con membresías únicas y activas verificadas en MySQL, ausencia de roles de plataforma, aislamiento de GET roles/usuarios y rechazo de lectura entre empresas.
- Negativos HTTP 403: A asignando rol empresarial ajeno, rol de plataforma, editando usuario B; sin autenticación no hay acceso administrativo; A/B sin acceso a consola plataforma. Ningún negativo se cuenta como PASS si se obtiene 200.
- Positivo: edición idempotente del administrador de prueba A existente con **el mismo** nombre, rol y sin cambio de contraseña; comprobar membresía intacta, ausencia de grants plataforma y auditoría `Editar` durable.
- Positivo adicional: con las cuentas existentes A/B intactas, crear una **identidad efímera** mediante la API de DEV, con password aleatorio no registrado, membresía exclusiva A y rol empresarial no administrador. Cambiar temporalmente a la plantilla administradora *de empresa* de A, verificar `Usuarios.RolId` y `UsuarioEmpresas.RolId` y auditoría `Editar`, **restaurar el rol no administrador** y después eliminar lógicamente la cuenta efímera por API. La eliminación se acredita con `Activo=0,Eliminado=1`. Cualquier fallo de restitución o limpieza sigue siendo NO_GO.
- No se crean roles ni se alteran credenciales/roles de las cuentas originales. Solo se crea y elimina lógicamente por API una identidad efímera autorizada de **DEV**. SQL se limita a `SELECT` sin DML; los eventos de auditoría y la fila de borrado lógico se conservan. Véase `docs/DEV_EMPRESAS_USUARIOS_PRUEBA.md`.
- A no se reabre. B solo pasa LISTO después de LIVE SUCCESS sobre SHA real, revisión y Scope Lock exact-head, readback de `dev`, conciliación PLAN/COLA/BITACORA y verificación de que Render DEV publicó A.

## Corrección del preflight tras primera LIVE (2026-11-02 Honduras)

El primer job LIVE de B, run #41566437486, se detuvo en `P1005B_NO_GO=TENANT_OWNED_ROLE_2` antes de cualquier mutación: el test exigía un rol personalizado A que no estaba presente en MySQL DEV. **Eso no es un fallo de permisos del backend**. El modelo `Rol.EsAsignableAEmpresa` acepta roles de empresa propios y también plantillas de sistema con `EmpresaId=null` y `Ambito=Empresa`. El preflight ahora acepta ambos para asignación/restauración y no fabrica IDs. Roles realmente privados A/B se comprueban por `EmpresaId`; cuando no existe ninguno, el negativo HTTP privado se declara `NOT_APPLICABLE_NO_PRIVATE_ROLE` sin contarlo fraudulentamente como PASS. Los rechazos entre usuarios A/B, roles de plataforma y ausencia de autoridad global siguen obligatorios; xUnit valida roles propios vs plantilla vs plataforma.

Se añadió un control no destructivo: un `POST /usuarios` con nombre que ya existe y rol válido debe devolver 400 por duplicado (no 201 ni 403), antes de comprobar 403 para un rol ajeno real o global. Se verifica también que el listado HTTP no contiene roles de `Ambito=Plataforma`. La auditoría del mutador `UsuarioAdministracionEmpresaService.ActualizarAsync` registra `Accion=Editar` aun al cambiar `RolId`, no `AsignarRol`; el readback LIVE exige el valor que efectivamente escribe el backend.

La ausencia de un usuario `qa_` no es un bloqueo: el propietario confirmó que los usuarios y empresas de DEV son datos de prueba. El certificador usa las cuentas A/B existentes y crea su propio usuario temporal no administrador por API. En DEV las plantillas de sistema disponibles son `Vendedor` (inicio/restauración) y `Administrador` empresarial (transición efímera); no exigir dos roles no administradores. La cuenta temporal no se autentica ni tiene roles de plataforma. La restauración y el borrado lógico son obligatorios, sin falsos PASS. Certificación B = LISTO solamente si los controles aplicables son auténticos, las excepciones N/A están explícitas y el cierre está autorizado.

## Arquitectura e impacto

API/Angular/Domain/Application/Infrastructure: **sin cambios**. Solo script `scripts/ci/p1-005-b-live-enterprise-roles-dev.mjs` y workflow GH `.github/workflows/p1-005-b-live-enterprise-roles-dev.yml`, bajo permisos `contents:read` y environment `DEV`. Las contraseñas de A/B y MySQL se inyectan como secretos sin imprimir. MySQL bajo identidad exacta y consultas `SELECT`. No modificar QA/main/PROD, proveedor, migraciones, credenciales o roles de las tres cuentas preexistentes, tareas ni políticas de autorización.

### Riesgos / fail-closed

Si Render sigue en versión anterior de P1-005.A, las rutas autorizadas fallarán; no alterar el middleware ni admitir HTTP 503 como 403. No detener por ausencia de usuario `qa_` preexistente: crear identidad efímera en DEV usando la API autorizada. Si falla la creación, el permiso de borrado, la restauración o el readback, NO_GO; no exigir roles personalizados ni dos plantillas no administrativas. Si falla una restauración, registrar `P1005B_NO_GO=QA_ROLE_RESTORE_UNVERIFIED` e impedir cierre; se requiere conciliación manual por operador autorizado, nunca intentar rescate con rol global ni SQL DML. Reversión del código de pruebas mediante PR protegido.

### Evidencia pendiente

No declarar LIVE PASS hasta comprobar en el job de GitHub las marcas `P1005B_MYSQL_TWO_TENANT_PREFLIGHT=PASS`, `P1005B_HTTP_JWT_A_B_ISOLATION=PASS`, `P1005B_OWN_ROLE_IDEMPOTENT_EDIT_AUDIT=PASS`, `P1005B_ROLE_ASSIGNMENT_REVOCATION_MYSQL=PASS` y `P1005B_LIVE_DEV_HTTP_JWT_MYSQL=PASS` sin fallos y con readback MySQL.
