# P1-001.B — Certificacion dirigida del rechazo de roles entre tenants (DEV)

**Scope:** P1-001.B, prioridad P1, PLAN_MAESTRO fila 27, dependencia P1-001.A LISTO. **Propietario:** Task `6adc2e4bd8a48191ac7d8641ed95a1a3` (Javier Primary Dev 02), cuenta GitHub `jmejia31`. **Base GitHub del claim:** `dev@0cec1a3b06793c95dceb3f316c1ae4cb22951f76`. La rama exclusiva es `work/solqaryn-p1-001-b`. Autoridad: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES y contrato DEV v2.0, Scope Lock STRICT.

## Frontera probada

Un administrador de la empresa A no puede conceder a su membresia empresarial un rol personalizado cuyo `EmpresaId` sea B, ni un rol de ámbito Plataforma. El servicio `UsuarioService` comprueba `Rol.EsAsignableAEmpresa(empresaId)` antes de ejecutar `AddEmpresaAsync`, `UpdateEmpresa` o `SaveChangesAsync`. Un rol sin propietario ni carácter de plantilla no se convierte automáticamente en rol empresarial.

La prueba añadida `P1001BRoleAssignmentCertificationTests` llama al servicio real en memoria con repositorios simulados y cubre cinco negativas: rol de B, rol de plataforma, huérfano no plantilla, inactivo y eliminado. Para cada negativa intenta tanto alta como cambio de rol y verifica que la membresia previa permanece intacta y que no se invoca persistencia. La positiva usa un rol personalizado legítimo de A y comprueba tanto alta como actualización propias.

**Regresiones preexistentes, no duplicar ni exagerar:** `UsuarioTenantRoleOwnershipTests` ya comprueba denegaciones A/B y plantilla válida; `UsuarioTenantRoleOwnershipMySqlIntegrationTests` usa MySQL local descartable para validar lectura de membresías con rol cruzado; `P1001ARolesHttpJwtIsolationTests` prueba HTTP/JWT en TestServer con EF InMemory. Ninguna de estas fixtures demuestra por sí sola una asignación real rechazada por la API Render DEV usando cuentas JWT humanas ni una transacción en Aiven.

## Impacto y gates

- **Componentes afectados:** solo test backend y este documento; consumo de `Rol`, `UsuarioService` e interfaces ya existentes. Sin cambios en API, DTO, migraciones, frontend, infraestructura, MySQL, JWT, RBAC de runtime ni secretos.
- **Riesgo:** mocks no equivalen a API DEV/MySQL real; se mantiene declarado el límite. El caso real A/B que requiera credenciales autorizadas debe acreditarse separadamente con infraestructura aprobada, sin leer secretos desde la Task.
- **Validación dirigida:** `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --filter FullyQualifiedName~P1001BRoleAssignmentCertificationTests` y suite backend/seguridad de CI. CI y Scope Lock deben aprobar el SHA final del PR; sin dotnet local aquí no se afirma PASS.
- **Cierre:** revisión APPROVED previa al merge por login que no sea `jmejia31`, CI exact-SHA 3/3, merge protegido a dev, readback de PLAN_MAESTRO/COLA/BITACORA. No declarar `LISTO` por abrir PR ni por PASS de fixtures.
- **Rollback:** revertir este test y acta mediante PR protegido; no existen mutaciones de datos que revertir.

**Estado de entrega inicial:** candidato de prueba dirigido. La certificación institucional queda pendiente de GitHub CI, revisión independiente y readback.

## Proveniencia de CI: diferencia entre status asociado y checkout

En el primer SHA de PR `fc1322d77cd77be65d7957789dcd2a06b872c1f6` los tres checks requeridos aparecieron `SUCCESS` en GitHub y la suite backend registró 2912/2912 PASS, seguridad 952/952 PASS. **Sin embargo, los jobs nativos ejecutaron el commit de fusión sintético `c06f52bf744cc1d2cd1a7e65acdfe0c8e086b22d` (`refs/remotes/pull/3802/merge`), no un checkout directo de `fc1322d...`.** El status asociado no se presenta como prueba de checkout directo del HEAD.

La prueba dirigida adicional `.github/workflows/p1-001-b-role-isolation.yml` utiliza `actions/checkout` con `ref: ${{ github.event.pull_request.head.sha || github.sha }}` tanto en PR como en push, seguido de `dotnet test` filtrado. El resultado se certifica solamente después de verificar el log original, la identidad del SHA final y los seis escenarios (cinco mutaciones negativas y una positiva). El workflow no modifica Aiven ni ejecuta requests autenticadas a Render; la certificación completa de dichos proveedores sigue siendo independiente.
