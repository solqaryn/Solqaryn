# Certificación ERP-N2.2 — OrdenCompra

## Estado

`LISTO`.

Este documento consolida la evidencia de cierre de N2.2. El baseline funcional anterior al paquete documental es `b4d477e2de25077c459d02b479968c93c93bc910`.

## Matriz A-H

| Punto | Estado | Evidencia principal |
| --- | --- | --- |
| N2.2.A Preflight | LISTO | `73ef31c49f08c8bff9732978ffc86dbe74e0a116` + `ERP_N2_2_ORDEN_COMPRA_PREFLIGHT.md` |
| N2.2.B Dominio/contratos | LISTO | `88047cde42929c1b2dcd8faf77da1c6543a2f2a9`, fix `f17983ef49bb8f5032e6fb328564f36c02f103b9` |
| N2.2.C Persistencia/migración | LISTO | `adff03723b4336b570328179e468e8470e611b95`; M12 `#32184108722` SUCCESS MySQL 8.4 |
| N2.2.D Aplicación/API | LISTO | hasta `a5340f991b0f93438ac184afeac41cc9ed82a756`; Development/Recovery verdes |
| N2.2.E Frontend/UX | LISTO | E.1 `26a7eada...`; E.2 `9ede060d...`; E.3 `f9000061...` con cinco gates verdes |
| N2.2.F RBAC/auditoría/seguridad | LISTO | hasta `1eb26cf60a3d4e1e37f9c89b60929f432de3c1ac`; snapshot EF reconciliado |
| N2.2.G QA/regresión/CI | LISTO | G.1 `23fa5ac6...`; G.2/G.3 `b4d477e2...`; Development/Acceptance/Fase8/M10/M13 verdes |
| N2.2.H Documentación/certificación | LISTO | H.1 `da05e6625ec6caf98f4e7e4a6dc4912d284dd805`; H.2 Jules `COMPLETED`, Issue #10, run `32289418163`, artifact `9379841062`; cross-review VAEP reconciliado |

## Cross-review independiente N2.2.H.2 — Jules + reconciliación VAEP

Jules completó la sesión `sessions/18298973172218991232` para `VAEP-JULES-N22H2-20260819T1846Z` y entregó ChangeSet/gitPatch con `baseCommitId=b0f5f4b38c67e361b44f3b79950a33c0b5fca59a`.

VAEP revisó el artifact completo antes de integrar. Se preservó únicamente el contenido documental válido y se rechazaron/corrigieron tres defectos del patch literal:

1. el ChangeSet más reciente incluía accidentalmente `N2.2.H.2_jules_patch.patch`, archivo fuera del `FILE_SCOPE_HINT`; no se integra;
2. la observación de Jules que ubicaba CxP en N2.3 era imprecisa: N2.3 corresponde a recepción de mercancía y la capacidad formal de cuentas por pagar pertenece a N2.8;
3. la recomendación de avanzar a N2.3.A estaba desactualizada frente al estado actual del proyecto; N2.3.A–E ya fueron materializados/certificados y, una vez reconciliado N2.2.H, el siguiente punto bloqueado por esta precedencia es N2.3.F.

### Dictamen de la revisión

- **Observaciones válidas:** lifecycle, restricciones fail-closed y evidencia A–G son coherentes con el certificado y los gates registrados.
- **Limitaciones/riesgos:** la OrdenCompra no materializa recepción, stock, Kardex, costeo, factura ni CxP. Recepción pertenece a N2.3; factura de proveedor a N2.4; CxP a N2.8. Las fronteras deben mantenerse desacopladas y trazables.
- **P0/P1:** no se identifican P0/P1 atribuibles al alcance N2.2.
- **Clasificación VAEP:** el archivo extra fuera de scope y las dos referencias desactualizadas se clasifican `REQUIRED_BEFORE_PARENT_LISTO` y quedan corregidas en esta reconciliación. No requieren reabrir trabajo funcional N2.2.
- **Conclusión:** el contenido útil de H.2 es aceptado tras reconciliación VAEP; el patch literal no se aplica ciegamente.

## Evidencia funcional final

Baseline: `b4d477e2de25077c459d02b479968c93c93bc910`.

- Development `#32218997006` — SUCCESS.
- Acceptance `#32218996971` — SUCCESS.
- Fase 8 `#32218996994` — SUCCESS.
- M10 `#32218996973` — SUCCESS.
- M13 `#32218996978` — SUCCESS.
- Migración N2.2.C / MySQL 8.4: M12 `#32184108722` — SUCCESS.
- Candidato documental previo `73668347...`: Development `#32227719896` — SUCCESS; Recovery MySQL `#32227719707` — SUCCESS.

## Controles certificados

### Dominio

- agregado independiente de `Compra` y `SolicitudCompra`;
- lifecycle `Borrador -> PendienteAprobacion -> Aprobada` + `Cancelada`;
- documento no editable después de Borrador;
- moneda/proveedor/líneas/totales validados;
- transiciones inválidas fail-closed;
- idempotencia durable e inmutable.

### Datos

- tablas dedicadas OrdenCompra/cabecera-detalles;
- número único;
- FKs restrictivas y cascade sólo cabecera→detalle;
- guards pre/post de migración;
- `Down` bloqueado cuando existen documentos;
- MySQL 8.4 e integration tests certificados.

### API y seguridad

- `[Authorize]` global;
- permisos `Compras:Ver/Crear/Editar/Confirmar/Aprobar/Anular` según operación;
- `Idempotency-Key` obligatorio al crear;
- 401/403/404/ProblemDetails cubiertos;
- auditoría y correlation-id mediante infraestructura común.

### UX

- listado, detalle, creación/edición y lifecycle;
- loading/error/vacío;
- paginación server-side y umbral operativo;
- limpieza de filas stale ante error;
- permisos runtime y E2E comprador/aprobador/cancelación;
- accesibilidad/responsive validados por M10/Fase8.

### Frontera empresarial

Aprobar una OrdenCompra no materializa:

- recepción;
- `ExistenciaVariante`;
- Kardex;
- costeo;
- movimiento financiero/CxP;
- factura de proveedor.

La recepción física empieza en N2.3. La factura de proveedor corresponde a N2.4 y las cuentas por pagar formales a N2.8, respetando la secuencia del Plan Maestro.

## Documentos canónicos

- `docs/ERP_N2_2_ORDEN_COMPRA_PREFLIGHT.md`
- `docs/ERP_N2_2_ORDEN_COMPRA.md`
- `docs/RUNBOOK_N2_2_ORDEN_COMPRA.md`
- `docs/ADR_N2_2_ORDEN_COMPRA_AUTORIDAD_DOCUMENTAL.md`
- `docs/OPENAPI_N2_2_ORDEN_COMPRA.md`
- `docs/CERTIFICACION_N2_2_ORDEN_COMPRA.md`

## Criterio de cierre H

El cierre `N2.2.H = LISTO` queda respaldado por:

1. gates funcionales y documentales causales suficientes en verde;
2. `TASKS.md` reconciliado mediante H.1;
3. `CHANGELOG_AI.md` conserva trazabilidad del cierre y debe interpretarse junto con esta reconciliación H.2;
4. COLA/BITACORA/CONFIG reconciliados por VAEP;
5. PR #2 permanece Draft, sin merge, y `main` no cambió;
6. Jules no publicó directamente: su resultado fue artifact-only y fue revisado/reconciliado por VAEP antes de esta integración.

## Handoff

N2.3.A–E ya cuentan con trabajo/certificación posterior preservada. Al quedar N2.2.H reconciliado, el siguiente punto que esta precedencia puede desbloquear es `N2.3.F — RBAC, auditoría, seguridad y observabilidad`, sujeto a la revalidación normal de sus dependencias y al modelo de doble carril vigente.

N2.3 mantiene la recepción total/parcial/múltiple como autoridad del incremento de stock por mercancía realmente recibida.