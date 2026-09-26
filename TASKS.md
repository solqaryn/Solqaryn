# TASKS — Solqaryn

Registro operativo resumido de cierres ERP certificados. La autoridad de ejecución detallada permanece en COLA/CONFIG y el Plan Maestro.

> [!IMPORTANT]
> `TASKS.md` es historial/bitácora resumida y **NO es una fuente machine-readable de CURRENT_PARENT, NEXT_ACTION ni estado operativo vigente**. Un consumidor automático debe leer `CONFIG/COLA/BITACORA` frescos; las ocurrencias históricas de esas claves en este archivo son snapshots preservados y nunca deben seleccionarse por “primera coincidencia”.

## Gobierno VAEP

- [x] Autoridad operativa unificada en `docs/VAEP_AUTHORITY.md` como **MAESTRO único**.
- [x] Las reglas se editan en la misma ruta; no se crean protocolos/documentos/scripts operativos numerados.
- [x] Jules A/B/C/D y las cinco automatizaciones consumen el mismo MAESTRO.
- [x] Históricos en Git/CHANGELOG/BITACORA son evidencia únicamente y no gobiernan ejecución.
- [x] Retry, QA takeover, parent-close, ACTIVE_REAL, throughput y gates se definen exclusivamente en el MAESTRO.

## ERP-N2.2 — Orden de compra

- [x] N2.2.A-H completados y certificados por evidencia autoritativa VAEP.

## ERP-N2.7 — Nota de crédito de proveedor

- [x] N2.7.A-H completados — paquete documental canónico `c466ec3099c2a498c2353af82b99ce0be9d46e29`. Baseline funcional certificado `42f83b365392f45de39bd0e0ca4fa0638dd0eb10`; Development, Acceptance, Fase 8 y M13 en SUCCESS. Sin defectos bloqueantes P0/P1 conocidos.

**ERP-N2.7 queda formalmente cerrado.**

## ERP-N2.8 — Cuentas por pagar

- [x] N2.8.A-G completados y certificados en COLA.
- [x] N2.8.H paquete documental canónico materializado: ERP/ADR/contrato HTTP/runbook/rollback/certificación.
- [x] N2.8.H certificado sobre `81de833f5104d98f0aad02cf32c714640b6cea2b`: Development `#32605604928`, Acceptance `#32605604908`, Fase8 `#32605604911` y M13 `#32605604886` en SUCCESS; P0/P1 bloqueantes conocidos=0.

**ERP-N2.8 queda formalmente cerrado.**

## ERP-N2.9 — Evaluación de proveedores

- [x] N2.9.A-G completados y certificados en COLA.
- [x] Persistencia N2.9.C: migración/snapshot/preflight/postcheck/DownGuard certificados sobre `69419edf2ccb62b7d5849d242ca723f6d64b9ee5`.
- [x] Application/API N2.9.D certificado sobre `ca03082ff6bdbb587a58ee65052dd3b70df47957`.
- [x] Frontend/UX N2.9.E certificado sobre `1d7c10a9ee0132032716144ad726c3261522868f`.
- [x] QA/regresión/CI N2.9.G certificado sobre `19db085b630814b814f8c877010cc83f665b27a3`.
- [x] N2.9.H cierre documental canónico y `CHANGELOG_AI.md` reconciliados; HEAD `8b6e7e7df4b01b8f7226d7a9631506d0540f4fa5` certificado con Development `#32635282523`, Acceptance `#32635282571`, Fase8 `#32635282564` y M13 `#32635282546` en SUCCESS; P0/P1 bloqueantes conocidos=0.

**ERP-N2.9 queda formalmente cerrado. Parent40=22/40; GAP=18. El siguiente gate obligatorio es `GATE-N2`; ERP-N3 no se promueve antes de `GATE-N2=LISTO`.**

Documentación: `docs/ERP_N2_9_EVALUACION_PROVEEDORES.md`, `docs/RUNBOOK_N2_9_EVALUACION_PROVEEDORES.md`, `docs/CERTIFICACION_N2_9_EVALUACION_PROVEEDORES.md` y matriz QA `docs/qa/N2_9_H_CLOSURE_MATRIX_A1.md`.

## ERP-N3.1 — Cotizaciones

- [x] N3.1.A-G completados y certificados en COLA; baseline funcional inmediato `d4d296e229d266a1442de3bc4e07b03bfab35a9f`.
- [x] N3.1.H certificado con Development `#32687639976`, Acceptance `#32687639981`, Fase 8 `#32687640010`, M13 `#32687640016` y Recovery MySQL `#32687640017` en SUCCESS; P0/P1 bloqueantes conocidos=0.
- [x] Certificación canónica: `docs/CERTIFICACION_N3_1_COTIZACIONES.md`.

**ERP-N3.1 queda formalmente cerrado. Parent40=30/40; GAP=10. Siguiente MICROTAREA dependency-valid: `N3.2.A — Pedidos de venta / Auditoría y preflight`.**

## ERP-N3.2 — Pedidos de venta

- [x] N3.2.A-H completados y certificados en COLA.
- [x] PedidoVenta preserva lifecycle, idempotencia durable y persistencia propia sin adelantar la reserva automática de inventario.
- [x] Certificación canónica: `docs/CERTIFICACION_N3_2_PEDIDOS.md`.

**ERP-N3.2 queda formalmente cerrado. El siguiente foco dependency-valid es `N3.3.A — Reserva automática de inventario / Auditoría y preflight`.**

## ERP-N3.3 — Reserva automática de inventario

- [x] N3.3.A-G completados y certificados en COLA.
- [x] La confirmación de PedidoVenta reutiliza `ReservaInventario` y la autoridad física `ExistenciaVariante`; no crea una segunda autoridad cuantitativa ni inventa selección automática de almacén/ubicación.
- [x] N3.3.H documentación/certificación publicada mediante paquete atómico VAEP bajo el MAESTRO operativo.
- [x] Certificación: `docs/CERTIFICACION_N3_3_RESERVA_AUTOMATICA.md`.
- [x] Runbook: `docs/RUNBOOK_N3_3_RESERVA_AUTOMATICA.md`.
- [x] ADR de autoridad reutilizado: `docs/ADR_N1_8_RESERVAS_STOCK_RESERVADO_Y_OVERSELLING.md`.

**ERP-N3.3 queda formalmente cerrado. Siguiente MICROTAREA dependency-valid: `N3.4.A — Remisiones/entregas / Auditoría y preflight`.**

## ERP-N3.4 — Preparación y despacho

- [x] N3.4.A-G completados y certificados en COLA.
- [x] Persistencia/migración N3.4.C certificada sobre `cb476879203ffb3da40fb7a670c74935c794081d` con M13 `#32803906340` SUCCESS.
- [x] Application/API N3.4.D certificada sobre `1fab396541d8ecf33e605703789809ebc1a997ef` con M13 `#32807131468` SUCCESS.
- [x] Frontend/UX N3.4.E certificado sobre `a167434880eab07c3b08ca651ae9309da964c23b` con M13 `#32809392404` SUCCESS.
- [x] N3.4.F RBAC/auditoría/seguridad/observabilidad y N3.4.G QA/regresión/CI cerrados sobre el mismo HEAD funcional, con P0/P1 atribuibles conocidos=0.
- [x] Certificación canónica: `docs/CERTIFICACION_N3_4_PREPARACION_DESPACHO.md`.
- [x] Runbook: `docs/RUNBOOK_N3_4_PREPARACION_DESPACHO.md`.
**ERP-N3.4 queda formalmente cerrado. Siguiente MICROTAREA dependency-valid: `N3.5.A — Venta/factura — Auditoría y preflight`.**

## ERP-N3.5 — Venta/factura

- [x] N3.5.A-G completados y certificados en COLA mediante preflight y N/A grounded donde el desacople ya estaba implementado.
- [x] `Venta` conserva la autoridad operativa/financiera; `Factura` permanece ligada a `VentaId`; `PedidoVenta` conserva lifecycle independiente sin duplicar stock, Kardex, facturación ni finanzas.
- [x] Baseline funcional reutilizado: `a167434880eab07c3b08ca651ae9309da964c23b`, M13 `#32809392404` SUCCESS; delta funcional de N3.5.B-G=0 y P0/P1 atribuibles conocidos=0.
- [x] Certificación canónica publicada: `docs/CERTIFICACION_N3_5_VENTA_FACTURA.md`.
- [x] N3.5.H cierre documental publicado byte-perfect en `4296e72b8b5a87ef4e779e3ec6f8af083e396374`; `CHANGELOG_AI.md` quedó exactamente `+17/-0` y blob `9e0f74a66e0064543aa1a92d9ffc02c15a6d3862`.

**ERP-N3.5 queda formalmente cerrado. El siguiente foco dependency-valid es `N3.6.A — Devoluciones de cliente / Auditoría y preflight`.**

## ERP-N3.6 — Devoluciones de cliente

- [x] N3.6.A auditoría/preflight reconciliado después del cierre real de N3.5.H.
- [x] N3.6.B dominio/contratos completado y certificado.
- [x] N3.6.C persistencia/migración/datos completada y certificada.
- [x] N3.6.D Application/API completado y certificado.
- [x] N3.6.E frontend/UX completado y certificado.
- [x] N3.6.F RBAC/auditoría/seguridad/observabilidad completado y certificado.
- [x] N3.6.G QA/regresión/CI completado; baseline funcional `6c5a3164ab11a1dcdcdfa9418c61bb0165251239`, con Development `#32913855654`, Acceptance `#32913854936`, Fase 8 `#32913854958` y M13 `#32913854923` en SUCCESS; P0/P1 funcionales conocidos=0.
- [x] N3.6.H certificación canónica publicada en `4fe25e8cf656f82e3883f0585fa29358769aa48c` y runbook en `d906393fc26b0073ac782721ea08cb0fa35827b5`.
- [ ] Cierre formal H pendiente únicamente de reconciliar `CHANGELOG_AI.md` de forma aditiva/history-preserving y obtener gate causal del rollup final; no false LISTO.

**N3.6 permanece abierto exclusivamente por el rollup final de CHANGELOG. Siguiente parent dependency-valid tras H=LISTO: `N3.7.A — Nota de crédito / Auditoría y preflight`.**

## Fuentes VAEP v2

Plan rector:
https://docs.google.com/document/d/1rWGOP_Z64kM4Q2NZbrTvge3ReqJkJ_vJmhByogbPbR8/edit

Tablero:
https://docs.google.com/spreadsheets/d/19RrOmbhcqQf7zXWCuqjNPORlVOfuHMa9i43wjOyy8eY/edit

## ERP-N3.7 — Nota de crédito de cliente — ROLLUP SUPERSEDING 2026-08-26

Este bloque es aditivo y supersede únicamente el estado operativo stale de N3.6/N3.7 registrado arriba; no elimina ni reescribe la historia previa.

- [x] N3.6.H quedó cerrado realmente antes de iniciar N3.7; el `CHANGELOG_AI.md` canónico preserva ese cierre en blob `d53c56416ac7ac01beef761adab5172cf5297487`.
- [x] N3.7.A auditoría/preflight — `LISTO_REAL`, Issue #752, P0=0/P1=0.
- [x] N3.7.B dominio/contratos — `LISTO_REAL` en `46a250fcc0cfd1562306538375e772a94c39bea5`; Development #32972568129, Acceptance #32972568251, Fase 8 #32972568127 y M13 #32972568118 en SUCCESS.
- [x] N3.7.C persistencia/migración/datos — `LISTO_REAL` en `9810cf2e7fd0289a9374a8477a4131f3f73fef38`; Acceptance #32983744613, M13 #32983745546 y Recovery MySQL #32983743533 SUCCESS; migración/snapshot/tests certificados.
- [x] N3.7.D Application/API — `LISTO_REAL` en `8bcacae8a45fe3c0072bf519610bcc1ec1203a4f`; Development #32988607673, Acceptance #32988607652, Fase 8 #32988607675 y M13 #32988607632 SUCCESS.
- [x] N3.7.E Frontend/UX — `LISTO_REAL` en `f9ef582749a79c8900741d1a40ff393039c7b287`; M10 #32998936899 SUCCESS; Issue #770 cerrado.
- [x] N3.7.F RBAC/auditoría/seguridad/observabilidad — `LISTO_REAL` en `943aa0e607af3221ed8987a0edac37a539561696`; M10 #33001097160 SUCCESS; Issue #776 cerrado.
- [x] N3.7.G QA/regresión/CI — `LISTO_REAL` por rollup de regresión; Issue #781 cerrado y P0/P1 atribuibles=0.
- [ ] N3.7.H documentación/certificación — cierre canónico en curso: este TASKS rollup + entrada aditiva en `CHANGELOG_AI.md`; solo después del hard verify documental y P0=0/P1=0 pasa a `LISTO`.

**Promoción de N3.8 permanece bloqueada hasta N3.7.H=LISTO.**
## ERP-N3.8 — Nota de débito de cliente — CIERRE CONDICIONAL/N/A

- [x] N3.8.A preflight certificado en `034ec3305422016d6c571d0ffcf1332e3bbbe6b6`.
- [x] N3.8.B dominio/contratos cerrado N/A con evidencia en `affb58f2b9e7d8ab25c051fed5b9f4ee5f317584`.
- [x] N3.8.C-G cerrados N/A con evidencia en `3a89725e4a76c4d85c0c4adc04f0affa4a61e79a`.
- [x] N3.8.H certificación canónica: `docs/CERTIFICACION_N3_8_NOTA_DEBITO_CLIENTE.md`.

**ERP-N3.8 queda formalmente cerrado como N/A condicionado al requisito legal/operativo. No se implementó `NotaDebitoCliente`. Siguiente MICROTAREA dependency-valid: `N3.9.A — Cuentas por cobrar / Auditoría y preflight`.**

## ERP-N3.9 — Cuentas por cobrar — ROLLUP 2026-08-26

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N3.9.A auditoría/preflight — `LISTO_REAL`.
- [x] N3.9.B dominio/contratos — `LISTO_REAL`; `Factura` + `FacturaPago` permanecen autoridad y CxC es read-model, no segundo ledger.
- [x] N3.9.C persistencia/migración/datos — `LISTO_REAL / N_A_CERTIFIED`; no se creó tabla, migración ni backfill CxC.
- [x] N3.9.D Application/API — `LISTO_REAL` sobre `59e0c41362fe3a63765d9218a9817272ce6a7602`; GET-only `/cuentas-por-cobrar`, `[Authorize]` + `Facturacion/Ver`; Development #33019598078, Acceptance #33019598096, Fase8 #33019598185 y M13 #33019598074 SUCCESS.
- [x] N3.9.E Frontend/UX — `LISTO_REAL` sobre `9b0db22c26bce42f42f97ba1e0c6124c54d86af9`; Issue #843 cerrado.
- [x] N3.9.F RBAC/auditoría/seguridad/observabilidad — `LISTO_REAL / QA_TAKEOVER_CERTIFIED` sobre `0d621920f8ebd0a7bb3f1b3af30ffbadd0f91f9c`; Issue #851 cerrado, P0/P1=0.
- [x] N3.9.G QA/regresión/CI — `LISTO_REAL / QA_REGRESSION_CERTIFIED`; Issue #858 cerrado, P0/P1=0.
- [x] Certificación canónica publicada: `docs/CERTIFICACION_N3_9_CUENTAS_POR_COBRAR.md`.
- [ ] N3.9.H documentación/certificación permanece `EN_PROGRESO` hasta reconciliar `CHANGELOG_AI.md` de forma aditiva/history-preserving, verificar checks aplicables y P0/P1=0.

**Promoción de N3.10 permanece bloqueada hasta N3.9.H=LISTO.**

## ERP-N3.10 — Crédito de cliente — ROLLUP SUPERSEDING 2026-08-27

Este bloque es aditivo y supersede únicamente el estado operativo stale de N3.9/N3.10 registrado arriba; no elimina ni reescribe historia previa.

- [x] N3.9.H quedó cerrado formalmente antes de N3.10; `CHANGELOG_AI.md` contiene el cierre canónico history-preserving.
- [x] N3.10.A auditoría/preflight — `LISTO_REAL`.
- [x] N3.10.B dominio/contratos — `LISTO_REAL`; la capacidad de crédito permanece integrada a Cliente y no introduce un motor autónomo de scoring ni una segunda autoridad comercial.
- [x] N3.10.C persistencia/migración/datos — `LISTO_REAL` en `619a0ba2a53ad70fb332c9f61198eb3b022ddcc1`; Development #33068581067, Acceptance #33068581028, Fase 8 #33068581188 y M13 #33068581299 SUCCESS.
- [x] N3.10.D Application/API — `LISTO_REAL` en `3c5a2c30a3d8427d0d0764ef1d4bc4e895d4d585`; Development #33073610169, Acceptance #33073610154, Fase 8 #33073610151 y M13 #33073610159 SUCCESS.
- [x] N3.10.E Frontend/UX — `LISTO_REAL` en `615d1a4878854bf22770b945256db39fea44e08f`; M10 #33083576709 SUCCESS.
- [x] N3.10.F RBAC/auditoría/seguridad/observabilidad — `LISTO_REAL` en `98b7777555cd6f7ee881edb76321cd1226ca69eb`; Development #33086814120, Acceptance #33086814176, Fase 8 #33086814189, M13 #33086814163 y M10 #33086818401 SUCCESS.
- [x] N3.10.G QA/regresión/CI — `LISTO_REAL`, reutilizando la misma autoridad exact-head `98b7777555cd6f7ee881edb76321cd1226ca69eb` sin fabricar evidencia duplicada.
- [x] N3.10.H documentación/certificación — certificación canónica `docs/CERTIFICACION_N3_10_CREDITO_CLIENTE.md`; rollup aditivo TASKS+CHANGELOG hard-verificado; P0/P1 atribuibles conocidos=0.

**ERP-N3.10 queda formalmente cerrado. Siguiente parent dependency-valid: `N3.11.A`; su promoción solo ocurre después de este cierre real.**

## ERP-N3.11 — Punto de Venta (POS) — ROLLUP SUPERSEDING 2026-08-27

Este bloque es aditivo y supersede únicamente el estado operativo stale de N3.10/N3.11 registrado arriba; no elimina ni reescribe historia previa.

- [x] N3.10.H quedó cerrado formalmente antes de iniciar N3.11; `CHANGELOG_AI.md` contiene el cierre canónico.
- [x] N3.11.A auditoría/preflight — `LISTO_REAL`.
- [x] N3.11.B dominio/contratos — `LISTO_REAL`; `Venta` se reutiliza como autoridad central transaccional y financiera. Capacidades no soportadas (cashier/session/terminal, split-tender/change, suspension/reprint, offline, POS-specific idempotency, POS-specific RBAC) permanecen `DECISION_PENDING`.
- [x] N3.11.C persistencia/migración/datos — `LISTO_REAL`; esquema reutilizado de Ventas, sin tablas ni migraciones específicas de POS.
- [x] N3.11.D Application/API — `LISTO_REAL`.
- [x] N3.11.E Frontend/UX — `LISTO_REAL`.
- [x] N3.11.F RBAC/auditoría/seguridad/observabilidad — `LISTO_REAL`; se aplican permisos generales de Venta (Ventas/Crear).
- [x] N3.11.G QA/regresión/CI — `LISTO_REAL`; P0/P1 atribuibles conocidos=0.
- [ ] N3.11.H documentación/certificación — cierre canónico en curso: este TASKS rollup aditivo hard-verificado; `LISTO` es `TARGET_AFTER_PUBLICATION`.

**ERP-N3.11 queda formalmente cerrado (TARGET_AFTER_PUBLICATION).**

## ERP-N4.3 — Conciliación bancaria — ROLLUP 2026-09-03

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N4.3.A-G — DoD técnico satisfecho sobre baseline funcional certificado `ad0cf70fc6ced126de1878b61fe4ae02c8d41a01`.
- [x] N4.3.F — RBAC/auditoría/seguridad/observabilidad reconciliado por QA takeover, sin R3.
- [x] N4.3.H — certificación canónica publicada en `docs/CERTIFICACION_N4_3_CONCILIACION_BANCARIA.md` mediante `3d7b8c776b813c359e373780f1a1039c1baed8b1`.
- [x] Matriz exact-head de `3d7b8c776b813c359e373780f1a1039c1baed8b1`: gates aplicables terminales `SUCCESS`; `Solqaryn CI=SKIPPED` excluido como PASS.
- [x] P0 abiertos=0; P1 abiertos=0.
- [x] `TASKS.md` reconciliado de forma aditiva/history-preserving por este rollup.
- [ ] `CHANGELOG_AI.md` pendiente de reconciliación aditiva/history-preserving y nueva certificación exact-head; no false `LISTO_REAL`.

**CURRENT_PARENT=N4.3.H. CLOSABLE_NOW=NO hasta reconciliar `CHANGELOG_AI.md` y obtener gates exact-head terminales sobre el HEAD resultante. La promoción del siguiente parent permanece bloqueada.**

## ERP-N4.4 — Cuentas por cobrar — ROLLUP 2026-09-03

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N4.4.A — `LISTO_REAL / PREFLIGHT_CERTIFIED`; autoridad CxC preservada en `Factura + FacturaPago` ligada a `Venta`.
- [x] N4.4.B — `LISTO_REAL / DOMAIN_NA_CERTIFIED`; no se creó dominio ni contrato CxC duplicado.
- [x] N4.4.C — `LISTO_REAL / DB_MIG_NA_CERTIFIED`; sin schema delta, migración ni backfill CxC.
- [x] N4.4.D — `LISTO_REAL / COVERED_EXISTING_CONTRACT`; Application/API cubiertos por contratos existentes.
- [x] N4.4.E — `LISTO_REAL / FRONTEND_CXC` sobre `61c8445ff948912a1a3e7a2792106849064e51c7`.
- [x] N4.4.F — `LISTO_REAL / SEC_AUDIT_CERTIFIED` sobre `2487badc4759db4ca87d60f823c6fffd9899f0d2`.
- [x] N4.4.G — `LISTO_REAL / TEST_CI_CERTIFIED` sobre `a85396b8e5d6ed579f2815cb7a193f45ed3d54e0`; run `33772443239` SUCCESS; P0/P1=0.
- [x] Certificación canónica publicada: `docs/CERTIFICACION_N4_4_CUENTAS_POR_COBRAR.md`.
- [x] Runbook canónico publicado: `docs/RUNBOOK_N4_4_CUENTAS_POR_COBRAR.md`.
- [x] `TASKS.md` reconciliado aditivamente por este rollup.
- [ ] `CHANGELOG_AI.md` y checks exact-head documentales pendientes antes de `N4.4.H=LISTO_REAL`; no false LISTO.

**CURRENT_PARENT=N4.4.H. CLOSABLE_NOW=NO hasta reconciliar `CHANGELOG_AI.md`, obtener gates exact-head terminales y revalidar P0=0/P1=0. `N4.5.A` permanece promotion-held aunque se permite prework seguro.**

## ERP-N4.5 — Cuentas por pagar — ROLLUP 2026-09-03

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N4.5.A-G — `LISTO_REAL`: alcance CxP reconciliado mediante reutilización de la autoridad ERP-N2.8, sin duplicar dominio, subledger, persistencia, API, UI, RBAC, auditoría ni pruebas.
- [x] Certificación canónica publicada: `docs/CERTIFICACION_N4_5_CUENTAS_POR_PAGAR.md`.
- [x] Baseline funcional certificado: `541ec12b72912c769c6f54b8821771e509818375`; el HEAD posterior contiene únicamente documentación y manifests de coordinación VAEP/Jules.
- [x] P0 abiertos=0; P1 abiertos=0 para el alcance A-G según la evidencia canónica revisada.
- [ ] N4.5.H documentación/certificación permanece `EN_PROGRESO` hasta reconciliar este rollup con `CHANGELOG_AI.md`, obtener gates exact-head terminales y revalidar P0=0/P1=0.

**CURRENT_PARENT=N4.5.H. CLOSABLE_NOW=NO hasta completar la reconciliación documental y los gates exact-head. `N4.6.A` permanece `PREARMED/PROMOTION_HELD`; no se promueve antes del cierre real de N4.5.H.**

## ERP-N4.6 — Plan de cuentas jerárquico — ROLLUP DE CIERRE 2026-09-04

Este rollup es append-only y supersede únicamente el estado operativo stale anterior; conserva íntegramente la historia previa.

- [x] N4.6.A — `LISTO_REAL`; preflight y dependencia de N4.6.B confirmados.
- [x] N4.6.B — `LISTO_REAL`; dominio `CuentaContable` y `TipoCuentaContable` en `2658d5b0139e85957463cb227f11ea65f42bef13`.
- [x] N4.6.C — `LISTO_REAL`; persistencia EF/migración/snapshot certificados en `12542f37132dfd4488e27e197ef548af19dee337`.
- [x] N4.6.D — `LISTO_REAL`; repositorio, servicio, DTOs, API jerárquica y validación de ciclos materializados en `a4392c44` y `9d649bbb`.
- [x] N4.6.E — `LISTO_REAL`; UI `/plan-cuentas`, árbol, formularios, permisos y estados de interfaz en `9d649bbb`.
- [x] N4.6.F — `LISTO_REAL`; autorización, RBAC, auditoría y seguridad verificadas por contratos y gates del exact-head.
- [x] N4.6.G — `LISTO_REAL`; Development `#33828121004`, aceptación `#33828121038`, Fase 8 `#33828121029`, M13 `#33828121086` y M10 `#33828121034` en `SUCCESS`; `Solqaryn CI` `SKIPPED` no se cuenta como PASS.
- [x] N4.6.H — `LISTO_REAL`; certificación canónica `docs/CERTIFICACION_N4_6_PLAN_CUENTAS.md`, operación/rollback documentados, `P0=0`, `P1=0`.
- [x] Fase 2 queda registrada como `EXTERNAL_INFRA`: HTTP 503 de `registry.npmjs.org` durante `npm audit`; no es fallo causal del producto.

**CURRENT_PARENT=N4.6.H. N4.6 queda cerrado como `LISTO_REAL`; siguiente parent dependency-valid: `N4.7.A`. Este cierre no inicia ni modifica el scope de N4.7.**

### Reconciliación QA append-only

Este rollup supersede únicamente el estado operativo stale anterior del bloque N4.4; las menciones históricas `CURRENT_PARENT=N4.4.H` permanecen como evidencia histórica, ya no gobiernan el estado actual y no se reescribe ni elimina ninguna historia previa. El estado operativo vigente de este cierre es `CURRENT_PARENT=N4.5.H`. La certificación documental exacta corresponde al commit `fde578dd69cbfe91c054138d33404cec342093f6`; el `productBaseHead` es `541ec12b72912c769c6f54b8821771e509818375`.


## Integración AntiG — Reviewer/Fixer automático — 2026-09-03

- [x] Custom Agent de workspace `solqaryn-reviewer` definido con gobierno Solqaryn y separación `READY_FOR_VAEP != LISTO_REAL`.
- [x] Worker automático Jules -> AntiG -> VAEP implementado fail-closed con mutex, watermark, artifact causal, scope guard y publicación sin force/rebase.
- [x] Schema estructurado y self-test CI agregados.
- [x] Instalador local seguro preparado con permisos finos de Antigravity CLI; no usa bypass global de permisos.
- [x] Gobernanza `AGENTS.md` y `docs/COLABORACION_IA.md` actualizada con ATTEMPT1/R2, QA takeover y prohibición de auto-certificación.
- [x] AntiG RESERVED_INACTIVE: no existe activación pendiente; scheduler y handoff processing permanecen deshabilitados por MASTER.


## AntiG P1 hardening — 2026-09-03

- [x] Aislar review/fix en Git worktree temporal para preservar trabajo concurrente del checkout primario.
- [x] Eliminar `git add --all`; staging exacto por paths autorizados.
- [x] Validar causalidad Issue/run/artifact/dispatch/result/gitpatch/patch, base, attempt, scope y rutas protegidas.
- [x] Cuarentena de handoffs inválidos con avance de watermark para evitar lane starvation.
- [x] Self-tests funcionales de contrato, rutas protegidas y aislamiento de worktree.
- [x] Rollback transaccional del instalador si falla la fase mutante.
- [x] AntiG RESERVED_INACTIVE: scheduler deshabilitado; no existe tarea operativa AntiG requerida por el flujo vigente.

## AntiG P1 hardening — cierre técnico Codex — 2026-09-04

- [x] Causalidad estricta de `gitpatch.json` y `changes.patch`, incluyendo contenido unidiff, base SHA40 e identidad opcional coherente.
- [x] Compatibilidad histórica reconciliada; guards fail-closed y validación estructurada completas bajo el MAESTRO.
- [x] Delta staged/unstaged/untracked, staging explícito e igualdad dura de paths.
- [x] Rutas Vercel protegidas explícitamente; AntiG no puede modificar `frontend/vercel.json` ni `frontend/scripts/vercel-ignore-build.mjs`.
- [x] Handoffs inválidos en cuarentena; errores transitorios retryables sin avance de watermark.
- [x] Estado de publicación durable `COMMENT_PENDING` tras confirmar `evidenceHead`, con deduplicación antes de comentarios.
- [x] Rollback del instalador restaura XML de tarea preexistente o elimina solo la tarea nueva.
- [x] Self-tests funcionales ampliados para contrato, aislamiento, staging, paths protegidos, patch causal, watermark, idempotencia y planes de rollback.
- [x] AntiG RESERVED_INACTIVE: instalación/scheduler/handoff processing están bloqueados fail-closed; futura reincorporación exige autorización explícita.

## ERP-N4.7 — Asientos Contables — ROLLUP 2026-09-04

Este bloque es aditivo y no reescribe estados históricos anteriores.

- [x] N4.7.A-G — `LISTO_REAL`; baseline funcional certificado `c8d1e373ba8ea008bf773e69afa10f5f18d6de8b`.
- [x] Certificación canónica de H publicada y REVIEW_FIRST aprobada: `docs/CERTIFICACION_N4_7_ASIENTOS.md`.
- [x] Exact-head documental previo `6986874048985e4746d21e23254479b391220445`: gates aplicables terminales `SUCCESS`; `Solqaryn CI=SKIPPED` excluido como PASS; P0=0/P1=0.
- [x] `TASKS.md` reconciliado de forma aditiva/history-preserving por este rollup.
- [ ] `N4.7.H` permanece fail-closed hasta certificar los gates aplicables y P0/P1=0 del exact-head resultante de completar también `CHANGELOG_AI.md`; no false `LISTO_REAL`.

**CURRENT_PARENT=N4.7.H. NEXT_ACTION=reconciliar `CHANGELOG_AI.md` aditivamente, drenar/certificar el exact-head final y solo entonces cerrar H y promover `N4.8.A`.**

## VAEP MASTER migration — cierre F0/F1 — 2026-09-04

- [x] FASE 0 — EXCLUSIVIDAD: `PASS/CLOSED`. `docs/VAEP_AUTHORITY.md` permanece como autoridad operativa única; ChatGPT/VAEP conserva controller/REVIEW_FIRST/QA/certificación; Codex y AntiG no tienen autoridad operativa vigente; PR #2 continúa `OPEN+DRAFT` y `main` permanece congelada.
- [x] FASE 1 — FREEZE + RECONCILIACIÓN: `PASS/CLOSED`. El guard técnico de admisión está activo en `.github/scripts/vaep-jules-master.sh` y el estado declarativo `vaep/control/dispatch-admission.json` está en `FROZEN`.
- [x] Admisión Fase 1: cero manifests nuevos => `NO_OP/exit 0`; más de uno => fail-closed; exactamente uno => valida control state antes de sesión/attempt/ownership/recovery; `FROZEN` rechaza nuevos dispatches sin invalidar sesiones `ACTIVE_REAL` preexistentes.
- [x] Reconciliación N4.7 preservada: `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`. El cierre de Fase 1 NO declara `N4.7.H=LISTO_REAL` ni promueve N4.8.
- [x] `ADMISSION_BATTERY=PASS` sobre los casos aislados ya validados: `NO_OP`, `FROZEN`, `OPEN`, múltiples manifests y estados inválidos.
- [x] `FALSE_PASS=NO`, `FALSE_LISTO=NO`, `SCOPE_LEAK=NO`.

**MIGRATION_PHASE_0=CLOSED/PASS. MIGRATION_PHASE_1=CLOSED/PASS. NEW_DISPATCH_ADMISSION=FROZEN. FASE_2=CLOSED/PASS. FASE_3=NOT_STARTED.**

## VAEP MASTER migration — cierre F2 — 2026-09-04

- [x] FASE 2 — CONSOLIDACIÓN DE POLÍTICA OPERATIVA MACHINE-READABLE: `PASS/CLOSED`.
- [x] Bloque canónico único `BEGIN_AUTOMATION_POLICY ... END_AUTOMATION_POLICY` embebido en `docs/VAEP_AUTHORITY.md`.
- [x] Parser fail-closed `.github/scripts/vaep-policy-parser.sh` implementado y validado (`--self-test`, missing/duplicate/unknown/invalid keys fail-closed, hash determinístico SHA-256).
- [x] Runtime unificado: `.github/scripts/vaep-jules-master.sh` y `.github/scripts/vaep-jules-worker.sh` consumen la política vía parser dinámico; duplicación de valores eliminada.
- [x] Workflows saneados: 8 workflows `vaep-jules-*.yml` sin redefinición de budget (safety-net `timeout-minutes: 25`).
- [x] Consumidores activos alineados a `docs/VAEP_AUTHORITY.md` (`AUTOMATION_AUTHORITY=MASTER`): `AGENTS.md`, `docs/VAEP_JULES.md`, `PLAN_EJECUCION_AUTONOMA.md`, `PROJECT_CONTEXT.md`, `docs/COLABORACION_IA.md`, `docs/CONTEXTO_CHATGPT_VAEP.md`.
- [x] AntiG preservado intacto como `RESERVED_INACTIVE` en documentación y componentes.
- [x] Protocolos numéricos en superficies activas eliminados (`NUMERIC_PROTOCOL_LABELS=0`).
- [x] Reconciliación N4.7 preservada: `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`.
- [x] `FALSE_PASS=NO`, `FALSE_LISTO=NO`, `SCOPE_LEAK=NO`.

## VAEP Fase 2 — corrección final REVIEW_FIRST — 2026-09-04

- [x] REVIEW_FIRST detectó dos gaps reales posteriores al handoff AntiG: el worker todavía repetía límites de retry como literales en prompts/validación y master/worker podían ocultar el exit code del parser mediante process substitution.
- [x] Corregido en `dd518b608f0404f249a6315b10793da1c500226c`: master/worker capturan y validan explícitamente el exit status del parser; cualquier fallo del parser aborta startup fail-closed.
- [x] Eliminados los literales runtime restantes de `JULES_MAX_ATTEMPTS`, `JULES_REWORK_MAX`, SLA/dwell/budget en self-tests y prompts; retry validation consume valores emitidos por MASTER.
- [x] `result.json` de worker y timeout incorpora `MASTER_COMMIT_SHA` + `AUTOMATION_POLICY_HASH`.
- [x] Ocho workflows Jules permanecen sin overrides `5400/1080`; `timeout-minutes: 25` queda únicamente como safety-net externo.
- [x] `VAEP engine lightweight checks` exact-head de la corrección: run `33909740182` = `SUCCESS`. `Solqaryn CI=SKIPPED` no se usa como PASS.
- [x] PR #2 permanece `OPEN+DRAFT`; `main`, Producción, Vercel, secretos, dominios y BD productiva no fueron modificados.

**MIGRATION_PHASE_2=CLOSED/PASS. RUNTIME_POLICY_DUPLICATES=0. PARSER_FAIL_CLOSED=PASS. MASTER_SHA_POLICY_HASH_EVIDENCE=PASS. FASE_3=NOT_STARTED. CURRENT_PARENT=N4.7.H. N4.8.A=HELD.**

## VAEP Fase 3 — Runtime Jules hardening — 2026-09-04

- [x] `POLICY_SOURCE=MASTER`; `JULES_LANE_BUDGET_SECONDS`, retry cap y rework se consumen desde `docs/VAEP_AUTHORITY.md`.
- [x] `5400_OVERRIDES=0`; A/B/C/D + recoveries conservan `timeout-minutes: 25` únicamente como safety-net externo.
- [x] `NO_OP=PASS`: cero manifests nuevos produce exit 0 sin sesión, attempt, ownership ni recovery.
- [x] `MULTI_MANIFEST_FAIL_CLOSED=PASS`.
- [x] El worker persiste estado de runtime antes y después de crear/reusar sesión con worker/dispatch/task/attempt/session/base/MASTER SHA/policy hash.
- [x] Timeout: señal segura best-effort usando operación Jules ya soportada; no se inventa endpoint de cancelación.
- [x] Timeout revoca ownership, marca `SUPERSEDED`, bloquea late-result automatic integration, libera lane y entrega `QA_TAKEOVER_AND_ASSIGN_NEXT_SAFE_IMMEDIATELY`.
- [x] Supersession queda durable en artifact estructurado + Issue `[VAEP-JULES-SUPERSEDED]`; el worker consulta ese ledger antes de iniciar/reanudar y antes de publicar resultado.
- [x] Auxiliares stop/session-health/feedback/diagnostic quedaron MASTER-bound y sin writer de producto/LISTO_REAL.
- [x] Se corrigió el YAML heredoc preexistente de `vaep-jules-diagnostic.yml`; el workflow volvió a crear jobs y el run PR `33911803009` terminó `SUCCESS`.
- [x] Gate runtime causal: `VAEP engine lightweight checks` push `33911698939` = `SUCCESS` sobre `b130d044497af09ad81ed33ec4e182bad1a8769f`; cambio posterior `8310372f...` es únicamente reparación del workflow diagnóstico y su check propio quedó SUCCESS.
- [x] `Solqaryn CI=SKIPPED` no se usa como PASS.
- [x] PR #2 permanece `OPEN+DRAFT`; `main`, Producción, Vercel, secretos, dominios y BD productiva intactos.

**MIGRATION_PHASE_3=CLOSED/PASS. NO_OP=PASS. TIMEOUT_SUPERSESSION=PASS. LATE_RESULT_GUARD=PASS. DURABLE_TIMEOUT_EVIDENCE=PASS. FASE_4=NOT_STARTED. CURRENT_PARENT=N4.7.H. N4.8.A=HELD. FALSE_PASS=NO. FALSE_LISTO=NO. SCOPE_LEAK=NO.**

## VAEP Fase 4 — Historical writers cleanup — 2026-09-04

- [x] Eliminados los siete workflows históricos/autopublishers autorizados para retiro:
  - `.github/workflows/vaep-control-plane-ci-guard.yml`
  - `.github/workflows/vaep-n36h-exact-publish.yml`
  - `.github/workflows/vaep-n37h-exact-publish.yml`
  - `.github/workflows/vaep-n38h-exact-publish.yml`
  - `.github/workflows/vaep-n39h-exact-publish.yml`
  - `.github/workflows/vaep-n310h-exact-publish.yml`
  - `.github/workflows/vaep-n311h-exact-changelog-publisher.yml`
- [x] `.github/workflows/ci.yml` conserva backend/frontend/acceptance, artifacts y validaciones; `permissions.contents` quedó `read` y se eliminó únicamente el step que hacía `git commit` + `git push` a `agent/mejoras-solqaryn`.
- [x] Auditoría exhaustiva de los 38 workflows restantes: `contents: write=0`, `git push=0`, `git commit=0`, `git reset --hard=0`, `update-ref/force-push=0`.
- [x] `PRODUCT_CI_PRESERVED=PASS`.
- [x] Gate causal sobre el commit de implementación `e1ff079ef8645da4c1cc4bff8e9967b8d31ed954`: `VAEP engine lightweight checks` run `33912398582` = `SUCCESS`; `VAEP Jules Diagnostic` run `33912398627` = `SUCCESS`.
- [x] `Solqaryn CI=SKIPPED` no se usa como PASS.
- [x] PR #2 permanece `OPEN+DRAFT`; `main`, Producción, Vercel, secretos, dominios y BD productiva no fueron modificados.

**MIGRATION_PHASE_4=CLOSED/PASS. HISTORICAL_GIT_WRITERS=0. UNAUTHORIZED_COMMIT_PUSH_RESET=0. PRODUCT_CI_PRESERVED=PASS. FASE_5=NOT_STARTED. CURRENT_PARENT=N4.7.H. N4.8.A=HELD. FALSE_PASS=NO. FALSE_LISTO=NO. SCOPE_LEAK=NO.**

## VAEP Fase 5 — Cleanup documental y manifests — 2026-09-04

- [x] Semantic-diff de `docs/VAEP_V320_RETRY_CAP.md`, `docs/VAEP_V320_SPRINT40_QUEUE.md`, `docs/VAEP_V321_PARENT40_QUEUE.md` y `docs/VAEP_V321_PARENT_CLOSURE.md` contra `docs/VAEP_AUTHORITY.md`: `UNIQUE_LIVE_RULES_NOT_IN_MASTER=0`. Las reglas aún vigentes (retry cap, pre-session no consume attempt, doble self-review, parent-close, zero-idle/failover, QA takeover y seguridad) ya están incorporadas en MASTER; Sprint40/Rolling40/version labels/checkpoints antiguos son historia expirada, no autoridad activa.
- [x] Eliminados del árbol activo los cuatro documentos versionados; Git conserva su historia completa.
- [x] Ledger previo de manifests activos: A=780, B=693, C=656, D=663, `.vaep/jules/dispatch`=3; total=2795.
- [x] Clasificación: los manifests ya estaban comprometidos/disparados y por tanto son transporte consumido/histórico; `N4.7.A-G=LISTO_REAL` según estado vigente y el manifest Jules B de `N4.7.H.DOC_CERT` ya produjo terminal `COMPLETED` (Issue #2654) y quedó sujeto a REVIEW_FIRST del controller. No se conserva ningún manifest JSON histórico dentro de las rutas activas.
- [x] Las cuatro rutas canónicas A/B/C/D se preservan con `.gitkeep`; JSON histórico en rutas activas=0.
- [x] `.vaep/jules/dispatch` eliminado por redundancia; sus A74/A75 ya estaban explícitamente reemplazados/corregidos por rutas canónicas y A73 pertenece a N4.1 histórico.
- [x] Limpieza disparó A/B/C/D sobre el commit `e34c148a06a2f054a40c790d28d7aaf01e30eedb`; los cuatro ejecutaron `VAEP/Jules MASTER` y terminaron SUCCESS sin artifact, validando el camino `NO_OP` para deletions/cleanup:
  - A `33913519604=SUCCESS`
  - B `33913519565=SUCCESS`
  - C `33913519551=SUCCESS`
  - D `33913519538=SUCCESS`
- [x] Gate VAEP causal del cleanup: `VAEP engine lightweight checks #33913519654=SUCCESS`; `VAEP Jules Diagnostic #33913524944=SUCCESS`.
- [x] No se usó `[skip ci]` para proteger la limpieza.
- [x] Los prompts históricos N4.7 con labels numéricas salieron de las rutas activas junto con sus manifests; los próximos dispatches consumen MASTER.
- [x] PR #2 permanece `OPEN+DRAFT`; `main`, Producción, Vercel, secretos, dominios y BD productiva intactos.

**MIGRATION_PHASE_5=CLOSED/PASS. UNIQUE_LIVE_RULES_NOT_IN_MASTER=0. ACTIVE_VERSIONED_PROTOCOL_DOCS=0. HISTORICAL_MANIFESTS_IN_ACTIVE_PATHS=0. DOT_VAEP_DISPATCH=0. CLEANUP_TRIGGER=PASS. FASE_6=NOT_STARTED. CURRENT_PARENT=N4.7.H. N4.8.A=HELD. FALSE_PASS=NO. FALSE_LISTO=NO. SCOPE_LEAK=NO.**

## VAEP Fase 6 — AntiG RESERVED_INACTIVE — 2026-09-04

- [x] MASTER contiene el estado canónico AntiG completo y fail-closed.
- [x] Preservados los seis componentes AntiG requeridos; ninguno fue eliminado.
- [x] Agent deshabilitado como main/subagent; worker normal devuelve `NO_ACTION`; instalador no contiene ruta de creación de scheduler.
- [x] Self-test AntiG valida componentes, MASTER, no scheduler creation, no handoff runtime, no LISTO_REAL y cero labels protocolarios numéricos activos.
- [x] Runbook y consumidores vigentes quedaron alineados al estado reservado.
- [x] Los tres checkboxes históricos de activación pendiente quedaron cerrados; `ANTIG_CURRENT_QUEUE_ITEM=NO`.
- [x] `CURRENT_PARENT=N4.7.H`; `N4.8.A=HELD`. Fase 6 no certifica N4.7.H ni promueve N4.8.
**MIGRATION_PHASE_6=CLOSED/PASS. ANTIG_STATUS=RESERVED_INACTIVE. ANTIG_OPERATIONAL_NOW=FALSE. ANTIG_SCHEDULER=DISABLED. ANTIG_HANDOFF_PROCESSING=DISABLED. ANTIG_CURRENT_QUEUE_ITEM=NO. ANTIG_CAN_CERTIFY_LISTO_REAL=FALSE. FASE_7=NOT_STARTED. FALSE_PASS=NO. FALSE_LISTO=NO. SCOPE_LEAK=NO.**

## VAEP Fase 7 — Certificación integral y retorno operativo — 2026-09-04

- [x] REVIEW_FIRST de migración F0-F6 detectó una regresión concreta: el overlay de admisión de Fase 1 había sido retirado anteriormente por no estar definido en MASTER. Fase 7 lo corrigió sin crear autoridad paralela: `docs/VAEP_AUTHORITY.md` ahora define explícitamente `vaep/control/dispatch-admission.json` y `.github/scripts/vaep-jules-master.sh` lo consume fail-closed.
- [x] Gate de implementación `66d4ded1fca2f51854a50ca3f6a44725dc6c1ef6`: `VAEP engine lightweight checks #33917014608=SUCCESS`; `VAEP Jules Diagnostic #33917014756=SUCCESS`; `Solqaryn CI=SKIPPED` excluido de PASS.
- [x] Admisión probada por self-test: `NO_OP`, múltiples manifests fail-closed, `FROZEN`, `OPEN`, valor inválido, clave desconocida y state ausente. `FROZEN` rechaza antes de session/attempt/ownership/recovery y no invalida sesiones ACTIVE_REAL preexistentes.
- [x] Política MASTER: exactamente un bloque parseable con seis claves; parser fail-closed sin `source`/`eval`; hash determinístico del bloque preservado; runtime emite `MASTER_COMMIT_SHA` + `AUTOMATION_POLICY_HASH`.
- [x] Runtime Jules final: `NO_OP=PASS`, budget interno desde MASTER, safety-net externo 25m, timeout supersession, ownership revoke, lane release, durable evidence y late-result guard.
- [x] Cleanup final: documentos protocolarios versionados activos=0; manifests históricos JSON en rutas activas=0; `.vaep/jules/dispatch`=0; historical publishers/writers=0; `ci.yml` conserva producto con permisos read-only y sin commit/push.
- [x] AntiG preservado como `RESERVED_INACTIVE`: seis componentes presentes; scheduler deshabilitado; handoff processing deshabilitado; sin LISTO_REAL; reincorporación futura solo mediante autorización explícita + cambio del mismo MASTER.
- [x] Cinco automatizaciones operativas verificadas ENABLED: `VAEP MASTER 00/15/30/45/55`; todas leen `docs/VAEP_AUTHORITY.md` y sus prompts activos no usan protocolos numéricos como autoridad. Telemetría de ejecución reciente presente en las cinco.
- [x] Flujo end-to-end certificado con evidencia real: dispatch N4.7.H `0014e8f1...` -> Jules B run `33892158842=SUCCESS` -> artifact `9944920873` -> Issue #2654 `COMPLETED` + controller handoff -> REVIEW_FIRST/CI registrados en BITACORA -> `N4.7.H LISTO_REAL` -> handoff `N4.8.A`.
- [x] Reconciliación de estado: COLA/BITACORA frescas prevalecen sobre snapshots de fase anteriores. `N4.7.H=LISTO_REAL RE-CERTIFIED`; `CURRENT_PARENT=N4.8.A`; A/B/C/D `ACTIVE_REAL=NO` hasta sesión+actividad útil correlacionada.
- [x] Retorno operativo autorizado: `vaep/control/dispatch-admission.json` cambia a `OPEN` únicamente después del gate causal de implementación PASS.
- [x] PR #2 permanece `OPEN+DRAFT`, merged=false; `main`, Producción, Vercel, secretos, dominios, certificados y BD productiva intactos.

**MIGRATION_PHASE_7=CLOSED/PASS. MIGRATION_F0_F7=CLOSED/PASS. NEW_DISPATCH_ADMISSION=OPEN. MASTER_UNIQUE=PASS. POLICY_PARSE=PASS. FIVE_AUTOMATIONS=PASS. END_TO_END_FLOW=PASS. CURRENT_PARENT=N4.8.A. N4.7.H=LISTO_REAL_RECERTIFIED. FALSE_PASS=NO. FALSE_LISTO=NO. SCOPE_LEAK=NO.**

## Auditoría Codex post-migración — remediación 2026-09-04

- [x] Eliminado `vaep/jules-a/dispatch/`: 54 manifests históricos residuales retirados del árbol activo; historia preservada por Git.
- [x] `TASKS.md` marcado explícitamente como historial/no machine-readable para `CURRENT_PARENT`, `NEXT_ACTION` y estado operativo; consumidores deben leer `CONFIG/COLA/BITACORA` frescos.
- [x] Constantes restantes movidas al bloque machine-readable del MAESTRO: `PARENT_STALL_NO_PROGRESS_MINUTES`, `MAX_VOLUNTARY_IDLE`, `VAEP_CHECKPOINTS`.
- [x] Parser ampliado fail-closed y master/worker actualizados para consumir esas claves; se eliminó el literal runtime de checkpoints.
- [x] Gate exact-head de hardening `09ee682712ba29d79d235a62415de20c308db7c9`: `VAEP engine lightweight checks #33920294318=SUCCESS`; `VAEP Jules Diagnostic #33920294338=SUCCESS`; `Solqaryn CI=SKIPPED` no se usa como PASS.
- [x] Existencia/habilitación de `VAEP MASTER 00/15/30/45/55` verificada externamente en el control-plane de automatizaciones; no depende de que existan cinco cron equivalentes en Git.
- [x] La limitación local de Bash reportada por Codex es de su entorno Windows y no se usa como PASS; la validación causal se tomó de GitHub Actions.

**CODEX_AUDIT_REMEDIATION=PASS. LEGACY_JULES_A_MANIFESTS=0. MASTER_POLICY_KEYS=9. TASKS_MACHINE_CURRENT_STATE=PROHIBITED. FIVE_AUTOMATIONS_EXTERNAL_EVIDENCE=PASS. FALSE_PASS=NO.**

## ERP-N6.4 — Usuarios por empresa — ROLLUP DE CIERRE 2026-09-11

Este bloque es estrictamente aditivo y supersede únicamente cualquier snapshot operativo stale anterior; conserva íntegramente la historia previa. La autoridad viva continúa en `CONFIG/COLA/BITACORA`.

- [x] N6.4.A-G — `LISTO_REAL` con evidencia canónica individual y P0/P1 atribuibles conocidos=0.
- [x] N6.4.E — frontend/UX certificado sobre functional head `810ce5d070a0871cc8ff4e8b63245746a8bdea0d`, M10 `34635599759=SUCCESS`.
- [x] N6.4.F — RBAC/auditoría/seguridad certificado sobre functional head `3f788ec78b03b820592c7a514fa3a63e345b3754`; defecto xUnit1031 corregido antes del cierre; gates `34638305008`, `34638304864`, `34638305000` y `34638304858` en SUCCESS.
- [x] N6.4.G — QA/regresión/CI certificado con REVIEW_FIRST `5f8a25d23e71093cc3faedededa531724b8ea310` y receipt `b5f992691907199a3239c9cc0b2afc1891446322`; P0/P1=0.
- [x] N6.4.H — certificación canónica preparada en `docs/CERTIFICACION_N6_4_USUARIOS_POR_EMPRESA.md`; throughput guard inicialmente detectó `nextParent=null`, corregido en `cbfe07f8f0d9c37e7c15e939ff11caf3849c14de` y revalidado SUCCESS en run `34641058359`.
- [ ] Cierre H permanece condicionado a `CHANGELOG_AI.md`, REVIEW_FIRST final, checkpoint exact-head y receipt. Este rollup no declara por sí mismo `LISTO_REAL`.

**CURRENT_PARENT=N6.4.H. NEXT_PARENT_PREARMED=N6.5.A. N6.5.A permanece bloqueado hasta el cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N6.8 — Storage aislado — ROLLUP DE CIERRE 2026-09-12

Este bloque es estrictamente aditivo y supersede únicamente cualquier snapshot histórico stale; la autoridad viva permanece en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N6.8.A-G — `LISTO_REAL` con evidencia canónica y P0/P1 atribuibles conocidos=0.
- [x] N6.8.G — candidate de TEST_CI `f5d29e7471762a3fe6fe735ba98c2ad1f0188727`; causal storage-isolation run `34714029058`, job `103607801664=SUCCESS`.
- [x] N6.8.H — certificación canónica publicada en `docs/CERTIFICACION_N6_8_STORAGE_AISLADO.md` mediante material DOC_CERT `dae43dfc5a3211643565c7002abcc67a845ef179`.
- [x] `CHANGELOG_AI.md` reconciliado de forma aditiva/history-preserving en el changeset documental de cierre.
- [x] `TASKS.md` reconciliado de forma aditiva/history-preserving por este rollup; este archivo no cambia por sí mismo el estado machine-readable.
- [ ] `N6.8.H` sólo pasa a `LISTO_REAL` después de REVIEW_FIRST fresco P0=0/P1=0, DoD PASS y receipt persistido/releído; `N6.9.A` permanece sequence-held hasta ese cierre.

**N6.8.H queda documentalmente reconciliado y listo para REVIEW_FIRST final; no hay falsa promoción de N6.9.A desde este archivo.**

## ERP-N7.1 — Outbox Pattern — ROLLUP DOCUMENTAL 2026-09-13

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.1.A-G — `LISTO_REAL` con receipts canónicos y sin P0/P1 abiertos atribuibles al alcance certificado.
- [x] N7.1.E — receipt `1a53847ce0126755f42e5eb27c337ddd41f896df`.
- [x] N7.1.F — receipt `433af6740735367eaa6937debcd1b5f01a4af360`.
- [x] N7.1.G — receipt `9a29a444e5d1e55be5af77b0f16fc00ab568cc87`; functional candidate heredado `2bb8148c1119a0a6c433b60cd9a893a3933e4a3e`.
- [x] N7.1.H — certificación canónica `docs/CERTIFICACION_N7_1_OUTBOX.md` materializada en `9741584e0c5439648dda26c1faad0eab69f768d7`.
- [x] REVIEW_FIRST inicial `6d010df403c0b27520f950f0e837a0aefdd7b736` confirmó `P0=0/P1=1`, siendo el único P1 la reconciliación aditiva de `CHANGELOG_AI.md` y `TASKS.md`.
- [ ] Este rollup resuelve únicamente el lado `TASKS.md` del P1 documental. N7.1.H no pasa a `LISTO_REAL` hasta reconciliar también `CHANGELOG_AI.md`, repetir REVIEW_FIRST con `P0=0/P1=0`, DoD PASS y persistir/releer receipt H.

**CURRENT_PARENT=N7.1.H. NEXT_PARENT_PREARMED=N7.2.A. No se promueve N7.2.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N7.2 — Retry controlado del Outbox — ROLLUP DOCUMENTAL 2026-09-13

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.2.A-G — `LISTO_REAL` con receipts/evidencia VAEP y sin reabrir gates ya certificados.
- [x] N7.2.F — candidate funcional `dce32d1336a4f541e75818115b346003a7b250f6`; REVIEW_FIRST P0=0/P1=0; hardening run `34755931334` job `103720179912=SUCCESS`; backend/MySQL run `34755931419` job `103720190895=SUCCESS`.
- [x] N7.2.G — candidate congelado `6f297cd6107312ad9e301491d47d9f88b5497539`; REVIEW_FIRST P0=0/P1=0; gate exact-head run `34757554438`, job `103724457658=SUCCESS`; receipt `vaep/evidence/fragments/N7.2.G_LISTO_REAL_20260913T124538Z.json`.
- [x] N7.2.H — certificación canónica `docs/CERTIFICACION_N7_2_RETRY.md` publicada en `3fda2525d0a9f0931e25cfa146c372c2e48fb5f5`.
- [x] Este rollup resuelve únicamente `TASKS_ADDITIVE_STATE_RECONCILIATION` preservando toda historia previa.
- [ ] N7.2.H no pasa a `LISTO_REAL` hasta completar también `CHANGELOG_AI_ADDITIVE_RECONCILIATION`, repetir REVIEW_FIRST con P0=0/P1=0 y validar los gates causales exact-head aplicables.

**CURRENT_PARENT=N7.2.H. NEXT_PARENT_PREARMED=N7.3.A. No se promueve N7.3.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N7.3 — Dead-letter del Outbox — ROLLUP DOCUMENTAL 2026-09-13

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.3.A-G — `LISTO_REAL` con receipts/evidencia VAEP y P0/P1 atribuibles conocidos=0.
- [x] N7.3.G — candidate funcional final `2c19221fdbb0a3cdfdcf8afdc839999bf8e80265`; gates causales `34776343835/103775053849`, `34776340372/103775044416` y `34776340372/103775044622` en `SUCCESS`; receipt `vaep/evidence/receipts/N7.3.G_LISTO_REAL_20260913T191310Z.json`.
- [x] N7.3.H — certificación canónica `docs/CERTIFICACION_N7_3_DEAD_LETTER.md` publicada en `c7c0d449991419bc7e1dd9c13b9a70ab5a15764c`.
- [x] Este rollup resuelve únicamente `TASKS_ADDITIVE_STATE_RECONCILIATION` preservando toda historia previa.
- [ ] N7.3.H no pasa a `LISTO_REAL` hasta completar también `CHANGELOG_AI_ADDITIVE_RECONCILIATION`, repetir REVIEW_FIRST con P0=0/P1=0 y validar los gates causales exact-head aplicables.

**CURRENT_PARENT=N7.3.H. NEXT_PARENT_PREARMED=N7.4.A. No se promueve N7.4.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N7.4 — Idempotencia — ROLLUP DOCUMENTAL 2026-09-13

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.4.A-G — `LISTO_REAL` con receipts/evidencia VAEP y P0/P1 atribuibles conocidos=0.
- [x] N7.4.G — functional candidate `1c366562a90c590cc2925a153298fd1b758e8dab`; causal gates `34782712890/103792518381`, `34782712890/103792518352`, `34782712890/103792518396`, `34782712884/103792518243` y `34782712884/103792518046` en `SUCCESS`; receipt `vaep/evidence/receipts/N7.4.G_LISTO_REAL_20260913T220100Z_SUP48.json`.
- [x] N7.4.H — certificación canónica `docs/CERTIFICACION_N7_4_IDEMPOTENCIA.md` publicada en `6097f60ff68d163878d5d3d85c590c59f0cd2f75`.
- [x] Este rollup resuelve únicamente `TASKS_ADDITIVE_STATE_RECONCILIATION` preservando toda historia previa.
- [ ] N7.4.H no pasa a `LISTO_REAL` hasta completar también `CHANGELOG_AI_ADDITIVE_RECONCILIATION`, repetir REVIEW_FIRST con P0=0/P1=0, demostrar equivalencia funcional y persistir/releer receipt H.

**CURRENT_PARENT=N7.4.H. NEXT_PARENT_PREARMED=N7.5.A. No se promueve N7.5.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N7.5 — Webhooks seguros — ROLLUP DOCUMENTAL 2026-09-14

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.5.A-G — `LISTO_REAL` con receipts/evidencia VAEP y P0/P1 atribuibles conocidos=0.
- [x] N7.5.G — functional candidate `dbd515909d98893f4924bb73d0a48f74fe9b97c3`; causal gates `34800735379/103842737649` y `34800735411/103842739664` en `SUCCESS`; receipt `vaep/evidence/receipts/N7.5.G_LISTO_REAL_20260914T030250Z_SUP36.json`.
- [x] N7.5.H — certificación canónica `docs/CERTIFICACION_N7_5_WEBHOOKS.md` persistida y releída.
- [x] Este rollup resuelve `TASKS_ADDITIVE_STATE_RECONCILIATION` preservando byte-for-byte toda historia previa; la reconciliación `CHANGELOG_AI_ADDITIVE_RECONCILIATION` se publica en el mismo recovery commit mediante append byte-exacto.
- [ ] N7.5.H sólo pasa a `LISTO_REAL` después de REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional contra `dbd515909d98893f4924bb73d0a48f74fe9b97c3`, verificación de ambos compares con additions>0/deletions=0 y receipt H persistido/releído.

**CURRENT_PARENT=N7.5.H. NEXT_PARENT_PREARMED=N7.6.A. No se promueve N7.6.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N7.6 — Integración API de WhatsApp Business — ROLLUP DOCUMENTAL 2026-09-14

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.6.A-G — `LISTO_REAL` con receipts/evidencia VAEP y P0/P1 atribuibles conocidos=0.
- [x] N7.6.B — modelo tenant-scoped `ConfiguracionWhatsAppEmpresa` con teléfono E.164 y referencias opacas a secretos, sin credenciales en claro; receipt `vaep/evidence/receipts/N7.6.B_LISTO_REAL_20260914T112812Z_SUP12.json`.
- [x] N7.6.C — persistencia/migración de configuración WhatsApp certificada sobre candidate `1492e1cd6fb915ceebf9c360134c84db8d29405a`; receipt `vaep/evidence/receipts/N7.6.C_LISTO_REAL_20260914T122316Z_SUP12.json`.
- [x] N7.6.D — boundary Application/API seguro certificado sobre `987e07e2f2d328ad21aa1558f1674e67048970b6`; receipt `vaep/evidence/receipts/N7.6.D_LISTO_REAL_20260914T142400Z_SUP00.json`.
- [x] N7.6.E — frontend/UX de configuración WhatsApp certificado con tenant verificado, RBAC reactivo y estados truthful; receipt `vaep/evidence/receipts/N7.6.E_LISTO_REAL_20260914T153315Z_SUP12.json`.
- [x] N7.6.F — seguridad/RBAC/auditoría certificada sin exposición de secretos; receipt `vaep/evidence/receipts/N7.6.F_LISTO_REAL_20260914T154700Z_SUP24.json`.
- [x] N7.6.G — QA/regresión/CI certificado sobre functional candidate `abb4a3bfdbe0d2896abcf33e5c9547e1dfc1016b`; receipt `vaep/evidence/receipts/N7.6.G_LISTO_REAL_20260914T155920Z_SUP48.json` y gates exact-head terminales en SUCCESS.
- [x] N7.6.H — certificación canónica `docs/CERTIFICACION_N7_6_WHATSAPP_BUSINESS.md` y reconciliación documental append-only preparadas para cierre mediante hard verify byte-prefix.
- [ ] N7.6.H sólo pasa a `LISTO_REAL` después de comprobar que los blobs históricos completos son prefijo byte-exacto, crecimiento de tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional contra `abb4a3bfdbe0d2896abcf33e5c9547e1dfc1016b` y receipt H persistido/releído.

**CURRENT_PARENT=N7.6.H. NEXT_PARENT_PREARMED=N7.7.A. No se promueve N7.7.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N7.7 — Email empresarial — ROLLUP DOCUMENTAL 2026-09-14

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.7.A-G — `LISTO_REAL` con receipts/evidencia VAEP y P0/P1 atribuibles conocidos=0.
- [x] N7.7.G — functional candidate `7a0765aa37533b8df2e52807f0a63800872c009d`; receipt `vaep/evidence/receipts/N7.7.G_LISTO_REAL_20260914T181734Z_SUP12.json`.
- [x] N7.7.H — certificación canónica `docs/CERTIFICACION_N7_7_EMAIL_EMPRESARIAL.md` publicada y releída.
- [x] Este rollup resuelve `TASKS_ADDITIVE_STATE_RECONCILIATION` preservando byte-for-byte toda historia previa.
- [ ] N7.7.H sólo pasa a `LISTO_REAL` después de completar también `CHANGELOG_AI_ADDITIVE_RECONCILIATION`, repetir REVIEW_FIRST con P0=0/P1=0, demostrar equivalencia funcional contra `7a0765aa37533b8df2e52807f0a63800872c009d` y persistir/releer receipt H.

**CURRENT_PARENT=N7.7.H. NEXT_PARENT_PREARMED=N7.8.A. No se promueve N7.8.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**


## ERP-N7.8 — Pagos online — ROLLUP DOCUMENTAL 2026-09-14

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.8.A-G — `LISTO_REAL` con receipts/evidencia VAEP y P0/P1 atribuibles conocidos=0.
- [x] N7.8.G — functional candidate `b217cc00bfa9bfc452674f0bdacbef52e50186a7`; receipt `vaep/evidence/receipts/N7.8.G_LISTO_REAL_20260914T212600Z_SUP48.json`; gates causales aplicables exact-head en SUCCESS.
- [x] N7.8.H — certificación canónica `docs/CERTIFICACION_N7_8_PAGOS_ONLINE.md` publicada y releída.
- [x] Este rollup resuelve `TASKS_ADDITIVE_STATE_RECONCILIATION` preservando byte-for-byte toda historia previa; `CHANGELOG_AI_ADDITIVE_RECONCILIATION` se publica en el mismo recovery commit mediante append byte-exacto.
- [ ] N7.8.H sólo pasa a `LISTO_REAL` después de hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional contra `b217cc00bfa9bfc452674f0bdacbef52e50186a7` y receipt H persistido/releído.

**CURRENT_PARENT=N7.8.H. No se promueve el sucesor antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N7.9 — Ecommerce — ROLLUP DOCUMENTAL 2026-09-14

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.9.A-G — `LISTO_REAL` con receipts/evidencia VAEP y P0/P1 atribuibles conocidos=0.
- [x] N7.9.G — TEST_CI certificado sobre functional candidate `5de96492290218639f18c1648668215339c34a1e`; receipt `vaep/evidence/receipts/N7.9.G_LISTO_REAL_20260914T230920Z_SUP00.json`; workflow causal `34905794077` con backend, Docker, frontend, higiene y MySQL/migraciones en `SUCCESS`.
- [x] N7.9.H — certificación canónica `docs/CERTIFICACION_N7_9_ECOMMERCE.md` preparada para cierre documental.
- [x] Este rollup resuelve `TASKS_ADDITIVE_STATE_RECONCILIATION` preservando byte-for-byte toda historia previa; `CHANGELOG_AI_ADDITIVE_RECONCILIATION` se publica en el mismo recovery commit mediante append byte-exacto.
- [ ] N7.9.H sólo pasa a `LISTO_REAL` después de hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional contra `5de96492290218639f18c1648668215339c34a1e` y receipt H persistido/releído.

**CURRENT_PARENT=N7.9.H. NEXT_PARENT_PREARMED=N7.10.A. No se promueve N7.10.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N7.10 — Facturación fiscal/electrónica — ROLLUP DOCUMENTAL 2026-09-14

Este bloque es estrictamente aditivo/history-preserving y supersede únicamente snapshots operativos stale; la autoridad viva continúa en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`.

- [x] N7.10.A-G — `LISTO_REAL` con receipts/evidencia VAEP y P0/P1 atribuibles conocidos=0.
- [x] N7.10.F — seguridad/RBAC/auditoría/observabilidad certificada mediante `vaep/evidence/receipts/N7.10.F_LISTO_REAL_20260915T011651Z_SUP00.json`.
- [x] N7.10.G — TEST_CI certificado sobre functional candidate `903901c6b30a4a2d70b4a52dc440ef8fc61adc5b`; receipt `vaep/evidence/receipts/N7.10.G_LISTO_REAL_20260915T012330Z_SUP00.json`; workflow causal `34916355954` con backend, Docker, frontend, higiene y MySQL/migraciones/integración en `SUCCESS`.
- [x] N7.10.H — certificación canónica `docs/CERTIFICACION_N7_10_FACTURACION_FISCAL_ELECTRONICA.md` preparada para cierre documental.
- [x] Este rollup reconcilia `TASKS.md` y `CHANGELOG_AI.md` mediante append byte-exacto sin reescribir historia previa.
- [ ] N7.10.H sólo pasa a `LISTO_REAL` después de hard verify de prefijo/tamaño, REVIEW_FIRST fresco P0=0/P1=0, equivalencia funcional contra `903901c6b30a4a2d70b4a52dc440ef8fc61adc5b` y receipt H persistido/releído.

**CURRENT_PARENT=N7.10.H. NEXT_PARENT_PREARMED=GATE-N7. No se promueve GATE-N7 antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**

## ERP-N8.2 — Compatibilidad de dispositivos — ROLLUP DOCUMENTAL 2026-09-15

Este bloque es aditivo e histórico. El estado operativo vigente se determina únicamente por CONFIG/COLA y receipts VAEP bajo `docs/VAEP_AUTHORITY.md`.

- [x] N8.2.D cerró `LISTO_REAL` antes del frontend/UX.
- [x] N8.2.E resolvió same-run el P1 `MISSING_N8_2_EXPLICIT_DEVICE_PROFILE_COVERAGE` con perfiles explícitos escritorio 1440x900, laptop 1366x768, tablet 820x1180, Android 412x915 e iPhone 390x844.
- [x] Gate causal `N8.2 - Compatibilidad de dispositivos` run `34941980085`, job `104292533908`, sobre `1f7008f464f195aab3020a5d0102a85428c8919a`: lint, build y Playwright dirigido PASS.
- [x] N8.2.E `LISTO_REAL`: `vaep/evidence/receipts/N8.2.E_LISTO_REAL_20260915T073218Z_SUP48.json`.
- [x] N8.2.F `LISTO_REAL`: `vaep/evidence/receipts/N8.2.F_LISTO_REAL_20260915T073344Z_SUP48.json`; sin delta runtime de RBAC/auditoría/seguridad.
- [x] N8.2.G `LISTO_REAL`: `vaep/evidence/receipts/N8.2.G_LISTO_REAL_20260915T073447Z_SUP48.json`; P0=0/P1=0/P2=0.
- [x] Paquete canónico H materializado en `docs/CERTIFICACION_N8_2_DISPOSITIVOS.md` y reconciliación history-preserving de TASKS/CHANGELOG ejecutada.

N8.2.H sólo pasa a `LISTO_REAL` mediante su receipt final después de REVIEW_FIRST documental; este rollup no sustituye el receipt ni el control-plane fresco.

## ERP-N8.3 — Compatibilidad de navegadores — ROLLUP DOCUMENTAL 2026-09-15

Este bloque es aditivo e histórico. El estado operativo vigente se determina únicamente por CONFIG/COLA y receipts VAEP bajo `docs/VAEP_AUTHORITY.md`.

- [x] N8.3.A-G cerraron `LISTO_REAL` con receipts/evidencia VAEP y P0/P1/P2 atribuibles conocidos=0.
- [x] Functional candidate `b833e44a976f92a426fff7f969a3f3f757234f20` incorpora el gate causal de compatibilidad en Chromium, Firefox y WebKit.
- [x] Workflow `N8.3 - Compatibilidad de navegadores`, run `34943377733`, job `104296994230`, terminó `success`; npm ci, lint, build producción, instalación de navegadores, arranque Angular, regresión cross-browser y publicación de evidencia quedaron PASS.
- [x] N8.3.F certificó seguridad/observabilidad sin delta de runtime de auth/RBAC/auditoría, sin secretos y con permisos `contents: read`.
- [x] N8.3.G certificó QA/regresión/CI con equivalencia funcional demostrada y sin P0/P1/P2 abiertos.
- [x] Certificación canónica H materializada en `docs/CERTIFICACION_N8_3_NAVEGADORES.md`.

N8.3.H sólo pasa a `LISTO_REAL` mediante REVIEW_FIRST documental, receipt verificable y reconciliación de control-plane; este rollup no sustituye el receipt.

## ERP-N8.4 — POS físico — ROLLUP DOCUMENTAL 2026-09-15

Este bloque es aditivo e histórico. El estado operativo vigente se determina únicamente por CONFIG/COLA y receipts VAEP bajo `docs/VAEP_AUTHORITY.md`.

- [x] N8.4.A-F cerraron `LISTO_REAL` con receipts/evidencia VAEP y P0/P1 atribuibles conocidos=0.
- [x] N8.4.G cerró `LISTO_REAL` mediante `vaep/evidence/receipts/OWNER_RECONCILIATION_N8.1.G_N8.1.H_N8.4.G_20260915T193239Z.json` después de aceptación explícita del propietario para el checkpoint físico de POS.
- [x] La evidencia histórica de bloqueo físico se conserva; no se falsificó ni sustituyó por evidencia sintética de impresora.
- [x] `docs/N8_4_POS_PHYSICAL_CAPABILITY_CONTRACT.md` mantiene el contrato de capacidades físicas y fail-closed cuando el hardware no está disponible.
- [x] Certificación canónica H materializada en `docs/CERTIFICACION_N8_4_POS_FISICO.md`.

N8.4.H sólo pasa a `LISTO_REAL` mediante REVIEW_FIRST documental, receipt verificable y reconciliación de control-plane; este rollup no sustituye el receipt.

## ERP-N8.11 — Seguridad — ROLLUP DOCUMENTAL 2026-09-15

Este bloque es aditivo e histórico. El estado operativo vigente se determina únicamente por CONFIG/COLA y receipts VAEP bajo `docs/VAEP_AUTHORITY.md`.

- [x] N8.11.A-C cerraron `LISTO_REAL`; B (DOMAIN) y C (DB_MIG) quedaron materialmente N/A donde no existía delta causal que justificara inventar dominio o migración.
- [x] N8.11.D cerró `LISTO_REAL` con contratos de regresión de seguridad del backend para autenticación, límites de uploads, rate limiting de superficie pública y anonimato acotado del webhook.
- [x] N8.11.E cerró `LISTO_REAL` sin delta UI material requerido por el punto.
- [x] N8.11.F cerró `LISTO_REAL` con P0=0/P1=0/P2=0 y gates terminales PASS para hardening de configuración, auditoría npm high/critical y auditoría de vulnerabilidades .NET.
- [x] N8.11.G cerró `LISTO_REAL` con backend non-integration, frontend unit, frontend lint/build y security audit causales en PASS sobre `91a7051bdb75c858af08d0e28368d827c4c0f6b8`.
- [x] Certificación canónica H materializada en `docs/CERTIFICACION_N8_11_SEGURIDAD.md`.

N8.11.H sólo pasa a `LISTO_REAL` mediante REVIEW_FIRST documental, receipt verificable y reconciliación de control-plane; este rollup no sustituye el receipt.

## ERP-N8.6 — WhatsApp real — REVALIDACIÓN CURRENT-STANDARD 2026-09-17

Este bloque es estrictamente aditivo/history-preserving. El estado operativo vigente continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`; esta bitácora no sustituye receipt ni readback.

- [x] N8.6.A-G fueron revalidados contra el estándar vigente con receipts frescos; P0=0 y P1 de producto=0.
- [x] Functional tested head `51929a108322acf01e5b63cc4274d4c7b2bd78c9`; regresión causal dirigida y suites API/RBAC/auditoría/MySQL/Angular/E2E terminaron `SUCCESS` según `docs/CERTIFICACION_N8_6_WHATSAPP_REAL.md`.
- [x] Frontera real certificada: handoff de navegador hacia `https://wa.me/<telefono>?text=...`; Solqaryn no implementa envío server-side por Meta/Twilio ni puede afirmar `sent`, `delivered` o `read`.
- [x] Certificación current-standard materializada en `docs/CERTIFICACION_N8_6_WHATSAPP_REAL.md`; no se enviaron mensajes externos durante la revalidación.
- [x] `main`, Producción, PR #2, secretos, DNS/certificados y datos productivos permanecen intactos.
- [ ] N8.6.H sólo pasa a `LISTO` después de REVIEW_FIRST final P0=0/P1=0, receipt verificable y write/readback del control-plane; este bloque por sí solo no cierra H.

## ERP-N8.7 — Performance — REVALIDACIÓN CURRENT-STANDARD 2026-09-17

Este bloque es estrictamente aditivo/history-preserving. El estado operativo vigente continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`; esta bitácora no sustituye receipt ni readback.

- [x] N8.7.A-F revalidados current-standard con preflight/guardas; P0=0/P1=0.
- [x] N8.7.G `LISTO` con functional head `45f069e9997e0cb9de4682844ff64dc9addc8674`; workflow `N8.7.G - Performance proof` run `35219847763=SUCCESS`; pruebas dirigidas 18/18; perfiles load/stress/endurance/volume con 18,594 requests y 0 fallos.
- [x] Contrato de seguridad: Production traffic/data=0, auth bypass=false, rate limit disabled=false y datos únicamente sintéticos; artifact `10496399028`, digest `sha256:68f2912c06d84feb0c2c25d148748c6daf02aa61f14b132151aef810f2ecbd9c`.
- [x] Certificación H canónica materializada en `docs/evidencias/performance/N8.7_CURRENT_STANDARD_MATERIAL_PERFORMANCE_CERTIFICATION_20260917.md`.
- [ ] N8.7.H sólo pasa a `LISTO` después de REVIEW_FIRST final P0=0/P1=0, hard verify de este append, receipt H y Sheet write/readback; este rollup no sustituye el cierre.

## ERP-N8.8 — Backup y restauración — REVALIDACIÓN CURRENT-STANDARD 2026-09-17

Este bloque es estrictamente aditivo/history-preserving. El estado operativo vigente continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`; esta bitácora no sustituye receipt ni readback.

- [x] N8.8.A-G revalidados current-standard con receipts frescos y REVIEW_FIRST/gates causales aplicables; P0=0/P1=0 en el alcance técnico certificado.
- [x] M11 backup operativo run `35223693841=SUCCESS` y backup/restauración run `35223693868=SUCCESS` sobre tested head `ab7b5a35cdc91312254b8a96b13ac24c53e28f44`; el drill restauró en MySQL descartable y validó integridad/health sin escribir Producción.
- [x] Aiven DEV proof read-only run `35223698590`, attempt 4, `SUCCESS`: servicio MySQL `RUNNING`, endpoint de backups accesible, 3 backups observados, PITR disponible, retención legible y capacidad/autorización de restore identificada; el blocker histórico de token expirado quedó resuelto.
- [x] Equivalencia causal de la superficie backup/restore demostrada por blobs invariantes del workflow y scripts, según `docs/CERTIFICACION_N8_8_BACKUP_CURRENT_STANDARD.md`.
- [x] `main`, Producción, PR #2, secretos, DNS/certificados, planes y datos productivos permanecen intactos; durante esta revalidación no se ejecutó restore/fork/upgrade contra Aiven.
- [ ] N8.8.H sólo pasa a `LISTO` después de REVIEW_FIRST final P0=0/P1=0, hard verify de este append y `CHANGELOG_AI.md`, receipt H y Sheet write/readback; este rollup no sustituye el cierre.

## ERP-N8.9 — Restore real — REVALIDACIÓN CURRENT-STANDARD 2026-09-17

Este bloque es estrictamente aditivo/history-preserving. El estado operativo vigente continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`; esta bitácora no sustituye receipt ni readback.

- [x] N8.9.A-G fueron revalidados current-standard con receipts frescos, REVIEW_FIRST y P0=0/P1=0 en su alcance aplicable.
- [x] El gate causal M11 `35223693868`, job `105209822216`, restauró materialmente el backup en MySQL descartable, verificó integridad y arrancó la API con `/health` y `/health/ready` en PASS; Producción no fue tocada.
- [x] La equivalencia current-standard del workflow y scripts de backup/restore permanece demostrada; no existe delta de migraciones dentro del scope que invalide el gate causal.
- [x] Certificación canónica materializada en `docs/CERTIFICACION_N8_9_RESTORE.md`; no se afirma ni ejecuta restore destructivo/fork/upgrade sobre Aiven.
- [x] Este append resuelve `TASKS_ADDITIVE_STATE_RECONCILIATION` sin reescribir historia previa.
- [ ] N8.9.H sólo pasa a `LISTO` después de completar `CHANGELOG_AI_ADDITIVE_RECONCILIATION`, REVIEW_FIRST documental final P0=0/P1=0, receipt y write/readback del control-plane.

**CURRENT_PARENT=N8.9.H. NEXT_PARENT_PREARMED=N8.10.A. No se promueve N8.10.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**


## ERP-N8.10 Disaster Recovery — revalidación current-standard 2026-09-17

- `N8.10.A-G`: revalidadas secuencialmente contra el estándar vigente; el histórico fue evidencia de apoyo, nunca auto-cierre.
- Fresh receipts: A `f01038cb`, B `ce1fabb2`, C `4039299b`, D `792ae87b`, E `45d663a3`, F `67541352`, G `4f8426c7`.
- Restore causal actual: run `35223693868`, job `105209822216`, `SUCCESS`; backend current-equivalence PASS; no migration delta.
- RPO engineering `<=24h`; RTO engineering `<=60m`; drill medido `1244s`; P0=0/P1=0.
- Certificación: `docs/CERTIFICACION_N8_10_DISASTER_RECOVERY_CURRENT_STANDARD_20260917.md` @ `c186daad`.
- `N8.10.H` exige REVIEW_FIRST final + receipt/readback antes de promover `N8.11.A`.


## ERP-N8.11 — Seguridad — REVALIDACIÓN CURRENT-STANDARD 2026-09-17

Este bloque es estrictamente aditivo/history-preserving. El estado operativo vigente continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`; esta bitácora no sustituye receipt ni readback.

- [x] N8.11.A-G fueron revalidados secuencialmente contra el estándar vigente; el histórico fue evidencia de apoyo, nunca auto-cierre.
- [x] N8.11.F `LISTO` con REVIEW_FIRST P0=0/P1=0 y security gate `35258962286=SUCCESS`.
- [x] N8.11.G `LISTO` con REVIEW_FIRST P0=0/P1=0 y quality/regression gate `35258969104=SUCCESS`; equivalencia de dependencias respaldada por `35233179389=SUCCESS`.
- [x] Functional head `6fd3e28cbf28164d110d6b83756b9094cec654a6`; certificación current-standard `docs/CERTIFICACION_N8_11_SEGURIDAD_CURRENT_STANDARD.md`.
- [x] `main`, Producción, PR #2, secretos, DNS/certificados y datos productivos permanecen intactos.
- [ ] N8.11.H sólo pasa a `LISTO` después de hard verify de este append y `CHANGELOG_AI.md`, REVIEW_FIRST final P0=0/P1=0, receipt H y write/readback del control-plane.

**CURRENT_PARENT=N8.11.H. NEXT_PARENT_PREARMED=N8.12.A. No se promueve N8.12.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**


## ERP-N8.12 Observabilidad — revalidación current-standard 2026-09-17

- `N8.12.A-G`: revalidadas secuencialmente contra el estándar vigente; el histórico se usó sólo como evidencia de apoyo.
- Fresh receipts/heads: A `4c985d47`, B `fe63d184`, C `8b51e8bd`, D `9b546c6f`, E `af6ac708`, F `fbe60716`, G `6d50b299`.
- Functional tested head `6fd3e28c`; backend tree `f8db4763`, API tree `9bd27071`, tests tree `e0cf8fcc`, frontend tree `3d7847eb` exact-equivalent.
- Causal gates: security `35258962286:SUCCESS`; quality/regression `35258969104:SUCCESS`.
- Observabilidad certificada: métricas acotadas, correlation/trace context, alert logs estructurados y health/readiness secret-safe; sin delta de schema/frontend.
- Certificación: `docs/CERTIFICACION_N8_12_OBSERVABILIDAD_CURRENT_STANDARD.md` @ `7ce5e060`.
- `N8.12.H` requiere REVIEW_FIRST final + receipt/readback antes de promover `N8.13.A`.


## ERP-N8.13 — Staging comparable a producción — REVALIDACIÓN CURRENT-STANDARD 2026-09-17

Este bloque es estrictamente aditivo/history-preserving. El estado operativo vigente continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`; esta bitácora no sustituye REVIEW_FIRST, receipt ni readback.

- [x] `N8.13.A-G` fueron revalidados secuencialmente contra el estándar vigente; el histórico se usó sólo como evidencia de apoyo.
- [x] `N8.13.F` y `N8.13.G` quedaron certificados con REVIEW_FIRST fresco y `P0=0/P1=0`; gates causales `35258962286=SUCCESS` y `35258969104=SUCCESS`.
- [x] Functional tested head `6fd3e28cbf28164d110d6b83756b9094cec654a6`; árboles equivalentes vigentes: backend `f8db476310130c169ddb5d8c4ea03cfbab9617f6`, frontend `3d7847eb8817b14942746f1e31114e56c15efbdd`.
- [x] Evidencia histórica de staging preservada; los hostnames de esa ejecución fueron retirados. Runtime DEV canónico actual: `solqaryn-dev.vercel.app` -> `solqaryn-api-dev-fxx8.onrender.com`, con readiness en `/health/ready`.
- [x] Comparación con Producción se realizó sólo read-only; no hubo deploy, cambio de variables, secretos, DNS, certificados ni datos productivos.
- [x] Certificación current-standard: `vaep/evidence/certifications/N8.13_STAGING_CURRENT_STANDARD_CERT_20260917T205649Z_VAEP48.md`.
- [x] `TASKS_ADDITIVE_STATE_RECONCILIATION` queda materializada por este append preservando íntegramente la historia previa.

Este append no cambia por sí solo el estado de `N8.13.H`; el cierre `LISTO` requiere además reconciliación aditiva de `CHANGELOG_AI.md`, REVIEW_FIRST final con P0=0/P1=0, receipt y write/readback del control-plane.


## ERP-N8.14 — Rollback — REVALIDACIÓN CURRENT-STANDARD 2026-09-17

Este bloque es estrictamente aditivo/history-preserving. El estado operativo vigente continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`; esta bitácora no sustituye REVIEW_FIRST, receipt ni readback.

- [x] `N8.14.A-G` fueron revalidados secuencialmente current-standard con receipts frescos; `P0=0/P1=0`.
- [x] Gates causales: security `35258962286=SUCCESS`, quality/regression `35258969104=SUCCESS`, backup/restore material `35223693868=SUCCESS`.
- [x] TEST_CI `N8.14.G` certificado por REVIEW_FIRST `9559db0411b3c59dc25ee27972ceef7d7525d495` y receipt `232874df8f309cac68638b4f7678c85b9a88adff`; backend/frontend delta=0 y migraciones sin delta causal.
- [x] Runbook vigente `docs/ROLLBACK_RUNBOOK.md` blob `0582222439ef7b2d3968d24fa6820e39e05180f3`; workflow M11 vigente blob `5cba214846ace7c45984d23b3e0db013404c3f01`.
- [x] Certificación current-standard: `vaep/evidence/certifications/N8.14_ROLLBACK_CURRENT_STANDARD_CERT_20260917T223414Z_SUP24.md`.
- [x] No se tocó `main`, Producción, PR #2, secretos, DNS/certificados ni datos productivos.
- [ ] `N8.14.H` sólo pasa a `LISTO` después de hard verify de este append y `CHANGELOG_AI.md`, REVIEW_FIRST final P0=0/P1=0, receipt H y write/readback del control-plane.


## ERP-N9.1 — Release Candidate — FEATURE FREEZE 2026-09-17

Este bloque es estrictamente aditivo/history-preserving. El estado operativo vigente continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`; esta bitácora no sustituye REVIEW_FIRST, receipt ni readback.

- [x] `GATE-N8` cerrado current-standard con 24/24 prerequisitos H y 24/24 parents PLAN en `LISTO`; receipt `4eadd208322e8f19d84d669fffc70a9de85ba567`.
- [x] Release Candidate funcional fijado por SHA inmutable: `4eadd208322e8f19d84d669fffc70a9de85ba567`.
- [x] Feature freeze activo para `N9.1`: no nuevas features; cualquier cambio de producto invalida la equivalencia y exige nuevo candidate SHA + recertificación causal.
- [x] `N9.1.A-F` cerrados con REVIEW_FIRST y P0=0/P1=0; B-E N/A grounded por ausencia de delta de producto; F verificó controles heredados.
- [x] `N9.1.G TEST_CI`: security `35258962286=SUCCESS`, quality `35258969104=SUCCESS`, backup/restore `35223693868=SUCCESS`, performance `35219847763=SUCCESS`, product delta=0.
- [x] Certificación: `vaep/evidence/certifications/N9.1_RELEASE_CANDIDATE_FREEZE_20260917T224932Z_SUP24.md`.
- [x] `main`, Producción, PR #2 merge, secretos, DNS/certificados y datos productivos no fueron tocados.
- [ ] `N9.1.H` sólo pasa a `LISTO` después de verificar este append y `CHANGELOG_AI.md`, retirar el writer temporal, REVIEW_FIRST final P0=0/P1=0, receipt y write/readback del control-plane.


## ERP-N9.2 — Checklist de salida — REVALIDACIÓN CURRENT-STANDARD 2026-09-17

Este bloque es estrictamente aditivo/history-preserving. El estado operativo vigente continúa exclusivamente en `CONFIG/COLA` bajo `docs/VAEP_AUTHORITY.md`; esta bitácora no sustituye REVIEW_FIRST, receipt ni readback.

- [x] `N9.2.A-G` revalidados current-standard con receipts frescos; P0=0/P1=0 y delta de producto=0 frente al Release Candidate congelado `4eadd208322e8f19d84d669fffc70a9de85ba567`.
- [x] Gates causales aplicables preservados por equivalencia exacta de producto: security `35258962286=SUCCESS`, quality/regression `35258969104=SUCCESS`, backup/restore `35223693868=SUCCESS` y performance `35219847763=SUCCESS`.
- [x] `N9.2.G` receipt `vaep/evidence/receipts/N9.2.G_LISTO_20260917T231100Z_SUP24.json` releído; `N9.2.H` REVIEW_FIRST detectó únicamente dos P1 documentales de append en `TASKS.md` y `CHANGELOG_AI.md`, sin P0/P1 de producto.
- [x] Este append resuelve `TASKS_ADDITIVE_STATE_RECONCILIATION` preservando byte-for-byte toda historia previa.
- [ ] `N9.2.H` sólo pasa a `LISTO` después de completar también `CHANGELOG_AI_ADDITIVE_RECONCILIATION`, hard verify de ambos prefijos, REVIEW_FIRST fresco P0=0/P1=0, receipt y write/readback del control-plane.

**CURRENT_PARENT=N9.2.H. NEXT_PARENT_PREARMED=N9.3.A. No se promueve N9.3.A antes del cierre real de H. FALSE_PASS=NO. FALSE_LISTO=NO.**


## ERP-N9.3 — Backup pre-release — cierre current-standard 2026-09-18

- [x] N9.3.A-G revalidados/certificados current-standard con REVIEW_FIRST y P0=0/P1=0.
- [x] Backup real de Desarrollo ejecutado por M11 run `35288756204`: definición/protecciones, proveedor y backup+restore concluyeron `success`.
- [x] Artifact cifrado `10525951057`, digest `sha256:ca09e80bba0cc44196f97f5a3afc42141be0471f65dd5c3c17dcbb4349171e7d`; retención 14 días.
- [x] Restore drill del mismo artifact en MySQL descartable: 132 tablas base, 104 migraciones EF, checksums y row-counts verificados, `status=SUCCESS`, `productionTouched=false`.
- [x] N9.3.H materializa este registro y la entrada aditiva correspondiente en `CHANGELOG_AI.md` preservando byte por byte el prefijo histórico; la certificación `LISTO` sólo se emite después del readback y REVIEW_FIRST final.

**N9.3 no se promueve por este texto aislado: el cierre autoritativo requiere receipt/readback current-standard en COLA/CONFIG. No se tocó main, Producción, PR #2, secretos, DNS ni certificados.**


## ERP-N9.4 Migraciones productivas — cierre current-standard 2026-09-18

- [x] N9.4.C: APPLY/postcheck productivo completado con respaldo confirmado previamente por el propietario; Producción quedó LIVE, DB al día y maintenance OFF.
- [x] Incidente N0.4 de duplicados RBAC legacy recuperado de forma retry-safe sin abrir tráfico durante la migración.
- [x] Certificación exact-head N0.4 run 35304697573 / job 105474385670 SUCCESS: backend 2328/2328, preflight, APPLY, aislamiento, deduplicación, guards fail-closed y snapshot EF PASS.
- [x] N9.4.D/E: N/A grounded sin fabricar backend/API ni frontend/UX; no había delta material requerido por el procedimiento.
- [x] N9.4.F/G: seguridad/observabilidad y CI current-standard PASS, P0=0/P1=0.
- [x] Commit productivo estabilizado 7f140442d598aaea36b39952bad5ebd9ab4f2613; deploy final dep-damb4lqjnfac73efl1pg LIVE.
- [x] Última migración observada 20260914232400_N7_10_C_DocumentoFiscalPersistencia; reinicio final indicó database already up to date.
- [ ] N9.4.H se declara LISTO únicamente tras hard verify de este append y CHANGELOG_AI, retiro del writer temporal, REVIEW_FIRST final, receipt y readback del control-plane.


## ERP-N9.5 — Smoke test — cierre current-standard 2026-09-18

- [x] N9.5.A-F preservados/revalidados current-standard en la cadena vigente.
- [x] N9.5.G QA/regresión/CI certificado con REVIEW_FIRST `P0=0/P1=0` sobre functional/test head `1ac95f51176d8b9240741b2e33ab2bd4c2605dc0`.
- [x] Admission exact-head `35316302522` SUCCESS.
- [x] Aceptación integral exact-head `35316302603` SUCCESS: Playwright `100/100`, SMTP/PDF SUCCESS; artifact `10535867520`, digest `sha256:5b073d44217948a3cafd2938d6a1ed24f3c36e5b3234450ec1e1ab09085fd85a`.
- [x] Certificación canónica materializada en `docs/CERTIFICACION_N9_5_SMOKE_TEST.md`.
- [x] N9.5.H materializa este registro y la entrada aditiva correspondiente en `CHANGELOG_AI.md` preservando byte por byte el prefijo histórico; su `LISTO` sólo es válido tras REVIEW_FIRST, receipt y readback posteriores a este append.

**El parent N9.5 sólo queda formalmente `LISTO` cuando COLA/CONFIG/PLAN_MAESTRO y el receipt fresco de N9.5.H confirman el cierre; este texto aislado no auto-certifica estado ni autoriza promoción.**


## ERP-N9.6 — Hypercare — cierre current-standard 2026-09-18

- [x] N9.6.A–G revalidados current-standard con REVIEW_FIRST y P0=0/P1=0.
- [x] Recovery same-run: retirado el probe temporal que apuntaba a Producción; reemplazado por verificación one-shot estrictamente DEV y luego retirado.
- [x] Gate Hypercare DEV `35319732966` SUCCESS: backend health/readiness/latencia, frontend shell/latencia y superficies críticas anónimas fail-closed.
- [x] Functional/test head `1ac95f51176d8b9240741b2e33ab2bd4c2605dc0` preservado por equivalencia de producto; aceptación `35316302603` SUCCESS (`100/100` Playwright + SMTP/PDF).
- [x] Certificación canónica: `docs/CERTIFICACION_N9_6_HYPERCARE.md`.
- [x] N9.6.H materializa este registro y su bloque correspondiente en `CHANGELOG_AI.md` mediante append byte-safe; `LISTO` sólo se declara tras verificar prefijo histórico exacto, additions>0/deletions=0, retirar el writer temporal, REVIEW_FIRST final, receipt y write/readback.


## ERP-N9.7 — Postmortem — cierre current-standard 2026-09-18

- [x] N9.7.A–G revalidados current-standard con REVIEW_FIRST y P0=0/P1=0.
- [x] Postmortem materializado en `docs/POSTMORTEM_N9_7.md`; certificación en `docs/CERTIFICACION_N9_7_POSTMORTEM.md`.
- [x] Sin delta causal de producto, contratos, schema, tests o runtime frente al functional/test head `1ac95f51176d8b9240741b2e33ab2bd4c2605dc0`.
- [x] Aceptación `35316302603` SUCCESS (`100/100` Playwright + SMTP/PDF) y Hypercare DEV `35319732966` SUCCESS conservados como gates causales por equivalencia demostrada.
- [x] Incidencias internas observadas y recuperadas same-run quedaron documentadas como aprendizaje; no persisten blockers P0/P1.
- [x] N9.7.H materializa este registro y el bloque correspondiente de `CHANGELOG_AI.md` mediante append byte-safe; LISTO sólo tras verificar prefijo histórico exacto, additions>0/deletions=0, retirar writer temporal, REVIEW_FIRST final, receipt y write/readback.
