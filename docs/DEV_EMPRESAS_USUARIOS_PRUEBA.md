# SOLQARYN — empresas, usuarios y credenciales de prueba de DEV

**Autoridad:** declaración expresa del propietario el 2026-11-02 (America/Tegucigalpa).
**Entorno autorizado:** `solqaryn/Solqaryn`, rama `dev`, GitHub Actions Environment `DEV`,
backend Render `solqaryn-api-dev` y Aiven MySQL **`solqaryn_dev`**.

## Clasificación de los datos de DEV

El propietario confirma expresamente que **todas las empresas y todos los usuarios actualmente
cargados en `solqaryn_dev` son datos de prueba**. En particular, las tres identidades
administrativas utilizadas por las pruebas actuales son identidades de ensayo. Las empresas
y sus membresías de DEV pueden utilizarse para comprobaciones y certificaciones funcionales
autorizadas, sin tratar esos datos de ensayo como clientes de producción ni exigir un
prefijo `qa_` para reconocerlos.

Esta declaración **no sustituye las comprobaciones de identidad, titularidad, JWT,
RBAC, aislamiento por empresa, autorización del actor, auditoría ni restauración**.
Las operaciones de prueba deben proteger los accesos existentes o restaurarlos
antes de finalizar. No autoriza cambios fuera de DEV ni expone contraseñas.
Si se importan datos reales en DEV en el futuro, deberán identificarse y
excluirse antes de ejecutar operaciones mutables.

## Identidades y localización de contraseñas

La correspondencia siguiente proviene de los workflows GitHub actuales
`.github/workflows/dev-mf3-live-abc-readonly.yml` y
`.github/workflows/p1-005-b-live-enterprise-roles-dev.yml`, y del runbook
`docs/runbooks/P4_IDENTIDAD_PLATAFORMA_JAVIER_DEV.md`.
La clasificación «prueba» procede de la autorización del propietario; la relación
y el estado efectivo de cada cuenta y empresa siempre se revalidan en MySQL DEV
al momento de ejecutar la prueba.

| Usuario de DEV | Contexto de prueba documentado | Variable de contraseña en GitHub Environment DEV |
| --- | --- | --- |
| `admin_prueba_a` | Administrador empresarial A, `EmpresaId=2` | `SOLQARYN_DEV_B_PASSWORD` |
| `admin_prueba_b` | Administrador empresarial B, `EmpresaId=3` | `SOLQARYN_DEV_C_PASSWORD` |
| `javier` (nombre en el runbook de plataforma) | Identidad de plataforma de ensayo; slot C del flujo A/B/C ligado históricamente a `EmpresaId=1` | `SOLQARYN_DEV_A_PASSWORD` |

**Importante: los sufijos A/B/C de los secretos son slots históricos**, no la
correspondencia literal con las nuevas empresas A/B. El workflow A/B/C usa
`SOLQARYN_DEV_A_USERNAME` como nombre de usuario del tercer slot
(`EmpresaId=1`); su valor es un secreto que GitHub no permite leer tras
crearlo. El runbook de plataforma identifica `javier`, pero su vigencia
actual debe confirmarse contra la identidad real y el preflight, nunca
inventando el contenido del secreto.

**Ubicación exacta:** repositorio GitHub
`solqaryn/Solqaryn` → **Settings → Environments → DEV → Environment secrets**.
Los nombres de secretos son visibles por permisos; **sus valores no se pueden
recuperar en texto claro desde GitHub**. Se reemplazan o rotan en ese mismo
Environment cuando haga falta. Nunca poner valores de contraseñas, hashes ni
tokens en Git, documentación, PRs, logs o bitácoras.

Las consultas de certificación a MySQL DEV usan `vars.SOLQARYN_DEV_DB_HOST`,
`vars.SOLQARYN_DEV_DB_PORT`, `vars.SOLQARYN_DEV_DB_NAME` y
`vars.SOLQARYN_DEV_DB_USER`; la contraseña se referencia como
`secrets.SOLQARYN_DEV_DB_PASSWORD` del Environment `DEV`.
La API exacta para los tests actuales es
`https://solqaryn-api-dev-fxx8.onrender.com` y la base
`solqaryn_dev` con usuario técnico `solqaryn_dev_user`.

## Regla operativa de certificación P1-005.B

1. Usar **las cuentas A/B existentes** para probar login JWT real, membresías,
   roles administrativos legítimos y denegación entre empresas. No bloquear
   porque sus nombres no comiencen con `qa_`.
2. Para cambiar y revocar un rol sin degradar las tres cuentas originales,
   el certificador crea **una identidad efímera no administradora de DEV**
   exclusivamente en empresa A mediante `POST /usuarios` con password
   aleatorio generado dentro del job, sin imprimirlo ni almacenarlo.
3. Tras probar el cambio de rol empresarial y la auditoría duradera, se
   restaura por `PUT /usuarios/{id}` el rol inicial y se elimina lógicamente
   la identidad temporal mediante `DELETE /usuarios/{id}`.
   Se comprueba con SQL **solo SELECT** que el usuario temporal queda
   inactivo/eliminado y que los roles/membresías originales A/B no cambiaron.
   Las filas históricas de auditoría y borrado lógico se conservan por diseño.
4. Si la creación termina de manera ambigua, la limpieza localiza únicamente
   el nombre aleatorio propio y comprueba membresía única A y ausencia de rol
   de plataforma antes de restaurar o eliminar. Un cleanup no verificable
   produce `NO_GO`; **nunca se anuncia PASS ficticio**.
5. La certificación definitiva exige logs LIVE HTTP/JWT/MySQL, pruebas exact-SHA,
   Scope Lock, integración por PR protegido y readback del plan vigente.
   No crear privilegios globales ni publicar claves.

**Fuera de alcance:** entornos QA, `main`, PROD y sus datos/servicios/secretos.
No confundir «usuario QA de certificación» (cuenta de ensayo en DEV) con
el Environment denominado QA.
