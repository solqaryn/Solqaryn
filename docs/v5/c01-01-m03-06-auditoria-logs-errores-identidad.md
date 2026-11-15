# C01-01.M03.06 — Auditoria estructurada de identidad universal

## Entrega causal en dev

Se incorpora una envoltura opt-in de Application para auditar la guardia de autorizacion de identidad. La decision real permanece en la guardia y su consulta de membresia/permisos actuales. No se crea autoridad nueva desde UI, header, claim, rol legacy o DTO.

Se exige GUID de correlacion no vacio, EventId fijo 10306 y codigos cerrados ALLOW/NONE, DENY/ACCESS_DENIED, DENY/INVALID_SCOPE, ERROR/INTERNAL_ERROR, CANCELLED/REQUEST_CANCELLED. La bitacora del logger solo recibe decision, codigo y correlacion: nunca nombre, usuario, empresa, sucursal, JWT, payload, excepcion o dato secreto.

## Evidencia requerida

- A0: diff limitado a Application Security, xUnit de seguridad, documento y CHANGELOG.
- A1: ocho xUnit positivos/negativos para acceso legitimo, empresa B antes de I/O, membresia revocada, administrador sin permiso relacional, sucursal invalida, error interno, cancelacion y correlacion vacia.
- A2: pruebas de no filtracion de datos ni excepcion y preservacion del cierre fail-closed entre tenants A/B.
- A3: GitHub Actions backend y seguridad, con checks de autorizacion aplicables, exact SHA.
- A4: Scope Lock STRICT, procedencia V5 y CI causal completed/success de SHA publicado; queued/cancelled/skipped/failed no constituyen PASS.
- A5: revision genuina por otra de las seis Tasks Javier, distinta de esta autora.

## Alcance negativo y rollback

No modifica ni registra DI/API/endpoints, autenticacion JWT, RBAC, persistencia, migraciones, infraestructura, workers, dispatchers ni webhooks. No instala logs HTTP reales por si misma: el consumidor debera inyectar expresamente la envoltura en una futura unidad autorizada. No toca QA/main/PROD ni datos productivos. Rollback: git revert normal conservando historia compartida.
