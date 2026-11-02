# SOLQARYN — P1-004.B: certificación independiente de delegación DEV

## Dependencia y autoridad

Predecesor P1-004.A LISTO por PR #3819, reviewed jmejia31 en 3713a83b98e0cfc113dedd2ae888099391f152cc y merge protegido dev 665964be33f484b53a89cb59bf387ec27ccf5fc1. Este alcance es SOLO prueba LIVE, no reimplementación ni concesión comercial productiva.

## Script y workflow

- `.github/workflows/p1-004-b-live-delegation-dev.yml` ejecuta un contrato offline sin secretos en PR, y la prueba real SOLO tras push a `dev` protegido, con environment DEV y Node 24. El runner no imprime contraseñas, tokens ni datos de empresa.
- `scripts/ci/p1-004-b-live-delegation-dev.mjs` exige que la API sea exactamente `https://solqaryn-api-dev-fxx8.onrender.com`, que la conexión MySQL use Aiven `solqaryn_dev` y el principal `solqaryn_dev_user`, y que las dos identidades de prueba existentes sean las conocidas del workflow DEV de P1-003.B. Ninguna fuente legacy o PROD participa.
- Preflight MySQL solo SELECT: una autoridad global de sistema activa para `javier`, identidad empresarial existente `admin_prueba_a` sin asignaciones globales activas y membresía empresarial A intacta. Si hay otra asignación o falta credencial, fail-closed sin mutaciones.
- Preflight HTTP/JWT: login real de las dos identidades; `javier` entra a `/plataforma/sesion` y empresarial obtiene 403, al igual que el directorio delegado antes del grant. Un 404 indica Render sin el código de A desplegado y detiene el test.
- Negativo P1-004.B: la identidad empresarial no puede autoconcederse autoridad global antes ni después del grant, el superadmin tampoco puede autoasignarse delegación, y el delegado no entra a gestión global, soporte temporal ni membresías de empresa B por la consola.
- Positivo: concesión delimitada por POST `/plataforma/delegaciones` exclusivamente al usuario empresarial existente y diferente del superadmin; GET de directorio devuelve como máximo 50 pares empresaId/nombre con `private,no-store`, sin exponer datos de ERP.
- Revocación: DELETE del mismo grant y GET posterior usando el mismo JWT deben devolver 403. Consultas SELECT verifican grant fijo exacto, asignación final inactiva, ninguna autoridad superadmin y conteo de membresías sin cambio.
- Auditoría durable MySQL: verificar transacción de grant, consulta y revocación en `RegistrosAuditoria`, con resultado, módulo, acción, actor, referencia, ámbito Plataforma y EmpresaId nulo. Se usan motivos únicos asociados al commit, sin PII.
- `finally` revoca la delegación si la prueba falla después de un grant; cualquier cleanup no verificable queda expresamente NO_GO. No se usa DML SQL ni se crea usuario/membresía, empresa, contraseña o token de larga duración.

## Evidencia y criterio de cierre

El contrato offline no certifica B. Requiere job LIVE real SUCCESS, SHA y ejecución identificables, lectura de auditoría MySQL, CI exact-SHA, cierre y readback PLAN/COLA/BITACORA. Las pruebas EF InMemory de A no son sustituto. Si Render todavía sirve un SHA previo, esperar la publicación actualizada; no alterar el servicio automático ni saltarse el gate.

## Impacto

Solo archivos de CI, script y documentación. No modifica Angular, API, entidades, migraciones, RBAC runtime, proveedores o secretos. El resultado afecta únicamente evidencias de certificación. Rollback: revert protegido del workflow/script, sin revertir P1-004.A. La concesión temporal queda inactiva al finalizar.
