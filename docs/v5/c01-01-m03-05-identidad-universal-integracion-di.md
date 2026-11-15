# C01-01.M03.05 — integración scoped de identidad universal (DEV)

## Alcance y decisión
La fuente EF de solo lectura de M03.04 y los casos de uso y guardias de M03.02/M03.03 ya existen. Esta microtarea implementa su integración efectiva mediante registro DI *scoped* en la API ASP.NET Core 10. Como la identidad vigente se consulta sincrónicamente por petición, no se requiere worker, cola ni proveedor externo. No se incorpora dispatcher/evento/webhook.

## Fronteras y riesgos
- Solo el backend registra `IIdentidadUniversalSnapshotSource` como `IdentidadUniversalSnapshotEfSource`; no recibe identidades de parámetros frontend.
- `IdentidadUniversalApplicationService` exige empresa y rol actual de membresía; `IdentidadUniversalAuthorizationGuard` requiere permisos relacionales activos, sin bypass por rol legacy.
- Cada scope crea un origen EF distinto sobre el `AppDbContext` de la misma petición; un contexto no se comparte entre empresas ni solicitudes.
- El cambio no agrega endpoints, JWT, claims, headers de identidad, migraciones, tablas, escrituras, cuentas, servicios externos, workers ni cambios de infraestructura.
- Los tests DI positivos y negativos usan EF InMemory aislado: prueban resolución por scope, pertenencia A/B, permisos vacíos y revocación; **no certifican** concurrencia transaccional MySQL ni operación productiva.

## Validación causal y reversión
A0: verificar diff de registro DI, código, documentación y CHANGELOG, sin secretos ni autoridad frontend. A1/A2: pruebas xUnit positivas y negativas con tenant A/B y revocación. A3: CI de backend .NET 10 y seguridad; integración EF MySQL efímera cuando la política de workflow causal la exija. A4: publicación FF en `dev`, SHA exacto de Scope Lock, suite causal completada/success, y readbacks PLAN/COLA/BITACORA. A5: revisión posterior independiente por una de las otras cinco Tasks Javier. Rollback: revert normal del commit, sin force-push, sin tocar datos.

Esta integración no activa operaciones nuevas para clientes ni autoriza QA/main/PROD.
