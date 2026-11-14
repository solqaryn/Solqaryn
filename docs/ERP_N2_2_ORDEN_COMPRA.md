# ERP-N2.2 — OrdenCompra

## Dictamen

**Estado funcional:** `LISTO / CIERRE DOCUMENTAL EN CURSO`.

**Baseline funcional certificado:** `b4d477e2de25077c459d02b479968c93c93bc910`.

**Tree funcional:** `8ff8dc684fc6a38afec9d901a402fa75fc4e97ed`.

**Regresión principal:** Development `#32218997006` SUCCESS, Acceptance `#32218996971` SUCCESS, Fase 8 `#32218996994` SUCCESS, M10 `#32218996973` SUCCESS y M13 `#32218996978` SUCCESS.

Este documento es la fuente canónica final de ERP-N2.2. El preflight `docs/ERP_N2_2_ORDEN_COMPRA_PREFLIGHT.md` permanece como antecedente histórico y de decisiones de alcance.

## 1. Alcance y autoridad documental

ERP-N2.2 introduce `OrdenCompra` como documento empresarial independiente de `SolicitudCompra`, `Compra`, `RecepcionCompra` y `FacturaProveedor`.

La orden representa el **compromiso comercial** con el proveedor: proveedor, moneda, condiciones, fecha esperada, líneas, precios, descuentos, impuestos, observaciones y aprobación. No representa recepción física ni contabilización de factura.

Fronteras obligatorias:

- aprobar una OrdenCompra **no** aumenta `ExistenciaVariante`;
- no genera Kardex por sí sola;
- no crea `Compra`, cuentas por pagar, movimiento financiero ni asiento contable;
- la recepción física pertenece a ERP-N2.3;
- la factura de proveedor pertenece a ERP-N2.4;
- el three-way match Orden/Recepción/Factura pertenece a ERP-N2.5.

## 2. Lifecycle e invariantes

Lifecycle canónico:

```text
Borrador -> PendienteAprobacion -> Aprobada
   \-----------------------------> Cancelada
               \------------------> Cancelada
```

Reglas principales:

- sólo `Borrador` es editable;
- enviar a aprobación exige documento válido y al menos una línea;
- aprobar exige estado `PendienteAprobacion`;
- cancelar exige actor válido y motivo no vacío;
- moneda usa código ISO de tres caracteres;
- proveedor y snapshot de proveedor son obligatorios;
- `SolicitudCompraId`, cuando existe, debe ser válido y conserva identidad separada;
- cada detalle exige cantidad positiva, precio no negativo y descuentos/impuestos consistentes;
- la idempotencia persiste `IdempotencyKey + SHA-256 fingerprint` de forma atómica y no permite sustituirlos.

La entidad conserva fechas/actores de envío, aprobación y cancelación, más snapshot del aprobador.

## 3. Persistencia MySQL y migración

N2.2.C materializó la migración:

`20260818204700_N2_2_OrdenCompraPersistencia`

Commit funcional: `adff03723b4336b570328179e468e8470e611b95`.

La migración crea de forma aditiva:

- `OrdenesCompra`;
- `OrdenCompraDetalles`;
- PK/FK e índices de proveedor, solicitud, estado/fecha y número de orden único;
- constraints de estado, moneda, aprobación/cancelación y valores monetarios;
- validación producto↔variante mediante triggers;
- guards pre/post fail-closed.

El `Down` está protegido: sólo permite eliminar tablas cuando `OrdenesCompra` y `OrdenCompraDetalles` están vacías; después elimina triggers y objetos del punto. No se considera un mecanismo operativo normal sobre datos reales.

**Evidencia MySQL:** M12 `#32184108722` SUCCESS aplicó la migración en MySQL 8.4 y levantó API/readiness. El Development actual `#32218997006` SUCCESS aplica las migraciones actuales y ejecuta las pruebas `Category=Integration` en MySQL 8.4 sobre el baseline final.

## 4. Aplicación, API e idempotencia

La API canónica es `/ordenes-compra` y está protegida globalmente por autenticación y permisos relacionales por acción:

- `GET /ordenes-compra` — `Compras:Ver`;
- `GET /ordenes-compra/{id}` — `Compras:Ver`;
- `POST /ordenes-compra` — `Compras:Crear` + `Idempotency-Key` obligatorio;
- `PUT /ordenes-compra/{id}` — `Compras:Editar`;
- `POST /ordenes-compra/{id}/enviar-aprobacion` — `Compras:Confirmar`;
- `POST /ordenes-compra/{id}/aprobar` — `Compras:Aprobar`;
- `POST /ordenes-compra/{id}/cancelar` — `Compras:Anular`.

La creación rechaza un `Idempotency-Key` ausente/vacío antes de invocar negocio. El servicio valida formato/longitud, replay legítimo y conflicto de la misma clave con payload diferente.

Consulta/listado soportan paginación y filtros empresariales. Los errores de recurso inexistente y reglas de negocio se mantienen fail-closed mediante ProblemDetails/contrato común.

## 5. Frontend y UX

N2.2.E quedó dividido y certificado en tres bloques:

- **E.1** shell, navegación, modelo/servicio HTTP, listado, filtros, estados de carga/vacío/error y RBAC;
- **E.2** formulario crear/editar, proveedor, solicitud aprobada opcional, moneda/condiciones/fecha esperada/observaciones, líneas producto-variante, totales e `Idempotency-Key` estable por intento;
- **E.3** lifecycle UI para enviar, aprobar y cancelar, confirmaciones, motivo obligatorio, visibilidad por permiso/estado y E2E accesible.

Baseline E.3: `f9000061a8312124b02d50325aa70310035910dc`, certificado por Development, Acceptance, M10, Fase 8 y M13.

## 6. RBAC, auditoría, seguridad y observabilidad

N2.2.F certificó:

- permisos relacionales exactos por endpoint, sin bypass administrativo efectivo;
- auditoría transaccional de creación/edición/lifecycle;
- correlation-id y logging seguros mediante infraestructura común;
- snapshot EF reconciliado para SolicitudCompra/OrdenCompra;
- no exposición de secretos ni cambios de infraestructura productiva.

La superficie OrdenCompra continúa documental incluso después de aprobar: ninguna ruta de N2.2 debe materializar recepción, stock o finanzas.

## 7. QA y regresión

N2.2.G quedó subdividido para mantener cardinalidad y changesets pequeños:

### G.1 — backend, contratos, seguridad e idempotencia

Baseline `23fa5ac6c0390ba8e3236ee794924a2fac2d990b`.

Certificó unit/contract/runtime, 401/403, idempotencia replay/conflict y fail-closed. Development, Acceptance, Fase 8, M10, M13 y recovery MySQL quedaron verdes.

### G.2 — frontend, E2E y performance

Baseline final `b4d477e2de25077c459d02b479968c93c93bc910`.

Cobertura añadida para paginación/performance y limpieza de filas stale ante error + retry. Los cinco gates principales quedaron SUCCESS.

### G.3 — MySQL, migración y regresión CI

Certificó la migración N2.2.C sobre MySQL 8.4, integración actual y regresión final. El Development final `#32218997006` incluye `dotnet ef database update`, integration tests MySQL y verificaciones de migración; M13 `#32218996978` quedó SUCCESS.

No fue necesario modificar CI: no apareció un hueco causal relacionado con OrdenCompra.

## 8. Trazabilidad A-H

- **N2.2.A** — preflight: `73ef31c49f08c8bff9732978ffc86dbe74e0a116`.
- **N2.2.B** — dominio/contratos: `88047cde42929c1b2dcd8faf77da1c6543a2f2a9`, corrección `f17983ef49bb8f5032e6fb328564f36c02f103b9`.
- **N2.2.C** — persistencia/migración: `adff03723b4336b570328179e468e8470e611b95` y guards pre/post previos.
- **N2.2.D** — aplicación/API/idempotencia: hasta `a5340f991b0f93438ac184afeac41cc9ed82a756`.
- **N2.2.E** — frontend/UX: E.1 `26a7eada...`, E.2 `9ede060d...`, E.3 `f9000061...`.
- **N2.2.F** — RBAC/auditoría/seguridad/observabilidad y snapshot EF: hasta `1eb26cf60a3d4e1e37f9c89b60929f432de3c1ac`.
- **N2.2.G** — QA final: G.1 `23fa5ac6...`; G.2/G.3 baseline `b4d477e2...`.
- **N2.2.H** — este paquete documental, runbook, ADR y reconciliación final de estado.

## 9. Definition of Done N2.2

N2.2 sólo puede cerrarse cuando:

- A-G permanecen `LISTO` con evidencia real;
- lifecycle/invariantes/idempotencia están protegidos por pruebas;
- migración e integración MySQL 8.4 están verdes;
- backend, frontend, E2E, RBAC, auditoría y observabilidad están certificados;
- no existe efecto de stock/Kardex/costeo/finanzas por aprobar una orden;
- documentación, runbook, ADR, `CHANGELOG_AI.md`, `TASKS.md` y tablero operativo quedan reconciliados;
- el commit documental no invalida el baseline funcional.

## 10. Rollback y recuperación

- No cambiar estados directamente en base de datos como recuperación ordinaria.
- Revertir código mediante commit forward sobre `Desarrollo`; nunca force-push.
- Preferir migraciones correctivas forward sobre datos reales.
- El `Down` de N2.2.C sólo es admisible cuando las tablas de OrdenCompra están vacías; si existen datos, el guard debe abortar.
- Para pérdida/corrupción, usar backup/restauración certificados en ambiente autorizado.
- Producción permanece fuera de alcance de este cierre.

## 11. Dependencia siguiente

`N2.3.A — Recepción de mercancía — Auditoría y preflight` depende de N2.2.H.

La recepción debe modelarse como agregado separado contra OrdenCompra y será el punto donde **sí** se incremente stock por mercancía recibida. N2.2 no debe absorber esa responsabilidad.

## 12. Límites

Este cierre no autoriza merge a `main`, auto-merge del PR #2, ramas nuevas, force-push, secretos, cambios de infraestructura productiva ni despliegues a Producción. El scope reservado a Jules permanece excluido.