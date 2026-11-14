# ERP-N2.7 — Notas de crédito de proveedor

## Estado

**Cierre técnico/documental de N2.7.** Baseline funcional certificado: `42f83b365392f45de39bd0e0ca4fa0638dd0eb10`.

## Propósito

N2.7 incorpora la nota de crédito de proveedor como documento empresarial trazable dentro del módulo de Compras. Su responsabilidad es registrar y administrar el crédito documental asociado a una factura de proveedor y, cuando corresponda, a una devolución a proveedor.

N2.7 no adelanta la implementación completa de Cuentas por Pagar de N2.8 y no inventa mutaciones de stock, Kardex ni tesorería fuera de los contratos efectivamente implementados.

## Dominio y lifecycle

El agregado `NotaCreditoProveedor` mantiene el lifecycle:

`Borrador → Registrada → Anulada`

Reglas relevantes:

- sólo un borrador es editable;
- registrar consolida el documento y su crédito;
- anular sólo es válido desde `Registrada`;
- una nota anulada no vuelve a estado editable;
- los vínculos documentales hacia proveedor/factura y, cuando existe, devolución, se validan fail-closed;
- el crédito acumulado por factura se valida bajo serialización transaccional para evitar carreras de concurrencia.

## Persistencia

La persistencia N2.7 mantiene autoridad relacional para proveedor, factura de proveedor y devolución de proveedor, con índices/constraints de integridad, importes con precisión empresarial y snapshot EF reconciliado.

La migración N2.7, sus verificaciones pre/post y recovery MySQL fueron certificados dentro de los gates causales del módulo. El rollback se limita a recursos introducidos por N2.7 y no se presenta como mecanismo universal de recuperación de datos.

## Aplicación y API

Superficie canónica: `notas-credito-proveedor`.

Casos de uso implementados:

- consulta paginada y filtrada;
- consulta por ID;
- creación;
- edición de borrador;
- registro;
- anulación.

Las validaciones temporales, de usuario, vínculos documentales e importes operan fail-closed. Las mutaciones críticas se ejecutan dentro de unidad transaccional y conservan auditoría/correlación según los servicios transversales vigentes.

## Frontend y UX

La UI de Notas de crédito de proveedor dispone de listado, formulario/detalle, rutas protegidas y acciones de lifecycle consistentes con el backend:

- `Editar` y `Registrar`: sólo en `Borrador`;
- `Anular`: sólo en `Registrada`;
- estado `Anulada`: terminal.

El cierre de N2.7.E corrigió explícitamente el guard visual de anulación para impedir ofrecer una acción inválida sobre borradores.

## RBAC, auditoría y seguridad

El contrato de permisos usa el módulo `Compras` con las acciones:

- `Ver`
- `Crear`
- `Editar`
- `Confirmar`
- `Anular`

La superficie permanece autenticada y sin bypass administrativo nuevo. Las operaciones críticas conservan auditoría y comportamiento fail-closed ante usuario o contexto inválido.

## QA y certificación

Baseline final antes del cierre documental: `42f83b365392f45de39bd0e0ca4fa0638dd0eb10`.

Gates finales certificados sobre ese mismo SHA:

- Development `#32574284665` — `SUCCESS`
- Acceptance `#32574284640` — `SUCCESS`
- Fase 8 `#32574284638` — `SUCCESS`
- M13 `#32574284639` — `SUCCESS`
- Recovery MySQL `#32574284669` — `SUCCESS`
- M10 `#32574284658` — `SUCCESS`

M13 completó Backend/MySQL/migraciones, seguridad HTTP, frontend, Docker, Playwright integral y dictamen final.

## Fronteras de alcance

N2.7 no afirma:

- una implementación completa de CxP; esa responsabilidad continúa en N2.8;
- mutaciones de stock o Kardex no demostradas por el código;
- garantías de restore/backup no verificadas para el entorno;
- efectos financieros adicionales no materializados por los contratos vigentes.

## Dictamen

Con N2.7.A–G cerrados, los gates finales verdes y el paquete documental reconciliado, N2.7.H puede cerrarse cuando `TASKS.md`, `CHANGELOG_AI.md` y el tablero VAEP registren el mismo estado sin reescribir historia previa.
