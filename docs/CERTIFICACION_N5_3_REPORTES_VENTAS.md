# N5.3 — Reportes de ventas · certificación canónica

Autoridad operativa: `docs/VAEP_AUTHORITY.md`.

## Alcance certificado

N5.3 cubre reportes de ventas por período, usuario/vendedor, cliente, sucursal, categoría, producto, variante, marca, modelo, color y talla. La implementación conserva como fuente de datos las autoridades transaccionales existentes y no crea una segunda autoridad de inventario, ventas o contabilidad.

La cadena canónica cerrada es `N5.3.A → N5.3.B → N5.3.C → N5.3.D → N5.3.E → N5.3.F → N5.3.G`. `N5.3.H` documenta y certifica ese resultado sin reabrir scopes ya aceptados.

## Superficie resultante

- Backend: contratos de consulta, DTOs, servicio de reportes y endpoints de resumen/detalle con filtros, paginación y ordenamiento acotados al dominio aceptado.
- Datos: índices/snapshot y validaciones de migración correspondientes a N5.3.C, preservando integridad histórica.
- Frontend: modelos tipados, servicio HTTP, filtros, resumen, detalle y ruta `/centro-reportes/ventas` integrada al Centro de Reportes.
- Seguridad: autenticación y permiso relacional `Ventas/Ver`; el backend no confía en un vendedor arbitrario enviado por un usuario no administrador.
- Auditoría/observabilidad: las lecturas de reportes aceptadas registran evento de auditoría con entidad `ReportesVentas`, tipo de reporte y `CorrelationId`; las entradas inválidas no disparan consulta ni auditoría.

## Evidencia REVIEW_FIRST y cierres

- `N5.3.D`: `vaep/evidence/fragments/N5.3.D_LISTO_REAL_20260910T0204Z.json`.
- `N5.3.E`: `vaep/evidence/fragments/N5.3.E_LISTO_REAL_20260910T0328Z.json`.
- `N5.3.F`: `vaep/evidence/fragments/N5.3.F_LISTO_REAL_20260910T0412Z.json`.
- `N5.3.G`: `vaep/evidence/fragments/N5.3.G_LISTO_REAL_20260910T0416Z.json`.
- F1/J1: `vaep/evidence/fragments/N5.3.F.1_REVIEW_FIRST_20260910T0350Z.json`.
- F2/J2: `vaep/evidence/fragments/N5.3.F.2_REVIEW_FIRST_20260910T0407Z.json`.

F1 se resolvió mediante takeover acotado del controller y el resultado Jules tardío quedó sólo como evidencia forense. F2 produjo la sesión `sessions/6688018810981566090` y artifact `10136129280`; FIRST_DETECTOR revisó el patch tardío contra el contrato vivo, deduplicó cobertura ya implementada y reconcilió únicamente el gap útil de auditoría/correlación para detalle. No se consumió R2 ni R3 en F.

## Gates causales

El functional head de N5.3.F/G es `72e63bf7d14cc21b49b3f188ddb8facfa0b46790`.

Sobre ese SHA, `Desarrollo - Compilación y pruebas` run `34435975503` terminó `SUCCESS`, incluyendo:

- Backend Release y pruebas — `SUCCESS`.
- Migraciones EF, variantes y cargas masivas en MySQL 8.4 — `SUCCESS`.
- Frontend producción (lint + build) — `SUCCESS`.
- Docker y aislamiento de entornos — `SUCCESS`.
- Higiene del repositorio — `SUCCESS`.

Los guards VAEP exact-head también terminaron `SUCCESS`: engine `34435979055` y catalog throughput guard `34435978990`.

No se atribuye un PASS nuevo a E2E/accesibilidad por simple herencia: no hubo delta de código frontend posterior al functional head certificado de N5.3.E; la regresión nueva F fue backend/security-test only y sus gates causales aplicables están enumerados arriba.

## DoD y deuda

- REVIEW_FIRST: aceptado para las facetas materiales.
- Gates causales aplicables: terminales en verde.
- P0 abierto atribuible a N5.3: `0`.
- P1 abierto atribuible a N5.3: `0`.
- R3: `PROHIBIDO` y no utilizado.
- Busywork/filler: no utilizado; N5.3.F tuvo exactamente dos facetas source-backed y J3–J6 no recibieron copias nominales.

## Rollback / seguridad operacional

No se aplicó ningún cambio a Producción. Un rollback de código se realiza exclusivamente mediante un changeset posterior en `Desarrollo`, revirtiendo el commit causal correspondiente y volviendo a ejecutar los gates aplicables. Cualquier rollback de esquema debe seguir la evidencia/guard de N5.3.C y nunca ejecutarse contra Producción desde VAEP.

`main` permanece fuera del scope operativo, los secretos no se modifican y PR #2 no se mergea como parte de esta certificación.

## Resultado

`N5.3` queda materialmente listo para cerrar su etapa documental `N5.3.H` cuando este documento sea sometido a REVIEW_FIRST y se confirme que los receipts A–G y los gates citados siguen vigentes. El siguiente parent ordenado en `COLA` es `N5.4.A — Rentabilidad — Auditoría y preflight`; no debe publicarse como CURRENT hasta que `N5.3.H` sea `LISTO_REAL`.
