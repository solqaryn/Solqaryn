# Certificación N3.10 — Crédito del cliente

## Estado

- Parent: `N3.10 — Crédito del cliente`.
- Alcance de esta certificación: consolidar evidencia de N3.10.A–G y preparar el cierre documental N3.10.H.
- Rama autorizada: `Desarrollo`.
- PR de integración: `#2`, permanece Draft.
- Esta certificación no autoriza merge, producción, cambios de secretos ni despliegue.

## Autoridades funcionales certificadas

La capacidad de crédito permanece integrada al agregado/feature de Cliente y no introduce una segunda autoridad comercial ni un motor autónomo de scoring.

### Persistencia

- `N3.10.C` está certificado como `LISTO_REAL`.
- Autoridad de certificación: `619a0ba2a53ad70fb332c9f61198eb3b022ddcc1`.
- Development `33068581067`: `SUCCESS`.
- Acceptance `33068581028`: `SUCCESS`.
- Fase 8 `33068581188`: `SUCCESS`.
- M13 `33068581299`: `SUCCESS`.
- La persistencia incluye mapping EF, `DbSet`, migración, Snapshot Part28, constraints/rollback y regresiones dirigidas de `CreditoCliente`.

### Aplicación y API

- `N3.10.D` está certificado como `LISTO_REAL`.
- Autoridad funcional: `3c5a2c30a3d8427d0d0764ef1d4bc4e895d4d585`.
- Development `33073610169`: `SUCCESS`.
- Acceptance `33073610154`: `SUCCESS`.
- Fase 8 `33073610151`: `SUCCESS`.
- M13 `33073610159`: `SUCCESS`.
- Repository/service/API/DI y las acciones de estado usan usuario autenticado, transacción/UoW y auditoría estricta.

### Frontend y UX

- `N3.10.E` está certificado como `LISTO_REAL`.
- Autoridad funcional: `615d1a4878854bf22770b945256db39fea44e08f`.
- M10 `33083576709`: `SUCCESS` exact-head.
- El frontend reutiliza la feature existente de Clientes y aplica permisos fail-closed para creación y mutaciones de política de crédito.

### RBAC, auditoría y seguridad

- `N3.10.F` está certificado como `LISTO_REAL`.
- Autoridad de regresión dirigida: `98b7777555cd6f7ee881edb76321cd1226ca69eb`.
- Development `33086814120`: `SUCCESS`.
- Acceptance `33086814176`: `SUCCESS`.
- Fase 8 `33086814189`: `SUCCESS`.
- M13 `33086814163`: `SUCCESS`.
- M10 `33086818401`, intento 2: `SUCCESS`.
- Se certifican `[Authorize]`, permisos existentes `Clientes/Ver`, `Clientes/Crear`, `Clientes/Editar`, ausencia de bypass implícito de Administrador sin grant explícito y fail-closed cuando no existe usuario válido.

### QA, regresión y CI

- `N3.10.G` está certificado como `LISTO_REAL` reutilizando la misma autoridad exact-head `98b7777555cd6f7ee881edb76321cd1226ca69eb`.
- No se creó un segundo changeset funcional para fabricar evidencia duplicada.
- Los fallos legacy N0.x/2C.1/control-plane no causales permanecen separados de la certificación de CréditoCliente.

## Contrato funcional preservado

N3.10 no inventa ni certifica como existente ninguna de las siguientes capacidades:

- fórmula automática de crédito disponible/consumido no demostrada por el producto;
- scoring o ranking de clientes;
- thresholds automáticos adicionales;
- permisos RBAC nuevos distintos de los existentes de Clientes;
- efectos automáticos sobre venta, factura, stock, Kardex, caja o contabilidad fuera de los contratos ya implementados;
- una segunda autoridad de Cliente o un ledger paralelo.

Toda extensión futura de esas áreas requiere un parent/requisito autoritativo independiente.

## Seguridad y auditoría

- Lecturas: `Clientes/Ver`.
- Creación de política de crédito: `Clientes/Crear`.
- Actualizaciones, bloqueo/desbloqueo y excepción: `Clientes/Editar`.
- Las mutaciones exigen current-user válido y fallan antes de transacción/auditoría cuando falta identidad autorizada.
- Las mutaciones permanecen bajo UoW/transacción y auditoría estricta.

## Rollback y recuperación

El cierre documental N3.10.H no modifica esquema, datos ni lógica de producto. Por tanto:

1. No requiere una nueva migración ni rollback SQL.
2. Un rollback documental de esta certificación consiste únicamente en revertir el commit documental de H si se detecta inconsistencia de evidencia.
3. Los rollbacks de persistencia/producto continúan gobernados por la migración y guards certificados en N3.10.C.
4. No se debe revertir ni reescribir historia de `TASKS.md` o `CHANGELOG_AI.md`; cualquier rollup final debe ser aditivo/history-preserving.

## Evidencia Jules

Los artifacts Jules son evidencia consultiva y nunca sustituyen la autoridad controller-owned ni el CI causal:

- N3.10.F Jules C #1052: `PASS / EVIDENCE_ONLY / NOT_INTEGRATED / RELEASED`.
- N3.10.F Jules A #1053, B #1054 y D #1055: rechazados por gaps de self-review independiente; no integrados.
- Los lanes N3.10.G siguen siendo evidence-only; todo resultado terminal debe pasar `REVIEW_FIRST` antes de cualquier reutilización o rebinding.

## DoD de cierre N3.10.H

Para promover N3.10.H a `LISTO_REAL` todavía se debe:

- hard-verificar esta certificación contra la historia vigente de `Desarrollo`;
- reconciliar `TASKS.md` y `CHANGELOG_AI.md` solo donde sea necesario y de forma aditiva/history-preserving;
- confirmar P0=0 y P1=0 para el cierre documental;
- persistir COLA/CONFIG/BITÁCORA/TAREAS_PROGRAMADAS;
- ejecutar selector fail-closed y solo entonces promover `N3.11.A`.

Hasta completar ese rollup, `N3.10.H` permanece `EN_PROGRESO` y `N3.11.A` es únicamente PREWARM/promotion-blocked.
