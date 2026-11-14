# V5 FINAL C00-01.M03.04 — Adaptador de persistencia: paginacion segura

Autor: PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. Unico destino: `solqaryn/Solqaryn@dev`.

## Resultado exigido y causa
`OrdenCompraRepository.GetPagedAsync` evaluaba `(Page - 1) * PageSize` en int. `Page=int.MaxValue`, `PageSize=100` puede causar overflow, offset negativo y retorno accidental de primera pagina. La correccion normaliza `Page/PageSize`, calcula offset con `long`, y devuelve conjunto vacio con total filtrado si la pagina esta fuera de rango. Ninguna consulta extraordinaria ni privilegio nuevo.

## Impacto
Capa Infrastructure Repository, consumidores `OrdenCompraService.GetPagedAsync` y `OrdenesCompraController.Buscar`. Sin alteracion de API/DTO, EF model, MySQL schema, migraciones, interfaces, JWT, RBAC, filtros de empresa, frontend, servicios de terceros ni secretos. Mantener total tenant-scoped y ordenacion.

## Validacion real
- A0: diff acotado en persistencia, xUnit y documentacion; sin secretos, cambios de dependencias ni SQL crudo. Inspeccionar candidato antes de publicacion.
- A1: xUnit positivo pagina 1 y negativo pagina 2 / int.MaxValue; total siempre correcto.
- A2: no se modifica JWT, RBAC ni filtro tenant. La entidad legacy OrdenCompra no expone EmpresaId, por lo que este test NO afirma aislamiento A/B; las suites CI de seguridad verifican la cuarentena multiempresa por separado. InMemory NO acredita JWT/HTTP/MySQL live.
- A3: CI GitHub Actions real .NET 10 xUnit + security tenant + MySQL 8.4 Oracle por job efectivo de SHA nuevo; no contar jobs SKIPPED.
- A4: SHA exacto del commit directo FF, logs, rollback y readback PLAN/COLA/BITACORA.
- A5: revisora independiente con TASK_ID distinto; no promocion QA/main/PROD.
Rollback: `git revert` del commit propio tras revalidar HEAD, nunca reset/force. Sin escrituras productivas.
