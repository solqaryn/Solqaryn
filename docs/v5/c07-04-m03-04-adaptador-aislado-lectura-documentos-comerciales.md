# SOLQARYN V17 — C07-04.M03.04: adaptador aislado de lectura comercial

**Riesgo:** R2 (tenant, pagos y persistencia). **Ámbito:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. **Autor de implementación:** PRIMARY DEV 02 JAVIER (TASK_ID 6af16bb9492c8191b8e8c7d606f11507). **Estado:** implementación fuente pendiente de resultado causal de GitHub Actions y A5 independiente.

## Alcance concreto

Se añade un adaptador de Infrastructure con dos interfaces de fuentes confiables (identidad/permiso actual y documento filtrado por tenant+ID), no registradas en DI. La lectura de snapshots debe venir de backend autenticado y una persistencia que filtre físicamente por tenant; los campos no son datos de cliente. Se revalida tenant A/B, membresía actual, revocación, permiso, versión, pertenencia del cliente y padre, NULL legacy, SHA de fuente, moneda/importe/estado e idempotencia utilizando las puertas Domain/Application ya existentes en M03.01–03.

El resultado permite únicamente solicitar evaluación ulterior: **GrantsMutation/Payment/FiscalIssuance/Export/CrossTenantRead siempre FALSE**. No se añade acceso EF, consulta SQL, tabla, migración, DI, endpoint, pago PSP, factura, worker, evento ni transacción. La conexión con la persistencia real requerirá scope propio, pruebas EF Core + MySQL 8.4 efímero e integración autenticada.

## Pruebas y riesgos

Las pruebas xUnit de Infrastructure ejercitan: preflight positivo que sigue sin autorizar efecto; recursos/sha inválidos sin I/O; autenticación, membresía, revocación y concesiones (A/B); documentId/owner/parent NULL o ajenos; versión y SHA obsoletos; importe/referencia inseguros; errores opacos de cada fuente y cancelación. Son pruebas de contrato con fuentes sintéticas; **NO demuestran integración MySQL/PSP/HTTP**.

**A0:** alcance de cuatro archivos (adaptador, xUnit, este documento y CHANGELOG); comparar dependencias de Domain/Application. **A1:** ejecutar xUnit real bajo GitHub Actions (positivos, negativos, frontera y errores), no certificar mientras esté queued/in_progress. **A2:** aislamiento A/B, revocación y fail-closed, más checks de seguridad/autorización del SHA causal. **A3:** sólo contrato de adaptador aislado; migración/EF/MySQL no aplican porque no hay acceso a DB ni DI. **A4:** GitHub dev fast-forward CAS, verify-solqaryn-project-scope y V5, compilación Release/backend xUnit y controles pertinentes SUCCESS exact-SHA o descendiente comprobado, más PLAN/COLA/DAG/BITACORA readback. **A5:** debe revisarla una Task Javier distinta de DEV02.

**Reversión:** git revert normal del commit, nueva entrada compensatoria en BITACORA sin borrar historia. Riesgo residual: un fuente futura mal autenticada podría mentir; por eso queda deliberadamente sin registrar/conectar. No QA/main/PROD, secretos ni operaciones financieras reales.
